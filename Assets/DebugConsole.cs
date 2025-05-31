using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class DebugConsole : MonoBehaviour
{
    public class Command
    {
        public string name;
        public string[] args;

    }

    public GameObject consolePanel;
    public ScrollRect scrollRect;
    public TextMeshProUGUI textPrefab;
    public TMPro.TMP_InputField inputField;

    private List<string> logMessages = new List<string>();
    private static System.Action<Command> onCommand;
    private static DebugConsole instance;

    public static bool Active { get
        {
            return instance.consolePanel.activeSelf;
        }
    }

    private void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
    }

    private void Start()
    {
        InputManager.Instance.PlayerControls.UI.DebugConsole.performed += (_) => ToggleConsole();
        InputManager.Instance.PlayerControls.UI.Submit.performed += (_) =>
        {
            if (!consolePanel.activeSelf) return;
            if (String.IsNullOrEmpty(inputField.text)) return;
            SendCommand(inputField.text);
            inputField.text = "";
            inputField.Select();
            inputField.ActivateInputField();
        };
        consolePanel.SetActive(false);
        DontDestroyOnLoad(gameObject);
        Application.logMessageReceived += Application_logMessageReceived;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= Application_logMessageReceived;
    }

    private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
    {
        AddLogMessage(condition, type);
    }

    void AddLogMessage(string message, LogType type)
    {
        string color = "";
        switch (type)
        {
            case LogType.Error:
                color = "red";
                break;
            case LogType.Warning:
                color = "yellow";
                break;
            default:
                color = "white";
                break;
        }
        string formattedMessage = $"<color={color}>[{System.DateTime.Now.ToLongTimeString()}] [{type.ToString()}] {message}</color>";
        logMessages.Add(formattedMessage);

        var textMesh = Instantiate(textPrefab, scrollRect.content);
        StartCoroutine(scrollToBottomAfterText(textMesh, formattedMessage));
        scrollRect.verticalNormalizedPosition = 0f;
    }

    IEnumerator scrollToBottomAfterText(TextMeshProUGUI textMesh, string formattedMessage)
    {
        textMesh.text = formattedMessage;

        yield return new WaitForSeconds(0.1f);
        scrollRect.verticalNormalizedPosition = 0f;
    }

    public void ToggleConsole()
    {
        consolePanel.SetActive(!consolePanel.activeSelf);
        if (consolePanel.activeSelf)
        {
            scrollRect.verticalNormalizedPosition = 0f;
            inputField.Select();
        }
    }

    public void SendCommand(string message)
    {
        var parts = message.Split(' ');
        Command command = new Command();
        command.name = parts[0];
        command.args = new string[parts.Length - 1];
        Array.Copy(parts, 1, command.args, 0, parts.Length-1);
        onCommand?.Invoke(command);
    }

    public static void OnCommand(System.Action<Command> action, string name, params string[] args)
    {
        onCommand += (Command command) =>
        {
            if (args.Length > command.args.Length) return;
            for (int i = 0; i < args.Length; i++)
                if (args[i] != command.args[i]) return;
            if (!command.name.Equals(name)) return;
            action?.Invoke(command);
        };
    }
}
