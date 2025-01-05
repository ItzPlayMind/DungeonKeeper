using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class ObjectiveSystem : NetworkBehaviour
{
    [SerializeField] private float waveSpawnTimer = 5f;
    [SerializeField] private float minionsPerWave = 3f;
    [SerializeField] private NetworkObject redMinionPrefab;
    [SerializeField] private NetworkObject blueMinionPrefab;
    [SerializeField] private Transform redMinionSpawn;
    [SerializeField] private Transform blueMinionSpawn;
    public Transform redTeamNexusSpawn;
    public Transform blueTeamNexusSpawn;
    [SerializeField] private Transform[] redTeamTurretSpawns;
    [SerializeField] private Transform[] blueTeamTurretSpawns;
    [SerializeField] private Transform[] objectivesSpawns;
    [SerializeField] private NetworkObject nexusPrefab;
    [SerializeField] private NetworkObject turretPrefab;
    [SerializeField] private NetworkObject[] objectivesPrefabs;
    [SerializeField] private UIBar redNexusHealthbar;
    [SerializeField] private UIBar[] redTurretHealthbars;
    [SerializeField] private UIBar blueNexusHealthbar;
    [SerializeField] private UIBar[] blueTurretHealthbars;

    private string redTeamlayer;
    private string blueTeamlayer;

    public void Setup(string redTeamlayer, string blueTeamlayer)
    {
        this.redTeamlayer = redTeamlayer;
        this.blueTeamlayer = blueTeamlayer;
    }

    public override void OnNetworkDespawn()
    {
        StopAllCoroutines();
    }

    public void SpawnObjectives(ulong[] redTeam, ulong[] blueTeam)
    {
        if (!IsServer) return;
        var redNexus = Instantiate(nexusPrefab, redTeamNexusSpawn.position, redTeamNexusSpawn.rotation);
        var redNexusScript = redNexus.GetComponent<Nexus>();
        redNexus.Spawn();
        SetAggroSwap(redTeam, redNexusScript);
        var blueNexus = Instantiate(nexusPrefab, blueTeamNexusSpawn.position, blueTeamNexusSpawn.rotation);
        var blueNexusScript = blueNexus.GetComponent<Nexus>();
        blueNexus.Spawn();
        SetAggroSwap(redTeam, blueNexusScript);
        var redTurrets = new ulong[redTeamTurretSpawns.Length];
        for (int i = 0; i < redTeamTurretSpawns.Length; i++)
        {
            var turret = Instantiate(turretPrefab, redTeamTurretSpawns[i].position, Quaternion.identity);
            turret.Spawn();
            var ai = turret.GetComponent<ObjectiveAI>();
            redNexusScript.AddPreviousObjective(ai);
            redTurrets[i] = turret.NetworkObjectId;
            SetAggroSwap(redTeam, ai);
        }
        var blueTurrets = new ulong[blueTeamTurretSpawns.Length];
        for (int i = 0; i < blueTeamTurretSpawns.Length; i++)
        {
            var turret = Instantiate(turretPrefab, blueTeamTurretSpawns[i].position, Quaternion.identity);
            turret.Spawn();
            var ai = turret.GetComponent<ObjectiveAI>();
            blueNexusScript.AddPreviousObjective(ai);
            blueTurrets[i] = turret.NetworkObjectId;
            SetAggroSwap(blueTeam, ai);
        }
        for (int i = 0; i < objectivesPrefabs.Length; i++)
            Instantiate(objectivesPrefabs[i], objectivesSpawns[i].position, Quaternion.identity).Spawn();
        SpawnNexusClientRPC(redNexus.NetworkObjectId, blueNexus.NetworkObjectId, redTurrets, blueTurrets);
        redNexusScript.OnMinionSpawnEvent += () => StartCoroutine(SpawnMinions(redMinionPrefab, redMinionSpawn, blueNexus.GetComponent<CharacterStats>()));
        blueNexusScript.OnMinionSpawnEvent += () => StartCoroutine(SpawnMinions(blueMinionPrefab, blueMinionSpawn, redNexus.GetComponent<CharacterStats>()));
        StartCoroutine(StartSpawnMinionWave(redNexusScript));
        StartCoroutine(StartSpawnMinionWave(blueNexusScript));
    }

    private void SetAggroSwap(ulong[] team, ObjectiveAI ai)
    {
        foreach (var playerID in team)
        {
            var playerStats = NetworkManager.Singleton.ConnectedClients[playerID].PlayerObject.GetComponent<PlayerStats>();
            if (playerStats == null)
            {
                return;
            }
            var setTarget = new CharacterStats.DamageDelegate((ulong damager, int damage) => {
                var targetStats = NetworkManager.Singleton.SpawnManager.SpawnedObjects[damager].GetComponent<PlayerStats>();
                if (targetStats != null)
                    ai?.SetTarget(targetStats);
            });
            playerStats.OnServerTakeDamage += setTarget;
            ai.GetComponent<CharacterStats>().OnServerDeath += (_) => playerStats.OnServerTakeDamage -= setTarget;
        }
    }

    private IEnumerator SpawnMinions(NetworkObject minionPrefab, Transform spawn, CharacterStats target)
    {
        for (int i = 0; i < minionsPerWave; i++)
        {
            var minion = Instantiate(minionPrefab, spawn.transform.position, Quaternion.identity);
            minion.GetComponent<MinionAI>().SetBaseTarget(target);
            minion.Spawn();
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator StartSpawnMinionWave(Nexus nexus)
    {
        while (true)
        {
            yield return new WaitForSeconds(waveSpawnTimer - 1.3f);
            nexus.SpawnMinions();
            yield return new WaitForSeconds(1.3f);

        }
    }

    public void SetupTeamBasedObjective(ulong id, string layer, UIBar healthBar)
    {
        var objective = NetworkManager.Singleton.SpawnManager.SpawnedObjects[id].GetComponent<CharacterStats>();
        objective.gameObject.layer = LayerMask.NameToLayer(layer);
        objective.OnHealthChange += (int _, int newValue) =>
        {
            healthBar.UpdateBar((float)newValue / objective.stats.health.Value);
        };
    }

    [ClientRpc]
    private void SpawnNexusClientRPC(ulong redNexusID, ulong blueNexusID, ulong[] redTurrets, ulong[] blueTurrets)
    {
        SetupTeamBasedObjective(redNexusID, redTeamlayer, redNexusHealthbar);
        SetupTeamBasedObjective(blueNexusID, blueTeamlayer, blueNexusHealthbar);
        for (int i = 0; i < redTurrets.Length; i++)
        {
            SetupTeamBasedObjective(redTurrets[i], redTeamlayer, redTurretHealthbars[i]);
            redTurretHealthbars[i].UpdateBar(1);
        }
        for (int i = 0; i < blueTurrets.Length; i++)
        {
            SetupTeamBasedObjective(blueTurrets[i], blueTeamlayer, blueTurretHealthbars[i]);
            blueTurretHealthbars[i].UpdateBar(1);
        }
        redNexusHealthbar.UpdateBar(1);
        blueNexusHealthbar.UpdateBar(1);
    }
}
