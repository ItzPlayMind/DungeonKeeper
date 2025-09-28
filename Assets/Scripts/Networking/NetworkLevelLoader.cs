using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Cinemachine.DocumentationSortingAttribute;

public class NetworkLevelLoader : NetworkBehaviour
{
    public System.Action OnAllClientsLoaded;

    [SerializeField] private List<int> sceneIndices = new List<int>();

    private int clientsLoaded = 0;

    private void Awake()
    {
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
    }

    private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode _)
    {
        if (sceneIndices.Contains(scene.buildIndex))
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
