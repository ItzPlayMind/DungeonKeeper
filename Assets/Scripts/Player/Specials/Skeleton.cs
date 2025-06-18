using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class Skeleton : NetworkBehaviour
{
    [SerializeField] private SpriteRenderer gfx;
    [SerializeField] private bool isSpawned = false;
    [SerializeField] private float explosionRange = 1.5f;

    private NetworkVariable<bool> facingRight = new NetworkVariable<bool>(false);

    private enum State
    {
        Running, Explode
    }
    
    private State state;
    private CharacterStats target;
    private Animator animator;
    private NavMeshAgent agent;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.stoppingDistance = explosionRange-0.2f;
    }

    public override void OnNetworkSpawn()
    {
        facingRight.OnValueChanged += (bool previous, bool newValue) =>
        {
            gfx.flipX = newValue;
        };
    }

    public void SetTarget(CharacterStats target)
    {
        this.target = target;
    }

    private void Update()
    {
        if (!IsServer) return;
        if (!isSpawned) return;
        if (target == null)
        {
            Explode();
            return;
        }
        if (state != State.Running) return;
        Vector2 dir = (target.transform.position- transform.position).normalized;
        facingRight.Value = dir.x < 0;
        float distance = Vector2.Distance(transform.position, target.transform.position);
        if (distance <= explosionRange)
            Explode();
        agent.SetDestination(target.transform.position);
        if (agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            Explode();
            return;
        }
    }

    public void SpawnFinished()
    {
        isSpawned = true;
    }

    private void Explode()
    {
        agent.isStopped = true;
        agent.velocity = Vector2.zero;
        state = State.Explode;
        animator.SetTrigger("Explode");
    }
}
