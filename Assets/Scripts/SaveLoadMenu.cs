using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class SaveLoadMenu : MonoBehaviour
{
    public enum MenuType { Save, Load, Delete }

    [Header("Configuração")]
    [Tooltip("Escolha o comportamento deste menu.")]
    public MenuType menuType = MenuType.Load;
    
    [Header("Referências UI")]
    public Transform listContent; // Onde os slots serão criados
    public GameObject slotPrefab; // O prefab do SaveSlotUI
    [Tooltip("Prefab opcional para o slot 'New Save'. Se vazio, usa o slotPrefab normal.")]
    public GameObject newSaveSlotPrefab; // Prefab para o slot "Novo Save"
    public Button mainActionButton; // Botão de Ação (Save/Load/Delete)
    public TextMeshProUGUI mainActionText; // Texto do botão de ação

    [Header("Confirmação")]
    public GameObject confirmationPopup;
    public TextMeshProUGUI confirmationText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    private System.Action pendingAction;
    private SaveLoadSystem.GameSaveData selectedSave;
    private bool isCreatingNewSave = false;
    private List<SaveSlotUI> instantiatedSlots = new List<SaveSlotUI>();

    void Awake()
    {
        // --- AUTO-CONFIGURAÇÃO BASEADA NA HIERARQUIA ---
        Debug.Log($"[SaveLoadMenu - {menuType}] Awake: Iniciando auto-configuração.");

        // 1. Tenta achar o container da lista se não estiver atribuído
        if (listContent == null)
        {
            listContent = transform.Find("Scroll View/Viewport/Content");
            if (listContent) Debug.Log("[SaveLoadMenu] 'listContent' encontrado automaticamente.");
        }

        // 2. Tenta achar o botão principal (Save, Load ou Delete)
        if (mainActionButton == null)
        {
            Transform btnTr = transform.Find("Btn_SaveGame");
            if (btnTr == null) btnTr = transform.Find("Btn_LoadGame");
            if (btnTr == null) btnTr = transform.Find("Btn_DeleteGame");

            if (btnTr != null)
            {
                mainActionButton = btnTr.GetComponent<Button>();
                if (mainActionText == null) mainActionText = btnTr.GetComponentInChildren<TextMeshProUGUI>();
                Debug.Log($"[SaveLoadMenu] Botão de ação principal '{btnTr.name}' encontrado.");
            }
            else Debug.LogWarning("[SaveLoadMenu] Nenhum botão de ação principal (Btn_SaveGame, Btn_LoadGame, Btn_DeleteGame) foi encontrado.");
        }

        // 3. Tenta achar o Popup de Confirmação correto
        if (confirmationPopup == null)
        {
            Transform popupTr = transform.Find("ConfirmationSave");
            if (popupTr == null) popupTr = transform.Find("ConfirmationLoad");
            if (popupTr == null) popupTr = transform.Find("ConfirmationDelete");

            if (popupTr != null)
            {
                confirmationPopup = popupTr.gameObject;
                Debug.Log($"[SaveLoadMenu] Popup de confirmação '{popupTr.name}' encontrado.");
                if (confirmationText == null) confirmationText = popupTr.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
                
                Transform yesTr = popupTr.Find("Btn_Yes");
                if (yesTr != null) confirmYesButton = yesTr.GetComponent<Button>();
                
                Transform noTr = popupTr.Find("Btn_No");
                if (noTr != null) confirmNoButton = noTr.GetComponent<Button>();
            }
            else Debug.LogWarning("[SaveLoadMenu] Nenhum painel de confirmação (ConfirmationSave, etc.) foi encontrado.");
        }
    }

    void Start()
    {
        if (confirmationPopup != null) confirmationPopup.SetActive(false);
        if (confirmYesButton != null) confirmYesButton.onClick.AddListener(OnConfirmYes);
        if (confirmNoButton != null) confirmNoButton.onClick.AddListener(OnConfirmNo);
        if (mainActionButton != null) mainActionButton.onClick.AddListener(OnMainActionClicked);
    }

    void OnEnable()
    {
        Debug.Log($"[SaveLoadMenu] OnEnable: Menu ativado como '{menuType}'.");
        selectedSave = null;
        isCreatingNewSave = false;
        RefreshList();
        UpdateUI();
        CloseConfirmation();
    }

    public void RefreshList()
    {
        if (SaveLoadSystem.Instance == null)
        {
            Debug.LogError("[SaveLoadMenu] SaveLoadSystem.Instance é nulo. A lista não pode ser carregada.");
            return;
        }

        // Limpa lista antiga
        foreach (Transform child in listContent) Destroy(child.gameObject);
        instantiatedSlots.Clear();

        // Adiciona o slot "New Save" se estiver na tela de Save
        if (menuType == MenuType.Save)
        {
            GameObject prefabToUse = newSaveSlotPrefab != null ? newSaveSlotPrefab : slotPrefab;
            GameObject newSlotGo = Instantiate(prefabToUse, listContent);
            SaveSlotUI newSlot = newSlotGo.GetComponent<SaveSlotUI>();
            if (newSlot != null)
            {
                newSlot.SetupForNewSave(OnNewSaveClicked);
                instantiatedSlots.Add(newSlot); // Adiciona à lista para controle de seleção
            }
        }

        // Busca saves do disco
        List<SaveLoadSystem.GameSaveData> saves = SaveLoadSystem.Instance.GetAllSaves();
        Debug.Log($"[SaveLoadMenu] {saves.Count} saves encontrados no sistema.");

        // Cria slots
        foreach (var save in saves)
        {
            GameObject go = Instantiate(slotPrefab, listContent);
            SaveSlotUI slot = go.GetComponent<SaveSlotUI>();
            if (slot != null)
            {
                // Se um save já está selecionado, mantém a seleção visual
                bool isSelected = (selectedSave != null && selectedSave.saveID == save.saveID);
                slot.Setup(save, OnSlotClicked, isSelected);
                instantiatedSlots.Add(slot);
            }
        }
        Debug.Log($"[SaveLoadMenu] Lista atualizada com {instantiatedSlots.Count} slots visuais.");
    }

    void OnSlotClicked(SaveLoadSystem.GameSaveData data)
    {
        // Este método só deve ser chamado por slots de save existentes.
        if (data == null) {
            Debug.LogError("[SaveLoadMenu] OnSlotClicked foi chamado com dados nulos. Isso não deveria acontecer.");
            return;
        }
        Debug.Log($"[SaveLoadMenu] Slot clicado: {data.playerName} (ID: {data.saveID})");
        isCreatingNewSave = false;
        selectedSave = data;
        
        // Atualiza visualmente apenas o destaque, sem recriar a lista (evita flicker/perda de scroll)
        foreach (var slot in instantiatedSlots)
        {
            // Garante que não vai quebrar se o slot for o de "New Save" (MyData é null)
            bool isThisSlotSelected = slot.MyData != null && selectedSave != null && slot.MyData.saveID == selectedSave.saveID;
            if (slot != null) slot.SetSelected(isThisSlotSelected);
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        Debug.Log($"[SaveLoadMenu] UpdateUI chamado. Save selecionado: {(selectedSave != null ? selectedSave.playerName : "Nenhum")}, isCreatingNew: {isCreatingNewSave}");
        if (mainActionButton != null)
        {
            mainActionButton.interactable = (selectedSave != null) || (isCreatingNewSave && menuType == MenuType.Save);
            
            if (mainActionText != null)
            {
                if (menuType == MenuType.Save)
                {
                    if (isCreatingNewSave)
                        mainActionText.text = "Save New";
                    else
                        mainActionText.text = (selectedSave != null) ? "Overwrite" : "Save";
                }
                else if (menuType == MenuType.Load)
                    mainActionText.text = "Load Game";
                else if (menuType == MenuType.Delete)
                    mainActionText.text = "Delete";
            }
        }
    }

    void OnMainActionClicked()
    {
        if (isCreatingNewSave && menuType == MenuType.Save)
        {
            if (GameManager.Instance != null)
            {
                ShowConfirmation("Criar um novo arquivo de Save?", () =>
                {
                    var newSave = SaveLoadSystem.Instance.SaveGame(null); // Pega o novo save retornado
                    if (newSave != null)
                    {
                        isCreatingNewSave = false;
                        selectedSave = newSave; // Seleciona o novo save
                        RefreshList(); // Recarrega a lista inteira para garantir consistência
                    }
                    else
                    {
                        Debug.LogError("[SaveLoadMenu] Falha ao criar o novo save. O objeto retornado é nulo.");
                    }
                });
            }
            return;
        }

        if (selectedSave == null)
        {
            Debug.LogWarning("[SaveLoadMenu] Botão de ação principal clicado, mas nenhum save existente está selecionado.");
            return;
        }

        switch (menuType)
        {
            case MenuType.Save:
                ShowConfirmation($"Tem certeza que deseja sobrescrever o save de\n<color=yellow>{selectedSave.playerName}</color>?", () => 
                {
                    SaveLoadSystem.Instance.SaveGame(selectedSave.saveID);
                    RefreshList();
                });
                break;
            case MenuType.Load:
                // Carregar Save
                SaveLoadSystem.Instance.LoadGame(selectedSave.saveID);
                if (UIManager.Instance != null) UIManager.Instance.Btn_BackToNewGameMenu(); // Volta ao menu
                break;
            case MenuType.Delete:
                ShowConfirmation($"Tem certeza que deseja <color=red>DELETAR</color> o save de\n{selectedSave.playerName}?", () => 
                {
                    SaveLoadSystem.Instance.DeleteSave(selectedSave.saveID);
                    selectedSave = null;
                    RefreshList();
                    UpdateUI();
                });
                break;
        }
    }

    void OnNewSaveClicked()
    {
        Debug.Log("[SaveLoadMenu] Slot 'New Save' selecionado.");
        isCreatingNewSave = true;
        selectedSave = null;

        // Atualiza o destaque visual para o slot de "New Save"
        foreach (var slot in instantiatedSlots)
        {
            // O slot de "New Save" é o único com MyData == null
            if (slot != null) slot.SetSelected(slot.MyData == null);
        }

        UpdateUI();
    }

    void ShowConfirmation(string message, System.Action action)
    {
        pendingAction = action;
        if (confirmationText != null) confirmationText.text = message;
        if (confirmationPopup != null) confirmationPopup.SetActive(true);
    }

    void OnConfirmYes()
    {
        pendingAction?.Invoke();
        CloseConfirmation();
    }

    void OnConfirmNo()
    {
        CloseConfirmation();
    }

    void CloseConfirmation()
    {
        pendingAction = null;
        if (confirmationPopup != null) confirmationPopup.SetActive(false);
    }
}