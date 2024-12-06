using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;

    private void Awake()
    {
        if (instance == null) { instance = this; }
    }

    [SerializeField] private Transform redTeamSpawn;
    [SerializeField] private Transform blueTeamSpawn;
    [SerializeField] private LobbyPanel lobbyPanel;
    [SerializeField] private Button startButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private GameObject NetworkUI;
    [SerializeField] private string redTeamlayer;
    [SerializeField] private string blueTeamlayer;
    public Material UNLIT_MATERIAL;
    public Material LIT_MATERIAL;

    List<ulong> redTeam = new List<ulong>();
    List<ulong> blueTeam = new List<ulong>();
    Dictionary<ulong, bool> readyState = new Dictionary<ulong, bool>();
    bool inRedTeam;
    private int playerCount;
    private bool isStarted;
    private bool isShuttingDown;
    private bool isSetup;
    private int clientSetupCount = 0;

    private void Start()
    {
        InputManager.Instance.PlayerControls.UI.Close.performed += (_) =>
        {
            Shutdown();
        };
    }

    private IEnumerator WaitForShutdown()
    {
        while (NetworkManager.Singleton.ShutdownInProgress)
            yield return new WaitForEndOfFrame();
        redTeam = new List<ulong>();
        blueTeam = new List<ulong>();
        inRedTeam = false;
        isStarted = false;
        playerCount = 0;
        readyState = new Dictionary<ulong, bool>();
        lobbyPanel.gameObject.SetActive(false);
        NetworkUI.SetActive(true);
        isShuttingDown = false;
        clientSetupCount = 0;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            SetupServerRpc();
        startButton.gameObject.SetActive(IsHost);
        readyButton.gameObject.SetActive(!IsHost);
    }

    public override void OnNetworkDespawn()
    {
        Shutdown();
    }

    private void Shutdown()
    {
        if (isShuttingDown)
            return;
        isShuttingDown = true;
        NetworkManager.Singleton.Shutdown();
        StartCoroutine(WaitForShutdown());
    }

    public void StartGame()
    {
        StartGameServerRPC();
    }

    [ServerRpc]
    private void SetupServerRpc()
    {
        if (isSetup) return;
        NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
        {
            if (playerCount == 6 || isStarted)
            {
                NetworkManager.Singleton.DisconnectClient(id);
                return;
            }

            if (inRedTeam)
                redTeam.Add(id);
            else
                blueTeam.Add(id);
            inRedTeam = !inRedTeam;

            if (id != 0)
            {
                readyState.Add(id, false);
                startButton.enabled = false;
            }
            playerCount++;
            UpdatePlayerCountServerRpc(playerCount);
        };
        NetworkManager.Singleton.OnClientDisconnectCallback += (id) =>
        {
            if (redTeam.Contains(id))
                redTeam.Remove(id);
            if (blueTeam.Contains(id))
                blueTeam.Remove(id);
            readyState.Remove(id);
            inRedTeam = !inRedTeam;
            playerCount--;
            UpdatePlayerCountServerRpc(playerCount);
        };
        isSetup = true;
    }

    public void ChangeReadyState()
    {
        ChangeReadyStateServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangeReadyStateServerRpc(ulong client)
    {
        readyState[client] = !readyState[client];
        foreach (var item in readyState.Keys)
        {
            if (!readyState[item]) return;
        }
        startButton.enabled = true;
        ChangeReadyStateClientRpc(client, readyState[client], new ClientRpcParams()
        {
            Send = new ClientRpcSendParams() { TargetClientIds = new ulong[] { client } }
        });
    }

    [ClientRpc]
    private void ChangeReadyStateClientRpc(ulong client, bool value, ClientRpcParams clientRpcParams)
    {
        readyButton.enabled = false;
    }

    [ServerRpc]
    private void UpdatePlayerCountServerRpc(int count)
    {
        UpdatePlayerCountClientRpc(count);
    }

    [ClientRpc]
    private void UpdatePlayerCountClientRpc(int count)
    {
        lobbyPanel.SetPlayerCount(count);
    }

    [ServerRpc]
    private void StartGameServerRPC()
    {
        isStarted = true;
        SpawnCharacterClientRPC();
    }

    [ClientRpc]
    private void SpawnCharacterClientRPC()
    {
        lobbyPanel.gameObject.SetActive(false);
        SpawnCharacterServerRPC(lobbyPanel.GetSelectedIndex(), NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnCharacterServerRPC(int index, ulong id)
    {
        var spawn = redTeamSpawn;
        var teamLayer = 0;
        if (redTeam.Contains(id))
        {
            spawn = redTeamSpawn;
            teamLayer = LayerMask.NameToLayer(redTeamlayer);
        }
        if (blueTeam.Contains(id))
        {
            spawn = blueTeamSpawn;
            teamLayer = LayerMask.NameToLayer(blueTeamlayer);
        }
        var character = Instantiate(lobbyPanel.GetCharacterByIndex(index), spawn.transform.position, Quaternion.identity);
        character.layer = teamLayer;
        var network = character.GetComponent<NetworkObject>();
        network.SpawnAsPlayerObject(id);
        SetTeamLayerClientRPC(teamLayer, network.NetworkObjectId);
        clientSetupCount++;
        if (clientSetupCount == playerCount)
        {
            var clients = NetworkManager.Singleton.ConnectedClients;
            List<ulong> ids = new List<ulong>();
            foreach (var item in clients.Keys)
            {
                ids.Add(clients[item].PlayerObject.NetworkObjectId);
            }
            AllClientsSetupClientRPC(ids.ToArray());
        }
    }

    [ClientRpc]
    private void SetTeamLayerClientRPC(int id, ulong networkObjectID)
    {
        var player = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectID];
        player.gameObject.layer = id;
    }

    [ClientRpc]
    private void AllClientsSetupClientRPC(ulong[] clientIDs)
    {
        foreach (var client in clientIDs)
        {
            NetworkManager.Singleton.SpawnManager.SpawnedObjects[client].GetComponent<PlayerController>().OnTeamAssigned();
        }
    }

    public Transform GetSpawnPoint(int layer)
    {
        if (LayerMask.LayerToName(layer) == redTeamlayer) return redTeamSpawn;
        if (LayerMask.LayerToName(layer) == blueTeamlayer) return blueTeamSpawn;
        return redTeamSpawn;
    }
}
