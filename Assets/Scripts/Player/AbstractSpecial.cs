using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public abstract class AbstractSpecial : NetworkBehaviour
{

    [SerializeField] private Sprite icon;
    [SerializeField] private int damage = 5;
    [SerializeField] private float damageMultiplier = 1;
    [SerializeField] private float MaxCooldown;
    [SerializeField] private float activeTime;
    [SerializeField] private UIBar specialIcon;
    [SerializeField] private UIBar specialInUseIcon;
    private TMPro.TextMeshProUGUI amountText;

    protected CharacterStats characterStats;
    public int Damage { get => (int)(damage + characterStats.stats.specialDamage.Value * damageMultiplier); }

    private bool used = false;
    private float cooldown;

    public bool isUsing { get; private set; }

    private void Start()
    {
        characterStats = GetComponent<CharacterStats>();
        if (IsLocalPlayer)
        {
            characterStats.OnRespawn += () =>
            {
                used = false;
                isUsing = false;
                cooldown = 0;
                specialIcon.UpdateBar(0);
            };
            specialIcon.GetComponent<Image>().sprite = icon;
            amountText = specialIcon.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            amountText.text = "";
        }
        _Start();
    }

    protected void UpdateAmountText(string value)
    {
        if (IsLocalPlayer)
        {
            amountText.text = value;
        }
    }

    protected virtual void _Start() { }

    public bool OnCooldown { get => cooldown > 0; }
    public virtual bool canUse() { return !OnCooldown && !used; }

    public bool UseRotation { get; protected set; }

    public void Use()
    {
        used = true;
    }

    protected void Finish()
    {
        isUsing = false;
        used = false;
    }

    public void StartCooldown()
    {
        ResetActive();
        cooldown = MaxCooldown;
        Finish();
    }

    public void SetCooldown(float cooldown)
    {
        this.cooldown = cooldown;
        if (cooldown <= 0)
        {
            FinishedCooldown();
        }
        ResetActive();
        if (specialIcon != null)
            specialIcon.UpdateBar(cooldown / MaxCooldown);
    }

    protected void UpdateActive(float value) => specialInUseIcon.UpdateBar(value);
    protected void ResetActive() {
        specialInUseIcon.SetBar(0);
        activeTimer = 0;
    }


    protected virtual void FinishedCooldown() { }

    float activeTimer = 0;

    private void Update()
    {
        if (!IsLocalPlayer) return;

        if (cooldown > 0)
        {
            cooldown -= Time.deltaTime;
            if (cooldown <= 0)
            {
                FinishedCooldown();
            }
            if (specialIcon != null)
                specialIcon.UpdateBar(cooldown / MaxCooldown);
        }
        if (activeTimer > 0)
        {
            activeTimer -= Time.deltaTime;
            if (!OnCooldown)
                UpdateActive(activeTimer / activeTime);
            if (activeTimer <= 0 && !OnCooldown)
            {
                OnActiveOver();
            }
        }
        _Update();
    }

    protected virtual void OnActiveOver()
    {

    }

    protected void StartActive()
    {
        activeTimer = activeTime;
    }

    protected virtual void _Update()
    {
    }

    public void OnSpecialPress(PlayerController controller)
    {
        isUsing = true;
        _OnSpecialPress(controller);
    }

    public void OnSpecialFinish(PlayerController controller)
    {
        _OnSpecialFinish(controller);
    }

    protected abstract void _OnSpecialPress(PlayerController controller);
    protected abstract void _OnSpecialFinish(PlayerController controller);


}
