using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Fireball : NetworkBehaviour
{
    public System.Action onExplosion;
    public CollisionSender.OnCollosion onExplosionCollision;
    public System.Action<GameObject> onDirectHit;
    [SerializeField] private CollisionSender explosion;

    private Vector2 startPos;

    private Animator animator;
    private Rigidbody2D rb;
    private Collider2D col;

    private List<int> hits = new List<int>();
    protected void Start()
    {
        explosion.onCollisionEnter += onExplosionCollision;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        startPos = transform.position;
    }

    private void Update()
    {
        if(Vector3.Distance(startPos, transform.position) > 4)
            Explode();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) return;
        if (hits.Contains(collision.gameObject.GetInstanceID())) return;
        hits.Add(collision.gameObject.GetInstanceID());
        onDirectHit?.Invoke(collision.gameObject);
        Explode();
    }

    private void Explode()
    {
        onExplosion?.Invoke();
        col.enabled = false;
        rb.velocity = Vector2.zero;
        animator.SetTrigger("hit");
    }
}
