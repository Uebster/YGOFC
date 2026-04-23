using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DeclareNumberUI : MonoBehaviour
{
    public static DeclareNumberUI Instance;

    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI declaredNumberText;
    public TMP_InputField inputField;
    public Button confirmButton;
    public Button closeButton;

    [Header("Numpad Buttons")]
    [Tooltip("Arraste os botões do seu painel aqui na ordem exata de 0 a 9 (Botão 0 no índice 0, Botão 1 no índice 1, etc).")]
    public Button[] numberButtons = new Button[10];

    private Action<int> onConfirm;
    private string currentInput = "";
    private int minValue = 0;
    private int maxValue = 9999999;

    void Awake()
    {
        Instance = this;
        
        if (confirmButton) confirmButton.onClick.AddListener(ConfirmSelection);
        if (closeButton) closeButton.onClick.AddListener(CancelSelection);
        if (inputField) 
        {
            inputField.onValueChanged.AddListener(OnInputChanged);
            inputField.onSubmit.AddListener((val) => { if (confirmButton.interactable) ConfirmSelection(); });
            inputField.characterValidation = TMP_InputField.CharacterValidation.Integer;
        }

        for (int i = 0; i < numberButtons.Length; i++)
        {
            if (numberButtons[i] != null)
            {
                int num = i; // Closure capture obrigatório para botões no loop
                numberButtons[i].onClick.AddListener(() => AppendNumber(num));
            }
        }
        
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (gameObject.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelSelection();
            }
        }
    }

    public void Show(string title, int min, int max, Action<int> callback)
    {
        onConfirm = callback;
        minValue = min;
        maxValue = max;
        currentInput = "";

        if (titleText) titleText.text = title;
        if (inputField)
        {
            inputField.text = currentInput;
            inputField.Select();
            inputField.ActivateInputField();
        }
        
        UpdateVisuals();
        gameObject.SetActive(true);
    }

    void AppendNumber(int num)
    {
        // Se a string atual for "0", substitui pelo novo número em vez de encavalar
        if (currentInput == "0") currentInput = "";
        
        // Trava para não estourar a variável 'int' da Unity (Max 2 bilhões e pouco)
        if (currentInput.Length >= 9) return;

        currentInput += num.ToString();
        
        if (inputField) 
        {
            inputField.text = currentInput;
            inputField.caretPosition = inputField.text.Length; // Mantém o cursor piscando no final
        }
        UpdateVisuals();
    }

    void OnInputChanged(string input)
    {
        currentInput = input;
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (declaredNumberText) 
            declaredNumberText.text = string.IsNullOrEmpty(currentInput) ? "0" : currentInput;

        if (int.TryParse(currentInput, out int val))
        {
            bool isValid = val >= minValue && val <= maxValue;
            if (confirmButton) confirmButton.interactable = isValid;
        }
        else
        {
            if (confirmButton) confirmButton.interactable = false;
        }
    }

    void ConfirmSelection()
    {
        if (int.TryParse(currentInput, out int val))
        {
            gameObject.SetActive(false);
            onConfirm?.Invoke(val);
        }
    }

    void CancelSelection()
    {
        gameObject.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.justCanceledSomething = true;
        onConfirm?.Invoke(minValue); // Cancela retornando o valor mínimo aceitável
    }
}