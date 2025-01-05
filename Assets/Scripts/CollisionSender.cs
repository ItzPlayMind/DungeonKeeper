using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionSender : MonoBehaviour
{
    public delegate void OnCollosion(GameObject gb, ref bool hit);

    public OnCollosion onCollisionEnter;
    public OnCollosion onCollisionExit;
    public OnCollosion onCollisionStay;

    [SerializeField] private bool CanHitMultiple = false;

    private List<int> hits = new List<int>();
    private Collider2D coll;
    private bool isEnabled = false;
    protected bool hasHit = false;
    protected virtual void Start()
    {
        coll = GetComponent<Collider2D>();
        isEnabled = coll.enabled;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHit) return;
        var instanceID = collision.gameObject.GetInstanceID();
        if (!CanHitMultiple)
        {
            if (hits.Contains(instanceID)) return;
            hits.Add(instanceID);
        }
        onCollisionEnter?.Invoke(collision.gameObject, ref hasHit);
    }

    protected bool ContainsHit(int id) => hits.Contains(id);

    protected void AddToHit(int id)
    {
        hits.Add(id);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;
        var instanceID = collision.gameObject.GetInstanceID();
        if (!CanHitMultiple)
        {
            if (hits.Contains(instanceID)) return;
            hits.Add(instanceID);
        }
        onCollisionEnter?.Invoke(collision.gameObject, ref hasHit);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (hasHit) return;
        onCollisionStay?.Invoke(collision.gameObject, ref hasHit);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (hasHit) return;
        onCollisionStay?.Invoke(collision.gameObject, ref hasHit);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (hasHit) return;
        onCollisionExit?.Invoke(collision.gameObject, ref hasHit);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (hasHit) return;
        onCollisionExit?.Invoke(collision.gameObject, ref hasHit);
    }

    private void Update()
    {
        if (isEnabled && !coll.enabled)
        {
            hits = new List<int>();
        }
        isEnabled = coll.enabled;
    }
}
