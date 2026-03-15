using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class DeckImportExportManager : MonoBehaviour
{
    public enum MenuType { Import, Export }

    [Header("Configuração")]
    public Color selectedColor = Color.yellow;
    public Color defaultColor = Color.white;

    public MenuType currentMenuType;

    [Header("Referências de UI")]
    public GameObject importPanel;
    public GameObject exportPanel;
    public TextMeshProUGUI panelTitle;
    public Button btnAction; // Botão principal (Import ou Export)
    public Button btnBack;
    public Button btnDelete; // Botão para deletar
    public GameObject confirmationDialog;
    public GameObject warningDialog;
    public TMP_InputField inputDeckName; // Apenas para o painel de Export

    public Transform listContent;
    public GameObject deckSlotPrefab; // Prefab ImportExportItem

    private List<GameObject> spawnedSlots = new List<GameObject>();
    private bool isCreatingNewDeck = false;
    private SaveLoadSystem.DeckRecipe selectedDeck;
    
    // Ação pendente para o diálogo de confirmação local
    private System.Action pendingLocalConfirmAction;

    void Awake()
    {
        btnBack?.onClick.AddListener(ClosePanel);
        btnAction?.onClick.AddListener(PerformAction);
        btnDelete?.onClick.AddListener(OnDeleteClicked);
        inputDeckName?.onValueChanged.AddListener(delegate { UpdateUIState(); });

        // Configuração do Diálogo de Confirmação Local (se atribuído)
        if (confirmationDialog != null)
        {
            // Tenta encontrar os botões dentro do painel de confirmação
            Button localYes = confirmationDialog.transform.Find("Btn_Yes")?.GetComponent<Button>();
            Button localNo = confirmationDialog.transform.Find("Btn_No")?.GetComponent<Button>();

            // Se não achar por nome exato, tenta pegar componentes nos filhos (ordem: Sim, Não)
            if (localYes == null || localNo == null)
            {
                Button[] buttons = confirmationDialog.GetComponentsInChildren<Button>(true);
                if (buttons.Length >= 1) localYes = buttons[0];
                if (buttons.Length >= 2) localNo = buttons[1];
            }

            if (localYes != null)
            {
                localYes.onClick.RemoveAllListeners();
                localYes.onClick.AddListener(OnLocalConfirmYes);
            }
            else Debug.LogWarning("[DeckImportExportManager] Botão 'Sim' não encontrado no Confirmation Dialog local.");

            if (localNo != null)
            {
                localNo.onClick.RemoveAllListeners();
                localNo.onClick.AddListener(OnLocalConfirmNo);
            }
            else Debug.LogWarning("[DeckImportExportManager] Botão 'Não' não encontrado no Confirmation Dialog local.");

            confirmationDialog.SetActive(false);
        }

        // Configuração do Diálogo de Aviso Local (se atribuído)
        if (warningDialog != null)
        {
            Button localOk = warningDialog.transform.Find("Btn_Ok")?.GetComponent<Button>() ?? warningDialog.GetComponentInChildren<Button>(true);
            if (localOk != null)
            {
                localOk.onClick.AddListener(() => warningDialog.SetActive(false));
            }
            warningDialog.SetActive(false);
        }
    }

    public void Setup(MenuType menuType)
    {
        Debug.Log($"[DeckImportExportManager] Setup: Configurando painel para o modo {menuType}.");
        currentMenuType = menuType;
        isCreatingNewDeck = false;
        selectedDeck = null;
        
        gameObject.SetActive(true);
        
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
            inputDeckName.gameObject.SetActive(menuType == MenuType.Export);
            inputDeckName.text = "";
            inputDeckName.interactable = true;
        }

        if (btnDelete != null)
        {
            // Botão delete só deve aparecer no Export ou se quisermos permitir deletar no Import também
            btnDelete.gameObject.SetActive(true); 
        }

        RefreshList();
    }

    private void ClosePanel()
    {
        UIManager.Instance?.ShowScreen(UIManager.Instance.deckBuilderScreen);
    }

    private void PerformAction()
    {
        if (currentMenuType == MenuType.Export)
        {
            string nameToSave = inputDeckName != null ? inputDeckName.text.Trim() : "";

            if (string.IsNullOrEmpty(nameToSave))
            {
                ShowWarning("Por favor, insira um nome para o deck.");
                return;
            }

            System.Action exportAction = () => {
                DeckBuilderManager.Instance.ExportCurrentDeck(nameToSave);
                RefreshList();
                ShowWarning($"Deck '{nameToSave}' salvo com sucesso!");
                
                // Após salvar, seleciona o deck na lista
                var savedDeck = SaveLoadSystem.Instance.GetSavedDecks().FirstOrDefault(d => d.deckName.Equals(nameToSave, System.StringComparison.OrdinalIgnoreCase));
                if(savedDeck != null) {
                    SelectDeckRecipe(savedDeck);
                }
            };

            bool isOverwriting = SaveLoadSystem.Instance.GetSavedDecks().Any(d => d.deckName.Equals(nameToSave, System.StringComparison.OrdinalIgnoreCase));

            if (isOverwriting)
            {
                ShowConfirmation($"O deck '{nameToSave}' já existe. Deseja sobrescrevê-lo?", exportAction);
            }
            else
            {
                ShowConfirmation($"Deseja salvar o novo deck '{nameToSave}'?", exportAction);
            }
        }
        else // Import
        {
            if (selectedDeck != null)
            {
                ShowConfirmation($"Deseja importar o deck '{selectedDeck.deckName}'? O deck em edição será substituído.", () => {
                    Debug.Log($"Importing deck: {selectedDeck.deckName}");
                    DeckBuilderManager.Instance.ImportDeck(selectedDeck.deckName);
                    ClosePanel();
                });
            }
        }
    }

    private void RefreshList()
    {
        Debug.Log("[DeckImportExportManager] RefreshList: Atualizando a lista de decks salvos.");
        // Limpa a lista antiga
        foreach (GameObject slot in spawnedSlots)
        {
            Destroy(slot);
        }
        spawnedSlots.Clear();

        if (SaveLoadSystem.Instance == null || deckSlotPrefab == null)
        {
            Debug.LogError("[DeckImportExportManager] RefreshList: SaveLoadSystem ou deckSlotPrefab é nulo. Abortando.");
            return;
        }

        // --- Adiciona opção "Create New Deck" se for Export ---
        if (currentMenuType == MenuType.Export)
        {
            GameObject newSlotGO = Instantiate(deckSlotPrefab, listContent);
            spawnedSlots.Add(newSlotGO);
            DeckSlotUI newSlotUI = newSlotGO.GetComponent<DeckSlotUI>();
            if (newSlotUI != null)
            {
                newSlotUI.SetupForNewDeck(OnNewDeckClicked);
                newSlotUI.SetSelected(isCreatingNewDeck);
            }
        }

        // --- Lista Decks Salvos ---
        List<SaveLoadSystem.DeckRecipe> savedDecks = SaveLoadSystem.Instance.GetSavedDecks();
        Debug.Log($"[DeckImportExportManager] RefreshList: Encontrados {savedDecks?.Count ?? 0} decks salvos.");
        if (savedDecks == null) return;

        foreach (SaveLoadSystem.DeckRecipe deckRecipe in savedDecks.OrderBy(d => d.deckName))
        {
            GameObject slotGO = Instantiate(deckSlotPrefab, listContent);
            spawnedSlots.Add(slotGO);
            
            DeckSlotUI slotUI = slotGO.GetComponent<DeckSlotUI>();
            if (slotUI != null)
            {
                slotUI.Setup(deckRecipe, SelectDeckRecipe);
            }
        }
        
        UpdateUIState();
    }

    private void OnNewDeckClicked()
    {
        isCreatingNewDeck = true;
        selectedDeck = null;
        if (inputDeckName != null)
        {
            inputDeckName.text = "";
            inputDeckName.interactable = true;
            inputDeckName.Select(); // Foca no campo de texto
        }
        UpdateUIState();
    }

    public void SelectDeckRecipe(SaveLoadSystem.DeckRecipe deck)
    {
        isCreatingNewDeck = false;
        selectedDeck = deck;
        
        if (currentMenuType == MenuType.Export && inputDeckName != null)
        {
            inputDeckName.text = deck.deckName; // Preenche o nome para facilitar sobrescrever
        }
        UpdateUIState();
    }

    private void UpdateUIState()
    {
        // Atualiza visual dos slots
        foreach (var slotGO in spawnedSlots)
        {
            var slotUI = slotGO.GetComponent<DeckSlotUI>();
            if (slotUI != null)
            {
                // Se for o slot "New Deck" (MyData é null), destaca se isCreatingNewDeck for true
                // Se for um slot normal, destaca se selectedDeck bater
                bool isSelected = (slotUI.MyData == null && isCreatingNewDeck) || 
                                  (slotUI.MyData != null && slotUI.MyData == selectedDeck);
                slotUI.SetSelected(isSelected);
            }
        }

        // Habilita botões
        bool hasSelection = (selectedDeck != null);
        bool hasName = inputDeckName != null && !string.IsNullOrEmpty(inputDeckName.text);

        if (currentMenuType == MenuType.Export)
        {
            // Pode exportar se estiver criando novo (e tiver nome) OU se selecionou um existente (sobrescrever)
            btnAction.interactable = (isCreatingNewDeck && hasName) || (hasSelection) || (hasName); 
            // Opcionalmente, pode forçar que tenha nome sempre
        }
        else // Import
        {
            btnAction.interactable = hasSelection;
        }

        if (btnDelete != null)
        {
            btnDelete.interactable = hasSelection;
        }
    }

    public void OnDeleteClicked()
    {
        Debug.Log("[DeckImportExportManager] OnDeleteClicked: Botão de deletar pressionado.");
        if (selectedDeck == null)
        {
            Debug.Log("[DeckImportExportManager] Nenhum deck selecionado para deletar.");
            return;
        }

        ShowConfirmation($"Tem certeza que deseja deletar o deck '{selectedDeck.deckName}'?", () => 
        {
            if (DeckBuilderManager.Instance != null)
            {
                DeckBuilderManager.Instance.DeleteDeckRecipe(selectedDeck.deckName);
                // Limpa seleção após deletar
                selectedDeck = null;
                if (inputDeckName != null) inputDeckName.text = "";
                RefreshList();
            }
        });
    }

    // Wrapper para decidir se usa Confirmação Local ou Global
    private void ShowConfirmation(string message, System.Action onConfirm)
    {
        if (confirmationDialog != null)
        {
            Debug.Log("[DeckImportExportManager] Usando diálogo de confirmação LOCAL.");
            pendingLocalConfirmAction = onConfirm;
            
            TextMeshProUGUI txt = confirmationDialog.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = message;
            
            confirmationDialog.SetActive(true);
        }
        else if (UIManager.Instance != null)
        {
            Debug.Log("[DeckImportExportManager] Usando diálogo de confirmação GLOBAL (UIManager).");
            UIManager.Instance.ShowConfirmation(message, onConfirm);
        }
        else
        {
            Debug.LogWarning("[DeckImportExportManager] Nenhum diálogo de confirmação encontrado. Executando ação direta.");
            onConfirm?.Invoke();
        }
    }

    private void ShowWarning(string message)
    {
        if (warningDialog != null)
        {
            TextMeshProUGUI txt = warningDialog.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = message;
            warningDialog.SetActive(true);
        }
        else if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMessage(message);
        }
    }

    private void OnLocalConfirmYes()
    {
        if (confirmationDialog != null) confirmationDialog.SetActive(false);
        pendingLocalConfirmAction?.Invoke();
        pendingLocalConfirmAction = null;
    }

    private void OnLocalConfirmNo()
    {
        if (confirmationDialog != null) confirmationDialog.SetActive(false);
        pendingLocalConfirmAction = null;
    }
}
