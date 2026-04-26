using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class TypeDeclareUI : MonoBehaviour
{
    public static TypeDeclareUI Instance;

    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI declaredText; // O "DeclaredNumber" da sua estrutura
    public Transform contentArea;
    public GameObject optionButtonPrefab; 
    
    public Button btnConfirm;
    public Button btnClose;
    public Button btnErase;

    private Action<int> onSelectionMade;
    private int selectedValue = -1;
    private List<GameObject> spawnedButtons = new List<GameObject>();

    void Awake()
    {
        Instance = this;
        if (btnConfirm) btnConfirm.onClick.AddListener(ConfirmSelection);
        if (btnClose) btnClose.onClick.AddListener(CancelSelection);
        if (btnErase) btnErase.onClick.AddListener(EraseSelection);
        gameObject.SetActive(false);
    }

    public void Show(List<int> typeOptions, Action<int> callback)
    {
        onSelectionMade = callback;
        selectedValue = -1;
        
        if (titleText) titleText.text = "Declare 1 Card Type";
        if (declaredText) declaredText.text = "...";

        // Limpa opções antigas
        foreach (var btn in spawnedButtons) Destroy(btn);
        spawnedButtons.Clear();

        // Instancia as cartas/botões 180x252
        foreach (int opt in typeOptions)
        {
            string optName = GetTypeName(opt);
            GameObject go = Instantiate(optionButtonPrefab, contentArea);
            spawnedButtons.Add(go);

            TextMeshProUGUI txt = go.GetComponentInChildren<TextMeshProUGUI>();
            if (txt) txt.text = optName;

            Button b = go.GetComponent<Button>();
            if (b) b.onClick.AddListener(() => SelectType(opt, optName));
        }

        UpdateButtons();
        gameObject.SetActive(true);
    }

    private string GetTypeName(int val)
    {
        if (val == 70) return "Monster Card";
        if (val == 71) return "Spell Card";
        if (val == 72) return "Trap Card";
        return $"Option {val}";
    }

    private void SelectType(int val, string name)
    {
        selectedValue = val;
        if (declaredText) declaredText.text = name;
        UpdateButtons();
    }

    private void EraseSelection()
    {
        selectedValue = -1;
        if (declaredText) declaredText.text = "...";
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        if (btnConfirm) btnConfirm.interactable = (selectedValue != -1);
        if (btnErase) btnErase.interactable = (selectedValue != -1);
    }

    private void ConfirmSelection() { if (selectedValue != -1) { gameObject.SetActive(false); onSelectionMade?.Invoke(selectedValue); } }
    private void CancelSelection() { gameObject.SetActive(false); onSelectionMade?.Invoke(70); /* Fallback p/ Monstro pra evitar crash */ }
}