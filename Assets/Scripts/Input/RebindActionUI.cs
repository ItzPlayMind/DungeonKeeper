using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

////TODO: localization support

////TODO: deal with composites that have parts bound in different control schemes

/// <summary>
/// A reusable component with a self-contained UI for rebinding a single action.
/// </summary>
public class RebindActionUI : MonoBehaviour
{
    /// <summary>
    /// Reference to the action that is to be rebound.
    /// </summary>
    public InputAction action
    {
        get => m_Action;
        set
        {
            m_Action = value;
            UpdateIcon();
        }
    }

    /// <summary>
    /// ID (in string form) of the binding that is to be rebound on the action.
    /// </summary>
    /// <seealso cref="InputBinding.id"/>
    public int bindingId
    {
        get => m_BindingIndex;
        set
        {
            m_BindingIndex = value;
            UpdateIcon();
        }
    }

    /// <summary>
    /// Event that is triggered every time the UI updates to reflect the current binding.
    /// This can be used to tie custom visualizations to bindings.
    /// </summary>
    public UpdateBindingUIEvent updateBindingUIEvent
    {
        get
        {
            if (m_UpdateBindingUIEvent == null)
                m_UpdateBindingUIEvent = new UpdateBindingUIEvent();
            return m_UpdateBindingUIEvent;
        }
    }

    /// <summary>
    /// Event that is triggered when an interactive rebind is started on the action.
    /// </summary>
    public InteractiveRebindEvent startRebindEvent
    {
        get
        {
            if (m_RebindStartEvent == null)
                m_RebindStartEvent = new InteractiveRebindEvent();
            return m_RebindStartEvent;
        }
    }

    /// <summary>
    /// Event that is triggered when an interactive rebind has been completed or canceled.
    /// </summary>
    public InteractiveRebindEvent stopRebindEvent
    {
        get
        {
            if (m_RebindStopEvent == null)
                m_RebindStopEvent = new InteractiveRebindEvent();
            return m_RebindStopEvent;
        }
    }

    /// <summary>
    /// When an interactive rebind is in progress, this is the rebind operation controller.
    /// Otherwise, it is <c>null</c>.
    /// </summary>
    public InputActionRebindingExtensions.RebindingOperation ongoingRebind => m_RebindOperation;

    /// <summary>
    /// Return the action and binding index for the binding that is targeted by the component
    /// according to
    /// </summary>
    /// <param name="action"></param>
    /// <param name="bindingIndex"></param>
    /// <returns></returns>
    public bool ResolveActionAndBinding(out InputAction action)
    {

        action = m_Action;
        if (action == null)
            return false;

        
        // Look up binding index.
        if (m_BindingIndex == -1)
        {
            Debug.LogError($"Cannot find binding with ID '{bindingId}' on '{action}'", this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Trigger a refresh of the currently displayed binding.
    /// </summary>
    public void UpdateIcon()
    {
        // Get display string from action.
        var action = m_Action;
        if (action != null)
        {
            var binding = action.bindings[m_BindingIndex];
            if (binding != null)
                icon.sprite = InputManager.GetIconForBinding(binding);
        }

        // Give listeners a chance to configure UI in response.
        m_UpdateBindingUIEvent?.Invoke(this);
    }

    /// <summary>
    /// Remove currently applied binding overrides.
    /// </summary>
    public void ResetToDefault()
    {
        if (!ResolveActionAndBinding(out var action))
            return;

        action.RemoveBindingOverride(m_BindingIndex);
        UpdateIcon();
    }

    public static void ResetAllToDefault()
    {
        foreach (var rebindUI in s_RebindActionUIs)
        {
            rebindUI.ResetToDefault();
        }
    }

    /// <summary>
    /// Initiate an interactive rebind that lets the player actuate a control to choose a new binding
    /// for the action.
    /// </summary>
    public void StartInteractiveRebind()
    {
        PerformInteractiveRebind(m_Action, m_BindingIndex);
    }


    private void PerformInteractiveRebind(InputAction action, int bindingIndex)
    {
        m_RebindOperation?.Cancel(); // Will null out m_RebindOperation.

        void CleanUp()
        {
            m_RebindOperation?.Dispose();
            m_RebindOperation = null;
        }

        action.Disable();
        var oldBinding = action.bindings[bindingIndex];
        m_Button.interactable = false;
        // Configure the rebind.
        m_RebindOperation = action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .OnCancel(
                operation =>
                {
                    m_Button.interactable = true;
                    action.Enable();
                    m_RebindStopEvent?.Invoke(this, operation);
                    UpdateIcon();
                    CleanUp();
                })
            .OnComplete(
                operation =>
                {
                    m_Button.interactable = true;
                    RemoveDuplicateBindings(action, bindingIndex, oldBinding);
                    action.Enable();
                    m_RebindStopEvent?.Invoke(this, operation);
                    UpdateIcon();
                    CleanUp();
                });

        // Give listeners a chance to act on the rebind starting.
        m_RebindStartEvent?.Invoke(this, m_RebindOperation);

        m_RebindOperation.Start();
    }

    private void RemoveDuplicateBindings(InputAction action, int bindingIndex, InputBinding oldBinding)
    {
        InputBinding newBinding = action.bindings[bindingIndex];
        foreach (var map in m_ActionAsset.actionMaps)
        {
            foreach (var act in map.actions)
            {
                for (int i = 0; i < act.bindings.Count; i++)
                {
                    if (act.bindings[i].effectivePath == newBinding.effectivePath && (newBinding.name != act.bindings[i].name || act != action))
                    {
                        act.Disable();
                        // Unbind the duplicate
                        act.ApplyBindingOverride(i, oldBinding.effectivePath);
                        act.Enable();
                        Debug.Log($"Unbound {newBinding.effectivePath} from {act.name} and replaced with {oldBinding.effectivePath}");
                    }
                }
            }
        }
    }


    protected void OnEnable()
    {
        if (s_RebindActionUIs == null)
            s_RebindActionUIs = new List<RebindActionUI>();
        s_RebindActionUIs.Add(this);
        if (s_RebindActionUIs.Count == 1)
            InputSystem.onActionChange += OnActionChange;
    }

    protected void OnDisable()
    {
        m_RebindOperation?.Dispose();
        m_RebindOperation = null;

        s_RebindActionUIs.Remove(this);
        if (s_RebindActionUIs.Count == 0)
        {
            s_RebindActionUIs = null;
            InputSystem.onActionChange -= OnActionChange;
        }
    }

    private void Start()
    {
        m_Button = GetComponent<Button>();
        m_ActionAsset = InputManager.Instance.PlayerControls.asset;
        m_Action = m_ActionAsset.FindAction(m_BindingName);
        UpdateIcon();
    }

    // When the action system re-resolves bindings, we want to update our UI in response. While this will
    // also trigger from changes we made ourselves, it ensures that we react to changes made elsewhere. If
    // the user changes keyboard layout, for example, we will get a BoundControlsChanged notification and
    // will update our UI to reflect the current keyboard layout.
    private static void OnActionChange(object obj, InputActionChange change)
    {
        if (change != InputActionChange.BoundControlsChanged)
            return;

        var action = obj as InputAction;
        var actionMap = action?.actionMap ?? obj as InputActionMap;
        var actionAsset = actionMap?.asset ?? obj as InputActionAsset;

        for (var i = 0; i < s_RebindActionUIs.Count; ++i)
        {
            var component = s_RebindActionUIs[i];
            var referencedAction = component.action;
            if (referencedAction == null)
                continue;

            if (referencedAction == action ||
                referencedAction.actionMap == actionMap ||
                referencedAction.actionMap?.asset == actionAsset)
                component.UpdateIcon();
        }
    }

    private Button m_Button;

    private InputActionAsset m_ActionAsset;
    private InputAction m_Action;
    [SerializeField]
    private string m_BindingName = "Movement/Movement";
    [SerializeField]
    private int m_BindingIndex = 0;

    [SerializeField]
    private Image icon;

    [Tooltip("Event that is triggered when the way the binding is display should be updated. This allows displaying "
        + "bindings in custom ways, e.g. using images instead of text.")]
    [SerializeField]
    private UpdateBindingUIEvent m_UpdateBindingUIEvent;

    [Tooltip("Event that is triggered when an interactive rebind is being initiated. This can be used, for example, "
        + "to implement custom UI behavior while a rebind is in progress. It can also be used to further "
        + "customize the rebind.")]
    [SerializeField]
    private InteractiveRebindEvent m_RebindStartEvent;

    [Tooltip("Event that is triggered when an interactive rebind is complete or has been aborted.")]
    [SerializeField]
    private InteractiveRebindEvent m_RebindStopEvent;

    private InputActionRebindingExtensions.RebindingOperation m_RebindOperation;

    private static List<RebindActionUI> s_RebindActionUIs;

    [Serializable]
    public class UpdateBindingUIEvent : UnityEvent<RebindActionUI>
    {
    }

    [Serializable]
    public class InteractiveRebindEvent : UnityEvent<RebindActionUI, InputActionRebindingExtensions.RebindingOperation>
    {
    }
}
