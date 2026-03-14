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
    public TextMeshProUGUI panelTitle; // Novo: Título do painel (Import/Export)
    public Button btnAction; // Botão principal (Import ou Export)
    public Button btnBack;
    public TMP_InputField inputDeckName; // Apenas para o painel de Export
    public Transform listContent;
    public GameObject deckSlotPrefab; // Prefab ImportExportItem

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
        
        // A visibilidade dos painéis agora é controlada pelo UIManager.
        // Este script apenas configura o painel em que está.

        TextMeshProUGUI actionButtonText = btnAction?.GetComponentInChildren<TextMeshProUGUI>();
        if (actionButtonText != null)
        {
            actionButtonText.text = menuType.ToString();
        }
        
        if (panelTitle != null)
        {
            panelTitle.text = (menuType == MenuType.Import) ? "Import Deck" : "Export Deck";
        }

        if (inputDeckName != null)
        {
            // O campo de input só deve existir no painel de Export
            inputDeckName.gameObject.SetActive(menuType == MenuType.Export);
            inputDeckName.text = "";
        }

        RefreshList();
    }

    private void ClosePanel()
    {
        // Pede ao UIManager para voltar para a tela anterior (DeckBuilder)
        UIManager.Instance?.ShowScreen(UIManager.Instance.deckBuilderScreen);
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
                UIManager.Instance?.ShowMessage($"Deck '{deckName}' exportado com sucesso!");
            }
            else
            {
                // Plano B: Informar erro se o nome estiver vazio
                UIManager.Instance?.ShowMessage("Por favor, digite um nome para o deck antes de exportar.");
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
            
            // Usa o SaveSlotUI para configurar o prefab
            SaveSlotUI slotUI = slotGO.GetComponent<SaveSlotUI>();
            if (slotUI != null)
            {
                // Criamos um GameSaveData "falso" para reutilizar a lógica do SaveSlotUI
                var fakeSaveData = new SaveLoadSystem.GameSaveData
                {
                    playerName = deckRecipe.deckName,
                    lastPlayedTime = "" // Não precisamos de data aqui
                };
                slotUI.Setup(fakeSaveData, (data) => OnSlotClicked(data.playerName, slotGO), false);
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
            SaveSlotUI slotUI = slot.GetComponent<SaveSlotUI>();
            if (slotUI != null && slotUI.MyData != null)
            {
                bool isSelected = slotUI.MyData.playerName == selectedDeckName;
                slotUI.SetSelected(isSelected);
            }
        }
    }
}
