using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class AbstractSpecial: NetworkBehaviour
{

    [SerializeField] private int damage = 5;
    [SerializeField] private float damageMultiplier = 1;
    [SerializeField] private float MaxCooldown;

    protected CharacterStats characterStats;
    public int Damage { get => (int)(damage + characterStats.stats.specialDamage.Value * damageMultiplier); }

    private bool used = false;
    private float cooldown;

    private void Start()
    {
        characterStats = GetComponent<CharacterStats>();
        _Start();
    }

    protected virtual void _Start() { }

    public bool OnCooldown { get => cooldown > 0; }
    public virtual bool canUse() { return !OnCooldown && !used; }

    public bool UseRotation { get; protected set; }

    public void Use() {
        used = true;
    }

    public void StartCooldown()
    {
        cooldown = MaxCooldown;
        used = false;
    }

    protected virtual void FinishedCooldown(){}

    private void Update()
    {
        if(!IsLocalPlayer) return;
        if (cooldown > 0)
        {
            cooldown -= Time.deltaTime;
            if (cooldown <= 0)
            {
                FinishedCooldown();
            }
        }
        _Update();
    }

    protected virtual void _Update()
    {
    }

    public abstract void OnSpecialPress(PlayerController controller);
    public abstract void OnSpecialFinish(PlayerController controller);
}
