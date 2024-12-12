using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ChatSystem : NetworkBehaviour
{
    [SerializeField] private Transform chatTransform;
    [SerializeField] private TMPro.TextMeshProUGUI chatMessagePrefab;
    public void AddMessage(string message)
    {
        AddMessageServerRPC(message);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddMessageServerRPC(string message)
    {
        AddMessageClientRPC(message);
    }

    [ClientRpc]
    private void AddMessageClientRPC(string message)
    {
        var text = Instantiate(chatMessagePrefab, chatTransform);
        text.transform.SetAsFirstSibling();
        text.text = message;
    }

    public void Clear()
    {
        for (int i = 0; i < chatTransform.childCount; i++)
            Destroy(chatTransform.GetChild(0).gameObject);
    }
}
