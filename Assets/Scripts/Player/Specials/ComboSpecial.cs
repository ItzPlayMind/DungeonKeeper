using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ComboSpecial : AbstractSpecial
{
    [System.Serializable]
    private class ComboHitbox
    {
        public float knockBackForce = 10;
        public float damageMultiplier = 1;
        public float dashForce = 55;
        public CollisionSender hitbox;
        public AnimationClip clip;
    }

    [SerializeField] private ComboHitbox[] hitboxes;
    [SerializeField] private AnimatorOverrideController originalAnimator;
    [SerializeField] private float automaticResetTime = 5f;
    private PlayerController controller;
    Rigidbody2D rb;
    Vector2 mouseWorldPos;
    private int currentComboIndex = 0;
    private Animator animator;
    protected override void _Start()
    {
        animator = GetComponentInChildren<Animator>();
        if (!IsLocalPlayer) return;
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController>();
        foreach (var hitbox in hitboxes)
        {
            hitbox.hitbox.gameObject.layer = gameObject.layer;
            hitbox.hitbox.onCollisionEnter += (GameObject collider) =>
            {
                controller.HitTarget(collider.GetComponent<CharacterStats>(), (int)(Damage * hitbox.damageMultiplier), hitbox.knockBackForce);
            };
        }
    }

    protected override void _OnSpecialFinish(PlayerController controller)
    {
        if (IsLocalPlayer)
        {
            rb.velocity = Vector2.zero;
            var dir = (mouseWorldPos - (Vector2)transform.position).normalized;
            dir.y = 0;
            rb.AddForce(dir * hitboxes[currentComboIndex].dashForce, ForceMode2D.Impulse);
            currentComboIndex = (currentComboIndex + 1) % hitboxes.Length;
        }
        if (currentComboIndex == 0)
            StartCooldown();
        else
            Finish();
    }

    float automaticResetTimer = 0;
    protected override void _Update()
    {
        if (!IsLocalPlayer) return;
        if(automaticResetTimer > 0)
        {
            automaticResetTimer -= Time.deltaTime;
            if(!OnCooldown)
                UpdateActive(automaticResetTimer / automaticResetTime);
            if(automaticResetTimer <= 0 && !OnCooldown)
            {
                ResetAllServerRPC(NetworkObjectId);
                StartCooldown();
            }
        }
    }

    public void ResetComboIndex()
    {
        currentComboIndex = 0;
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
        automaticResetTimer = automaticResetTime;
        UpdateActive(1);
        mouseWorldPos = Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePosition);
        ChangeAnimatiorServerRPC(NetworkObjectId, currentComboIndex);
        Use();
    }

    [ServerRpc]
    private void ChangeAnimatiorServerRPC(ulong client, int comboIndex)
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
