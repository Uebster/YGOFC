using UnityEngine;
using System.IO;
using System.Linq;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class QAAutoSpawner : MonoBehaviour
{
    public static string currentTestCardId = "";
    public static string currentTestCardName = "";
    public static string currentStatus = "N/A";
    
    // Rastreia a linha atual para facilitar o "Próximo"
    private static int currentLineIndex = -1;

    // --- INTERFACE DRAGGABLE (IMGUI) ---
    private bool showWindow = false;
    private Rect windowRect = new Rect(20, 20, 320, 220);
    private GUIStyle titleStyle;
    private GUIStyle btnStyle;
    private bool styleInitialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        // Auto-injeta a ferramenta no jogo sem precisar arrastar para a Hierarchy
        GameObject go = new GameObject("QAAutoSpawner_Tool");
        go.AddComponent<QAAutoSpawner>();
        DontDestroyOnLoad(go);
    }

    void Update()
    {
        bool toggle = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed) && Keyboard.current.qKey.wasPressedThisFrame)
            toggle = true;
#else
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.Q))
            toggle = true;
#endif
        if (toggle) showWindow = !showWindow;
    }

    void InitStyles()
    {
        if (styleInitialized) return;
        btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontSize = 13;
        btnStyle.fixedHeight = 26;
        btnStyle.margin = new RectOffset(4, 4, 4, 4);

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 14;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.yellow;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        styleInitialized = true;
    }

    void OnGUI()
    {
        if (!showWindow) return;
        InitStyles();
        
        // Trava de segurança para manter o painel na frente de tudo
        GUI.depth = -100;
        windowRect = GUI.Window(10101, windowRect, DrawQAWindow, "QA Auto-Spawner (Ctrl+Q para Ocultar)");
    }

    void DrawQAWindow(int windowID)
    {
        GUILayout.Space(5);
        if (string.IsNullOrEmpty(currentTestCardId))
        {
            GUILayout.Label("Pronto para iniciar a homologação LUA.", titleStyle);
            GUILayout.Space(10);
            if (GUILayout.Button("INICIAR BATERIA DE TESTES", btnStyle))
                TestNextCard();
        }
        else
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label($"<b>Carta:</b> {currentTestCardName} ({currentTestCardId})");
            
            string color = currentStatus.Contains("x") ? "green" : (currentStatus.Contains("R") ? "orange" : "white");
            GUILayout.Label($"<b>Status:</b> <color={color}>{currentStatus}</color>");
            
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("✅ Confirmar [x]", btnStyle)) MarkCurrentAs("x");
            if (GUILayout.Button("⚠️ Revisar [R]", btnStyle)) MarkCurrentAs("R");
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("⏮️ Ant.", btnStyle)) TestPreviousCard();
            if (GUILayout.Button("🔄 Reiniciar", btnStyle)) RestartCurrentTest();
            if (GUILayout.Button("Próx. ⏭️", btnStyle)) TestNextCard(true);
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("➕ Adicionar Cópia", btnStyle)) SpawnCopyOfCurrentCard();
            if (GUILayout.Button("🧹 Limpar Campo", btnStyle)) CleanFieldQA();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        
        // Torna a janela arrastável clicando em qualquer lugar vazio dela
        GUI.DragWindow(new Rect(0, 0, 10000, 10000));
    }

    public static void TestNextCard(bool skipCurrent = false)
    {
        string path = Path.Combine(Application.dataPath, "Support", "QA_Card_Checklist.md");
        if (!File.Exists(path))
        {
            Debug.LogError("[QA] Arquivo de Checklist não encontrado em: " + path);
            return;
        }

        string[] lines = File.ReadAllLines(path);

        int startIndex = 0;
        if (skipCurrent && currentLineIndex >= 0)
        {
            startIndex = currentLineIndex + 1; // Começa a buscar depois da atual
        }

        bool found = false;

        for (int i = startIndex; i < lines.Length; i++)
        {
            if (lines[i].Contains("- [ ]") || lines[i].Contains("- [R]"))
            {
                int idStart = lines[i].IndexOf('`') + 1;
                int idEnd = lines[i].IndexOf('`', idStart);
                if (idStart > 0 && idEnd > idStart)
                {
                    currentTestCardId = lines[i].Substring(idStart, idEnd - idStart);
                    
                    int nameStart = lines[i].IndexOf("**") + 2;
                    int nameEnd = lines[i].LastIndexOf("**");
                    if (nameStart > 1 && nameEnd > nameStart)
                        currentTestCardName = lines[i].Substring(nameStart, nameEnd - nameStart);
                    else 
                        currentTestCardName = "Unknown";

                    currentStatus = lines[i].Contains("- [R]") ? "[R] REVISAR" : "[ ] PENDENTE";
                    currentLineIndex = i;
                    found = true;
                    break;
                }
            }
        }

        if (!found)
        {
            if (skipCurrent && currentLineIndex >= 0) { currentLineIndex = -1; TestNextCard(false); return; }
            Debug.Log("<color=green>🏆 [QA] ABSOLUTO CINEMA! Nenhuma carta pendente encontrada no Checklist!</color>");
            currentTestCardId = "";
            return;
        }

        SetupQABoard(currentTestCardId);
    }

    public static void TestPreviousCard()
    {
        string path = Path.Combine(Application.dataPath, "Support", "QA_Card_Checklist.md");
        if (!File.Exists(path)) return;

        string[] lines = File.ReadAllLines(path);

        int startIndex = lines.Length - 1;
        if (currentLineIndex > 0)
        {
            startIndex = currentLineIndex - 1;
        }

        bool found = false;

        for (int i = startIndex; i >= 0; i--)
        {
            if (lines[i].Contains("- [") && lines[i].Contains("`"))
            {
                int idStart = lines[i].IndexOf('`') + 1;
                int idEnd = lines[i].IndexOf('`', idStart);
                if (idStart > 0 && idEnd > idStart)
                {
                    currentTestCardId = lines[i].Substring(idStart, idEnd - idStart);
                    
                    int nameStart = lines[i].IndexOf("**") + 2;
                    int nameEnd = lines[i].LastIndexOf("**");
                    if (nameStart > 1 && nameEnd > nameStart)
                        currentTestCardName = lines[i].Substring(nameStart, nameEnd - nameStart);
                    else 
                        currentTestCardName = "Unknown";

                    currentStatus = lines[i].Contains("- [x]") ? "[x] OK" : (lines[i].Contains("- [R]") ? "[R] REVISAR" : "[ ] PENDENTE");
                    currentLineIndex = i;
                    found = true;
                    break;
                }
            }
        }

        if (found)
        {
            SetupQABoard(currentTestCardId);
        }
        else
        {
            Debug.Log("<color=yellow>[QA] Já estamos na primeira carta da lista.</color>");
        }
    }

    public static void SpawnCopyOfCurrentCard()
    {
        if (string.IsNullOrEmpty(currentTestCardId)) return;
        if (GameManager.Instance == null || GameManager.Instance.cardDatabase == null) return;
        
        CardData testCard = GameManager.Instance.cardDatabase.GetCardById(currentTestCardId);
        if (testCard != null) {
            GameManager.Instance.AddCardToHand(testCard, true);
            Debug.Log($"<color=cyan>➕ [QA] Cópia extra de {testCard.name} adicionada à mão.</color>");
        }
    }

    public static void RestartCurrentTest()
    {
        if (!string.IsNullOrEmpty(currentTestCardId))
            SetupQABoard(currentTestCardId);
    }

    public static void CleanFieldQA()
    {
        if (FullTestManager.Instance != null)
            FullTestManager.Instance.TestCleanField();
        else if (GameManager.Instance != null)
            GameManager.Instance.ClearFieldZonesOnly();
    }

    public static void MarkCurrentAs(string mark) // Aceita "x" ou "R"
    {
        if (string.IsNullOrEmpty(currentTestCardId)) return;

        string path = Path.Combine(Application.dataPath, "Support", "QA_Card_Checklist.md");
        if (!File.Exists(path)) return;

        string[] lines = File.ReadAllLines(path);
        bool marked = false;

        if (currentLineIndex >= 0 && currentLineIndex < lines.Length && lines[currentLineIndex].Contains(currentTestCardId))
        {
            lines[currentLineIndex] = System.Text.RegularExpressions.Regex.Replace(lines[currentLineIndex], @"-\s\[.\]", $"- [{mark}]");
            marked = true;
        }
        else
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains($"`{currentTestCardId}`"))
                {
                    lines[i] = System.Text.RegularExpressions.Regex.Replace(lines[i], @"-\s\[.\]", $"- [{mark}]");
                    marked = true;
                    currentLineIndex = i;
                    break;
                }
            }
        }

        if (marked)
        {
            File.WriteAllLines(path, lines);
            currentStatus = mark == "x" ? "[x] OK" : "[R] REVISAR";
            Debug.Log($"<color=yellow>✅ [QA] Carta {currentTestCardName} marcada com [{mark}] no Markdown!</color>");
        }
    }

    private static void SetupQABoard(string cardId)
    {
        if (GameManager.Instance == null) return;

        // 1. Limpeza Segura do Tabuleiro e Mãos (Para evitar conflitos com Efeitos Anteriores)
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel != null) {
            CardEffectManager.Instance.luaDuel.currentAttacker = null;
            CardEffectManager.Instance.luaDuel.currentAttackTarget = null;
        }
        if (TargetingSwordUI.Instance != null) TargetingSwordUI.Instance.Hide();
        
        GameManager.Instance.ClearFieldZonesOnly();
        
        foreach (var go in GameManager.Instance.playerHand) Destroy(go);
        GameManager.Instance.playerHand.Clear();
        
        foreach (var go in GameManager.Instance.opponentHand) Destroy(go);
        GameManager.Instance.opponentHand.Clear();

        GameManager.Instance.GetPlayerGraveyard().Clear();
        GameManager.Instance.GetOpponentGraveyard().Clear();
        GameManager.Instance.GetPlayerRemoved().Clear();
        GameManager.Instance.GetOpponentRemoved().Clear();

        // 2. Injeta a carta a ser testada na mão do jogador
        CardData testCard = GameManager.Instance.cardDatabase.GetCardById(cardId);
        if (testCard != null) {
            GameManager.Instance.AddCardToHand(testCard, true);
            Debug.Log($"<color=cyan>🧪 [QA] Injetando {testCard.name} ({cardId}) para Homologação.</color>");
            GameManager.Instance.Dev_InjectDependencies(testCard);
        } else Debug.LogError($"[QA] Carta {cardId} não encontrada no banco de dados!");

        // 3. Invoca os Sacos de Pancada (Inimigos)
        CardData dummyMonster = GameManager.Instance.cardDatabase.cardDatabase.FirstOrDefault(c => c.name.Contains("Blue-Eyes White Dragon") || (c.type.Contains("Monster") && c.atk >= 2500));
        if (dummyMonster != null) 
        {
            GameManager.Instance.SpecialSummonFromData(dummyMonster, false, 2, true, false); // Coloca pro Oponente
            GameManager.Instance.SpecialSummonFromData(dummyMonster, true, 2, true, false);  // Coloca pro Jogador (Para poder destruir/tributar)
        }

        CardData dummySpell = GameManager.Instance.cardDatabase.cardDatabase.FirstOrDefault(c => c.type.Contains("Spell") && !c.name.Contains("7") && c.id != "DM0004");
        if (dummySpell != null) GameManager.Instance.SetSpellTrapFromData(dummySpell, false, 2, true); // Coloca setada (face-down)
    }
}