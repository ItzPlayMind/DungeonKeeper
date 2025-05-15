using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;

    public static float OUT_OF_COMBAT_TIME = 10f;
    private void Awake()
    {
        instance = this;
        Chat = GetComponent<ChatSystem>();
        PrefabSystem = GetComponent<PrefabSystem>();
        OnInstanceSet?.Invoke();
    }

    public static System.Action OnInstanceSet;

    [SerializeField] protected Transform[] redTeamSpawns;
    [SerializeField] protected Transform[] blueTeamSpawns;
    
    [SerializeField] protected string redTeamlayer;
    [SerializeField] protected string blueTeamlayer;
    [SerializeField] private GameObject nexusUI;
    [SerializeField] private GameObject redTeamWinUI;
    [SerializeField] private GameObject blueTeamWinUI;
    [SerializeField] private LayerMask redCameraLayer;
    [SerializeField] private LayerMask blueCameraLayer;

    [SerializeField] private ShopPanel shopPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private DeathScreen deathScreen;

    public Material UNLIT_MATERIAL;
    public Material LIT_MATERIAL;
    public int GOLD_FOR_KILL;
    public int GOLD_PER_SECOND;
    public int GOLD_PER_TURRET;
    private int clientSetupCount = 0;
    public NetworkVariable<int> RESPAWN_TIME = new NetworkVariable<int>(5);

    public bool GameOver { get; private set; }
    public ChatSystem Chat { get; private set; }
    public PrefabSystem PrefabSystem { get; private set; }
    public DeathScreen DeathScreen { get => deathScreen; }

    private Dictionary<ulong, ulong> playerIDNetworkID = new Dictionary<ulong, ulong>();

    private List<Light2D> lights = new List<Light2D>();

    public override void OnNetworkSpawn()
    {
        StartGame();
        shopPanel.SetInstanceToThis();
        InputManager.Instance.PlayerControls.UI.Close.performed += CloseShopOrExit;
    }

    public void SetTorch(Vector2 pos)
    {
        SetTorchServerRpc(pos);
    }

    public override void OnNetworkDespawn()
    {
        InputManager.Instance.PlayerControls.UI.Close.performed -= CloseShopOrExit;
    }

    private void CloseShopOrExit(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (shopPanel.IsActive)
            shopPanel.Toggle();
        else
            pausePanel.SetActive(!pausePanel.activeSelf);
    }


    [ServerRpc(RequireOwnership = false)]
    private void SetTorchServerRpc(Vector2 pos)
    {
        PrefabSystem.SetTorch(pos);
    }

    private float respawnTimeTimer = 30f;

    protected virtual void Update()
    {
        if (!IsServer) return;
        if(respawnTimeTimer <= 0)
        {
            RESPAWN_TIME.Value++;
            respawnTimeTimer = 30f;
        }
        respawnTimeTimer -= Time.deltaTime;
    }


    public void StartGame()
    {
        if (IsServer)
        {
            RESPAWN_TIME.Value = 5;
            respawnTimeTimer = 30f;
        }
        var virtualCamera = GameObject.FindGameObjectWithTag("PlayerCamera").GetComponent<Cinemachine.CinemachineVirtualCamera>();
        Transform followTarget = null;
        if (Lobby.Instance.CurrentTeam == Lobby.Team.Red)
            followTarget = redTeamSpawns[0];
        if (Lobby.Instance.CurrentTeam == Lobby.Team.Blue)
            followTarget = blueTeamSpawns[0];
        virtualCamera.Follow = followTarget;
        SetReadyForSpawnServerRPC(Lobby.Instance.CharacterIndex, NetworkManager.Singleton.LocalClientId);
    }

    private Dictionary<ulong,int> readyCharacterSpawnPlayers = new Dictionary<ulong, int>();  

    [ServerRpc(RequireOwnership = false)]
    private void SetReadyForSpawnServerRPC(int characterIndex,ulong id)
    {
        readyCharacterSpawnPlayers.Add(id,characterIndex);
        if(readyCharacterSpawnPlayers.Count >= Lobby.Instance.PlayerCount)
        {
            foreach (var playerCharacter in readyCharacterSpawnPlayers)
            {
                SpawnCharacterServerRPC(playerCharacter.Value, playerCharacter.Key);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnCharacterServerRPC(int index, ulong id)
    {
        var spawnIndex = clientSetupCount / 2;
        var spawn = redTeamSpawns[spawnIndex];
        var teamLayer = 0;
        if (Lobby.Instance.GetTeam(Lobby.Team.Red).Contains(id))
        {
            spawn = redTeamSpawns[spawnIndex];
            teamLayer = LayerMask.NameToLayer(redTeamlayer);
        }
        if (Lobby.Instance.GetTeam(Lobby.Team.Blue).Contains(id))
        {
            spawn = blueTeamSpawns[spawnIndex];
            teamLayer = LayerMask.NameToLayer(blueTeamlayer);
        }
        var character = Instantiate(Lobby.Instance.GetCharacterByIndex(index), spawn.transform.position, Quaternion.identity);
        character.layer = teamLayer;
        var network = character.GetComponent<NetworkObject>();
        network.SpawnAsPlayerObject(id);
        playerIDNetworkID.Add(id,NetworkObjectId);
        SetTeamLayerClientRPC(teamLayer, network.NetworkObjectId);
        SetTeamLightLayerClientRPC(teamLayer, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = new List<ulong>() { id } } });
        OnPlayerSpawned(network);
        clientSetupCount++;
        if (clientSetupCount == Lobby.Instance.PlayerCount)
        {
            var clients = NetworkManager.Singleton.ConnectedClients;
            List<ulong> ids = new List<ulong>();
            foreach (var item in clients.Keys)
            {
                ids.Add(clients[item].PlayerObject.NetworkObjectId);
            }
            AllClientsSetupClientRPC(ids.ToArray());
            lights.AddRange(FindObjectsOfType<Light2D>());
        }
    }

    protected virtual void OnPlayerSpawned(NetworkObject player)
    {

    }

    public Light2D[] GetLights() => lights.ToArray();

    public ulong GetNetworkIDFromPlayerID(ulong id) => playerIDNetworkID[id];

    [ClientRpc]
    private void SetTeamLayerClientRPC(int id, ulong networkObjectID)
    {
        var player = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectID];
        player.gameObject.layer = id;
    }

    [ClientRpc]
    private void SetTeamLightLayerClientRPC(int id, ClientRpcParams param)
    {
        if (id == LayerMask.NameToLayer(redTeamlayer))
            Camera.main.cullingMask = redCameraLayer;
        else
            Camera.main.cullingMask = blueCameraLayer;
    }

    [ClientRpc]
    private void AllClientsSetupClientRPC(ulong[] clientIDs)
    {
        nexusUI.SetActive(true);
        foreach (var client in clientIDs)
        {
            NetworkManager.Singleton.SpawnManager.SpawnedObjects[client].GetComponent<PlayerController>().OnTeamAssigned();
        }
    }

    public void Win(Lobby.Team team)
    {
        WinServerRPC(team);
    }

    [ServerRpc(RequireOwnership = false)]
    private void WinServerRPC(Lobby.Team team)
    {
        WinClientRPC(team);
    }

    [ClientRpc]
    private void WinClientRPC(Lobby.Team team)
    {
        GameOver = true;
        var virtualCamera = GameObject.FindGameObjectWithTag("PlayerCamera").GetComponent<Cinemachine.CinemachineVirtualCamera>();
        if (team == Lobby.Team.Red)
        {
            redTeamWinUI.SetActive(true);
        }
        else
        {
            blueTeamWinUI.SetActive(true);
        }
    }

    public void AddCashToTeamFromPlayer(ulong id, int cash)
    {
        List<ulong> team = null;
        if (checkIfIsInTeam(id,Lobby.Team.Red))
            team = Lobby.Instance.GetTeam(Lobby.Team.Red);
        if (checkIfIsInTeam(id, Lobby.Team.Blue))
            team = Lobby.Instance.GetTeam(Lobby.Team.Blue);
        if (team == null) return;
        foreach (var player in team)
            NetworkManager.Singleton.ConnectedClients[player].PlayerObject.GetComponent<Inventory>()?.AddCash(cash);
    }


    private bool checkIfIsInTeam(ulong id, Lobby.Team team)
    {
        var teamIds = Lobby.Instance.GetTeam(team);
        foreach (var item in teamIds)
            if (NetworkManager.Singleton.ConnectedClients[item].PlayerObject.NetworkObjectId == id) return true;
        return false;
    }

    public Transform GetSpawnPoint(int layer)
    {
        if (LayerMask.LayerToName(layer) == redTeamlayer) return redTeamSpawns[0];
        if (LayerMask.LayerToName(layer) == blueTeamlayer) return blueTeamSpawns[0];
        return redTeamSpawns[0];
    }

    public ulong GetPlayerIDFromNetworkID(ulong id)
    {
        foreach (var item in playerIDNetworkID)
        {
            if (item.Value == id)
                return item.Key;
        }
        return 0;
    }

    public void Shutdown()
    {
        Lobby.Instance.Shutdown();
    }

    public void UnlockUpgradeForAllInTeamFromPlayer(ulong id, int index)
    {
        List<ulong> team = null;
        if (checkIfIsInTeam(id, Lobby.Team.Red))
            team = Lobby.Instance.GetTeam(Lobby.Team.Red);
        if (checkIfIsInTeam(id, Lobby.Team.Blue))
            team = Lobby.Instance.GetTeam(Lobby.Team.Blue);
        if (team == null) return;
        foreach (var player in team)
            NetworkManager.Singleton.ConnectedClients[player].PlayerObject?.GetComponent<AbstractSpecial>()?.UnlockUpgrade(index);
    }

    public void UnlockUpgradeForAll(int index)
    {
        List<ulong> team = Lobby.Instance.GetTeam(Lobby.Team.Red).Concat(Lobby.Instance.GetTeam(Lobby.Team.Blue)).ToList();
        foreach (var player in team)
            NetworkManager.Singleton.ConnectedClients[player].PlayerObject?.GetComponent<AbstractSpecial>()?.UnlockUpgrade(index);
    }
}
