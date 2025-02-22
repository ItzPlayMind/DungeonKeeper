using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Lobby : NetworkBehaviour
{
    [System.Serializable]
    public class GameMode
    {
        public string name;
        public GameManager gameManagerPrefab;
        public int gameModeSceneIndex;
    }

    public enum Team
    {
        None, Red, Blue
    }

    [System.Serializable]
    public class CharacterPortrait
    {
        public GameObject character;
        public CharacterType type;
        public Sprite characterSprite;
        [HideInInspector] public bool locked;
        [HideInInspector] public Button characterPotrait;
        [HideInInspector] public GameObject selectionObject;
    }
    public static Lobby Instance { get; private set; }
    // Start is called before the first frame update
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
        PlayerStatistic = GetComponent<PlayerStatisticsSystem>();
    }
    [SerializeField] private List<CharacterPortrait> characterPortraits = new List<CharacterPortrait>();
    [SerializeField] private NetworkManagerUI NetworkUI;
    [SerializeField] private List<GameMode> GameModes = new List<GameMode>();
    public PlayerStatisticsSystem PlayerStatistic { get; private set; }
    public List<CharacterPortrait> CharacterPortraits { get => characterPortraits; }

    private bool isStarted;
    private bool isShuttingDown;
    private int playerCount;
    private int gameModeIndex = 0;

    public GameMode CurrentGameMode { get => GameModes[gameModeIndex]; }

    public int PlayerCount { get => playerCount; }
    public int CharacterIndex { get; private set; }

    List<ulong> redTeam = new List<ulong>();
    public List<ulong> RedTeam { get => redTeam; }
    List<ulong> blueTeam = new List<ulong>();
    public List<ulong> BlueTeam { get => blueTeam; }
    Dictionary<ulong, bool> readyState = new Dictionary<ulong, bool>();
    bool inRedTeam;
    public Team CurrentTeam { get; private set; }

    private void Start()
    {
        InputManager.Instance.PlayerControls.UI.Close.performed += ShutdownOnEscape;
    }

    public void SetGameModeIndex(int index) => gameModeIndex = index;

    private void ShutdownOnEscape(InputAction.CallbackContext _)
    {
        Shutdown();
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
        LobbyPanel.Instance.SwitchToCharacterSelection();
    }

    [ClientRpc]
    private void UpdateTeamPanelClientRpc(ulong[] redTeam, ulong[] blueTeam)
    {
        LobbyPanel.Instance.UpdateTeamPanel(redTeam, blueTeam);
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
        isShuttingDown = false;
        CurrentTeam = Team.None;
        NetworkUI.gameObject.SetActive(true);
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        SceneManager.LoadScene(0);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SetupServerRpc();
            GameManager.OnInstanceSet += () =>
            {
                var network = GameManager.instance.GetComponent<NetworkObject>();
                if (!network.IsSpawned)
                    network.Spawn();
            };
        }
        LobbyPanel.Instance.NextButton.gameObject.SetActive(IsHost);
        LobbyPanel.Instance.StartButton.gameObject.SetActive(IsHost);
        LobbyPanel.Instance.ReadyButton.GetComponent<Image>().color = Color.red;
    }

    [ServerRpc]
    private void SetupServerRpc()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        Shutdown();
        LobbyPanel.Instance.ResetOnDisconnect();
    }

    public void Shutdown()
    {
        if (isShuttingDown)
            return;
        isShuttingDown = true;
        NetworkManager.Singleton.Shutdown();
        StartCoroutine(WaitForShutdown());
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
        SetClientDataClientRPC(inRedTeam, gameModeIndex, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = new ulong[] { id } } });
        inRedTeam = !inRedTeam;

        readyState.Add(id, false);
        LobbyPanel.Instance.AddReadyState(id);
        LobbyPanel.Instance.StartButton.interactable = false;

        UpdateTeamPanelClientRpc(redTeam.ToArray(), blueTeam.ToArray());
        playerCount++;
        UpdatePlayerCountServerRpc(playerCount);
    }

    [ClientRpc]
    private void SetClientDataClientRPC(bool isRedTeam, int gameModeIndex, ClientRpcParams param)
    {
        if (isRedTeam) CurrentTeam = Team.Red;
        else CurrentTeam = Team.Blue;
        this.gameModeIndex = gameModeIndex;
    }

    private void OnClientDisconnected(ulong id)
    {
        if (redTeam.Contains(id))
            redTeam.Remove(id);
        if (blueTeam.Contains(id))
            blueTeam.Remove(id);
        readyState.Remove(id);
        LobbyPanel.Instance.RemoveReadyState(id);
        inRedTeam = !inRedTeam;
        UpdateTeamPanelClientRpc(redTeam.ToArray(), blueTeam.ToArray());
        playerCount--;
        UpdatePlayerCountServerRpc(playerCount);
    }

    public void ChangeReadyState(int characterIndex)
    {
        CharacterIndex = characterIndex;
        ChangeReadyStateServerRpc(NetworkManager.Singleton.LocalClientId, characterIndex);
    }
    [ServerRpc(RequireOwnership = false)]
    private void ChangeReadyStateServerRpc(ulong client, int characterIndex)
    {
        readyState[client] = !readyState[client];
        LobbyPanel.Instance.SetReadyState(client, readyState[client]);
        ChangeLockStateClientRpc(characterIndex, readyState[client], redTeam.Contains(client));
        ChangeReadyStateClientRpc(readyState[client], new ClientRpcParams()
        {
            Send = new ClientRpcSendParams() { TargetClientIds = new ulong[] { client } }
        });
        foreach (var item in readyState.Keys)
        {
            if (!readyState[item])
            {
                LobbyPanel.Instance.StartButton.interactable = false;
                return;
            }
        }
        LobbyPanel.Instance.StartButton.interactable = true;

    }

    [ClientRpc]
    private void ChangeReadyStateClientRpc(bool value, ClientRpcParams clientRpcParams)
    {
        LobbyPanel.Instance.ReadyButton.GetComponent<Image>().color = value ? Color.green : Color.red;
    }

    [ClientRpc]
    private void ChangeLockStateClientRpc(int characterIndex, bool value, bool isRed)
    {
        LobbyPanel.Instance.ChangeLockStateByIndex(characterIndex, value, isRed ? Color.red : Color.blue);
    }

    [ServerRpc]
    private void UpdatePlayerCountServerRpc(int count)
    {
        UpdatePlayerCountClientRpc(count);
    }

    [ClientRpc]
    private void UpdatePlayerCountClientRpc(int count)
    {
        LobbyPanel.Instance.SetPlayerCount(count);
    }

    public GameObject GetCharacterByIndex(int index)
    {
        return characterPortraits[index].character;
    }

    public void StartGame()
    {
        StartGameServerRPC();
    }

    [ServerRpc]
    private void StartGameServerRPC()
    {
        StartGameClientRPC();
    }

    [ClientRpc]
    private void StartGameClientRPC()
    {
        SceneManager.LoadScene(CurrentGameMode.gameModeSceneIndex);
    }

    private void OnLevelWasLoaded(int level)
    {
        if (!IsServer) return;
        if(level != 0)
        {
            var currentGameMode = Instantiate(CurrentGameMode.gameManagerPrefab);
        }
    }
}
