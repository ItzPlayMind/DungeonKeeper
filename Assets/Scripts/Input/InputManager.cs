using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.UIElements;

public class InputManager : MonoBehaviour
{
    private static InputManager instance;

    public static InputManager Instance
    {
        get
        {
            return instance;
        }
    }

    public enum ActionMap
    {
        Movement, Camera, Combat, UI
    }

    [SerializeField] private float bufferTime;
    private Dictionary<InputAction, float> buffer = new Dictionary<InputAction, float>();


    private static Stack<EscapeClosableWindow> windows = new Stack<EscapeClosableWindow>();

    private PlayerInput playerControls;

    public PlayerInput PlayerControls { get => playerControls; }

    public InputActionMap GetActionMap(ActionMap value)
    {
        switch (value)
        {
            case ActionMap.Movement:
                return playerControls.Movement.Get();
            case ActionMap.Camera:
                return playerControls.Camera.Get();
            case ActionMap.Combat:
                return playerControls.Combat.Get();
            case ActionMap.UI:
                return playerControls.UI.Get();
        }
        return null;
    }

    public InputAction FindAction(string name)
    {
        InputAction action;
        action = playerControls.Movement.Get().FindAction(name);
        if (action != null)
            return action;
        action = playerControls.Camera.Get().FindAction(name);
        if (action != null)
            return action;
        action = playerControls.Combat.Get().FindAction(name);
        if (action != null)
            return action;
        action = playerControls.UI.Get().FindAction(name);
        if (action != null)
            return action;
        return null;
    }

    private float GetValueOrDefault(InputAction action, float defaultValue)
    {
        if (buffer.ContainsKey(action))
        {
            float value = buffer[action];
            if (value > 0)
                buffer.Remove(action);
            return value;
        }
        return defaultValue;
    }

    private bool isOverUI = false;
    private bool fullscreen = false;

    public void SetIsOverUI(bool value)
    {
        isOverUI = value;
    }

    public float Sensitivity { get; set; } = 1f;
    public Vector2 MousePosition { get => playerControls.Camera.MousePosition.ReadValue<Vector2>(); }

    public Vector2 PlayerMovement { get { return playerControls.Movement.Movement.ReadValue<Vector2>(); } }
    public bool PlayerShopTrigger { get { return playerControls.Camera.Shop.triggered; } }
    public bool PlayerInventoryActiveItemTriggered(int slot) => FindAction("Active Item " + slot).triggered;
    //public Vector2 MouseDelta { get { return playerControls.Camera.Look.ReadValue<Vector2>(); } }
    //public Vector2 MousePosition { get { return playerControls.Additional.MousePosition.ReadValue<Vector2>(); } }
    //public bool PlayerDashTriggered { get { return GetValueOrDefault(playerControls.Movement.Dash, -1) > 0; } }

    //public bool PlayerSprinting { get { return GetValueOrDefault(playerControls.Movement.Sprint, -1) > 0; } }

    public bool PlayerAttackTrigger { get { return GetValueOrDefault(playerControls.Combat.Attack, -1) > 0 && !isOverUI; } }
    private bool playerAttackHold;
    public bool PlayerAttackHold { get => playerAttackHold && !isOverUI; private set => playerAttackHold = value; }
    public bool PlayerSpecialTrigger { get { return GetValueOrDefault(playerControls.Combat.Special, -1) > 0 && !isOverUI; } }

    private bool playerInteractTrigger;
    private bool playerInteractHold;
    private bool playerDetailsHold;

    public bool PlayerInteractTrigger { get => playerInteractTrigger && playerControls.Camera.Interact.triggered; }
    public bool PlayerInteractHold { get => playerInteractHold && playerControls.Camera.Interact.triggered; }
    public bool PlayerDetailsHold { get => playerDetailsHold; }
    //public bool PlayerInventoryTrigger { get => playerControls.Additional.Inventory.triggered; }
    //public bool PlayerMenuTrigger { get => playerControls.Additional.Menu.triggered; }

    //public bool PlayerAbility1 { get => GetValueOrDefault(playerControls.Abilities.Ability1, -1) > 0; }
    //public bool PlayerAbility2 { get => GetValueOrDefault(playerControls.Abilities.Ability2, -1) > 0; }
    //public bool PlayerUltimate { get => GetValueOrDefault(playerControls.Abilities.Ultimate, -1) > 0; }

    private void Awake()
    {
        fullscreen = Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen;
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        else
            instance = this;
        DontDestroyOnLoad(gameObject);
        playerControls = new PlayerInput();

        playerControls.Combat.Attack.started += (state) => PlayerAttackHold = true;
        playerControls.Combat.Attack.canceled += (state) => PlayerAttackHold = false;
        playerControls.UI.Details.started += (state) => playerDetailsHold = true;
        playerControls.UI.Details.canceled += (state) => playerDetailsHold = false;
        //playerControls.Combat.Scope.started += (state) => AimDownSightsHold = true;
        //playerControls.Combat.Scope.canceled += (state) => AimDownSightsHold = false;
        //playerControls.Combat.Reload.started += (state) => PlayerReloadHold = true;
        //playerControls.Combat.Reload.canceled += (state) => PlayerReloadHold = false;
        playerControls.Camera.Interact.performed += (state) =>
        {
            playerInteractHold = state.interaction is HoldInteraction;
            playerInteractTrigger = state.interaction is TapInteraction;
        };
        playerControls.UI.Close.performed += (state) =>
        {
            if (windows.Count > 0)
            {
                windows.Pop().CloseOnEscape();
            }
        };
        //AddToSave(this);
    }

    private void CheckForTriggerInActionMap(InputActionMap map)
    {
        foreach (var action in map.actions)
        {
            if (action.triggered)
            {
                buffer[action] = bufferTime;
            }
        }
    }

    private void Update()
    {
        CheckForTriggerInActionMap(playerControls.Movement.Get());
        CheckForTriggerInActionMap(playerControls.Camera.Get());
        CheckForTriggerInActionMap(playerControls.Combat.Get());
        CheckForTriggerInActionMap(playerControls.UI.Get());

        List<InputAction> removeActions = new List<InputAction>();

        var keys = buffer.Keys.ToList();
        foreach (var item in keys)
        {
            buffer[item] -= Time.deltaTime;
            if (buffer[item] <= 0)
                removeActions.Add(item);
        }

        foreach (var item in removeActions)
            buffer.Remove(item);

        if (playerControls.UI.Fullscreen.triggered)
        {
            fullscreen = !fullscreen;
            Screen.fullScreenMode = fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
        }
    }

    private void OnEnable()
    {
        var rebinds = PlayerPrefs.GetString(Config.KEYBINDS_SAVE_KEY);
        if (!string.IsNullOrEmpty(rebinds))
            playerControls.asset.LoadBindingOverridesFromJson(rebinds);
        playerControls?.Enable();
    }

    private void OnDisable()
    {
        if (playerControls != null)
        {
            var rebinds = playerControls.asset.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString(Config.KEYBINDS_SAVE_KEY, rebinds);
            playerControls.Disable();
        }
    }

    public static Sprite GetIconForKey(InputAction action)
    {
        foreach (var binding in action.bindings)
        {
            // Skip composite parts if needed
            if (binding.isPartOfComposite)
                continue;
            var path = binding.effectivePath.Replace("<", "").Replace(">", "").ToUpper();
            return Resources.Load<Sprite>("Buttons/" + path);
        }
        return null;
    }

    public static Sprite GetIconForBinding(InputBinding binding)
    {
        var path = binding.effectivePath.Replace("<", "").Replace(">", "").ToUpper();
        //Debug.Log(path);
        return Resources.Load<Sprite>("Buttons/" + path);
    }

    public void AddEscapeClosableWindow(EscapeClosableWindow window)
    {
        windows.Push(window);
    }

    public void RemoveEscapeClosableWindow(EscapeClosableWindow window)
    {
        var stack = windows.ToList();
        windows.Clear();
        foreach (var item in stack)
        {
            if(item != window)
            {
                windows.Push(item);
            }
        }
    }

    private void OnLevelWasLoaded(int level)
    {
        windows.Clear();
    }

    //private Cinemachine.CinemachinePOV pov;

    //public void SetActiveCamera(CinemachineVirtualCamera cam) => pov = cam?.GetCinemachineComponent<Cinemachine.CinemachinePOV>();
    //private float defaultHorizontalSpeed = -1f;
    //private float defaultVerticalSpeed = -1f;

    //public void ApplySensitivity(bool scoped = false)
    //{
    //    if (pov == null)
    //        return;
    //    if (defaultHorizontalSpeed == -1)
    //        defaultHorizontalSpeed = pov.m_HorizontalAxis.m_MaxSpeed;
    //    if (defaultVerticalSpeed == -1)
    //        defaultVerticalSpeed = pov.m_VerticalAxis.m_MaxSpeed;
    //    pov.m_HorizontalAxis.m_MaxSpeed = defaultHorizontalSpeed * (scoped ? Scope_Sensitivity : Sensitivity);
    //    pov.m_VerticalAxis.m_MaxSpeed = defaultVerticalSpeed * (scoped ? Scope_Sensitivity : Sensitivity);
    //}


}
