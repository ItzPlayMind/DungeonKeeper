using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientNetworkTransform: Unity.Netcode.Components.NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
