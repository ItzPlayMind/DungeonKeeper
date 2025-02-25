using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static DescriptionCreator;

public abstract class AbstractSpecial : NetworkBehaviour
{
    [System.Serializable]
    public class Upgrade
    {
        [SerializeField] private string name;
        public string Name { get => name; }
        [SerializeField] private string description;

        [SerializeField] private HoverEvent hoverEvent;
        public string Description { get => DescriptionCreator.Generate(description, special?.GetVariablesForDescription()); }
        private AbstractSpecial special;
        public void ApplyToSpecial(AbstractSpecial special)
        {
            this.special = special;
            hoverEvent.gameObject.SetActive(true);
            hoverEvent.onPointerEnter += () =>
            {
                UpgradeHoverOver.Show(this);
            };
            hoverEvent.onPointerExit += () => { 
                UpgradeHoverOver.Hide();
            };
        }
        public bool Unlocked { get; private set; } = false;
        public void Unlock()
        {
            Unlocked = false;
        }
    }

    [DescriptionVariable("white")]
    public string Name;
    [SerializeField] private Sprite icon;
    [SerializeField] private HoverEvent hoverEvent;
    [Multiline][SerializeField] private string description;
    [SerializeField] private Upgrade[] upgrades = new Upgrade[0];
    [SerializeField] private int damage = 5;
    [DescriptionVariable]
    [SerializeField] private float damageMultiplier = 1;
    [SerializeField] private float MaxCooldown;
    [SerializeField] private float activeTime;
    [SerializeField] private UIBar specialIcon;
    [SerializeField] private UIBar specialInUseIcon;
    [SerializeField] private TextUIBar resourceBar;
    private TMPro.TextMeshProUGUI amountText;

    public delegate void SpecialDelegate();
    public SpecialDelegate onSpecial;

    protected PlayerStats characterStats;
    [DescriptionVariable("white")]
    public virtual float Cooldown { get => MaxCooldown; }
    [DescriptionVariable]
    public int Damage { get => (int)(damage + characterStats.stats.specialDamage.Value * damageMultiplier); }

    private bool used = false;
    private float cooldown;
    private int resource;

    [DescriptionVariable("white")]
    public virtual float ActiveTime { get => activeTime; }

    public bool IsActive { get => activeTimer > 0 && !OnCooldown; }

    [DescriptionVariable("white")]
    public int MaxResource
    {
        get => characterStats.stats.resource.Value;
    }

    public int Resource
    {
        get => Mathf.Clamp(resource,0, characterStats.stats.resource.Value); set => resource = Mathf.Clamp(value, 0, characterStats.stats.resource.Value);
    }

    protected void UpdateResourceBar()
    {
        resourceBar.UpdateBar(Resource / (float)characterStats.stats.resource.Value);
        if (resource == 0 && characterStats.stats.resource.Value == 0) return;
        resourceBar.Text = Resource + "/" + characterStats.stats.resource.Value;
    }

    public virtual bool CanMoveWhileUsing() => false;

    public bool isUsing { get; private set; }

    private void Start()
    {
        foreach (var item in upgrades) item.ApplyToSpecial(this);
        hoverEvent.onPointerEnter += () => AbilityHoverOver.Show(this);
        hoverEvent.onPointerExit += () => AbilityHoverOver.Hide();
        characterStats = GetComponent<PlayerStats>();
        if (IsLocalPlayer)
        {
            resource = characterStats.stats.resource.Value;
            UpdateResourceBar();
            characterStats.OnClientRespawn += () =>
            {
                used = false;
                isUsing = false;
                cooldown = 0;
                specialIcon.UpdateBar(0);
                ResetActive();
            };
            specialIcon.GetComponent<Image>().sprite = icon;
            amountText = specialIcon.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            amountText.text = "";
        }
        _Start();
    }

    public string Description(bool detailed = false) {
        if (characterStats == null)
            characterStats = GetComponent<PlayerStats>();
        return DescriptionCreator.Generate(description, GetVariablesForDescription(),detailed);
    }
    protected virtual Dictionary<string, Variable> GetVariablesForDescription()
    {
        var dictionary = new Dictionary<string, Variable>();
        AddVariablesToDictionary(this, GetType().BaseType, dictionary);
        AddVariablesToDictionary(this, GetType(), dictionary);
        AddPropertiesToDictionary(this, GetType(), dictionary);
        return dictionary;
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
    public virtual bool canUse() { return !OnCooldown && !used && HasResource(); }

    public bool UseRotation { get; protected set; }

    protected virtual bool HasResource() { return true; }
    protected virtual void RemoveResource() { }

    public void Use()
    {
        used = true;
        onSpecial?.Invoke();
    }

    protected void Finish()
    {
        isUsing = false;
        used = false;
    }

    public void StartCooldown()
    {
        ResetActive();
        cooldown = Cooldown;
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
            specialIcon.UpdateBar(cooldown / Cooldown);
    }

    public void ReduceCooldown(int seconds)
    {
        cooldown -= seconds;
        if (cooldown <= 0)
        {
            FinishedCooldown();
        }
        if (specialIcon != null)
            specialIcon.UpdateBar(cooldown / Cooldown);
    }

    protected void UpdateActive(float value) => specialInUseIcon.UpdateBar(value);
    protected void ResetActive()
    {
        specialInUseIcon.SetBar(0);
        activeTimer = 0;
    }


    protected virtual void FinishedCooldown() { }

    float activeTimer = 0;

    private void Update()
    {
        _UpdateAll();
        if (!IsLocalPlayer) return;

        if (cooldown > 0)
        {
            cooldown -= Time.deltaTime;
            if (cooldown <= 0)
            {
                FinishedCooldown();
            }
            if (specialIcon != null)
                specialIcon.UpdateBar(cooldown / Cooldown);
        }
        if (activeTimer > 0)
        {
            activeTimer -= Time.deltaTime;
            if (!OnCooldown)
                UpdateActive(activeTimer / ActiveTime);
            if (activeTimer <= 0 && !OnCooldown)
            {
                OnActiveOver();
            }
        }
        _Update();
        UpdateResourceBar();
    }

    protected virtual void OnActiveOver()
    {

    }

    protected void StartActive()
    {
        activeTimer = ActiveTime;
    }
    protected virtual void _UpdateAll()
    {
    }

    protected virtual void _Update()
    {
    }

    public void OnSpecialPress(PlayerController controller)
    {
        isUsing = true;
        RemoveResource();
        _OnSpecialPress(controller);
    }

    public void OnSpecialFinish(PlayerController controller)
    {
        _OnSpecialFinish(controller);
    }

    protected abstract void _OnSpecialPress(PlayerController controller);
    protected abstract void _OnSpecialFinish(PlayerController controller);

    public void UnlockUpgrade(int index)
    {
        UnlockUpgradeServerRPC(index);
    }

    [ServerRpc(RequireOwnership = false)]
    private void UnlockUpgradeServerRPC(int index)
    {
        UnlockUpgradeClientRPC(index);
    }

    [ClientRpc]
    private void UnlockUpgradeClientRPC(int index)
    {
        if(upgrades.Length > index)
            if(!upgrades[index].Unlocked)
                upgrades[index]?.Unlock();
    }

    protected bool HasUpgradeUnlocked(int index) => upgrades[index]?.Unlocked ?? false;
}
