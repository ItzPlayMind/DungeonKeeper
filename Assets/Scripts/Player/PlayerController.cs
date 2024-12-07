using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerController : NetworkBehaviour
{
    [System.Serializable]
    private class AttackSetting
    {
        public float damageMultiplier = 1;
        public float knockBack = 4;
        public float selfKnockBack = 0;
    }

    [SerializeField] private float acceleration = 0.1f;
    [SerializeField] private SpriteRenderer gfx;
    [SerializeField] private float attackComboTime = 0.2f;
    [SerializeField] private AttackSetting[] attackSettings = new AttackSetting[2];
    [SerializeField] private Transform hitboxes;

    private NetworkVariable<bool> isFlipped = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private AbstractSpecial special;
    private AnimationEventSender animatorEvent;
    private InputManager inputManager;
    private Rigidbody2D rb;
    private Animator animator;
    private bool isAttacking;
    private CharacterStats stats;
    private static int LocalClientTeam;
    private int currentAttack = 0;
    private float attackComboTimer = 0;

    public bool canMove { get => !isAttacking && stats.CanBeHit; }

    public override void OnNetworkSpawn()
    {
        if (!IsLocalPlayer)
            return;
        var camera = GameObject.FindGameObjectWithTag("PlayerCamera").GetComponent<Cinemachine.CinemachineVirtualCamera>();
        camera.Follow = transform;
        special = GetComponent<AbstractSpecial>();
        inputManager = InputManager.Instance;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        animatorEvent = animator.GetComponent<AnimationEventSender>();
        stats = GetComponent<CharacterStats>();
        stats.OnTakeDamage += () =>
        {
            isAttacking = false;
        };
        stats.OnRespawn += () =>
        {
            transform.position = GameManager.instance.GetSpawnPoint(gameObject.layer).position;
        };
        if (animatorEvent != null)
            animatorEvent.OnAnimationEvent += AnimationEventCallaback;
        foreach (var item in hitboxes.GetComponentsInChildren<CollisionSender>())
        {
            item.onCollisionEnter += (collider) =>
            {
                if (collider == gameObject)
                    return;
                var stats = collider.GetComponent<CharacterStats>();
                if (stats != null)
                {
                    stats.TakeDamage((int)(stats.stats.damage.Value * attackSettings[currentAttack].damageMultiplier), stats.GenerateKnockBack(stats.transform, transform, attackSettings[currentAttack].knockBack));
                }
            };
        }
    }

    public void OnTeamAssigned()
    {
        if (!IsLocalPlayer)
        {
            if (NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.layer != gameObject.layer)
            {
                GetComponentInChildren<Light2D>().enabled = false;
            }
            return;
        }
        LocalClientTeam = gameObject.layer;
        for (int i = 0; i < hitboxes.childCount; i++)
            hitboxes.GetChild(i).gameObject.layer = gameObject.layer;
    }

    private void AnimationEventCallaback(AnimationEventSender.AnimationEvent animationEvent)
    {
        switch (animationEvent)
        {
            case AnimationEventSender.AnimationEvent.EndAttack:
                animator.ResetTrigger("attacking");
                attackComboTimer = attackComboTime;
                currentAttack = (currentAttack + 1) % attackSettings.Length;
                isAttacking = false;
                break;
            case AnimationEventSender.AnimationEvent.SelfKnockBack:
                rb.AddForce((isFlipped.Value ? transform.right : -transform.right) * attackSettings[currentAttack].selfKnockBack, ForceMode2D.Impulse);
                break;
            case AnimationEventSender.AnimationEvent.Special:
                Special();
                break;
            default:
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        gfx.transform.localScale = new Vector3(Math.Abs(gfx.transform.localScale.x) * (isFlipped.Value ? -1 : 1), gfx.transform.localScale.y, gfx.transform.localScale.z);
        if (!IsLocalPlayer)
            return;
        if (stats.IsDead) return;
        if (!isAttacking || (special != null && special.UseRotation && special.isUsing))
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(inputManager.MousePosition);
            if (transform.position.x > mouseWorldPos.x)
                isFlipped.Value = true;
            if (transform.position.x < mouseWorldPos.x)
                isFlipped.Value = false;
        }
        if (isAttacking)
            return; 
        if (inputManager.PlayerAttackTrigger || inputManager.PlayerAttackHold)
        {
            animator.SetInteger("attack", currentAttack);
            animator.SetTrigger("attacking");
            rb.velocity = Vector2.zero;
            isAttacking = true;
        }
        if (inputManager.PlayerSpecialTrigger)
        {
            if (special != null)
            {
                if(special.canUse()) { 
                    rb.velocity = Vector2.zero;
                    special.OnSpecialPress(this);
                    animator.SetTrigger("special");
                    isAttacking = true;
                }
            }
        }
        if (attackComboTimer > 0)
            attackComboTimer -= Time.deltaTime;
        else if (currentAttack != 0)
            currentAttack = 0;
    }

    private void Special()
    {
        if(IsLocalPlayer)
            if (special != null)
                special.OnSpecialFinish(this);
    }

    private void FixedUpdate()
    {
        if (!IsLocalPlayer) return;
        if (stats.IsDead) return;
        Vector2 input = inputManager.PlayerMovement;
        input = input.normalized;
        if (animator != null)
            animator.SetBool("walking", input != Vector2.zero);
        Move(input);
    }

    void Move(Vector2 input)
    {
        if (canMove)
            rb.AddForce(input*stats.stats.speed.Value, ForceMode2D.Force);
    }
}
