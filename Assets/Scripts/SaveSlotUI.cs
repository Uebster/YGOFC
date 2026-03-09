using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class SaveSlotUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public Button actionButton; // O botão principal (fundo do slot)
    public Image selectionHighlight; // Imagem de destaque

    private SaveLoadSystem.GameSaveData myData;
    private System.Action<SaveLoadSystem.GameSaveData> onAction;

    public void Setup(SaveLoadSystem.GameSaveData data, System.Action<SaveLoadSystem.GameSaveData> selectCallback, bool isSelected)
    {
        myData = data;
        onAction = selectCallback;

        // Formata tudo em uma linha: "Nome  |  Data  |  Act X"
        int act = ((data.campaignProgress - 1) / 10) + 1;
        string displayText = $"{data.playerName}   |   {data.lastPlayedTime}   |   Act {act}";
        
        if (nameText) nameText.text = displayText;

        // Configura destaque
        if (selectionHighlight == null) selectionHighlight = GetComponent<Image>();
        if (selectionHighlight != null)
        {
            selectionHighlight.color = isSelected ? new Color(1f, 1f, 0f, 1f) : new Color(1f, 1f, 1f, 0.5f); // Amarelo se selecionado
        }

        if (actionButton)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => onAction?.Invoke(myData));
        }
    }
}