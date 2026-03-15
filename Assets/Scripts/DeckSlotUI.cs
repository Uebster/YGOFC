using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

/// <summary>
/// Controla a exibição e interação de um único slot na lista de Importação/Exportação de Decks.
/// </summary>
public class DeckSlotUI : MonoBehaviour
{
    [Header("UI References (Assign in Prefab)")]
    [SerializeField] private TextMeshProUGUI nameText;
    [Tooltip("Usado para mostrar a contagem de cartas (M:X E:Y S:Z).")]
    [SerializeField] private TextMeshProUGUI cardCountText;
    [SerializeField] private Button actionButton;
    [SerializeField] private Image selectionHighlight;

    // Internal State
    private SaveLoadSystem.DeckRecipe myData;
    private DeckImportExportManager menuManager;

    /// <summary>
    /// Os dados da receita de deck associados a este slot.
    /// </summary>
    public SaveLoadSystem.DeckRecipe MyData => myData;

    void Awake()
    {
        // Auto-find references if not assigned in the Inspector.
        // This makes the prefab more robust to hierarchy changes.
        if (actionButton == null) actionButton = GetComponent<Button>();
        if (selectionHighlight == null) selectionHighlight = GetComponent<Image>();

        // Find text components by name if not assigned.
        if (nameText == null || cardCountText == null)
        {
            var allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            if (nameText == null) nameText = allTexts.FirstOrDefault(t => t.name == "Name");
            // For backward compatibility with prefabs where this field is still named "Date"
            if (cardCountText == null) cardCountText = allTexts.FirstOrDefault(t => t.name == "Date");
        }
        
        menuManager = GetComponentInParent<DeckImportExportManager>();
    }

    /// <summary>
    /// Configures the slot to display an existing deck recipe.
    /// </summary>
    /// <param name="data">The deck recipe data to display.</param>
    /// <param name="selectCallback">The action to perform when the slot is clicked.</param>
    public void Setup(SaveLoadSystem.DeckRecipe data, System.Action<SaveLoadSystem.DeckRecipe> selectCallback)
    {
        myData = data;

        if (data == null)
        {
            if (nameText) nameText.text = "--- ERRO ---";
            if (cardCountText) cardCountText.text = "";
            if (actionButton) actionButton.interactable = false;
            return;
        }

        if (nameText) nameText.text = data.deckName;
        
        // Display card counts for Main, Extra, and Side decks.
        if (cardCountText)
        {
            int main = data.mainDeckCardIDs?.Count ?? 0;
            int extra = data.extraDeckCardIDs?.Count ?? 0;
            int side = data.sideDeckCardIDs?.Count ?? 0;
            cardCountText.text = $"M:{main}  E:{extra}  S:{side}";
        }

        if (actionButton)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => selectCallback?.Invoke(myData));
        }
    }

    /// <summary>
    /// Configures the slot to be the "[ Create New Deck ]" button.
    /// </summary>
    /// <param name="newDeckCallback">The action to perform when this slot is clicked.</param>
    public void SetupForNewDeck(System.Action newDeckCallback)
    {
        myData = null;

        if (nameText) nameText.text = "[ Create New Deck ]";
        if (cardCountText) cardCountText.text = "Crie e salve uma nova receita de deck.";

        if (actionButton)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => newDeckCallback?.Invoke());
        }
    }

    /// <summary>
    /// Updates the visual state (color) of the slot based on whether it is selected.
    /// </summary>
    /// <param name="isSelected">True if the slot should be highlighted as selected.</param>
    public void SetSelected(bool isSelected)
    {
        if (selectionHighlight != null && menuManager != null)
        {
            selectionHighlight.color = isSelected ? menuManager.selectedColor : menuManager.defaultColor;
        }
        else if (selectionHighlight != null)
        {
            // Fallback if manager is not found, just use simple colors.
            selectionHighlight.color = isSelected ? Color.yellow : Color.white;
        }
    }
}