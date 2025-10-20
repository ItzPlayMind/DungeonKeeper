using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class HealField : NetworkBehaviour
{
    private CollisionSender collisionSender;
    

    private List<CharacterStats> healTargets = new List<CharacterStats>();

    private int healAmount;
    private PlayerController controller;

    public void SetHealAmount(int healAmount) => this.healAmount = healAmount;
    public void SetController(PlayerController controller) => this.controller = controller;

    public override void OnNetworkSpawn()
    {
        if(!IsServer) return;
        collisionSender = GetComponentInChildren<CollisionSender>();
        collisionSender.onCollisionEnter += (GameObject col, ref bool hit) =>
        {
            var target = col.GetComponent<CharacterStats>();
            if (target == null) return;
            if (!controller.TeamController.HasSameTeam(target.gameObject)) return;
            healTargets.Add(target);
        };
        collisionSender.onCollisionExit += (GameObject col, ref bool hit) =>
        {
            var target = col.GetComponent<CharacterStats>();
            if (target == null) return;
            healTargets.Remove(target);
        };
    }

    private float timer = 0;
    private void Update()
    {
        if (!IsServer) return;
        if(timer <= 0)
        {
            foreach (var target in healTargets.Distinct())
            {
                controller.Heal(target, healAmount);
            }
            timer = 0.1f;
        }
        else
        {
            timer -= Time.deltaTime;
        }
    }
}
