using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerStatisticsSystem : NetworkBehaviour
{
    public string Name { get; private set; }

    private Dictionary<ulong, string> idNames = new Dictionary<ulong, string>();

    private const string PLAYER_NAME = "PlayerName";

    private void Awake()
    {
        Name = PlayerPrefs.GetString(PLAYER_NAME,"");
    }

    public void SetName(string playerName)
    {
        Name = playerName;
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    public override void OnNetworkSpawn()
    {
        PlayerPrefs.SetString(PLAYER_NAME, Name);
        RequestNamesServerRPC(NetworkManager.Singleton.LocalClientId);
        SendOwnNameServerRPC(NetworkManager.Singleton.LocalClientId, Name);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestNamesServerRPC(ulong client)
    {
        foreach(var idName in idNames)
            RequestNamesClientRPC(idName.Key, idName.Value, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = new List<ulong>() { client } } });
    }

    [ClientRpc]
    private void RequestNamesClientRPC(ulong clientID, string playerName, ClientRpcParams param)
    {
        idNames.Add(clientID, playerName);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendOwnNameServerRPC(ulong client, string playerName)
    {
        SendOwnNameClientRPC(client, playerName);
    }

    [ClientRpc]
    private void SendOwnNameClientRPC(ulong client, string playerName)
    {
        idNames.Add(client, playerName);
    }

    public string GetNameByClientID(ulong id) => idNames[id];

    public void Clear()
    {
        idNames.Clear();
    }
}
