using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionSender : MonoBehaviour
{
    public System.Action<GameObject> onCollisionEnter;
    public System.Action<GameObject> onCollisionExit;
    public System.Action<GameObject> onCollisionStay;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        onCollisionEnter?.Invoke(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
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
        onCollisionEnter?.Invoke(collision.gameObject);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        onCollisionEnter?.Invoke(collision.gameObject);
    }
}
