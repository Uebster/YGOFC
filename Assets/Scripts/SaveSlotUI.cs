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

    public SaveLoadSystem.GameSaveData MyData => myData; // Propriedade para acesso externo

    public void Setup(SaveLoadSystem.GameSaveData data, System.Action<SaveLoadSystem.GameSaveData> selectCallback, bool isSelected)
    {
        myData = data;
        onAction = selectCallback;

        if (nameText) nameText.text = data.playerName;
        if (dateText) dateText.text = data.lastPlayedTime;

        int act = ((data.campaignProgress - 1) / 10) + 1;
        if (infoText) infoText.text = $"Act {act}";

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

    public void SetSelected(bool isSelected)
    {
        if (selectionHighlight == null) selectionHighlight = GetComponent<Image>();
        if (selectionHighlight != null)
        {
            selectionHighlight.color = isSelected ? new Color(1f, 1f, 0f, 1f) : new Color(1f, 1f, 1f, 0.5f); // Amarelo se selecionado
        }
    }
}