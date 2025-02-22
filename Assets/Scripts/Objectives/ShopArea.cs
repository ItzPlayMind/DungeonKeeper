using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopArea : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        var stats = collision.GetComponent<PlayerStats>();
        if (stats != null)
        {
            if (stats.IsLocalPlayer)
            {
                ShopPanel.Instance.SetAbleToShop(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        var stats = collision.GetComponent<PlayerStats>();
        if (stats != null)
        {
            if (stats.IsLocalPlayer)
            {
                ShopPanel.Instance.SetAbleToShop(false);
            }
        }
    }
}
