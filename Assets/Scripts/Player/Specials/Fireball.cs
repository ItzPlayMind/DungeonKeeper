using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fireball : MonoBehaviour
{
    public System.Action<GameObject> onExplosionCollision;
    public System.Func<GameObject,bool> onDirectHit;
    [SerializeField] private CollisionSender explosion;

    private Animator animator;
    private Rigidbody2D rb;

    protected void Start()
    {
        explosion.onCollisionEnter += onExplosionCollision;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (onDirectHit?.Invoke(collision.gameObject) ?? false)
        {
            rb.velocity = Vector2.zero;
            animator.SetTrigger("hit");
        }
    }
}
