using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static Lobby;

public abstract class TeamController : NetworkBehaviour
{

    public abstract Team Team { get; }

    public System.Action<Team> OnTeamChanged;

    public abstract void SetTeam(Team team);


    public abstract bool HasSameTeam(GameObject gb);
}
