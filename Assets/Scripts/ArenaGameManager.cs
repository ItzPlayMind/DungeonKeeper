using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using static DebugConsole;
using static Lobby;

public class ArenaGameManager : GameManager
{
    private NetworkVariable<float> phaseTimer = new NetworkVariable<float>(10);
    private NetworkVariable<Phase> phase = new NetworkVariable<Phase>(Phase.Prepare);
    private NetworkVariable<int> bonusObjectiveIndex = new NetworkVariable<int>(-1);
    private NetworkVariable<int> round = new NetworkVariable<int>(1);

    [SerializeField] private int battleTime = 300;
    [SerializeField] private int prepareTime = 20;
    [SerializeField] private CardSelection cardSelection;
    [SerializeField] private TextMeshProUGUI phaseText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI bonusObjectiveText;
    private enum Phase
    {
        Prepare,
        Battle,
        GameOver
    }

    [System.Serializable]
    private class BonusObjective
    {
        public string name;
        public string description;
        public Objective objective;
        public bool active = true;
    }

    public int GOLD_PER_ROUND = 1000;
    public int DAMAGE_PER_ROUND = 20;

    private int redPlayersDead = 0;
    private int bluePlayersDead = 0;

    [SerializeField] private BonusObjective[] bonusObjectives;
    [SerializeField] private Transform redTeamArenaSpawn;
    [SerializeField] private Transform blueTeamArenaSpawn;
    [SerializeField] private Transform[] flaskPoints;
    [SerializeField] private NetworkObject flaskObject;
    [SerializeField] private CharacterStats redTeamHealth;
    [SerializeField] private CharacterStats blueTeamHealth;
    [SerializeField] private Animator uiAnimator;
    [SerializeField] private Cinemachine.CinemachineVirtualCamera lastKillCamera;

    private Objective currentBonusObjective;
    private List<NetworkObject> removeAfterRound = new List<NetworkObject>();

    private BonusObjective[] BonusObjectives { get => bonusObjectives.ToList().FindAll(x => x.active).ToArray(); }

    public CardSelection CardSelection { get => cardSelection; }

    public override void OnNetworkSpawn()
    {
        ChangePhaseText("Prepare", Color.blue);
        phaseTimer.OnValueChanged += (float old, float newValue) =>
        {
            if (phase.Value == Phase.Prepare)
            {
                if (newValue <= 2)
                    uiAnimator.ResetTrigger("Countdown");
                if (Mathf.CeilToInt(newValue) == 3)
                    uiAnimator.SetTrigger("Countdown");
            }
            UpdatePhaseTimer();
        };
        phase.OnValueChanged += (Phase oldValue, Phase newValue) =>
        {
            if (cardSelection.gameObject.activeSelf)
                cardSelection.PickRandom();
            switch (newValue)
            {
                case Phase.Prepare:
                    ChangePhaseText("Prepare", Color.blue);
                    GOLD_PER_SECOND = 0;
                    break;
                case Phase.Battle:
                    GOLD_PER_SECOND = 5;
                    if (ShopPanel.Instance.IsActive)
                        ShopPanel.Instance.Toggle();
                    ChangePhaseText("Battle", Color.red);
                    break;
            }
        };
        bonusObjectiveIndex.OnValueChanged += (int old, int newValue) =>
        {
            if (bonusObjectiveIndex.Value == -1)
            {
                currentBonusObjective = null;
                bonusObjectiveText.text = "";
            }
            else
            {
                bonusObjectiveText.text = BonusObjectives[bonusObjectiveIndex.Value].description;
            }
        };
        if (IsServer)
        {
            SetupWinConditionForTeams();
            DebugConsole.OnCommand((_) =>
            {
                phaseTimer.Value = 0;
            }, "end", "round");
        }
        DebugConsole.OnCommand((Command command) =>
        {
            if (command.args.Length != 2) return;
            Card card = CardRegistry.Instance.GetByID(command.args[1]);
            if (card == null) return;
            AddCardToPlayer(card);
            Debug.Log("Card added");
        }, "card", "add");

        base.OnNetworkSpawn();
    }

    private void SetupWinConditionForTeams()
    {
        redTeamHealth.OnServerDeath += (_) =>
        {
            Win(Team.Blue);
            phase.Value = Phase.GameOver;
        };
        blueTeamHealth.OnServerDeath += (_) =>
        {
            Win(Team.Red);
            phase.Value = Phase.GameOver;
        };
    }

    protected override void Update()
    {
        if(phase.Value == Phase.Prepare)
        {
            if (InputManager.Instance.PlayerShopTrigger)
            {
                ShopPanel.Instance.Toggle();
            }
        }
        if (!IsServer) return;
        if (phaseTimer.Value <= 0 && phase.Value != Phase.GameOver)
        {
            ChangePhase();
        }
        else
        {
            phaseTimer.Value -= Time.deltaTime;
        }
    }

    public void ChangePhase()
    {
        if (!IsServer) return;
        switch (phase.Value)
        {
            case Phase.Prepare:
                phase.Value = Phase.Battle;
                phaseTimer.Value = battleTime;
                if (bonusObjectiveIndex.Value != -1)
                {
                    currentBonusObjective = Instantiate(BonusObjectives[bonusObjectiveIndex.Value].objective, transform.position, Quaternion.identity);
                    currentBonusObjective.GetComponent<NetworkObject>().Spawn();
                    currentBonusObjective.OnObjectiveComplete += (ulong completer) =>
                    {
                        var playerID = NetworkManager.Singleton.SpawnManager.SpawnedObjects[completer].OwnerClientId;
                        if (Lobby.Instance.GetTeam(Team.Red).Contains(playerID))
                        {
                            bluePlayersDead = 4;
                        }
                        if (Lobby.Instance.GetTeam(Team.Blue).Contains(playerID))
                        {
                            redPlayersDead = 4;
                        }
                        ChangePhase();
                    };
                }
                foreach (var point in flaskPoints)
                {
                    if (point.childCount == 0)
                    {
                        var flask = Instantiate(flaskObject, point.position, Quaternion.identity);
                        flask.Spawn();
                        removeAfterRound.Add(flask);
                    }
                }
                SpawnAllPlayersInArena();
                break;
            case Phase.Battle:
                if (bluePlayersDead < 3 && redPlayersDead < 3 && phaseTimer.Value <= 0)
                {
                    if (bluePlayersDead <= redPlayersDead)
                        redTeamHealth.TakeDamage(DAMAGE_PER_ROUND * round.Value, Vector2.zero, null);
                    if (redPlayersDead <= bluePlayersDead)
                        blueTeamHealth.TakeDamage(DAMAGE_PER_ROUND * round.Value, Vector2.zero, null);
                }
                if (currentBonusObjective != null)
                    Destroy(currentBonusObjective.gameObject);
                currentBonusObjective = null;
                foreach (var obj in removeAfterRound)
                    if(obj != null) Destroy(obj.gameObject);
                removeAfterRound.Clear();
                int index = UnityEngine.Random.Range(0, BonusObjectives.Length + 1) - 1;
                bonusObjectiveIndex.Value = index;
                phase.Value = Phase.Prepare;
                redPlayersDead = 0;
                bluePlayersDead = 0;
                phaseTimer.Value = prepareTime;
                SetAllPlayersToSpawn();
                AddCashToAllPlayers(GOLD_PER_ROUND);
                round.Value++;
                break;
        }
    }

    private void ChangePhaseText(string text, Color color)
    {
        phaseText.text = text;
        phaseText.color = color;
    }

    private void UpdatePhaseTimer()
    {
        int minutes = Mathf.CeilToInt(phaseTimer.Value) / 60;
        int seconds = Mathf.CeilToInt(phaseTimer.Value) % 60;
        timerText.text = $"{minutes:d2}:{seconds:d2}";
    }

    protected override void OnPlayerSpawned(NetworkObject player)
    {
        var stats = player.GetComponent<PlayerStats>();
        if (Lobby.Instance.GetTeam(Team.Red).Contains(player.OwnerClientId))
        {
            stats.OnServerDeath += (id) =>
            {
                redPlayersDead++;
                CheckForEndOfRound(id);
            };
            stats.OnServerRespawn += () =>
            {
                redPlayersDead=Mathf.Max(0,redPlayersDead-1);
            };
        }
        if (Lobby.Instance.GetTeam(Team.Blue).Contains(player.OwnerClientId))
        {
            stats.OnServerDeath += (id) =>
            {
                bluePlayersDead++;
                CheckForEndOfRound(id);
            };
            stats.OnServerRespawn += () =>
            {
                bluePlayersDead= Mathf.Max(0, bluePlayersDead - 1);
            };
        }
    }

    private void CheckForEndOfRound(ulong lastID)
    {
        if (redPlayersDead == Lobby.Instance.GetTeam(Team.Red).Count)
        {
            redTeamHealth.TakeDamage(DAMAGE_PER_ROUND * round.Value, Vector2.zero, null);
            StartCoroutine(BattlePhaseEnd(lastID));
        }
        if (bluePlayersDead == Lobby.Instance.GetTeam(Team.Blue).Count)
        {
            blueTeamHealth.TakeDamage(DAMAGE_PER_ROUND * round.Value, Vector2.zero, null);
            StartCoroutine(BattlePhaseEnd(lastID));
        }
    }

    private IEnumerator BattlePhaseEnd(ulong id)
    {
        ShowLastKillClientRPC(id);
        yield return new WaitForSeconds(4f*Time.timeScale);
        ChangePhase();
        ResetLastKillClientRPC();
    }

    [ClientRpc]
    private void ShowLastKillClientRPC(ulong lastID)
    {
        lastKillCamera.gameObject.SetActive(true);
        var networkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[lastID];
        lastKillCamera.Follow = networkObject.transform;
        Time.timeScale = 0.5f;
    }

    [ClientRpc]
    private void ResetLastKillClientRPC()
    {
        lastKillCamera.gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    private void AddCashToAllPlayers(int cash)
    {
        foreach (var player in Lobby.Instance.GetTeam(Team.Red))
            NetworkManager.Singleton.ConnectedClients[player].PlayerObject.GetComponent<Inventory>()?.AddCash(cash);
        foreach (var player in Lobby.Instance.GetTeam(Team.Blue))
            NetworkManager.Singleton.ConnectedClients[player].PlayerObject.GetComponent<Inventory>()?.AddCash(cash);
    }

    private void SpawnAllPlayersInArena()
    {
        SpawnPlayersFromTeamAtPointClientRpc(redTeamArenaSpawn.position, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = Lobby.Instance.GetTeam(Lobby.Team.Red).ToArray() } });
        SpawnPlayersFromTeamAtPointClientRpc(blueTeamArenaSpawn.position, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = Lobby.Instance.GetTeam(Lobby.Team.Blue).ToArray() } });
    }

    [ClientRpc]
    private void SpawnPlayersFromTeamAtPointClientRpc(Vector3 pos, ClientRpcParams param)
    {
        var player = NetworkManager.Singleton.LocalClient.PlayerObject;
        player.transform.position = pos;
    }

    private void SetAllPlayersToSpawn()
    {
        SpawnPlayersFromTeamAtPointClientRpc(redTeamSpawns[0].position, new ClientRpcParams() { Send=new ClientRpcSendParams() { TargetClientIds= Lobby.Instance.GetTeam(Lobby.Team.Red).ToArray()} });
        SpawnPlayersFromTeamAtPointClientRpc(blueTeamSpawns[0].position, new ClientRpcParams() { Send=new ClientRpcSendParams() { TargetClientIds= Lobby.Instance.GetTeam(Lobby.Team.Blue).ToArray()} });
        HealAllPlayers();
    }

    private void HealAllPlayers()
    {
        var players = Lobby.Instance.GetTeam(Team.Red).Concat(Lobby.Instance.GetTeam(Team.Blue)).ToArray();
        foreach (var id in players)
        {
            var player = NetworkManager.Singleton.ConnectedClients[id].PlayerObject;
            var stats = player.GetComponent<PlayerStats>();
            stats.Respawn();
            stats.Heal(stats.stats.health.Value);
        }
    }

    public void AddCardToPlayer(Card card)
    {
        AddCardToPlayerServerRPC(card.ID, PlayerController.LocalPlayer.NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddCardToPlayerServerRPC(string cardID, ulong player)
    {
        AddCardToPlayerClientRPC(cardID, player);
    }

    [ClientRpc]
    private void AddCardToPlayerClientRPC(string cardID, ulong playerID)
    {
        var card = CardRegistry.Instance.GetByID(cardID);
        var player = NetworkManager.Singleton.SpawnManager.SpawnedObjects[playerID].GetComponent<CharacterStats>();
        card.Select(player);
    }
}
