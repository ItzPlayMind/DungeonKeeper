using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.InputSystem;
using static Lobby;

public class ArenaGameManager : GameManager
{
    private NetworkVariable<float> phaseTimer = new NetworkVariable<float>(20f);
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
    private class BonusObjectives
    {
        public string name;
        public string description;
        public Objective objective;
    }

    public int GOLD_PER_ROUND = 1000;
    public int DAMAGE_PER_ROUND = 20;

    private int redPlayersDead = 0;
    private int bluePlayersDead = 0;

    [SerializeField] private BonusObjectives[] bonusObjectives;
    [SerializeField] private Transform redTeamArenaSpawn;
    [SerializeField] private Transform blueTeamArenaSpawn;
    [SerializeField] private CharacterStats redTeamHealth;
    [SerializeField] private CharacterStats blueTeamHealth;

    private Objective currentBonusObjective;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        ChangePhaseText("Prepare", Color.blue);
        phaseTimer.OnValueChanged += (float old, float newValue) =>
        {
            UpdatePhaseTimer();
        };
        phase.OnValueChanged += (Phase oldValue, Phase newValue) =>
        {
            switch (newValue)
            {
                case Phase.Prepare:
                    ChangePhaseText("Prepare", Color.blue);
                    break;
                case Phase.Battle:
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
                bonusObjectiveText.text = bonusObjectives[bonusObjectiveIndex.Value].description;
            }
        };
        round.OnValueChanged += (int old, int value) =>
        {
            if ((value + 1) % 3 == 0)
                cardSelection.gameObject.SetActive(true);
        };
        if (IsServer)
        {
            SetupWinConditionForTeams();
        }
    }

    private void SetupWinConditionForTeams()
    {
        redTeamHealth.OnServerDeath += (_) =>
        {
            Win(LayerMask.NameToLayer(blueTeamlayer));
            phase.Value = Phase.GameOver;
        };
        blueTeamHealth.OnServerDeath += (_) =>
        {
            Win(LayerMask.NameToLayer(redTeamlayer));
            phase.Value = Phase.GameOver;
        };
    }

    protected override void Update()
    {
        if (!IsServer) return;
        if (phaseTimer.Value <= 0 && phase.Value != Phase.GameOver)
        {
            if (cardSelection.gameObject.activeSelf)
                cardSelection.PickRandom();
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
                    currentBonusObjective = Instantiate(bonusObjectives[bonusObjectiveIndex.Value].objective, transform.position, Quaternion.identity);
                    currentBonusObjective.GetComponent<NetworkObject>().Spawn();
                    currentBonusObjective.OnObjectiveComplete += (ulong completer) =>
                    {
                        var playerID = NetworkManager.Singleton.SpawnManager.SpawnedObjects[completer].OwnerClientId;
                        if (Lobby.Instance.RedTeam.Contains(playerID))
                        {
                            bluePlayersDead = 4;
                        }
                        if (Lobby.Instance.BlueTeam.Contains(playerID))
                        {
                            redPlayersDead = 4;
                        }
                        ChangePhase();
                    };
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
                int index = UnityEngine.Random.Range(0, bonusObjectives.Length + 1) - 1;
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
        int minutes = (int)phaseTimer.Value / 60;
        int seconds = (int)phaseTimer.Value % 60;
        timerText.text = $"{minutes:d2}:{seconds:d2}";
    }

    protected override void OnPlayerSpawned(NetworkObject player)
    {
        var stats = player.GetComponent<PlayerStats>();
        if (Lobby.Instance.RedTeam.Contains(player.OwnerClientId))
        {
            stats.OnServerDeath += (_) =>
            {
                redPlayersDead++;
                CheckForEndOfRound();
            };
        }
        if (Lobby.Instance.BlueTeam.Contains(player.OwnerClientId))
        {
            stats.OnServerDeath += (_) =>
            {
                bluePlayersDead++;
                CheckForEndOfRound();
            };
        }
    }

    private void CheckForEndOfRound()
    {
        if (redPlayersDead == Lobby.Instance.RedTeam.Count)
        {
            redTeamHealth.TakeDamage(DAMAGE_PER_ROUND * round.Value, Vector2.zero, null);
            ChangePhase();
        }
        if (bluePlayersDead == Lobby.Instance.BlueTeam.Count)
        {
            blueTeamHealth.TakeDamage(DAMAGE_PER_ROUND * round.Value, Vector2.zero, null);
            ChangePhase();
        }
    }

    private void AddCashToAllPlayers(int cash)
    {
        foreach (var player in Lobby.Instance.RedTeam)
            NetworkManager.Singleton.ConnectedClients[player].PlayerObject.GetComponent<Inventory>()?.AddCash(cash);
        foreach (var player in Lobby.Instance.BlueTeam)
            NetworkManager.Singleton.ConnectedClients[player].PlayerObject.GetComponent<Inventory>()?.AddCash(cash);
    }

    private void SpawnAllPlayersInArena()
    {
        SpawnPlayersFromTeamAtPointClientRpc(redTeamArenaSpawn.position, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = Lobby.Instance.RedTeam.ToArray() } });
        SpawnPlayersFromTeamAtPointClientRpc(blueTeamArenaSpawn.position, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = Lobby.Instance.BlueTeam.ToArray() } });
    }

    [ClientRpc]
    private void SpawnPlayersFromTeamAtPointClientRpc(Vector3 pos, ClientRpcParams param)
    {
        var player = NetworkManager.Singleton.LocalClient.PlayerObject;
        player.transform.position = pos;
    }

    private void SetAllPlayersToSpawn()
    {
        HealPlayerFromTeamClientRPC(new ClientRpcParams() { Send=new ClientRpcSendParams() { TargetClientIds= Lobby.Instance.RedTeam.ToArray()} });
        SpawnPlayersFromTeamAtPointClientRpc(redTeamSpawns[0].position, new ClientRpcParams() { Send=new ClientRpcSendParams() { TargetClientIds= Lobby.Instance.RedTeam.ToArray()} });
        HealPlayerFromTeamClientRPC(new ClientRpcParams() { Send=new ClientRpcSendParams() { TargetClientIds= Lobby.Instance.BlueTeam.ToArray()} });
        SpawnPlayersFromTeamAtPointClientRpc(blueTeamSpawns[0].position, new ClientRpcParams() { Send=new ClientRpcSendParams() { TargetClientIds= Lobby.Instance.BlueTeam.ToArray()} });
    }

    [ClientRpc]
    private void HealPlayerFromTeamClientRPC(ClientRpcParams param)
    {
        var player = NetworkManager.Singleton.LocalClient.PlayerObject;
        var stats = player.GetComponent<PlayerStats>();
        stats.Respawn();
        stats.Heal(100000);
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
