using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MultipleChoiceUI : MonoBehaviour
{
    public static MultipleChoiceUI Instance;

    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public Transform contentArea;
    public GameObject optionButtonPrefab; // Deve conter um Button e um TextMeshProUGUI
    public Button confirmButton;
    public Button closeButton;

    private List<string> selectedOptions = new List<string>();
    private int minSelection = 1;
    private int maxSelection = 1;
    private System.Action<List<string>> onConfirm;

    private List<GameObject> spawnedButtons = new List<GameObject>();

    void Awake()
    {
        Instance = this;
        if (confirmButton) confirmButton.onClick.AddListener(ConfirmSelection);
        if (closeButton) closeButton.onClick.AddListener(CancelSelection);
        gameObject.SetActive(false);
    }

    public void Show(List<string> options, string title, int min, int max, System.Action<List<string>> callback)
    {
        selectedOptions.Clear();
        minSelection = min;
        maxSelection = max;
        onConfirm = callback;

        if (titleText) titleText.text = title;

        foreach (var obj in spawnedButtons) Destroy(obj);
        spawnedButtons.Clear();

        foreach (var opt in options)
        {
            GameObject go = Instantiate(optionButtonPrefab, contentArea);
            spawnedButtons.Add(go);
            
            TextMeshProUGUI txt = go.GetComponentInChildren<TextMeshProUGUI>();
            if (txt) txt.text = opt;

            Button btn = go.GetComponent<Button>();
            string currentOpt = opt; // Closure capture
            btn.onClick.AddListener(() => ToggleSelection(currentOpt, go));

            UpdateVisual(go, false);
        }

        UpdateConfirmButton();
        gameObject.SetActive(true);
    }

    void ToggleSelection(string opt, GameObject btnObj)
    {
        if (selectedOptions.Contains(opt)) {
            selectedOptions.Remove(opt);
        } else {
            if (selectedOptions.Count < maxSelection) {
                selectedOptions.Add(opt);
            } else if (maxSelection == 1) {
                selectedOptions.Clear();
                selectedOptions.Add(opt);
                foreach (var go in spawnedButtons) UpdateVisual(go, go.GetComponentInChildren<TextMeshProUGUI>().text == opt);
                UpdateConfirmButton();
                return;
            }
        }
        UpdateVisual(btnObj, selectedOptions.Contains(opt));
        UpdateConfirmButton();
    }

    void UpdateVisual(GameObject btnObj, bool isSelected) {
        Image img = btnObj.GetComponent<Image>();
        if (img) img.color = isSelected ? new Color(0.3f, 0.7f, 1f) : Color.white; // Azul para selecionado
    }

    void UpdateConfirmButton() {
        if (confirmButton) confirmButton.interactable = (selectedOptions.Count >= minSelection && selectedOptions.Count <= maxSelection);
    }

    void ConfirmSelection() { gameObject.SetActive(false); onConfirm?.Invoke(selectedOptions); }
    void CancelSelection() { gameObject.SetActive(false); onConfirm?.Invoke(new List<string>()); }
}