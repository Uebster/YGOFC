using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveSlotUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI infoText; // Ex: "Act 3 - Nível 25"
    public Button actionButton; // O botão principal (fundo do slot)
    public Button deleteButton; // Botão de lixeira

    private SaveLoadSystem.GameSaveData myData;
    private System.Action<SaveLoadSystem.GameSaveData> onAction;
    private System.Action<SaveLoadSystem.GameSaveData> onDelete;

    public void Setup(SaveLoadSystem.GameSaveData data, System.Action<SaveLoadSystem.GameSaveData> actionCallback, System.Action<SaveLoadSystem.GameSaveData> deleteCallback, bool isSaveMode)
    {
        myData = data;
        onAction = actionCallback;
        onDelete = deleteCallback;

        if (nameText) nameText.text = data.playerName;
        if (dateText) dateText.text = data.lastPlayedTime;
        
        // Calcula o Ato atual baseado no nível (1-10 = Act 1, 11-20 = Act 2...)
        int act = ((data.campaignProgress - 1) / 10) + 1;
        if (infoText) infoText.text = $"Campanha: Act {act}";

        if (actionButton)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => onAction?.Invoke(myData));
        }

        if (deleteButton)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() => onDelete?.Invoke(myData));
        }
    }
}