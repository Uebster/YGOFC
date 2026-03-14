using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class DeckImportExportManager : MonoBehaviour
{
    public enum MenuType { Import, Export }

    [Header("Configuração")]
    public MenuType currentMenuType;
    public Color selectedColor = Color.yellow;
    public Color defaultColor = Color.white;

    [Header("Referências de UI")]
    public GameObject importPanel;
    public GameObject exportPanel;
    public Button btnAction; // Botão principal (Import ou Export)
    public Button btnBack;
    public TMP_InputField inputDeckName; // Apenas para o painel de Export
    public Transform listContent;
    public GameObject deckSlotPrefab; // Prefab para exibir um deck salvo

    private string selectedDeckName;
    private List<GameObject> spawnedSlots = new List<GameObject>();

    void Awake()
    {
        btnBack?.onClick.AddListener(ClosePanel);
        btnAction?.onClick.AddListener(PerformAction);
    }

    public void Setup(MenuType menuType)
    {
        currentMenuType = menuType;
        gameObject.SetActive(true);

        if (importPanel != null) importPanel.SetActive(menuType == MenuType.Import);
        if (exportPanel != null) exportPanel.SetActive(menuType == MenuType.Export);

        TextMeshProUGUI actionButtonText = btnAction?.GetComponentInChildren<TextMeshProUGUI>();
        if (actionButtonText != null)
        {
            actionButtonText.text = menuType.ToString();
        }

        if (inputDeckName != null)
        {
            inputDeckName.text = "";
        }

        RefreshList();
    }

    private void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    private void PerformAction()
    {
        if (currentMenuType == MenuType.Export)
        {
            string deckName = inputDeckName?.text.Trim();
            if (!string.IsNullOrEmpty(deckName))
            {
                DeckBuilderManager.Instance.ExportCurrentDeck(deckName);
                RefreshList(); // Atualiza a lista para mostrar o novo deck salvo
                inputDeckName.text = "";
            }
        }
        else // Import
        {
            if (!string.IsNullOrEmpty(selectedDeckName))
            {
                DeckBuilderManager.Instance.ImportDeck(selectedDeckName);
                ClosePanel();
            }
        }
    }

    private void RefreshList()
    {
        // Limpa a lista antiga
        foreach (GameObject slot in spawnedSlots)
        {
            Destroy(slot);
        }
        spawnedSlots.Clear();
        selectedDeckName = null;

        if (SaveLoadSystem.Instance == null || deckSlotPrefab == null) return;

        List<SaveLoadSystem.DeckRecipe> savedDecks = SaveLoadSystem.Instance.GetSavedDecks();

        if (savedDecks == null) return;

        foreach (SaveLoadSystem.DeckRecipe deckRecipe in savedDecks.OrderBy(d => d.deckName))
        {
            GameObject slotGO = Instantiate(deckSlotPrefab, listContent);
            spawnedSlots.Add(slotGO);

            // Assumindo que o prefab tem um TextMeshProUGUI e um Button
            TextMeshProUGUI nameText = slotGO.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = deckRecipe.deckName;
            }

            Button slotButton = slotGO.GetComponent<Button>();
            if (slotButton != null)
            {
                string deckName = deckRecipe.deckName; // Captura para o lambda
                slotButton.onClick.AddListener(() => OnSlotClicked(deckName, slotGO));
            }
        }
        UpdateSelectionVisuals();
    }

    private void OnSlotClicked(string deckName, GameObject clickedSlot)
    {
        selectedDeckName = deckName;
        if (currentMenuType == MenuType.Export && inputDeckName != null)
        {
            inputDeckName.text = deckName; // Preenche o nome para sobrescrever
        }
        UpdateSelectionVisuals();
    }
    
    private void OnDeleteClicked(string deckName)
    {
        // Lógica para deletar um deck
        SaveLoadSystem.Instance.DeleteDeckRecipe(deckName);
        SaveLoadSystem.Instance.SaveGame(GameManager.Instance.currentSaveID);
        RefreshList();
    }

    private void UpdateSelectionVisuals()
    {
        foreach (GameObject slot in spawnedSlots)
        {
            Image bgImage = slot.GetComponent<Image>();
            TextMeshProUGUI nameText = slot.GetComponentInChildren<TextMeshProUGUI>();
            if (bgImage != null && nameText != null)
            {
                bool isSelected = nameText.text == selectedDeckName;
                bgImage.color = isSelected ? selectedColor : defaultColor;
            }
        }
    }
}
