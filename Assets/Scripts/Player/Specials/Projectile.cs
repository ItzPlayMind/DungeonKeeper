using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class Projectile : CollisionSender
{
    Vector2 lastPos = Vector2.zero;
    protected override void Start()
    {
        base.Start();
        lastPos = transform.position;
    }

    public void Update()
    {
        RaycastHit2D hit = Physics2D.Linecast(lastPos, transform.position);

        if (hit.transform != null)
        {
            if (hit.transform.gameObject.layer != gameObject.layer)
            {
                if (!ContainsHit(hit.transform.GetInstanceID()))
                {
                    onCollisionEnter?.Invoke(hit.transform.gameObject);
                    AddToHit(hit.transform.GetInstanceID());
                }
            }
        }
        lastPos = transform.position;
    }
}
