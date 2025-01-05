using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class Projectile : CollisionSender
{
    Vector2 lastPos = Vector2.zero;
    public System.Action OnMaxRangeReached;
    public float range = 3f;
    [HideInInspector] public Vector2 startPos;

    private bool maxReached = false;

    protected override void Start()
    {
        base.Start();
        lastPos = transform.position;
    }

    public void Update()
    {
        if(Vector3.Distance(transform.position, startPos) >= range) {
            OnMaxRangeReached?.Invoke();
            maxReached = true;
        }
        if (maxReached) return;

        RaycastHit2D hit = Physics2D.Linecast(lastPos, transform.position);

        if (hit.transform != null)
        {
            if (hit.transform.gameObject.layer != gameObject.layer)
            {
                if (!ContainsHit(hit.transform.GetInstanceID()))
                {
                    onCollisionEnter?.Invoke(hit.transform.gameObject, ref hasHit);
                    AddToHit(hit.transform.GetInstanceID());
                }
            }
        }
        lastPos = transform.position;
    }
}
