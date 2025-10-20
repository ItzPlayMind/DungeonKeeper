using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalTeamController : TeamController
{
    public override Lobby.Team Team { get => PlayerController.LocalPlayer.TeamController.Team; }

    public override bool HasSameTeam(GameObject gb)
    {
        return true;
    }

    public override void SetTeam(Lobby.Team team) { }
}
