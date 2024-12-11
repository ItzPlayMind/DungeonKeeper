using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ObjectiveSystem : NetworkBehaviour
{
    [SerializeField] private float waveSpawnTimer = 5f;
    [SerializeField] private float minionsPerWave = 3f;
    [SerializeField] private NetworkObject redMinionPrefab;
    [SerializeField] private NetworkObject blueMinionPrefab;
    [SerializeField] private Transform redMinionSpawn;
    [SerializeField] private Transform blueMinionSpawn;
    [SerializeField] private Transform redTeamNexusSpawn;
    [SerializeField] private Transform blueTeamNexusSpawn;
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

    public void SpawnObjectives()
    {
        if (!IsServer) return;
        var redNexus = Instantiate(nexusPrefab, redTeamNexusSpawn.position, Quaternion.identity);
        redNexus.Spawn();
        var blueNexus = Instantiate(nexusPrefab, blueTeamNexusSpawn.position, Quaternion.identity);
        blueNexus.Spawn();
        var redTurrets = new ulong[redTeamTurretSpawns.Length];
        for (int i = 0; i < redTeamTurretSpawns.Length; i++)
        {
            var turret = Instantiate(turretPrefab, redTeamTurretSpawns[i].position, Quaternion.identity);
            turret.Spawn();
            redTurrets[i] = turret.NetworkObjectId;
        }
        var blueTurrets = new ulong[blueTeamTurretSpawns.Length];
        for (int i = 0; i < blueTeamTurretSpawns.Length; i++)
        {
            var turret = Instantiate(turretPrefab, blueTeamTurretSpawns[i].position, Quaternion.identity);
            turret.Spawn();
            blueTurrets[i] = turret.NetworkObjectId;
        }
        for (int i = 0; i < objectivesPrefabs.Length; i++)
            Instantiate(objectivesPrefabs[i], objectivesSpawns[i].position, Quaternion.identity).Spawn();
        SpawnNexusClientRPC(redNexus.NetworkObjectId, blueNexus.NetworkObjectId, redTurrets, blueTurrets);
        StartCoroutine(SpawnMinionWave(redMinionPrefab, redMinionSpawn, blueNexus.GetComponent<CharacterStats>()));
        StartCoroutine(SpawnMinionWave(blueMinionPrefab, blueMinionSpawn, redNexus.GetComponent<CharacterStats>()));
    }

    private IEnumerator SpawnMinionWave(NetworkObject minionPrefab, Transform spawn, CharacterStats target)
    {
        while (true)
        {
            for (int i = 0; i < minionsPerWave; i++)
            {
                var minion = Instantiate(minionPrefab, spawn.transform.position, Quaternion.identity);
                minion.GetComponent<MinionAI>().SetBaseTarget(target);
                minion.Spawn();
                yield return new WaitForSeconds(1f);
            }
            yield return new WaitForSeconds(waveSpawnTimer);
        }
    }

    public void SetupTeamBasedObjective(ulong id, string layer, UIBar healthBar)
    {
        var objective = NetworkManager.Singleton.SpawnManager.SpawnedObjects[id].GetComponent<CharacterStats>();
        objective.gameObject.layer = LayerMask.NameToLayer(layer);
        objective.OnTakeDamage += (ulong damager, int damage) =>
        {
            healthBar.UpdateBar((float)objective.Health / objective.stats.health.Value);
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
