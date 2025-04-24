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
    private static bool HasInstance;

    private void Awake()
    {
        if (HasInstance) Destroy(gameObject);
        HasInstance = true;
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
        };
        consolePanel.SetActive(false);
        DontDestroyOnLoad(gameObject);
        Application.logMessageReceived += Application_logMessageReceived;
    }

    private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
    {
        AddLogMessage(condition, type);
    }

    void AddLogMessage(string message, LogType type)
    {
        string formattedMessage = $"[{System.DateTime.Now.ToLongTimeString()}] [{type.ToString()}] {message}";
        logMessages.Add(formattedMessage);

        var textMesh = Instantiate(textPrefab, scrollRect.content);
        StartCoroutine(UpdateTextAfterFrame(formattedMessage, textMesh));
        scrollRect.verticalNormalizedPosition = 0f;
    }

    IEnumerator UpdateTextAfterFrame(string newText, TextMeshProUGUI textMesh)
    {
        textMesh.text = newText;
        yield return new WaitForEndOfFrame(); // Wait until the next frame

        float preferredWidth = textMesh.preferredWidth;
        float preferredHeight = textMesh.preferredHeight;

        textMesh.rectTransform.sizeDelta = new Vector2(textPrefab.rectTransform.sizeDelta.x, preferredHeight);
    }

    public void ToggleConsole()
    {
        consolePanel.SetActive(!consolePanel.activeSelf);
        if (consolePanel.activeSelf)
        {
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
        AddLogMessage(message, LogType.Log);
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
