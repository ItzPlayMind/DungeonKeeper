using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using static DescriptionCreator;

public class ComboSpecial : AbstractSpecial
{
    [System.Serializable]
    private class ComboHitbox
    {
        public float knockBackForce = 10;
        public float damageMultiplier = 1;
        public float dashForce = 55;
        public CollisionSender hitbox;
        [HideInInspector] public int index;
        public AnimationClip clip;
        public bool canMoveWhileUsing;
        public bool needHit;
    }

    [SerializeField] private ComboHitbox[] hitboxes;
    [SerializeField] private AnimatorOverrideController originalAnimator;
    Rigidbody2D rb;
    Vector2 mouseWorldPos;
    private int currentComboIndex = 0;
    private Animator animator;

    public override bool CanMoveWhileUsing() => hitboxes[currentComboIndex].canMoveWhileUsing;

    private bool hit;

    protected override Dictionary<string, Variable> GetVariablesForDescription()
    {
        var variables = base.GetVariablesForDescription();
        for (int i = 0; i < hitboxes.Length; i++)
            variables.Add("damage" + (i + 1), new Variable() { value = (int)(Damage * hitboxes[i].damageMultiplier), color = "blue"});
        return variables;
    }

    protected override void _Start()
    {
        animator = GetComponentInChildren<Animator>();
        if (!IsLocalPlayer) return;
        rb = GetComponent<Rigidbody2D>();
        int index = 0;
        foreach (var hitbox in hitboxes)
        {
            hitbox.hitbox.gameObject.layer = gameObject.layer;
            hitbox.index = index;
            hitbox.hitbox.onCollisionEnter += (GameObject collider, ref bool hit) =>
            {
                var target = collider.GetComponent<CharacterStats>();
                if (target == null) return;
                if (target.gameObject == gameObject) return;
                if (controller.TeamController.HasSameTeam(target.gameObject))
                    return;
                this.hit = true;
                int damage = (int)(Damage * hitbox.damageMultiplier);
                DealDamage(target, damage, target.GenerateKnockBack(target.transform.transform, transform, hitbox.knockBackForce));
                OnAttackHit(hitbox.index, damage,target);
            };
            index++;
        }
    }

    protected virtual void OnAttackHit(int index, int damage, CharacterStats target)
    {

    }

    protected virtual void OnAttackMiss(int index)
    {

    }

    protected override void _OnSpecialFinish(PlayerController controller)
    {
        if (IsLocalPlayer)
        {
            int oldIndex = currentComboIndex;
            rb.velocity = Vector2.zero;
            var dir = (mouseWorldPos - (Vector2)transform.position).normalized;
            dir.y = 0;
            rb.AddForce(dir * hitboxes[currentComboIndex].dashForce, ForceMode2D.Impulse);
            currentComboIndex = (currentComboIndex + 1) % hitboxes.Length;
            if (((hitboxes[currentComboIndex].needHit && hit) || !hitboxes[currentComboIndex].needHit) && currentComboIndex != 0)
            {
                StartActive();
                Finish();
            }
            else
            {
                ResetComboIndex();
                StartCooldown();
            }
            if (!hit)
                OnAttackMiss(oldIndex);
            hit = false;
        }
        /*if (currentComboIndex == 0)
            StartCooldown();
        else
            Finish();*/
    }

    public void ResetComboIndex()
    {
        currentComboIndex = 0;
    }

    protected override void OnActiveOver()
    {
        ResetAllServerRPC(NetworkObjectId);
        StartCooldown();
    }

    [ServerRpc]
    private void ResetAllServerRPC(ulong client)
    {
        ResetAllClientRPC(client);
    }

    [ClientRpc]
    private void ResetAllClientRPC(ulong client)
    {
        NetworkManager.Singleton.SpawnManager.SpawnedObjects[client].GetComponent<ComboSpecial>().ResetComboIndex();
    }

    protected override void _OnSpecialPress(PlayerController controller)
    {
        UpdateActive(1);
        mouseWorldPos = Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePosition);
        ChangeAnimatorServerRPC(NetworkObjectId, currentComboIndex);
        Use();
    }

    [ServerRpc]
    private void ChangeAnimatorServerRPC(ulong client, int comboIndex)
    {
        ChangeAnimatorClientRPC(client, comboIndex);
    }

    [ClientRpc]
    private void ChangeAnimatorClientRPC(ulong client, int comboIndex)
    {
        NetworkManager.Singleton.SpawnManager.SpawnedObjects[client].GetComponent<ComboSpecial>().SetComboIndex(comboIndex);
    }

    public void SetComboIndex(int comboIndex)
    {
        var overrideAnimator = new AnimatorOverrideController(animator.runtimeAnimatorController);
        var anims = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        for (int i = 0; i < overrideAnimator.animationClips.Length; i++)
        {
            var originalClip = overrideAnimator.animationClips[i];
            var overrideClip = originalAnimator.animationClips[i];
            if (originalClip.name.Contains("Special"))
                anims.Add(new KeyValuePair<AnimationClip, AnimationClip>(originalClip, hitboxes[comboIndex].clip));
            else
                anims.Add(new KeyValuePair<AnimationClip, AnimationClip>(originalClip, overrideClip));
        }
        overrideAnimator.ApplyOverrides(anims);
        animator.runtimeAnimatorController = overrideAnimator;
    }
}
