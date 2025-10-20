using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static Lobby;

public class SyncedTeamController : TeamController
{
    [SerializeField]
    private NetworkVariable<Team> team = new NetworkVariable<Team>(Team.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override Team Team { get => team.Value; }



    public override void OnNetworkSpawn()
    {
        team.OnValueChanged += (Team previousValue, Team newValue) =>
        {
            gameObject.layer = LayerMask.NameToLayer("Team " + newValue.ToString());
            OnTeamChanged?.Invoke(newValue);
        };
    }

    public override void SetTeam(Team team)
    {
        if (!IsOwner) return;
        this.team.Value = team;
        Camera.main.cullingMask = team == Team.Red ? GameManager.instance.RedCameraLayer : GameManager.instance.BlueCameraLayer;
    }

    public override bool HasSameTeam(GameObject gb)
    {
        if (gb == gameObject) return true;
        var teamController = gb.GetComponent<TeamController>();
        if (teamController == null) return false;
        return Team == teamController.Team;
    }
}
