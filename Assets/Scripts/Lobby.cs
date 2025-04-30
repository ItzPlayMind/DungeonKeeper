using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
        public AnimatorOverrideController animations;
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
            ResetTeams();
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
    private int gameModeIndex = 1;
    public string LobbyCode { get; private set; }
    private Dictionary<ulong, int> selectedCharacters = new Dictionary<ulong, int>();
    public System.Action OnGameStart;

    public void SetLobbyCode(string lobbyCode)
    {
        this.LobbyCode = lobbyCode;
    }

    public GameMode CurrentGameMode { get => GameModes[gameModeIndex]; }

    public int PlayerCount { get => playerCount; }
    public int CharacterIndex { get; private set; }

    Dictionary<Team, List<ulong>> teams = new Dictionary<Team, List<ulong>>();

    public List<ulong> GetTeam(Team team)
    {
        return teams[team];
    }

    Dictionary<ulong, bool> readyState = new Dictionary<ulong, bool>();

    private Team teamAssign = Team.Blue;
    private int TeamCount { get => Enum.GetValues(typeof(Team)).Length; }

    public Team CurrentTeam { get; private set; }

    public void SetGameModeIndex(int index) => gameModeIndex = index;

    private void ShutdownOnEscape(InputAction.CallbackContext _)
    {
        Shutdown();
    }

    private void ResetTeams()
    {
        for (int i = 1; i < TeamCount; i++)
            teams[(Team)i] = new List<ulong>();
    }

    private IEnumerator WaitForShutdown()
    {
        while (NetworkManager.Singleton.ShutdownInProgress)
            yield return new WaitForEndOfFrame();
        for (int i = 1; i < Enum.GetValues(typeof(Team)).Length; i++)
            teams[(Team)i] = new List<ulong>();
        ResetTeams();
        teamAssign = Team.Blue;
        isStarted = false;
        playerCount = 0;
        readyState = new Dictionary<ulong, bool>();
        isShuttingDown = false;
        CurrentTeam = Team.None;
        NetworkUI.gameObject.SetActive(true);
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        GameManager.OnInstanceSet -= SpawnGameManager;
        characterPortraits.ForEach(x => x.locked = false);
        selectedCharacters.Clear();
        SceneManager.LoadScene(0);
    }

    public override void OnNetworkSpawn()
    {
        InputManager.Instance.PlayerControls.UI.Close.performed += ShutdownOnEscape;
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            GameManager.OnInstanceSet += SpawnGameManager;
        }
        LobbyPanel.Instance.StartButton.gameObject.SetActive(IsHost);
        LobbyPanel.Instance.ReadyButton.GetComponent<Image>().color = Color.red;
    }

    private void SpawnGameManager()
    {
        var network = GameManager.instance.GetComponent<NetworkObject>();
        if (!network.IsSpawned)
            network.Spawn();
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
        Debug.Log("CLIENT CONNECTED");
        if (playerCount == 6 || isStarted)
        {
            NetworkManager.Singleton.DisconnectClient(id);
            Debug.Log("DISCONNECT CLIENT BECAUSE STARTED OR PLAYERCOUNT REACHED");
            return;
        }

        teams[teamAssign].Add(id);
        SetClientDataClientRPC(teamAssign, gameModeIndex, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = new ulong[] { id } } });
        Team[] team = new Team[playerCount];
        int[] teamIndices = new int[playerCount];
        int[] selectedCharacterIndices = new int[playerCount];
        int i = 0;
        foreach (var keyValuePair in selectedCharacters)
        {
            for (int j = 1; j < TeamCount; j++)
            {
                if (teams[(Team)j].Contains(keyValuePair.Key))
                {
                    var teamIndex = teams[(Team)j].IndexOf(keyValuePair.Key);
                    team[i] = (Team)j;
                    teamIndices[i] = teamIndex;
                    selectedCharacterIndices[i] = keyValuePair.Value;
                    i++;
                    break;
                }
            }
        }
        List<int> lockedCharacters = new List<int>();
        for (int j = 0; j < characterPortraits.Count; j++)
        {
            if (characterPortraits[j].locked)
                lockedCharacters.Add(j);
        }

        UpdateCharacterSelectionForNewClientRPC(lockedCharacters.ToArray(),team, teamIndices, selectedCharacterIndices, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = new ulong[] { id } } });
        int newTeamIndex = (int)teamAssign % (TeamCount-1)+1;
        teamAssign = (Team)newTeamIndex;

        readyState.Add(id, false);
        LobbyPanel.Instance.AddReadyState(id);
        LobbyPanel.Instance.StartButton.interactable = false;
        
        playerCount++;
        UpdatePlayerCountServerRpc(playerCount);
    }

    [ClientRpc]
    private void SetClientDataClientRPC(Team team, int gameModeIndex, ClientRpcParams param)
    {
        CurrentTeam = team;
        this.gameModeIndex = gameModeIndex;
    }

    [ClientRpc]
    private void UpdateCharacterSelectionForNewClientRPC(int[] lockedCharacters,Team[] team, int[] teamIndices, int[] selectedCharacterIndices, ClientRpcParams param)
    {
        for (int i = 0; i < teamIndices.Length; i++)
            LobbyPanel.Instance.SetCharacterForClientIndex(selectedCharacterIndices[i], team[i], teamIndices[i]);
        foreach (var characterIndex in lockedCharacters)
            LobbyPanel.Instance.ChangeLockStateByIndex(characterIndex, true);
    }

    private void OnClientDisconnected(ulong id)
    {
        Debug.Log("CLIENT DISCONNECTED");
        for (int j = 1; j < TeamCount; j++)
        {
            if (teams[(Team)j].Contains(id))
            {
                teams[(Team)j].Remove(id);
                teamAssign = (Team)j;
                break;
            }
        }
        readyState.Remove(id);
        LobbyPanel.Instance.RemoveReadyState(id);
        playerCount--;
        UpdatePlayerCountServerRpc(playerCount);
    }

    public void ChangeDisplayedCharacter(int characterIndex)
    {
        ChangeDisplayedCharacterServerRpc(NetworkManager.Singleton.LocalClientId, characterIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangeDisplayedCharacterServerRpc(ulong client, int characterIndex)
    {
        for (int j = 1; j < TeamCount; j++)
        {
            if (teams[(Team)j].Contains(client))
            {
                var teamIndex = teams[(Team)j].IndexOf(client);

                selectedCharacters[client] = characterIndex;
                ChangeDisplayedCharacterClientRpc(characterIndex, (Team)j, teamIndex);
                break;
            }
        }
    }

    [ClientRpc]
    private void ChangeDisplayedCharacterClientRpc(int characterIndex, Team team, int stanceIndex)
    {
        LobbyPanel.Instance.SetCharacterForClientIndex(characterIndex, team, stanceIndex);
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
        Team team = Team.None;
        foreach (var item in teams.Keys){
            if (teams[item].Contains(client))
            {
                team = item;
                break;
            }
        }
        ChangeLockStateClientRpc(characterIndex, readyState[client]);
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
    private void ChangeLockStateClientRpc(int characterIndex, bool value)
    {
        LobbyPanel.Instance.ChangeLockStateByIndex(characterIndex, value);
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
        isStarted = true;
        OnGameStart?.Invoke();
        StartCoroutine(WaitForEndOfAttackSequence());
    }

    IEnumerator WaitForEndOfAttackSequence()
    {
        StartStandsAttackServerRPC();
        yield return new WaitForSeconds(1);
        StartGameServerRPC();
    }

    [ServerRpc]
    private void StartStandsAttackServerRPC()
    {
        StartStandsAttackClientRPC();
    }

    [ClientRpc]
    private void StartStandsAttackClientRPC()
    {
        LobbyPanel.Instance.StartStandsAttack();
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
        InputManager.Instance.PlayerControls.UI.Close.performed -= ShutdownOnEscape;
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
