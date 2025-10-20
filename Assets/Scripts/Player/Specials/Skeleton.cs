using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class Skeleton : NetworkBehaviour
{
    [SerializeField] private SpriteRenderer gfx;
    [SerializeField] private bool isSpawned = false;

    public bool Spawned { get => isSpawned; }

    private NetworkVariable<bool> facingRight = new NetworkVariable<bool>(false);

    private enum State
    {
        Running, Attacking
    }
    
    private State state = State.Attacking;
    private Animator animator;
    private NavMeshAgent agent;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.stoppingDistance = 0;
    }

    public override void OnNetworkSpawn()
    {
        facingRight.OnValueChanged += (bool previous, bool newValue) =>
        {
            gfx.flipX = newValue;
        };
    }

    private void Update()
    {
        if (!IsServer) return;
        if (!isSpawned) return;
        switch (state)
        {
            case State.Running:
                if (Vector3.Distance(transform.position,agent.destination) <= agent.stoppingDistance+0.1)
                {
                    animator.SetBool("Attacking", true);
                    state = State.Attacking;
                }
                break;
            case State.Attacking:
                break;
        }
        if (state != State.Running) return;
        Vector2 dir = (agent.destination - transform.position).normalized;
        facingRight.Value = dir.x < 0;
    }

    public void SetDestination(Vector3 position)
    {
        animator.SetBool("Attacking", false);
        agent.SetDestination(position);
        state = State.Running;
    }

    public void SpawnFinished()
    {
        isSpawned = true;
    }

    public void Teleport(Vector3 position)
    {
        agent.Warp(position);
    }
}
