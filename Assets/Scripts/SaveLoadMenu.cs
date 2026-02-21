using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SaveLoadMenu : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("Marque TRUE se esta for a tela de SALVAR. Desmarque para CARREGAR.")]
    public bool isSaveMode = false; 
    
    [Header("Referências UI")]
    public Transform listContent; // Onde os slots serão criados
    public GameObject slotPrefab; // O prefab do SaveSlotUI
    public Button newSaveButton; // Botão "Novo Save" (Apenas para tela de Save)

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
            // Sobrescrever Save Existente
            SaveLoadSystem.Instance.SaveGame(data.saveID);
            RefreshList(); // Atualiza a data/hora
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
        SaveLoadSystem.Instance.DeleteSave(data.saveID);
        RefreshList();
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
}