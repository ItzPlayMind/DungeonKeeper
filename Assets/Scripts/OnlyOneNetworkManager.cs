using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnlyOneNetworkManager : MonoBehaviour
{
    private static OnlyOneNetworkManager instance;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
}
