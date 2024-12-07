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
    [SerializeField] private Transform redTeamNexusSpawn;
    [SerializeField] private Transform blueTeamNexusSpawn;
    [SerializeField] private NetworkObject nexusPrefab;
    [SerializeField] private LobbyPanel lobbyPanel;
    [SerializeField] private Button startButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private GameObject NetworkUI;
    [SerializeField] private string redTeamlayer;
    [SerializeField] private string blueTeamlayer;
    [SerializeField] private GameObject nexusUI;
    [SerializeField] private UIBar redNexusHealthbar;
    [SerializeField] private UIBar blueNexusHealthbar;
    public Material UNLIT_MATERIAL;
    public Material LIT_MATERIAL;
    public int GOLD_FOR_KILL;
    public int GOLD_PER_SECOND;

    List<ulong> redTeam = new List<ulong>();
    List<ulong> blueTeam = new List<ulong>();
    Dictionary<ulong, bool> readyState = new Dictionary<ulong, bool>();
    bool inRedTeam;
    private int playerCount;
    private bool isStarted;
    private bool isShuttingDown;
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
        nexusUI.SetActive(false);
        redTeam = new List<ulong>();
        blueTeam = new List<ulong>();
        inRedTeam = false;
        isStarted = false;
        playerCount = 0;
        readyButton.interactable = true;
        readyButton.GetComponent<Image>().color = Color.red;
        startButton.interactable = true;
        readyState = new Dictionary<ulong, bool>();
        lobbyPanel.gameObject.SetActive(false);
        NetworkUI.SetActive(true);
        isShuttingDown = false;
        clientSetupCount = 0;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            SetupServerRpc();
        startButton.gameObject.SetActive(IsHost);
        readyButton.gameObject.SetActive(!IsHost);
        readyButton.GetComponent<Image>().color = Color.red;
    }

    public override void OnNetworkDespawn()
    {
        Shutdown();
        lobbyPanel.ResetOnDisconnect();
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
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        var redNexus = Instantiate(nexusPrefab, redTeamNexusSpawn.position, Quaternion.identity);
        redNexus.gameObject.layer = LayerMask.NameToLayer(redTeamlayer);
        redNexus.Spawn();
        var blueNexus = Instantiate(nexusPrefab, blueTeamNexusSpawn.position, Quaternion.identity);
        blueNexus.gameObject.layer = LayerMask.NameToLayer(blueTeamlayer);
        blueNexus.Spawn();
        SetupClientRPC(redNexus.NetworkObjectId, blueNexus.NetworkObjectId);
    }

    [ClientRpc]
    private void SetupClientRPC(ulong redNexusID, ulong blueNexusID)
    {
        var redNexusStats = NetworkManager.Singleton.SpawnManager.SpawnedObjects[redNexusID].GetComponent<CharacterStats>();
        redNexusStats.OnTakeDamage += (ulong damager) =>
        {
            redNexusHealthbar.UpdateBar((float)redNexusStats.Health / redNexusStats.stats.health.Value);
        };
        var blueNexusStats = NetworkManager.Singleton.SpawnManager.SpawnedObjects[blueNexusID].GetComponent<CharacterStats>();
        blueNexusStats.OnTakeDamage += (ulong damager) =>
        {
            blueNexusHealthbar.UpdateBar((float)blueNexusStats.Health / blueNexusStats.stats.health.Value);
        };
        redNexusHealthbar.UpdateBar(1);
        blueNexusHealthbar.UpdateBar(1);
    }

    private void OnClientConnected(ulong id)
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
            lobbyPanel.AddReadyState(id);
            startButton.interactable = false;
        }
        playerCount++;
        UpdatePlayerCountServerRpc(playerCount);
    }

    private void OnClientDisconnected(ulong id)
    {
        if (redTeam.Contains(id))
            redTeam.Remove(id);
        if (blueTeam.Contains(id))
            blueTeam.Remove(id);
        readyState.Remove(id);
        lobbyPanel.RemoveReadyState(id);
        inRedTeam = !inRedTeam;
        playerCount--;
        UpdatePlayerCountServerRpc(playerCount);
    }
    public void ChangeReadyState()
    {
        ChangeReadyStateServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangeReadyStateServerRpc(ulong client)
    {
        readyState[client] = !readyState[client];
        lobbyPanel.SetReadyState(client, readyState[client]);
        ChangeReadyStateClientRpc(client, readyState[client], new ClientRpcParams()
        {
            Send = new ClientRpcSendParams() { TargetClientIds = new ulong[] { client } }
        });
        foreach (var item in readyState.Keys)
        {
            if (!readyState[item])
            {
                startButton.interactable = false;
                return;
            }
        }
        startButton.interactable = true;
        
    }

    [ClientRpc]
    private void ChangeReadyStateClientRpc(ulong client, bool value, ClientRpcParams clientRpcParams)
    {
        readyButton.GetComponent<Image>().color = value ? Color.green : Color.red;
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
        nexusUI.SetActive(true);
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

    public void Win(int team)
    {
        Debug.Log(LayerMask.LayerToName(team) + " win");
        Shutdown();
    }

    public Transform GetSpawnPoint(int layer)
    {
        if (LayerMask.LayerToName(layer) == redTeamlayer) return redTeamSpawn;
        if (LayerMask.LayerToName(layer) == blueTeamlayer) return blueTeamSpawn;
        return redTeamSpawn;
    }
}
