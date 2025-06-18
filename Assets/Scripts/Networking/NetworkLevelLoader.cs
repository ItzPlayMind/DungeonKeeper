using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkLevelLoader : NetworkBehaviour
{
    public System.Action OnAllClientsLoaded;

    [SerializeField] private List<int> sceneIndices = new List<int>();

    private int clientsLoaded = 0;

    private void OnLevelWasLoaded(int level)
    {
        if (sceneIndices.Contains(level))
            OnClientLoadedLevelServerRPC();
    }

    [ServerRpc(RequireOwnership = false)]
    private void OnClientLoadedLevelServerRPC()
    {
        clientsLoaded++;
        if(clientsLoaded == NetworkManager.Singleton.ConnectedClients.Count)
        {
            OnAllClientsLoaded?.Invoke();
            clientsLoaded = 0;
        }
    }
}
