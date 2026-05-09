using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class QAAutoSpawner : MonoBehaviour
{
    public static string currentTestCardId = "";
    public static string currentTestCardName = "";
    public static string currentStatus = "N/A";
    
    public static string currentEraPrefix = "DM";
    private static bool prefsLoaded = false;
    
    // Rastreia a linha atual para facilitar o "Próximo"
    private static int currentLineIndex = -1;
    private static int filterState = 0; // 0 = PENDENTES, 1 = REVISÃO, 2 = APROVADOS

    // --- INTERFACE DRAGGABLE (IMGUI) ---
    private bool showWindow = false;
    private Rect windowRect = new Rect(20, 20, 400, 700);
    private Texture2D bgTex;
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
        btnStyle.richText = true;

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 14;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.yellow;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.richText = true;

        bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.1f, 0.95f));
        bgTex.Apply();

        styleInitialized = true;
    }

    void OnGUI()
    {
        if (!showWindow) return;
        InitStyles();
        
        GUI.depth = -100;
        GUI.backgroundColor = Color.clear;
        windowRect = GUI.Window(10101, windowRect, DrawQAWindow, "");
        GUI.backgroundColor = Color.white;
    }

    void DrawQAWindow(int windowID)
    {
        if (!prefsLoaded) { currentEraPrefix = PlayerPrefs.GetString("QASpawner_EraPrefix", "DM"); prefsLoaded = true; }

        GUI.DrawTexture(new Rect(0, 0, windowRect.width, windowRect.height), bgTex);
        
        GUILayout.Space(5);
        GUILayout.Label("🛠️ QA Auto-Spawner (Ctrl+Q Ocultar)", titleStyle);
        
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Prefixo da Era:", GUILayout.Width(100));
        
        string newPrefix = GUILayout.TextField(currentEraPrefix, GUILayout.Width(50));
        if (newPrefix != currentEraPrefix)
        {
            currentEraPrefix = newPrefix;
            PlayerPrefs.SetString("QASpawner_EraPrefix", currentEraPrefix);
            PlayerPrefs.Save();
            currentLineIndex = -1; // Reseta a busca ao trocar de Era
            currentTestCardId = ""; // Limpa a carta atual do painel
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Filtro de Busca:", GUILayout.Width(100));
        
        string filterBtnText = "<color=white>✅ PENDENTES [ ]</color>";
        if (filterState == 1) filterBtnText = "<color=orange>⚠️ APENAS REVISÃO [R]</color>";
        else if (filterState == 2) filterBtnText = "<color=green>🏆 APROVADOS [x]</color>";

        if (GUILayout.Button(filterBtnText, btnStyle)) { filterState = (filterState + 1) % 3; currentLineIndex = -1; }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        if (string.IsNullOrEmpty(currentTestCardId))
        {
            string searchMsg = "Procurando cartas com tag [ ]...";
            if (filterState == 1) searchMsg = "Procurando cartas com tag [R]...";
            else if (filterState == 2) searchMsg = "Procurando cartas com tag [x]...";
            GUILayout.Label(searchMsg, titleStyle);
            GUILayout.Space(10);
            if (GUILayout.Button("INICIAR BATERIA DE TESTES", btnStyle))
                TestNextCard();
        }
        else
        {
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.richText = true;

            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label($"<b>Carta:</b> {currentTestCardName} ({currentTestCardId})", new GUIStyle(GUI.skin.label) { richText = true });
            
            string color = currentStatus.Contains("x") ? "green" : (currentStatus.Contains("R") ? "orange" : "white");
            GUILayout.Label($"<b>Status:</b> <color={color}>{currentStatus}</color>", new GUIStyle(GUI.skin.label) { richText = true });
            
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("✅ Aprovar [x]", btnStyle)) MarkCurrentAs("x");
            if (GUILayout.Button("⚠️ Revisar [R]", btnStyle)) MarkCurrentAs("R");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("⏮️ Ant.", btnStyle)) TestPreviousCard();
            if (GUILayout.Button("🔄 Cópia Nova", btnStyle)) RestartCurrentTest();
            if (GUILayout.Button("Próx. ⏭️", btnStyle)) TestNextCard(true);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.Label("<b>--- CONTROLE DA IA ---</b>", titleStyle);
            GUILayout.BeginVertical(boxStyle);
            
            if (GameManager.Instance != null)
            {
                bool isAIActive = OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeSelf;
                if (GUILayout.Button(isAIActive ? "🤖 IA: LIGADA (Desligar p/ Assumir)" : "🎮 IA: DESLIGADA (Você Controla)", btnStyle))
                {
                    if (OpponentAI.Instance != null)
                    {
                        bool newState = !isAIActive;
                        OpponentAI.Instance.gameObject.SetActive(newState);
                        GameManager.Instance.canPlaceOpponentCards = !newState;
                        GameManager.Instance.canOpponentDrawFromDeck = !newState;
                    }
                }

                bool isHandVisible = GameManager.Instance.showOpponentHand;
                if (GUILayout.Button(isHandVisible ? "👁️ Mão Inimiga: VISÍVEL" : "👁️ Mão Inimiga: OCULTA", btnStyle))
                {
                    GameManager.Instance.showOpponentHand = !isHandVisible;
                    GameManager.Instance.ToggleOpponentHandVisibility();
                }
                
                bool autoPhases = !GameManager.Instance.disableAutoPhases;
                if (GUILayout.Button(autoPhases ? "⏳ Fases: AUTOMÁTICAS" : "⏸️ Fases: PAUSADAS", btnStyle))
                {
                    GameManager.Instance.disableAutoPhases = autoPhases;
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.Label("<b>--- GOD MODE / SANDBOX ---</b>", titleStyle);
            
            GUILayout.BeginVertical(boxStyle);

            bool pegasus = GameManager.Instance != null && GameManager.Instance.pegasusEye;
            if (GUILayout.Button(pegasus ? "👁️ Olho de Pegasus: LIGADO" : "👁️ Olho de Pegasus: DESLIGADO", btnStyle))
            {
                if (GameManager.Instance != null) GameManager.Instance.pegasusEye = !pegasus;
            }
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Comprar (Draw):", GUILayout.Width(100));
            if (GUILayout.Button("Player", btnStyle)) { if (GameManager.Instance != null) GameManager.Instance.DrawCard(true); }
            if (GUILayout.Button("Oponente", btnStyle)) { if (GameManager.Instance != null) GameManager.Instance.DrawOpponentCard(); }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Mão:", GUILayout.Width(100));
            if (GUILayout.Button("Limpar (P)", btnStyle)) ClearHand(true);
            if (GUILayout.Button("Limpar (O)", btnStyle)) ClearHand(false);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Monstros:", GUILayout.Width(100));
            if (GUILayout.Button("Limpar (P)", btnStyle)) ClearMonsters(true);
            if (GUILayout.Button("Limpar (O)", btnStyle)) ClearMonsters(false);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Mag/Arm:", GUILayout.Width(100));
            if (GUILayout.Button("Limpar (P)", btnStyle)) ClearSpells(true);
            if (GUILayout.Button("Limpar (O)", btnStyle)) ClearSpells(false);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Token/Alvo:", GUILayout.Width(100));
            if (GUILayout.Button("Spawn (P)", btnStyle)) SpawnTestMonster(true);
            if (GUILayout.Button("Spawn (O)", btnStyle)) SpawnTestMonster(false);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Mag/Set:", GUILayout.Width(100));
            if (GUILayout.Button("Spawn (P)", btnStyle)) SpawnTestSpell(true);
            if (GUILayout.Button("Spawn (O)", btnStyle)) SpawnTestSpell(false);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Olhar Deck:", GUILayout.Width(100));
            if (GUILayout.Button("Player", btnStyle)) { if (GameManager.Instance != null) GameManager.Instance.ViewDeck(true); }
            if (GUILayout.Button("Oponente", btnStyle)) { if (GameManager.Instance != null) GameManager.Instance.ViewDeck(false); }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Zerar Tudo:", GUILayout.Width(100));
            if (GUILayout.Button("Limpar o Campo Completo", btnStyle)) CleanFieldQA();
            if (GUILayout.Button("Reiniciar Duelo", btnStyle)) { if (GameManager.Instance != null) GameManager.Instance.StartDuel(); }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        
        // Torna a janela arrastável clicando em qualquer lugar vazio dela
        GUI.DragWindow(new Rect(0, 0, 10000, 10000));
    }

    private static string GetChecklistPath()
    {
        string fileName = string.IsNullOrEmpty(currentEraPrefix) ? "QA_Card_Checklist.md" : $"QA_Card_Checklist_{currentEraPrefix}.md";
        return Path.Combine(Application.dataPath, "Support", fileName);
    }

    private static bool PassesAssetShield()
    {
        string luaDir = string.IsNullOrEmpty(currentEraPrefix) ? "LuaScripts" : $"{currentEraPrefix}LuaScripts";
        string luaPath = Path.Combine(Application.dataPath, "Scripts", luaDir);
        
        string imgDir = string.IsNullOrEmpty(currentEraPrefix) ? "CardImages" : $"{currentEraPrefix}CardImages";
        string imgPath = Path.Combine(Application.streamingAssetsPath, imgDir);

        if (!Directory.Exists(luaPath)) {
            Debug.LogError($"<color=red>🛑 [QA Asset Shield]</color> BLOQUEADO: A pasta de scripts '{luaDir}' não existe no projeto. Importe a Era '{currentEraPrefix}' primeiro!");
            return false;
        }
        if (!Directory.Exists(imgPath)) {
            Debug.LogError($"<color=red>🛑 [QA Asset Shield]</color> BLOQUEADO: A pasta de imagens '{imgDir}' não existe no projeto. Importe a Era '{currentEraPrefix}' primeiro!");
            return false;
        }
        return true;
    }

    public static void TestNextCard(bool skipCurrent = false)
    {
        if (!PassesAssetShield()) return;
        string path = GetChecklistPath();
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
            bool isMatch = false;
            if (filterState == 0) isMatch = lines[i].Contains("- [ ]");
            else if (filterState == 1) isMatch = lines[i].Contains("- [R]");
            else if (filterState == 2) isMatch = lines[i].Contains("- [x]");

            if (isMatch)
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
        if (!PassesAssetShield()) return;
        string path = GetChecklistPath();
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
            bool isMatch = false;
            if (filterState == 0) isMatch = lines[i].Contains("- [ ]");
            else if (filterState == 1) isMatch = lines[i].Contains("- [R]");
            else if (filterState == 2) isMatch = lines[i].Contains("- [x]");
            
            if (isMatch && lines[i].Contains("`"))
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
            GameManager.Instance.AddCardToHand(JsonUtility.FromJson<CardData>(JsonUtility.ToJson(testCard)), true);
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

        string path = GetChecklistPath();
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

    public static void ClearHand(bool isPlayer)
    {
        if (GameManager.Instance == null) return;
        var hand = isPlayer ? GameManager.Instance.playerHand : GameManager.Instance.opponentHand;
        foreach(var go in new List<GameObject>(hand))
        {
            if(go != null)
            {
                var cd = go.GetComponent<CardDisplay>();
                if(cd != null) GameManager.Instance.DiscardCard(cd);
            }
        }
    }

    public static void ClearMonsters(bool isPlayer)
    {
        if (GameManager.Instance == null || GameManager.Instance.duelFieldUI == null) return;
        var zones = isPlayer ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
        foreach(var z in zones)
        {
            if(z.childCount > 0)
            {
                var cd = z.GetComponentInChildren<CardDisplay>();
                if(cd != null) {
                    if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(cd);
                    GameManager.Instance.SendToGraveyard(cd.CurrentCardData, cd.ownerPlayer, CardLocation.Field, 0x400); // REASON_RULE
                    GameManager.Instance.SendToGraveyard(cd.CurrentCardData, cd.ownerPlayer, CardLocation.Field, 0x400); // REASON_RULE
                    Destroy(cd.gameObject);
                }
            }
        }
    }

    public static void ClearSpells(bool isPlayer)
    {
        if (GameManager.Instance == null || GameManager.Instance.duelFieldUI == null) return;
        var zones = isPlayer ? GameManager.Instance.duelFieldUI.playerSpellZones : GameManager.Instance.duelFieldUI.opponentSpellZones;
        foreach(var z in zones)
        {
            if(z.childCount > 0)
            {
                var cd = z.GetComponentInChildren<CardDisplay>();
                if(cd != null) {
                    if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(cd);
                    GameManager.Instance.SendToGraveyard(cd.CurrentCardData, cd.ownerPlayer, CardLocation.Field, 0x400); // REASON_RULE
                    Destroy(cd.gameObject);
                }
            }
        }
        var fz = isPlayer ? GameManager.Instance.duelFieldUI.playerFieldSpell : GameManager.Instance.duelFieldUI.opponentFieldSpell;
        if(fz.childCount > 0)
        {
            var cd = fz.GetComponentInChildren<CardDisplay>();
            if(cd != null) {
                if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(cd);
                GameManager.Instance.SendToGraveyard(cd.CurrentCardData, cd.ownerPlayer, CardLocation.Field, 0x400); // REASON_RULE
                Destroy(cd.gameObject);
            }
        }
    }

    public static void SpawnTestMonster(bool isPlayer)
    {
        if (GameManager.Instance == null) return;
        CardData dummyMonster = GameManager.Instance.cardDatabase.cardDatabase.FirstOrDefault(c => c.name.Contains("Blue-Eyes White Dragon") || (c.type.Contains("Monster") && c.atk >= 2500));
        if (dummyMonster != null) 
        {
            GameManager.Instance.SpecialSummonFromData(JsonUtility.FromJson<CardData>(JsonUtility.ToJson(dummyMonster)), isPlayer, -1, true, false, null, CardLocation.Unknown, isPlayer, 0x40000000);
            Debug.Log($"<color=cyan>➕ [QA] Monstro de Teste injetado para o {(isPlayer ? "Jogador" : "Oponente")}.</color>");
        }
    }

    public static void SpawnTestSpell(bool isPlayer)
    {
        if (GameManager.Instance == null) return;
        CardData dummySpell = GameManager.Instance.cardDatabase.cardDatabase.FirstOrDefault(c => c.type.Contains("Spell") && !c.name.Contains("7") && c.id != "DM0004");
        if (dummySpell != null) 
        {
            Transform zone = GameManager.Instance.GetFreeSpellZone(isPlayer);
            if (zone != null) {
                int zoneIdx = -1;
                var zones = isPlayer ? GameManager.Instance.duelFieldUI.playerSpellZones : GameManager.Instance.duelFieldUI.opponentSpellZones;
                for(int i=0; i<zones.Length; i++) if (zones[i] == zone) { zoneIdx = i; break; }
                
                if (zoneIdx != -1)
                {
                    GameManager.Instance.SetSpellTrapFromData(JsonUtility.FromJson<CardData>(JsonUtility.ToJson(dummySpell)), isPlayer, zoneIdx, false);
                    Debug.Log($"<color=cyan>➕ [QA] Magia Setada injetada para o {(isPlayer ? "Jogador" : "Oponente")}.</color>");
                }
            }
        }
    }

    private static void SetupQABoard(string cardId)
    {
        if (GameManager.Instance == null) return;

        // Limpa miras pendentes para evitar travar a UI
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel != null) {
            CardEffectManager.Instance.luaDuel.currentAttacker = null;
            CardEffectManager.Instance.luaDuel.currentAttackTarget = null;
        }
        if (TargetingSwordUI.Instance != null) TargetingSwordUI.Instance.Hide();
        
        // 1. Preenchimento de Segurança de Decks para QA
        // O GameManager pode ter um playerMainDeck criado no menu, mas o duelo físico (DeckManager) ainda não começou!
        if (DeckManager.Instance != null && GameManager.Instance.cardDatabase != null)
        {
            var pDeck = DeckManager.Instance.GetPlayerDeck();
            var oDeck = DeckManager.Instance.GetOpponentDeck();

            if (pDeck == null || pDeck.Count == 0 || oDeck == null || oDeck.Count == 0)
            {
                var allCards = GameManager.Instance.cardDatabase.cardDatabase.Where(c => !c.type.Contains("Fusion") && !c.type.Contains("Synchro") && !c.type.Contains("Token")).ToList();
                
                if (GameManager.Instance.playerMainDeck == null || GameManager.Instance.playerMainDeck.Count == 0)
                    GameManager.Instance.playerMainDeck = allCards.Take(40).ToList();
                    
                if (GameManager.Instance.opponentMainDeck == null || GameManager.Instance.opponentMainDeck.Count == 0)
                    GameManager.Instance.opponentMainDeck = allCards.Skip(40).Take(40).ToList();

                // Inicializa as pilhas do duelo oficialmente usando as listas criadas
                DeckManager.Instance.SetupDecks(
                    new System.Collections.Generic.List<CardData>(GameManager.Instance.playerMainDeck),
                    new System.Collections.Generic.List<CardData>(),
                    new System.Collections.Generic.List<CardData>(GameManager.Instance.opponentMainDeck),
                    new System.Collections.Generic.List<CardData>()
                );
                
                Debug.Log("<color=green>🔗 [QA] Decks físicos (DeckManager) injetados e embaralhados com sucesso!</color>");
            }
        }

        // 2. Injeta a carta a ser testada na mão do jogador
        CardData testCard = GameManager.Instance.cardDatabase.GetCardById(cardId);
        if (testCard != null) {
            GameManager.Instance.AddCardToHand(JsonUtility.FromJson<CardData>(JsonUtility.ToJson(testCard)), true);
            Debug.Log($"<color=cyan>🧪 [QA] Injetando {testCard.name} ({cardId}) para Homologação.</color>");
            GameManager.Instance.Dev_InjectDependencies(testCard);
        } else Debug.LogError($"[QA] Carta {cardId} não encontrada no banco de dados!");
    }
}