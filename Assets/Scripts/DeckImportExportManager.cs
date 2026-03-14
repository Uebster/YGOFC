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
    public Button btnDelete; // Botão para deletar
    public TMP_InputField inputDeckName; // Apenas para o painel de Export
    public Transform listContent;
    public GameObject deckSlotPrefab; // Prefab ImportExportItem

    private string selectedDeckName;
    private List<GameObject> spawnedSlots = new List<GameObject>();
    private bool isCreatingNewDeck = false;

    void Awake()
    {
        btnBack?.onClick.AddListener(ClosePanel);
        btnAction?.onClick.AddListener(PerformAction);
        btnDelete?.onClick.AddListener(OnDeleteClicked);
    }

    public void Setup(MenuType menuType)
    {
        currentMenuType = menuType;
        gameObject.SetActive(true);
        isCreatingNewDeck = false; // Reseta o estado ao abrir
        
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
            // Se não estiver criando um novo e nenhum deck estiver selecionado para sobrescrever, não faz nada.
            if (!isCreatingNewDeck && string.IsNullOrEmpty(selectedDeckName))
            {
                UIManager.Instance?.ShowMessage("Selecione um deck para sobrescrever ou crie um novo.");
                return;
            }

            string deckName = inputDeckName?.text.Trim();
            if (!string.IsNullOrEmpty(deckName))
            {
                // Confirmação para sobrescrever
                UIManager.Instance?.ShowConfirmation($"Salvar deck como '{deckName}'?", () => {
                    DeckBuilderManager.Instance.ExportCurrentDeck(deckName);
                    RefreshList();
                    UIManager.Instance?.ShowMessage($"Deck '{deckName}' salvo com sucesso!");
                });
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

        // Adiciona o slot "New Deck" se estiver na tela de Export
        if (currentMenuType == MenuType.Export)
        {
            GameObject newSlotGo = Instantiate(deckSlotPrefab, listContent);
            spawnedSlots.Add(newSlotGo);
            DeckSlotUI newSlot = newSlotGo.GetComponent<DeckSlotUI>();
            if (newSlot != null)
            {
                newSlot.SetupForNewDeck(OnNewDeckClicked);
            }
        }

        List<SaveLoadSystem.DeckRecipe> savedDecks = SaveLoadSystem.Instance.GetSavedDecks();

        if (savedDecks == null) return;

        foreach (SaveLoadSystem.DeckRecipe deckRecipe in savedDecks.OrderBy(d => d.deckName))
        {
            GameObject slotGO = Instantiate(deckSlotPrefab, listContent);
            spawnedSlots.Add(slotGO);
            
            // Usa o DeckSlotUI para configurar o prefab
            DeckSlotUI slotUI = slotGO.GetComponent<DeckSlotUI>();
            if (slotUI != null)
            {
                slotUI.Setup(deckRecipe, OnSlotClicked, (selectedDeckName == deckRecipe.deckName));
            }
        }
        UpdateUIState();
    }

    private void OnSlotClicked(SaveLoadSystem.DeckRecipe recipe)
    {
        if (recipe == null) return;
        isCreatingNewDeck = false;
        selectedDeckName = recipe.deckName;
        if (currentMenuType == MenuType.Export && inputDeckName != null)
        {
            inputDeckName.text = recipe.deckName; // Preenche o nome para sobrescrever
        }
        UpdateUIState();
    }

    private void OnNewDeckClicked()
    {
        isCreatingNewDeck = true;
        selectedDeckName = null;
        if (inputDeckName != null) inputDeckName.text = "";
        UpdateUIState();
    }

    private void UpdateUIState()
    {
        // Atualiza o destaque visual
        foreach (var slotGO in spawnedSlots)
        {
            var slotUI = slotGO.GetComponent<DeckSlotUI>();
            if (slotUI != null)
            {
                bool isSelected = (slotUI.MyData != null && slotUI.MyData.deckName == selectedDeckName) || (slotUI.MyData == null && isCreatingNewDeck);
                slotUI.SetSelected(isSelected);
            }
        }

        // Habilita/Desabilita input e botão de ação
        bool canInteract = isCreatingNewDeck || !string.IsNullOrEmpty(selectedDeckName);
        bool existingDeckSelected = !isCreatingNewDeck && !string.IsNullOrEmpty(selectedDeckName);

        if (inputDeckName != null) inputDeckName.interactable = isCreatingNewDeck;
        if (btnAction != null) btnAction.interactable = canInteract;

        if (btnDelete != null)
        {
            // O botão de deletar só deve aparecer e funcionar na tela de Export
            btnDelete.gameObject.SetActive(currentMenuType == MenuType.Export);
            btnDelete.interactable = existingDeckSelected;
        }
    }

    private void OnDeleteClicked()
    {
        if (string.IsNullOrEmpty(selectedDeckName) || isCreatingNewDeck) return;

        UIManager.Instance?.ShowConfirmation($"Tem certeza que deseja deletar o deck '{selectedDeckName}'?", () => {
            SaveLoadSystem.Instance.DeleteDeckRecipe(selectedDeckName);
            SaveLoadSystem.Instance.SaveGame(GameManager.Instance.currentSaveID); // Persiste a deleção
            RefreshList();
            UIManager.Instance?.ShowMessage($"Deck '{selectedDeckName}' deletado.");
        });
    }
}