using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionSender : MonoBehaviour
{
    public System.Action<GameObject> onCollisionEnter;
    public System.Action<GameObject> onCollisionExit;
    public System.Action<GameObject> onCollisionStay;

    [SerializeField] private bool CanHitMultiple = false;

    private List<int> hits = new List<int>();
    private Collider2D coll;
    private bool isEnabled = false;
    private void Start()
    {
        coll = GetComponent<Collider2D>();
        isEnabled = coll.enabled;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        var instanceID = collision.gameObject.GetInstanceID();
        if (!CanHitMultiple)
        {
            if (hits.Contains(instanceID)) return;
            hits.Add(instanceID);
        }
        onCollisionEnter?.Invoke(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var instanceID = collision.gameObject.GetInstanceID();
        if (!CanHitMultiple)
        {
            if (hits.Contains(instanceID)) return;
            hits.Add(instanceID);
        }
        onCollisionEnter?.Invoke(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        onCollisionStay?.Invoke(collision.gameObject);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        onCollisionStay?.Invoke(collision.gameObject);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        onCollisionExit?.Invoke(collision.gameObject);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        onCollisionExit?.Invoke(collision.gameObject);
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
