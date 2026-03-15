using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// A simple in-game console to display debug messages and copy them to the clipboard.
/// </summary>
public class InGameDebugConsole : MonoBehaviour
{
    public static InGameDebugConsole Instance { get; private set; }

    [Header("UI Elements")]
    [Tooltip("The parent GameObject for the entire console UI.")]
    public GameObject consoleUIParent;
    [Tooltip("The Text component used to display logs.")]
    public TextMeshProUGUI logText;
    [Tooltip("The ScrollRect containing the log text.")]
    public ScrollRect scrollRect;
    [Tooltip("The button to copy logs to the clipboard.")]
    public Button copyButton;
    [Tooltip("The button to close the console.")]
    public Button closeButton;
    [Tooltip("The button to clear the console logs.")]
    public Button clearButton;

    [Header("Configuration")]
    [Tooltip("The maximum number of log messages to store.")]
    public int maxMessages = 1000;

    private struct LogMessage
    {
        public string Message;
        public string StackTrace;
        public LogType Type;
        public string FormattedMessage;
    }

    private readonly List<LogMessage> logMessages = new List<LogMessage>();
    private bool isVisible = false;

    #region Singleton and Initialization
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (consoleUIParent != null)
        {
            consoleUIParent.SetActive(isVisible);
        }

        // Add listeners to buttons if they are assigned
        copyButton?.onClick.AddListener(CopyToClipboard);
        closeButton?.onClick.AddListener(HideConsole);
        clearButton?.onClick.AddListener(ClearConsole);
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }
    #endregion

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        bool ctrlPressed = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
        bool shiftPressed = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        bool dPressed = keyboard.dKey.wasPressedThisFrame;

        if (ctrlPressed && shiftPressed && dPressed) // Abre e fecha com Ctrl + Shift + D
        {
            ToggleVisibility();
        }
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        var newLog = new LogMessage
        {
            Message = logString,
            StackTrace = stackTrace,
            Type = type
        };

        // Add color formatting based on log type
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                newLog.FormattedMessage = $"<color=red>{logString}</color>";
                break;
            case LogType.Warning:
                newLog.FormattedMessage = $"<color=yellow>{logString}</color>";
                break;
            default:
                newLog.FormattedMessage = logString;
                break;
        }

        logMessages.Add(newLog);

        // Trim old messages if we exceed the max count
        if (logMessages.Count > maxMessages)
        {
            logMessages.RemoveAt(0);
        }

        // If the console is visible, refresh the text
        if (isVisible)
        {
            RefreshLogText();
        }
    }

    private void RefreshLogText()
    {
        if (logText == null) return;

        StringBuilder sb = new StringBuilder();
        foreach (var log in logMessages)
        {
            sb.AppendLine(log.FormattedMessage);
        }
        logText.text = sb.ToString();

        // Scroll to the bottom
        if (scrollRect != null)
        {
            StartCoroutine(ScrollToBottom());
        }
    }

    private IEnumerator ScrollToBottom()
    {
        // Espera a Unity calcular o novo tamanho do texto com o Content Size Fitter
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    public void ToggleVisibility()
    {
        isVisible = !isVisible;
        if (consoleUIParent != null)
        {
            consoleUIParent.SetActive(isVisible);
            if (isVisible)
            {
                RefreshLogText();
            }
        }
    }

    private void HideConsole()
    {
        isVisible = false;
        if (consoleUIParent != null)
        {
            consoleUIParent.SetActive(false);
        }
    }

    public void ClearConsole()
    {
        logMessages.Clear();
        RefreshLogText();
    }

    public void CopyToClipboard()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("--- In-Game Console Log ---");
        sb.AppendLine($"Timestamp: {System.DateTime.Now}");
        sb.AppendLine("---");

        foreach (var log in logMessages)
        {
            sb.AppendLine($"[{log.Type}] {log.Message}");
            // Include stack trace for errors and exceptions
            if ((log.Type == LogType.Error || log.Type == LogType.Exception) && !string.IsNullOrEmpty(log.StackTrace))
            {
                sb.AppendLine("Stack Trace:");
                sb.AppendLine(log.StackTrace);
            }
            sb.AppendLine("---");
        }

        GUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("[InGameDebugConsole] Logs copied to clipboard.");
    }
}
