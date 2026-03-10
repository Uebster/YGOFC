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
    public Button newSaveButton; // Botão "Novo Save" (Apenas para tela de Save)
    public Button mainActionButton; // Botão de Ação (Save/Load/Delete)
    public TextMeshProUGUI mainActionText; // Texto do botão de ação

    [Header("Confirmação")]
    public GameObject confirmationPopup;
    public TextMeshProUGUI confirmationText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    private System.Action pendingAction;
    private SaveLoadSystem.GameSaveData selectedSave;
    private List<SaveSlotUI> instantiatedSlots = new List<SaveSlotUI>();

    void Awake()
    {
        // --- AUTO-CONFIGURAÇÃO BASEADA NA HIERARQUIA ---
        
        // 1. Tenta achar o container da lista se não estiver atribuído
        if (listContent == null) listContent = transform.Find("Scroll View/Viewport/Content");

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
            }
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
                if (confirmationText == null) confirmationText = popupTr.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
                
                Transform yesTr = popupTr.Find("Btn_Yes");
                if (yesTr != null) confirmYesButton = yesTr.GetComponent<Button>();
                
                Transform noTr = popupTr.Find("Btn_No");
                if (noTr != null) confirmNoButton = noTr.GetComponent<Button>();
            }
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
        selectedSave = null;
        RefreshList();
        
        // Configura botão de Novo Save
        if (newSaveButton != null)
        {
            newSaveButton.gameObject.SetActive(menuType == MenuType.Save);
            newSaveButton.onClick.RemoveAllListeners();
            newSaveButton.onClick.AddListener(OnNewSaveClicked);
        }
        
        UpdateUI();
        CloseConfirmation();
    }

    public void RefreshList()
    {
        if (SaveLoadSystem.Instance == null) return;

        // Limpa lista antiga
        foreach (Transform child in listContent) Destroy(child.gameObject);
        instantiatedSlots.Clear();

        // Busca saves do disco
        List<SaveLoadSystem.GameSaveData> saves = SaveLoadSystem.Instance.GetAllSaves();

        // Cria slots
        foreach (var save in saves)
        {
            GameObject go = Instantiate(slotPrefab, listContent);
            SaveSlotUI slot = go.GetComponent<SaveSlotUI>();
            if (slot != null)
            {
                bool isSelected = (selectedSave != null && selectedSave.saveID == save.saveID);
                slot.Setup(save, OnSlotClicked, isSelected);
                instantiatedSlots.Add(slot);
            }
        }
    }

    void OnSlotClicked(SaveLoadSystem.GameSaveData data)
    {
        selectedSave = data;
        
        // Atualiza visualmente apenas o destaque, sem recriar a lista (evita flicker/perda de scroll)
        foreach (var slot in instantiatedSlots)
        {
            if (slot != null) slot.SetSelected(selectedSave != null && selectedSave.saveID == slot.MyData.saveID);
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        if (mainActionButton != null)
        {
            mainActionButton.interactable = (selectedSave != null);
            
            if (mainActionText != null)
            {
                if (menuType == MenuType.Save)
                    mainActionText.text = (selectedSave != null) ? "Overwrite" : "Select a Slot";
                else if (menuType == MenuType.Load)
                    mainActionText.text = "Load Game";
                else if (menuType == MenuType.Delete)
                    mainActionText.text = "Delete";
            }
        }
    }

    void OnMainActionClicked()
    {
        if (selectedSave == null) return;

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
        if (GameManager.Instance != null)
        {
            ShowConfirmation("Criar um novo arquivo de Save?", () =>
            {
                // Gera ID único baseado no nome e hora
                string newID = $"{GameManager.Instance.playerName}_{System.DateTime.Now:yyyyMMdd_HHmmss}";
                SaveLoadSystem.Instance.SaveGame(newID);
                RefreshList();
            });
        }
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