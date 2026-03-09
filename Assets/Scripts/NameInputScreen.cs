using UnityEngine;
using TMPro;
using System.Text;
using System.Collections.Generic;
using UnityEngine.UI;

public class NameInputScreen : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameDisplayText;
    public int maxCharacters = 8; // Forbidden Memories usa 8 caracteres
    public string defaultName = "DUELIST";

    [Header("Keyboard Grids")]
    public GameObject qwertyGrid;
    public GameObject abcGrid;
    public GameObject accentsGrid;
    public GameObject specialsGrid;

    [Header("Control Buttons")]
    public Button btnSwitchLayout; // Alterna entre ABC e QWERTY
    public Button btnAccents;
    public Button btnSpecials;

    private StringBuilder currentName = new StringBuilder();
    private bool isShifted = false;
    private bool isCaps = false;

    [Header("Keyboard Generation")]
    public GameObject keyPrefab; // Arraste um prefab de botão simples
    private List<VirtualKey> generatedKeys = new List<VirtualKey>();

    void Start()
    {
        if (btnSwitchLayout) btnSwitchLayout.onClick.AddListener(ToggleKeyboardLayout);
        if (btnAccents) btnAccents.onClick.AddListener(ShowAccents);
        if (btnSpecials) btnSpecials.onClick.AddListener(ShowSpecials);
    }

    void OnEnable()
    {
        // Limpa o nome ao abrir a tela
        currentName.Clear();
        UpdateDisplay();
        
        // Reseta o estado do teclado
        isShifted = false;
        isCaps = false;
        
        ShowQwerty(); // Padrão
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
        
        // Atualiza a lista de teclas se necessário (pode ter mudado de grid)
        RefreshKeyList();
        
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

    private void RefreshKeyList()
    {
        generatedKeys.Clear();
        // Adiciona teclas de todos os grids ativos e inativos para garantir que o Shift funcione em tudo
        if (qwertyGrid) generatedKeys.AddRange(qwertyGrid.GetComponentsInChildren<VirtualKey>(true));
        if (abcGrid) generatedKeys.AddRange(abcGrid.GetComponentsInChildren<VirtualKey>(true));
        if (accentsGrid) generatedKeys.AddRange(accentsGrid.GetComponentsInChildren<VirtualKey>(true));
        if (specialsGrid) generatedKeys.AddRange(specialsGrid.GetComponentsInChildren<VirtualKey>(true));
    }

    // --- CONTROLE DE ABAS ---

    public void ShowQwerty()
    {
        if (qwertyGrid) qwertyGrid.SetActive(true);
        if (abcGrid) abcGrid.SetActive(false);
        if (accentsGrid) accentsGrid.SetActive(false);
        if (specialsGrid) specialsGrid.SetActive(false);
        UpdateKeyboardCase();
    }

    public void ShowABC()
    {
        if (qwertyGrid) qwertyGrid.SetActive(false);
        if (abcGrid) abcGrid.SetActive(true);
        if (accentsGrid) accentsGrid.SetActive(false);
        if (specialsGrid) specialsGrid.SetActive(false);
        UpdateKeyboardCase();
    }

    public void ShowAccents()
    {
        if (qwertyGrid) qwertyGrid.SetActive(false);
        if (abcGrid) abcGrid.SetActive(false);
        if (accentsGrid) accentsGrid.SetActive(true);
        if (specialsGrid) specialsGrid.SetActive(false);
        UpdateKeyboardCase();
    }

    public void ShowSpecials()
    {
        if (qwertyGrid) qwertyGrid.SetActive(false);
        if (abcGrid) abcGrid.SetActive(false);
        if (accentsGrid) accentsGrid.SetActive(false);
        if (specialsGrid) specialsGrid.SetActive(true);
    }

    public void ToggleKeyboardLayout()
    {
        if (qwertyGrid != null && qwertyGrid.activeSelf)
        {
            ShowABC();
        }
        else
        {
            ShowQwerty();
        }
    }

    // --- FERRAMENTA DE GERAÇÃO AUTOMÁTICA ---
    // Clique com o botão direito no script no Inspector e escolha "Generate Keyboard"
    [ContextMenu("Generate Keyboard")]
    public void GenerateKeyboard()
    {
        if (keyPrefab == null)
        {
            Debug.LogError("NameInputScreen: Key Prefab não atribuído!");
            return;
        }
        if (qwertyGrid == null || abcGrid == null || accentsGrid == null || specialsGrid == null)
        {
            Debug.LogError("NameInputScreen: Um ou mais Grids de Teclado não foram atribuídos no Inspector.");
            return;
        }

        // Limpa todos os grids antes de gerar
        ClearGrid(qwertyGrid.transform);
        ClearGrid(abcGrid.transform);
        ClearGrid(accentsGrid.transform);
        ClearGrid(specialsGrid.transform);

        generatedKeys.Clear();

        // --- Gera Layout QWERTY ---
        string[] qwertyRows = { "1234567890", "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM" };
        GenerateLayout(qwertyGrid.transform, qwertyRows);
        GenerateFunctionalKeys(qwertyGrid.transform);

        // --- Gera Layout ABC ---
        string[] abcRows = { "ABCDEFGHI", "JKLMNOPQR", "STUVWXYZ", "0123456789" };
        GenerateLayout(abcGrid.transform, abcRows);
        GenerateFunctionalKeys(abcGrid.transform);

        // --- Gera Layout de Acentos ---
        string[] accentRows = { "ÁÀÂÃÉÊÍ", "ÓÔÕÚÇ" };
        GenerateLayout(accentsGrid.transform, accentRows);
        GenerateFunctionalKeys(accentsGrid.transform, true); // Com Shift/Caps

        // --- Gera Layout de Especiais ---
        string[] specialRows = { "!@#$%&*", "()_+-=[]{}", ";:,.<>/?" };
        GenerateLayout(specialsGrid.transform, specialRows);
        GenerateFunctionalKeys(specialsGrid.transform, false); // Sem Shift/Caps

        Debug.Log("Teclados virtuais gerados com sucesso!");
    }

    private void ClearGrid(Transform grid)
    {
        if (grid == null) return;
        for (int i = grid.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(grid.GetChild(i).gameObject);
        }
    }

    private void GenerateLayout(Transform container, string[] rows)
    {
        foreach (string row in rows)
        {
            foreach (char c in row)
            {
                CreateKey(c.ToString(), container);
            }
        }
    }

    private void GenerateFunctionalKeys(Transform container, bool includeShiftAndCaps = true)
    {
        CreateKey("SPACE", container);
        CreateKey("BS", container);
        if (includeShiftAndCaps)
        {
            CreateKey("SHIFT", container);
            CreateKey("CAPS", container);
        }
        CreateKey("ENTER", container);
    }

    private void CreateKey(string charOrFunc, Transform parent)
    {
        if (parent == null) return;
        GameObject newKey = Instantiate(keyPrefab, parent);
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