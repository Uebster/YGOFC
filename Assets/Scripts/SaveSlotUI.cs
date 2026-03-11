using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class SaveSlotUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI infoText;
    public Button actionButton; // O botão principal (fundo do slot)
    public Image selectionHighlight; // Imagem de destaque

    private SaveLoadSystem.GameSaveData myData;
    private System.Action<SaveLoadSystem.GameSaveData> onAction;
    private SaveLoadMenu menuManager; // Referência ao menu para pegar as cores

    public SaveLoadSystem.GameSaveData MyData => myData; // Propriedade para acesso externo

    public void Setup(SaveLoadSystem.GameSaveData data, System.Action<SaveLoadSystem.GameSaveData> selectCallback, bool isSelected)
    {
        Debug.Log($"[SaveSlotUI] Configurando slot para: {(data != null ? data.playerName : "null")}");
        myData = data;
        menuManager = GetComponentInParent<SaveLoadMenu>();
        onAction = selectCallback;

        // Auto-atribuição para robustez, caso as referências se percam no Inspector.
        // Procura pelos nomes exatos dos GameObjects na hierarquia do prefab.
        if (nameText == null) nameText = transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (dateText == null) dateText = transform.Find("Date")?.GetComponent<TextMeshProUGUI>();
        if (infoText == null) infoText = transform.Find("Info")?.GetComponent<TextMeshProUGUI>();

        if (data == null) // Verificação de segurança para dados nulos
        {
            if (nameText) nameText.text = "--- DADOS CORROMPIDOS ---";
            if (dateText) dateText.text = "Não foi possível ler o save.";
            if (infoText) infoText.text = "---";
            if (actionButton) actionButton.interactable = false;
            return;
        }

        if (nameText) nameText.text = string.IsNullOrWhiteSpace(data.playerName) ? "Save Slot" : data.playerName;
        if (dateText) dateText.text = string.IsNullOrWhiteSpace(data.lastPlayedTime) ? "---" : data.lastPlayedTime;

        int act = ((data.campaignProgress - 1) / 10) + 1;
        if (infoText) infoText.text = (act > 0) ? $"Act {act}" : "Act 1";

        // Configura destaque
        SetSelected(isSelected);

        // Auto-atribuição do botão para robustez
        if (actionButton == null)
        {
            actionButton = GetComponent<Button>();
        }

        if (actionButton)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => onAction?.Invoke(myData));
        }
        else
        {
            Debug.LogError($"[SaveSlotUI] Não foi possível encontrar o componente Button no prefab do slot: {gameObject.name}");
        }
    }

    public void SetupForNewSave(System.Action newSaveCallback)
    {
        myData = null; // Este slot não representa um save existente
        menuManager = GetComponentInParent<SaveLoadMenu>();

        // Auto-atribuição para robustez
        if (nameText == null) nameText = transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (dateText == null) dateText = transform.Find("Date")?.GetComponent<TextMeshProUGUI>();
        if (infoText == null) infoText = transform.Find("Info")?.GetComponent<TextMeshProUGUI>();

        if (nameText) nameText.text = "[ Create New Save ]";
        if (dateText) dateText.text = "";
        if (infoText) infoText.text = "";

        SetSelected(false); // Nunca começa selecionado

        if (actionButton == null) actionButton = GetComponent<Button>();
        if (actionButton)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => newSaveCallback?.Invoke());
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (selectionHighlight == null) selectionHighlight = GetComponent<Image>();
        if (selectionHighlight != null)
        {
            selectionHighlight.color = isSelected ? menuManager.selectedColor : menuManager.defaultColor;
        }
    }
}