using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dateText; // Usaremos para mostrar a contagem de cartas
    public TextMeshProUGUI infoText; // Não usado por enquanto, mas mantido para consistência
    public Button actionButton;
    public Image selectionHighlight;

    private SaveLoadSystem.DeckRecipe myData;
    private System.Action<SaveLoadSystem.DeckRecipe> onAction;
    private DeckImportExportManager menuManager;

    public SaveLoadSystem.DeckRecipe MyData => myData;

    public void Setup(SaveLoadSystem.DeckRecipe data, System.Action<SaveLoadSystem.DeckRecipe> selectCallback, bool isSelected)
    {
        myData = data;
        menuManager = GetComponentInParent<DeckImportExportManager>();
        onAction = selectCallback;

        // Auto-configuração
        if (nameText == null) nameText = transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (dateText == null) dateText = transform.Find("Date")?.GetComponent<TextMeshProUGUI>();
        if (infoText == null) infoText = transform.Find("Info")?.GetComponent<TextMeshProUGUI>();

        if (data == null)
        {
            if (nameText) nameText.text = "--- ERRO ---";
            if (dateText) dateText.text = "";
            if (actionButton) actionButton.interactable = false;
            return;
        }

        if (nameText) nameText.text = data.deckName;
        
        // Reutiliza o campo de data para mostrar a contagem
        if (dateText)
        {
            int main = data.mainDeckCardIDs?.Count ?? 0;
            int extra = data.extraDeckCardIDs?.Count ?? 0;
            int side = data.sideDeckCardIDs?.Count ?? 0;
            dateText.text = $"M:{main} E:{extra} S:{side}";
        }

        SetSelected(isSelected);

        if (actionButton == null) actionButton = GetComponent<Button>();
        if (actionButton)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => onAction?.Invoke(myData));
        }
    }

    public void SetupForNewDeck(System.Action newDeckCallback)
    {
        myData = null;
        menuManager = GetComponentInParent<DeckImportExportManager>();

        if (nameText == null) nameText = transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (dateText == null) dateText = transform.Find("Date")?.GetComponent<TextMeshProUGUI>();

        if (nameText) nameText.text = "[ Create New Deck ]";
        if (dateText) dateText.text = "";

        SetSelected(false);

        if (actionButton == null) actionButton = GetComponent<Button>();
        if (actionButton)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => newDeckCallback?.Invoke());
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (selectionHighlight == null) selectionHighlight = GetComponent<Image>();
        if (selectionHighlight != null && menuManager != null)
        {
            selectionHighlight.color = isSelected ? menuManager.selectedColor : menuManager.defaultColor;
        }
    }
}