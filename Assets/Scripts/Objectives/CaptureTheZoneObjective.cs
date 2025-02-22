using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CaptureTheZoneObjective : Objective
{
    [SerializeField] private float maxValue = 50f;
    [SerializeField] private UIBar redProgressBar;
    [SerializeField] private UIBar blueProgressBar;
    private NetworkVariable<int> redTeamProgress = new NetworkVariable<int>(0);
    private NetworkVariable<int> blueTeamProgress = new NetworkVariable<int>(0);
    private int redPlayers;
    private int bluePlayers;

    public override void OnNetworkSpawn()
    {
        redTeamProgress.OnValueChanged += (int old, int newValue) =>
        {
            redProgressBar.UpdateBar(newValue / maxValue);
        };
        blueTeamProgress.OnValueChanged += (int old, int newValue) =>
        {
            blueProgressBar.UpdateBar(newValue / maxValue);
        };
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;
        var playerStats = collision.GetComponent<PlayerStats>();
        if(playerStats != null)
        {
            if (Lobby.Instance.RedTeam.Contains(playerStats.OwnerClientId))
            {
                redPlayers++;
            }
            if (Lobby.Instance.BlueTeam.Contains(playerStats.OwnerClientId))
            {
                bluePlayers++;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsServer) return;
        var playerStats = collision.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            if (Lobby.Instance.RedTeam.Contains(playerStats.OwnerClientId))
            {
                redPlayers--;
            }
            if (Lobby.Instance.BlueTeam.Contains(playerStats.OwnerClientId))
            {
                bluePlayers--;
            }
        }
    }

    private float timer = 0;

    private void Update()
    {
        if (!IsServer) return;
        if (timer > 0)
            timer -= Time.deltaTime;
        else {
            if (redPlayers == 0 && bluePlayers > 0)
            {
                blueTeamProgress.Value++;
                if(blueTeamProgress.Value >= maxValue)
                {
                    Complete(NetworkManager.Singleton.ConnectedClients[Lobby.Instance.BlueTeam[0]].PlayerObject.NetworkObjectId);
                }
            }
            if (redPlayers > 0 && bluePlayers == 0)
            {
                redTeamProgress.Value++;
                if (redTeamProgress.Value >= maxValue)
                {
                    Complete(NetworkManager.Singleton.ConnectedClients[Lobby.Instance.RedTeam[0]].PlayerObject.NetworkObjectId);
                }
            }
            timer = 1f;
        }
    }
}
