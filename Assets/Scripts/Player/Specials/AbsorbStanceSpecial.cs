using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class AbsorbStanceSpecial : AbstractSpecial
{
    private bool isInStance = false;

    [DescriptionCreator.DescriptionVariable]
    [SerializeField] private int ResourceDrain = 5;
    [SerializeField] private int ResourceGain = 2;
    [SerializeField] private AnimatorOverrideController originalAnimator;
    [SerializeField] private AnimationClip walkAnimation;
    [SerializeField] private AnimationClip walkBlockAnimation;
    [SerializeField] private AnimationClip idleAnimation;
    [SerializeField] private AnimationClip idleBlockAnimation;
    [SerializeField] private ParticleSystem healEffect;

    [SerializeField]
    [DescriptionCreator.DescriptionVariable("white")]
    private int resourceRecover = 1;

    [SerializeField]
    [DescriptionCreator.DescriptionVariable("white")]
    private int rallyAmount = 10;

    [SerializeField]
    [DescriptionCreator.DescriptionVariable("white")]
    private int rallyDuration = 3;

    [SerializeField]
    [DescriptionCreator.DescriptionVariable("white")]
    private int healthThreshold = 10;


    private Animator animator;
    private PlayerAttack attack;

    protected override void _Start()
    {
        animator = GetComponentInChildren<Animator>();
        attack = GetComponent<PlayerAttack>();
        characterStats.OnServerTakeDamage += (ulong damagerID, ref int damage) =>
        {
            if (HasUpgradeUnlocked(2) && isInStance)
            {
                if(characterStats.Health-damage < characterStats.stats.health.Value * (healthThreshold / 100f))
                {
                    damage = Mathf.Min((int)(characterStats.stats.health.Value * (healthThreshold / 100f)) - characterStats.Health,0);
                }
            }
        };
        base._Start();
    }

    protected override bool HasResource()
    {
        return Resource > 0;
    }

    protected override void _OnSpecialFinish(PlayerController controller)
    {
        ChangeStance();
        StartCooldown();
    }

    protected override void _OnSpecialPress(PlayerController controller)
    {

    }

    private float timer = 0;

    protected override void _Update()
    {
        base._Update();
        if (!IsLocalPlayer) return;
        if (timer > 0)
            timer -= Time.deltaTime;
        else
        {
            if (isInStance)
            {
                List<int> ids = new List<int>();
                var colliders = Physics2D.OverlapCircleAll(transform.position, 3);
                foreach (var collider in colliders)
                {
                    if (!controller.TeamController.HasSameTeam(collider.gameObject)) continue;
                    var stats = collider.GetComponent<CharacterStats>();
                    var effectManager = collider.GetComponent<EffectManager>();
                    if (ids.Contains(collider.transform.root.gameObject.GetInstanceID()))
                        continue;
                    if (stats != null)
                    {
                        ids.Add(stats.transform.root.gameObject.GetInstanceID());
                        controller.Heal(stats,Damage);
                        if(HasUpgradeUnlocked(0))
                            Resource += resourceRecover;
                        if(effectManager != null && HasUpgradeUnlocked(1))
                            effectManager.AddEffect("rallied", rallyDuration, rallyAmount, characterStats);
                    }
                }
                Resource -= ResourceDrain;
                if (Resource <= 0)
                    ChangeStance();
            }
            else
            {
                Resource += ResourceGain;
            }
            timer = 1;
        }
    }

    private void ChangeStance()
    {
        ChangeStanceServerRPC(NetworkObjectId);
    }

    [ServerRpc]
    private void ChangeStanceServerRPC(ulong client)
    {
        ChangeStanceClientRPC(client);
    }

    [ClientRpc]
    private void ChangeStanceClientRPC(ulong client)
    {
        NetworkManager.Singleton.SpawnManager.SpawnedObjects[client].GetComponent<AbsorbStanceSpecial>().ChangeAnimation();
    }

    public void ChangeAnimation()
    {
        isInStance = !isInStance;
        if (IsLocalPlayer)
            attack.enabled = !isInStance;
        if (isInStance)
            healEffect.Play();
        else
            healEffect.Stop();
        var overrideAnimator = new AnimatorOverrideController(animator.runtimeAnimatorController);
        var anims = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        for (int i = 0; i < overrideAnimator.animationClips.Length; i++)
        {
            var originalClip = overrideAnimator.animationClips[i];
            var overrideClip = originalAnimator.animationClips[i];
            if (originalClip.name.Contains("Walk"))
                anims.Add(new KeyValuePair<AnimationClip, AnimationClip>(originalClip, isInStance ? walkBlockAnimation : walkAnimation));
            else if (originalClip.name.Contains("Idle"))
                anims.Add(new KeyValuePair<AnimationClip, AnimationClip>(originalClip, isInStance ? idleBlockAnimation : idleAnimation));
            else
                anims.Add(new KeyValuePair<AnimationClip, AnimationClip>(originalClip, overrideClip));

        }
        overrideAnimator.ApplyOverrides(anims);
        animator.runtimeAnimatorController = overrideAnimator;
    }
}
