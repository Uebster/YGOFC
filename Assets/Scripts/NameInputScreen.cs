using UnityEngine;
using TMPro;
using System.Text;
using System.Collections.Generic;

public class NameInputScreen : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameDisplayText;
    public int maxCharacters = 8; // Forbidden Memories usa 8 caracteres
    public string defaultName = "DUELIST";

    [Header("Keyboard Generation")]
    public Transform keyboardContainer; // Arraste o objeto com Grid Layout Group
    public GameObject keyPrefab; // Arraste um prefab de botão simples
    public string charactersToGenerate = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    public string accentedCharacters = "ÁÀÂÃÉÊÍÓÔÕÚÇ";
    public string specialCharacters = "!@#$%&*()_+-=[]{};:,.<>/?";

    private StringBuilder currentName = new StringBuilder();
    private bool isShifted = false;
    private bool isCaps = false;
    private List<VirtualKey> generatedKeys = new List<VirtualKey>();

    void OnEnable()
    {
        // Limpa o nome ao abrir a tela
        currentName.Clear();
        UpdateDisplay();
        
        // Reseta o estado do teclado
        isShifted = false;
        isCaps = false;
        UpdateKeyboardCase();
    }

    public void ProcessKey(string key)
    {
        switch (key.ToUpper())
        {
            case "BS":
            case "BACKSPACE":
                DeleteCharacter();
                break;
            case "ENTER":
            case "OK":
            case "END":
                ConfirmName();
                break;
            case "SPACE":
            case " ":
                AddCharacter(" ");
                break;
            case "SHIFT":
                ToggleShift();
                break;
            case "CAPS":
            case "CAPSLOCK":
                ToggleCaps();
                break;
            default:
                AddCharacter(key);
                // Se não estiver em Caps Lock, desativa o Shift após digitar uma letra
                if (isShifted && !isCaps)
                {
                    isShifted = false;
                    UpdateKeyboardCase();
                }
                break;
        }
    }

    public void AddCharacter(string c)
    {
        if (currentName.Length < maxCharacters)
        {
            currentName.Append(c);
            UpdateDisplay();
        }
    }

    public void DeleteCharacter()
    {
        if (currentName.Length > 0)
        {
            currentName.Length--;
            UpdateDisplay();
        }
    }

    public void ConfirmName()
    {
        string finalName = currentName.ToString().Trim();
        if (string.IsNullOrEmpty(finalName)) return; // Não permite nome vazio

        // Gera o ID do save: Nome_AnoMesDia_HoraMinutoSegundo
        string saveID = $"{finalName}_{System.DateTime.Now:yyyyMMdd_HHmmss}";

        // 1. Salva o nome no GameManager e PlayerPrefs
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPlayerProfile(finalName, saveID);
        }

        // 2. Reseta o progresso da campanha (pois é um New Game)
        if (CampaignManager.Instance != null)
        {
            CampaignManager.Instance.ResetProgress();
        }

        Debug.Log($"Perfil criado: {finalName}. Iniciando novo jogo...");

        // 3. Avança para o menu de seleção de modo (New Game Menu)
        if (UIManager.Instance != null)
        {
            UIManager.Instance.Btn_ConfirmNameInput();
        }
    }

    void UpdateDisplay()
    {
        if (nameDisplayText != null)
        {
            // Adiciona um underscore piscante ou fixo para simular o cursor
            nameDisplayText.text = currentName.ToString() + "_";
        }
    }

    public void ToggleShift()
    {
        isShifted = !isShifted;
        UpdateKeyboardCase();
    }

    public void ToggleCaps()
    {
        isCaps = !isCaps;
        isShifted = isCaps; // Caps Lock ativa o estado Shift visualmente
        UpdateKeyboardCase();
    }

    private void UpdateKeyboardCase()
    {
        bool upper = isShifted || isCaps;
        
        // Busca as teclas se a lista estiver vazia (ex: runtime)
        if (generatedKeys == null || generatedKeys.Count == 0)
        {
            if (keyboardContainer != null)
                generatedKeys = new List<VirtualKey>(keyboardContainer.GetComponentsInChildren<VirtualKey>());
        }

        foreach (var key in generatedKeys)
        {
            if (key == null) continue;
            
            string c = key.character;
            // Só altera se for uma única letra
            if (c.Length == 1 && char.IsLetter(c[0]))
            {
                key.SetCharacter(upper ? c.ToUpper() : c.ToLower());
            }
        }
    }

    // --- FERRAMENTA DE GERAÇÃO AUTOMÁTICA ---
    // Clique com o botão direito no script no Inspector e escolha "Generate Keyboard"
    [ContextMenu("Generate Keyboard")]
    public void GenerateKeyboard()
    {
        if (keyboardContainer == null || keyPrefab == null)
        {
            Debug.LogError("NameInputScreen: Por favor, atribua 'Keyboard Container' e 'Key Prefab' no Inspector.");
            return;
        }

        // Limpa botões existentes para evitar duplicatas (apenas no Editor)
        while (keyboardContainer.childCount > 0)
        {
            DestroyImmediate(keyboardContainer.GetChild(0).gameObject);
        }

        generatedKeys.Clear();

        // 1. Caracteres Padrão + Acentos + Especiais
        string allChars = charactersToGenerate + accentedCharacters + specialCharacters;

        foreach (char c in allChars)
        {
            CreateKey(c.ToString());
        }

        // 2. Teclas Funcionais
        CreateKey("SPACE");
        CreateKey("BS"); // Backspace
        CreateKey("SHIFT");
        CreateKey("CAPS");
        CreateKey("ENTER");

        Debug.Log("Teclado virtual gerado com sucesso!");
    }

    private void CreateKey(string charOrFunc)
    {
        GameObject newKey = Instantiate(keyPrefab, keyboardContainer);
        newKey.name = $"Key_{charOrFunc}";
        
        // Configura o texto do botão
        TextMeshProUGUI btnText = newKey.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null) btnText.text = charOrFunc;

        // Adiciona/Configura o script VirtualKey
        VirtualKey vKey = newKey.GetComponent<VirtualKey>();
        if (vKey == null) vKey = newKey.AddComponent<VirtualKey>();
        vKey.character = charOrFunc;
        
        // Adiciona MillenniumButton para efeito de Hover (Aumentar tamanho)
        MillenniumButton mb = newKey.GetComponent<MillenniumButton>();
        if (mb == null) mb = newKey.AddComponent<MillenniumButton>();
        mb.scaleAmount = 1.2f; // Aumenta 20%
        mb.useColorTint = false; // Desativa a mudança de cor, usa apenas escala
        mb.animationDuration = 0.1f;

        generatedKeys.Add(vKey);
    }
}