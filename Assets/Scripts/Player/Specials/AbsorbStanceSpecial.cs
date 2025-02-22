using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AbsorbStanceSpecial : AbstractSpecial
{
    private bool isInStance = false;

    [DescriptionCreator.DescriptionVariable]
    [SerializeField] private int ResourceDrain = 2;
    [SerializeField] private AnimatorOverrideController originalAnimator;
    [SerializeField] private AnimationClip walkAnimation;
    [SerializeField] private AnimationClip walkBlockAnimation;
    [SerializeField] private AnimationClip idleAnimation;
    [SerializeField] private AnimationClip idleBlockAnimation;


    private Animator animator;
    private PlayerAttack attack;

    protected override void _Start()
    {
        animator = GetComponentInChildren<Animator>();
        attack = GetComponent<PlayerAttack>();
        attack.OnAttack += (ulong target, ulong user, ref int amount) =>
        {
            Resource += 10;
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
        if (timer > 0)
            timer -= Time.deltaTime;
        else
        {
            if (isInStance)
            {
                List<int> ids = new List<int>();
                var colliders = Physics2D.OverlapCircleAll(transform.position, 5, LayerMask.GetMask(LayerMask.LayerToName(gameObject.layer)));
                foreach (var collider in colliders)
                {
                    var stats = collider.GetComponent<PlayerStats>();
                    if (ids.Contains(stats.GetInstanceID()))
                        continue;
                    if (stats != null)
                    {
                        ids.Add(stats.GetInstanceID());
                        stats.Heal(Damage);
                        Resource -= ResourceDrain;
                    }
                }
                if (Resource <= 0)
                    ChangeStance();
            }
            timer = 1f;
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
