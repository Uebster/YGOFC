using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class NumericSelectionUI : MonoBehaviour
{
    public static NumericSelectionUI Instance;

    [Header("Referências de Texto")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI displayText; // Visor principal
    
    [Header("Botões do Teclado")]
    [Tooltip("Arraste os botões de 0 a 9 em ordem (Índice 0 = Botão 0, Índice 1 = Botão 1...)")]
    public Button[] numberButtons = new Button[10]; 
    public Button clearButton; // Apagar / C
    public Button confirmButton; // OK / Enter
    public Button closeButton; // Opcional (Cancelar)

    private string currentInput = "";
    private int minValue;
    private int maxValue;
    private int requiredMultiple;
    private Action<int> onConfirmCallback;
    private Action onCancelCallback;
    private List<int> allowedDigits;

    private bool isVisible = false;

    void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);

        // Configura os botões numéricos automaticamente baseado no índice do array
        for (int i = 0; i < numberButtons.Length; i++)
        {
            int num = i; // Cópia local necessária para o lambda (closure)
            if (numberButtons[i] != null)
            {
                numberButtons[i].onClick.AddListener(() => OnNumberClicked(num));
            }
        }

        if (clearButton) clearButton.onClick.AddListener(OnClearClicked);
        if (confirmButton) confirmButton.onClick.AddListener(OnConfirmClicked);
        if (closeButton) closeButton.onClick.AddListener(OnCancelClicked);
    }

    void Update()
    {
        if (!isVisible) return;
        HandleKeyboardInput();
    }

    public void Show(string title, string message, int min, int max, int multipleOf, List<int> allowed, Action<int> onConfirm, Action onCancel = null)
    {
        titleText.text = title;
        messageText.text = message;
        minValue = min;
        maxValue = max;
        requiredMultiple = multipleOf;
        
        // Se não passar lista, permite todos os números de 0 a 9
        allowedDigits = allowed ?? new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        
        onConfirmCallback = onConfirm;
        onCancelCallback = onCancel;

        currentInput = "";
        UpdateDisplay();
        UpdateButtonStates();

        gameObject.SetActive(true);
        isVisible = true;
    }

    private void HandleKeyboardInput()
    {
        // Lê números do teclado principal e teclado numérico (Numpad)
        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
            {
                OnNumberClicked(i);
            }
        }

        // Apagar (Backspace)
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (currentInput.Length > 0)
            {
                currentInput = currentInput.Substring(0, currentInput.Length - 1);
                UpdateDisplay();
            }
        }

        // Confirmar (Enter)
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (confirmButton.interactable) OnConfirmClicked();
        }

        // Cancelar (Esc)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnCancelClicked();
        }
    }

    private void OnNumberClicked(int num)
    {
        if (!allowedDigits.Contains(num)) return;

        // Impede vários zeros à esquerda
        if (currentInput == "0") currentInput = "";

        string potentialInput = currentInput + num.ToString();
        
        // Trava de segurança para não ultrapassar o limite do int (9 dígitos)
        if (potentialInput.Length > 9) return; 

        currentInput = potentialInput;
        UpdateDisplay();
    }

    private void OnClearClicked()
    {
        currentInput = "";
        UpdateDisplay();
    }

    private void OnConfirmClicked()
    {
        if (int.TryParse(currentInput, out int val))
        {
            if (IsValidInput(val))
            {
                isVisible = false;
                gameObject.SetActive(false);
                onConfirmCallback?.Invoke(val);
            }
        }
    }

    private void OnCancelClicked()
    {
        isVisible = false;
        gameObject.SetActive(false);
        onCancelCallback?.Invoke();
    }

    private void UpdateDisplay()
    {
        if (string.IsNullOrEmpty(currentInput))
        {
            displayText.text = "0";
            displayText.color = new Color(1, 1, 1, 0.5f); // Branco transparente (Placeholder)
        }
        else
        {
            displayText.text = currentInput;
            int.TryParse(currentInput, out int val);
            
            if (IsValidInput(val))
                displayText.color = Color.cyan; // Número válido (pronto para confirmar)
            else
                displayText.color = Color.red; // Número inválido (ex: não é múltiplo ou excedeu max)
        }
        
        // Só libera o botão OK se o número atual for válido nas regras
        int.TryParse(currentInput, out int curVal);
        confirmButton.interactable = IsValidInput(curVal) && !string.IsNullOrEmpty(currentInput);
    }

    private void UpdateButtonStates()
    {
        // Desativa botões que não estão na lista de permitidos (Útil para rolar dados: só acende de 1 a 6)
        for (int i = 0; i < numberButtons.Length; i++)
        {
            if (numberButtons[i] != null)
            {
                numberButtons[i].interactable = allowedDigits.Contains(i);
            }
        }
    }

    private bool IsValidInput(int val)
    {
        if (val < minValue || val > maxValue) return false;
        if (requiredMultiple > 0 && val % requiredMultiple != 0) return false;
        return true;
    }
}