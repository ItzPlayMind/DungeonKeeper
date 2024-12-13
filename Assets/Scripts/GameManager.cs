using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;

    private void Awake()
    {
        if (instance == null) { instance = this; }
        Chat = GetComponent<ChatSystem>();
        Objectives = GetComponent<ObjectiveSystem>();
        PlayerStatistics = GetComponent<PlayerStatisticsSystem>();
    }

    [SerializeField] private Transform redTeamSpawn;
    [SerializeField] private Transform blueTeamSpawn;
    [SerializeField] private LobbyPanel lobbyPanel;
    [SerializeField] private Button startButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private GameObject NetworkUI;
    [SerializeField] private string redTeamlayer;
    [SerializeField] private string blueTeamlayer;
    [SerializeField] private GameObject nexusUI;
    [SerializeField] private Light2D globalLight;
    public NetworkObject TORCH_PREFAB;
    public Material UNLIT_MATERIAL;
    public Material LIT_MATERIAL;
    public int GOLD_FOR_KILL;
    public int GOLD_PER_SECOND;
    public int GOLD_PER_TURRET;
    public NetworkVariable<int> RESPAWN_TIME = new NetworkVariable<int>(5);

    public ChatSystem Chat { get; private set; }
    public ObjectiveSystem Objectives { get; private set; }
    public PlayerStatisticsSystem PlayerStatistics { get; private set; }

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
        Objectives.Setup(redTeamlayer,blueTeamlayer);
        InputManager.Instance.PlayerControls.UI.Close.performed += ShutdownOnEscape;
    }

    private void ShutdownOnEscape(InputAction.CallbackContext _)
    {
        Shutdown();
    }

    public void SetGlobalLight(bool value)
    {
        globalLight.enabled = value;
    }
    public void SetTorch(Vector2 pos)
    {
        SetTorchServerRpc(pos);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetTorchServerRpc(Vector2 pos)
    {
        Instantiate(TORCH_PREFAB, pos, Quaternion.identity).Spawn();
    }

    public void SwitchToCharacterSelection()
    {
        SwitchToCharacterSelectionServerRPC();
    }

    [ServerRpc]
    private void SwitchToCharacterSelectionServerRPC()
    {
        isStarted = true;
        SwitchToCharacterSelectionClientRpc();
    }

    [ClientRpc]
    private void SwitchToCharacterSelectionClientRpc()
    {
        InputManager.Instance.PlayerControls.UI.Close.performed -= ShutdownOnEscape;
        lobbyPanel.SwitchToCharacterSelection();
    }

    [ClientRpc]
    private void UpdateTeamPanelClientRpc(ulong[] redTeam, ulong[] blueTeam)
    {
        lobbyPanel.UpdateTeamPanel(redTeam, blueTeam);
    }

    private IEnumerator WaitForShutdown()
    {
        while (NetworkManager.Singleton.ShutdownInProgress)
            yield return new WaitForEndOfFrame();
        var virtualCamera = GameObject.FindGameObjectWithTag("PlayerCamera").GetComponent<Cinemachine.CinemachineVirtualCamera>();
        virtualCamera.Follow = null;
        virtualCamera.transform.position = new Vector3(0, 0,virtualCamera.transform.position.z);
        SetGlobalLight(true);
        PlayerStatistics.Clear();
        Chat.Clear();
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

    private float respawnTimeTimer = 10f;

    private void Update()
    {
        if (!IsServer) return;
        if (!isStarted) return;
        if(respawnTimeTimer <= 0)
        {
            RESPAWN_TIME.Value++;
            respawnTimeTimer = 10f;
        }
        respawnTimeTimer -= Time.deltaTime;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            SetupServerRpc();
        nextButton.gameObject.SetActive(IsHost);
        startButton.gameObject.SetActive(IsHost);
        readyButton.gameObject.SetActive(!IsHost);
        readyButton.GetComponent<Image>().color = Color.red;
    }

    public override void OnNetworkDespawn()
    {
        Shutdown();
        lobbyPanel.ResetOnDisconnect();
    }

    public void Shutdown()
    {
        if (isShuttingDown)
            return;
        isShuttingDown = true;
        NetworkManager.Singleton.Shutdown();
        StartCoroutine(WaitForShutdown());
    }

    public void StartGame()
    {
        if (!IsServer) respawnTimeTimer = 10f;
        StartGameServerRPC();
    }

    [ServerRpc]
    private void SetupServerRpc()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
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
        UpdateTeamPanelClientRpc(redTeam.ToArray(), blueTeam.ToArray());
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
        UpdateTeamPanelClientRpc(redTeam.ToArray(), blueTeam.ToArray());
        playerCount--;
        UpdatePlayerCountServerRpc(playerCount);
    }
    public void ChangeReadyState(int characterIndex)
    {
        ChangeReadyStateServerRpc(NetworkManager.Singleton.LocalClientId, characterIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangeReadyStateServerRpc(ulong client, int characterIndex)
    {
        readyState[client] = !readyState[client];
        lobbyPanel.SetReadyState(client, readyState[client]);
        ChangeLockStateClientRpc(characterIndex, readyState[client]);
        ChangeReadyStateClientRpc(readyState[client], new ClientRpcParams()
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
    private void ChangeReadyStateClientRpc(bool value, ClientRpcParams clientRpcParams)
    {
        readyButton.GetComponent<Image>().color = value ? Color.green : Color.red;
    }

    [ClientRpc]
    private void ChangeLockStateClientRpc(int characterIndex, bool value)
    {
        lobbyPanel.ChangeLockStateByIndex(characterIndex, value);
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
        Objectives.SpawnObjectives();
        SpawnCharacterClientRPC();
    }

    [ClientRpc]
    private void SpawnCharacterClientRPC()
    {
        SetGlobalLight(false);
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
