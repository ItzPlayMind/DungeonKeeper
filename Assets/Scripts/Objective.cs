using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Objective : NetworkBehaviour
{
    public Action<ulong> OnObjectiveComplete;

    protected void Complete(ulong id)
    {
        OnObjectiveComplete?.Invoke(id);
    }
}
