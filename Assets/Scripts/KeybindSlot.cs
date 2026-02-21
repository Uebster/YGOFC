using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KeybindSlot : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI actionNameText;
    public TextMeshProUGUI keyText;
    public Button rebindButton;

    private string actionName;
    private OptionsManager manager;

    public void Setup(string action, KeyCode key, OptionsManager optionsManager)
    {
        actionName = action;
        manager = optionsManager;
        
        if (actionNameText != null) actionNameText.text = action;
        UpdateKeyText(key);

        if (rebindButton != null)
        {
            rebindButton.onClick.RemoveAllListeners();
            rebindButton.onClick.AddListener(() => manager.StartRebindProcess(actionName, this));
        }
    }

    public void UpdateKeyText(KeyCode key)
    {
        if (keyText != null) keyText.text = key.ToString();
    }
}
