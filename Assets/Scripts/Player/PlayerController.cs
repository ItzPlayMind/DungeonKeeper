using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using static PlayerAttack;

public class PlayerController : NetworkBehaviour
{
    public static PlayerController LocalPlayer { get; private set; }
    public static System.Action OnLocalPlayerSetup;

    [SerializeField] private SpriteRenderer gfx;
    [SerializeField] private ShopPanel shopPanel;
    [SerializeField] private GameObject pausePanel; 
    private Rigidbody2D rb;
    private Animator animator;
    private Inventory inventory; 
    private AnimationEventSender animatorEvent;
    public NetworkVariable<bool> isFlipped = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private AbstractSpecial special;
    private InputManager inputManager;
    private PlayerStats stats;
    public ActionDelegate OnHeal;

    private PlayerAttack attack;

    public PlayerAttack Attack { get => attack; }

    public CharacterStats HoveredStats { get; private set; }

    public bool canMove { get => !attack.isAttacking || (special != null && special.isUsing && special.CanMoveWhileUsing()); }

    public override void OnNetworkSpawn()
    {
        if (!IsLocalPlayer)
            return;
        attack = GetComponent<PlayerAttack>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        stats = GetComponent<PlayerStats>();
        animatorEvent = animator.GetComponent<AnimationEventSender>();
        InputManager.Instance.PlayerControls.UI.Close.performed += CloseShopOrExit;
        LocalPlayer = this;
        OnLocalPlayerSetup?.Invoke();
        inventory = GetComponent<Inventory>();
        var camera = GameObject.FindGameObjectWithTag("PlayerCamera").GetComponent<Cinemachine.CinemachineVirtualCamera>();
        camera.Follow = transform;
        special = GetComponent<AbstractSpecial>();
        inputManager = InputManager.Instance;
        animatorEvent.OnAnimationEvent += AnimationEventCallaback;
    }


    public void Heal(CharacterStats stats, int amount)
    {
        if (stats == null) return;
        var currentAmount = amount;
        OnHeal?.Invoke(stats.NetworkObjectId, NetworkObjectId, ref currentAmount);
        stats.Heal(currentAmount);
    }

    private void CloseShopOrExit(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (shopPanel.IsActive)
            shopPanel.Toggle();
        else
            pausePanel.SetActive(!pausePanel.activeSelf);
    }

    public void ExitGame()
    {
        GameManager.instance.Shutdown();
    }

    public override void OnNetworkDespawn()
    {
        InputManager.Instance.PlayerControls.UI.Close.performed -= CloseShopOrExit;
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
        attack.OnTeamAssigned();
    }

    private void AnimationEventCallaback(AnimationEventSender.AnimationEvent animationEvent)
    {
        switch (animationEvent)
        {
            case AnimationEventSender.AnimationEvent.Special:
                Special();
                break;
            default:
                break;
        }
    }

    private float scanTimer = 0.05f;

    // Update is called once per frame
    void Update()
    {
        gfx.transform.localScale = new Vector3(Math.Abs(gfx.transform.localScale.x) * (isFlipped.Value ? -1 : 1), gfx.transform.localScale.y, gfx.transform.localScale.z);
        if (!IsLocalPlayer)
            return;
        if (GameManager.instance.GameOver) return;
        if (scanTimer <= 0f)
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePosition), Vector2.zero);
            if (hit.transform != null)
            {
                if (hit.transform.gameObject == stats.gameObject) return;
                var targetStats = hit.transform.GetComponent<CharacterStats>();
                HoveredStats = targetStats;
            }
            scanTimer = 0.05f;
        }
        else
            scanTimer -= Time.deltaTime;
        if (inputManager.PlayerShopTrigger)
            shopPanel.Toggle();
        if (stats.IsDead) return;
        inventory.UpdateItems();
        if (!attack.isAttacking || (special != null && special.UseRotation && special.isUsing))
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(inputManager.MousePosition);
            if (transform.position.x > mouseWorldPos.x)
                isFlipped.Value = true;
            if (transform.position.x < mouseWorldPos.x)
                isFlipped.Value = false;
        }
        if (attack.isAttacking)
            return; 
        if (inputManager.PlayerAttackTrigger || inputManager.PlayerAttackHold)
        {
            attack.Attack();
        }
        if (inputManager.PlayerSpecialTrigger)
        {
            if (special != null)
            {
                if(special.canUse()) { 
                    rb.velocity = Vector2.zero;
                    special.OnSpecialPress(this);
                    animator.SetTrigger("special");
                    attack.SetAttacking();
                }
            }
        }
        for (int i = 0; i < 6; i++)
            if(InputManager.Instance.PlayerInventoryActiveItemTriggered(i+1))
                inventory.UseItem(i);
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
            rb.AddForce(input*new Vector2(stats.stats.speed.Value, Mathf.Max(stats.stats.speed.Value - 10,0)), ForceMode2D.Force);
    }
}
