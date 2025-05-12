using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

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
    private Rigidbody2D rb;
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
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
        rb.velocity = dir * 1.5f;
        float distance = Vector2.Distance(transform.position, target.transform.position);
        if (distance <= explosionRange)
            Explode();
    }

    public void SpawnFinished()
    {
        isSpawned = true;
    }

    private void Explode()
    {
        rb.velocity = Vector2.zero;
        state = State.Explode;
        animator.SetTrigger("Explode");
    }
}
