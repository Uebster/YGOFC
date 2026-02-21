using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class SaveLoadMenu : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("Marque TRUE se esta for a tela de SALVAR. Desmarque para CARREGAR.")]
    public bool isSaveMode = false; 
    
    [Header("Referências UI")]
    public Transform listContent; // Onde os slots serão criados
    public GameObject slotPrefab; // O prefab do SaveSlotUI
    public Button newSaveButton; // Botão "Novo Save" (Apenas para tela de Save)

    [Header("Confirmação")]
    public GameObject confirmationPopup;
    public TextMeshProUGUI confirmationText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    private System.Action pendingAction;

    void Start()
    {
        if (confirmationPopup != null) confirmationPopup.SetActive(false);
        if (confirmYesButton != null) confirmYesButton.onClick.AddListener(OnConfirmYes);
        if (confirmNoButton != null) confirmNoButton.onClick.AddListener(OnConfirmNo);
    }

    void OnEnable()
    {
        RefreshList();
        
        // Configura botão de Novo Save
        if (newSaveButton != null)
        {
            newSaveButton.gameObject.SetActive(isSaveMode);
            newSaveButton.onClick.RemoveAllListeners();
            newSaveButton.onClick.AddListener(OnNewSaveClicked);
        }
        
        CloseConfirmation();
    }

    public void RefreshList()
    {
        if (SaveLoadSystem.Instance == null) return;

        // Limpa lista antiga
        foreach (Transform child in listContent) Destroy(child.gameObject);

        // Busca saves do disco
        List<SaveLoadSystem.GameSaveData> saves = SaveLoadSystem.Instance.GetAllSaves();

        // Cria slots
        foreach (var save in saves)
        {
            GameObject go = Instantiate(slotPrefab, listContent);
            SaveSlotUI slot = go.GetComponent<SaveSlotUI>();
            if (slot != null)
            {
                slot.Setup(save, OnSlotClicked, OnDeleteClicked, isSaveMode);
            }
        }
    }

    void OnSlotClicked(SaveLoadSystem.GameSaveData data)
    {
        if (isSaveMode)
        {
            ShowConfirmation($"Tem certeza que deseja sobrescrever o save de\n<color=yellow>{data.playerName}</color>?", () => 
            {
                SaveLoadSystem.Instance.SaveGame(data.saveID);
                RefreshList();
            });
        }
        else
        {
            // Carregar Save
            SaveLoadSystem.Instance.LoadGame(data.saveID);
            if (UIManager.Instance != null) UIManager.Instance.Btn_BackToNewGameMenu(); // Volta ao menu
        }
    }

    void OnDeleteClicked(SaveLoadSystem.GameSaveData data)
    {
        ShowConfirmation($"Tem certeza que deseja <color=red>DELETAR</color> o save de\n{data.playerName}?", () => 
        {
            SaveLoadSystem.Instance.DeleteSave(data.saveID);
            RefreshList();
        });
    }

    void OnNewSaveClicked()
    {
        if (GameManager.Instance != null)
        {
            // Gera ID único baseado no nome e hora
            string newID = $"{GameManager.Instance.playerName}_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            SaveLoadSystem.Instance.SaveGame(newID);
            RefreshList();
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