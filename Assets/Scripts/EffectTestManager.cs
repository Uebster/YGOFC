using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class EffectTestManager : MonoBehaviour
{
    // Referências para as cartas de teste (podem ser atribuídas manualmente ou criadas)
    public CardDisplay playerMonster;
    public CardDisplay opponentMonster;
    public CardDisplay playerSpell;
    public CardDisplay playerField;

    [Header("UI Settings")]
    public float windowWidth = 420f; // Ajuste livremente no Inspector!

    private Texture2D darkTex;
    private GUIStyle btnStyle;
    private GUIStyle titleStyle;
    private bool styleInitialized = false;
    private Vector2 scrollPos;
    private bool hideDummyCards = false;
    
    [Header("Testing Context")]
    public bool testAsOpponent = false;

    private int selectedFlightIndex = 0;
    private List<string> flightNames = new List<string>();
    private List<System.Action> flightActions = new List<System.Action>();
    private bool flightsInitialized = false;

    private int selectedExtIndex = 0;
    private List<string> extNames = new List<string>();
    private List<System.Action> extActions = new List<System.Action>();
    private bool extInitialized = false;

    private Rect windowRect = new Rect(20, 20, 400, 650);

    void Update()
    {
        bool toggleMenu = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed) && Keyboard.current.eKey.wasPressedThisFrame)
            toggleMenu = true;
#else
        if (Input.GetKeyDown(KeyCode.E) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            toggleMenu = true;
#endif

        // Abre ou fecha o painel de testes de VFX ao pressionar Ctrl + E
        if (toggleMenu)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.effectTestMode = !GameManager.Instance.effectTestMode;
            }
        }
    }

    void InitStyles()
    {
        if (styleInitialized) return;

        darkTex = new Texture2D(1, 1);
        darkTex.SetPixel(0, 0, new Color(0, 0, 0, 0.85f));
        darkTex.Apply();

        btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontSize = 13;
        btnStyle.fixedHeight = 26;
        btnStyle.margin = new RectOffset(0, 0, 3, 3);

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 15;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.yellow;
        titleStyle.alignment = TextAnchor.MiddleCenter;

        styleInitialized = true;
    }

    void OnGUI()
    {
        // Verifica se o modo de teste está ativo no GameManager
        if (GameManager.Instance == null || !GameManager.Instance.effectTestMode) return;

        InitStyles();
        InitFlights();
        InitExtractions();

        windowRect.width = windowWidth;
        windowRect.height = Screen.height - 40;
        windowRect = GUI.Window(10102, windowRect, DrawEffectTestWindow, "--- TESTE DE EFEITOS VFX ---");
    }

    void DrawEffectTestWindow(int windowID)
    {
        // Fundo e Layout interno
        GUI.DrawTexture(new Rect(0, 0, windowRect.width, windowRect.height), darkTex);
        GUILayout.BeginArea(new Rect(10, 25, windowRect.width - 20, windowRect.height - 35));

        GUILayout.Space(5);
        GUILayout.Label("<color=cyan><b>CONTEXTO DE TESTE</b></color>");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(testAsOpponent ? "[ ] Jogador" : "[X] Jogador", btnStyle)) { testAsOpponent = false; flightsInitialized = false; extInitialized = false; }
        if (GUILayout.Button(testAsOpponent ? "[X] Oponente" : "[ ] Oponente", btnStyle)) { testAsOpponent = true; flightsInitialized = false; extInitialized = false; }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("LIMPAR CENA", btnStyle)) { ClearFieldForTesting(); }

        bool newHide = GUILayout.Toggle(hideDummyCards, " Ocultar Cartas Base (Para ver Markers)");
        if (newHide != hideDummyCards)
        {
            hideDummyCards = newHide;
            if (playerMonster != null) playerMonster.SetVisibility(!hideDummyCards);
            if (opponentMonster != null) opponentMonster.SetVisibility(!hideDummyCards);
            if (playerSpell != null) playerSpell.SetVisibility(!hideDummyCards);
            if (playerField != null) playerField.SetVisibility(!hideDummyCards);
        }

        // Inicia a área de Rolagem (importante para caber todas as opções)
        scrollPos = GUILayout.BeginScrollView(scrollPos, false, true);

        GUILayout.Label("<color=cyan><b>AURAS DE POUSO COLORIDAS</b></color>");
        
        GUILayout.BeginHorizontal();
        bool useNat = DuelFXManager.Instance.usePlacementAuraRoutine;
        if (GUILayout.Button(useNat ? "[X] Nativa" : "[ ] Nativa", btnStyle)) DuelFXManager.Instance.usePlacementAuraRoutine = !useNat;
        bool usePref = DuelFXManager.Instance.usePlacementAuraPrefab;
        if (GUILayout.Button(usePref ? "[X] Prefab" : "[ ] Prefab", btnStyle)) DuelFXManager.Instance.usePlacementAuraPrefab = !usePref;
        GUILayout.EndHorizontal();

        TestAction("Aura: Monstro Normal (Amarelo)", () => TestColoredAura("Monster (Normal)", false));
        TestAction("Aura: Monstro Efeito (Marrom)", () => TestColoredAura("Monster (Effect)", false));
        TestAction("Aura: Monstro Fusão (Roxo)", () => TestColoredAura("Monster (Fusion)", false));
        TestAction("Aura: Monstro Ritual (Azul)", () => TestColoredAura("Monster (Ritual)", false));
        TestAction("Aura: Magia (Verde)", () => TestColoredAura("Spell", false));
        TestAction("Aura: Armadilha (Rosa)", () => TestColoredAura("Trap", false));
        TestAction("Aura: Carta Baixada (Cinza)", () => TestColoredAura("Monster (Normal)", true));

        GUILayout.Space(5);
        GUILayout.Label("<color=cyan><b>INVOCAÇÃO: NORMAL</b></color>");
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Impacto Simples (Nativo)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.normalSummonSettings.impact.useNativePulse = true; DuelFXManager.Instance.normalSummonSettings.impact.usePrefab = false; DuelFXManager.Instance.PlaySummonImpact(playerMonster, SummonVFXType.Normal); 
        }
        if (GUILayout.Button("Impacto Simples (Prefab)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.normalSummonSettings.impact.useNativePulse = false; DuelFXManager.Instance.normalSummonSettings.impact.usePrefab = true; DuelFXManager.Instance.PlaySummonImpact(playerMonster, SummonVFXType.Normal); 
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Space(5);
        GUILayout.Label("<color=cyan><b>INVOCAÇÃO: SPECIAL</b></color>");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Selection Icon (Nativo)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.specialSummonSettings.selectionIcon.useNative = true; DuelFXManager.Instance.specialSummonSettings.selectionIcon.usePrefab = false; DuelFXManager.Instance.SetSelectionIcon(playerMonster, SummonVFXType.Special, SelectionState.Available); }
        if (GUILayout.Button("Selection Icon (Prefab)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.specialSummonSettings.selectionIcon.useNative = false; DuelFXManager.Instance.specialSummonSettings.selectionIcon.usePrefab = true; DuelFXManager.Instance.SetSelectionIcon(playerMonster, SummonVFXType.Special, SelectionState.Available); }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Field Marker (Nativo)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.specialSummonSettings.fieldMarker.useNative = true; DuelFXManager.Instance.specialSummonSettings.fieldMarker.usePrefab = false; DuelFXManager.Instance.SpawnFieldMarker(playerMonster.transform.parent != null ? playerMonster.transform.parent : playerMonster.transform, SummonVFXType.Special); }
        if (GUILayout.Button("Field Marker (Prefab)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.specialSummonSettings.fieldMarker.useNative = false; DuelFXManager.Instance.specialSummonSettings.fieldMarker.usePrefab = true; DuelFXManager.Instance.SpawnFieldMarker(playerMonster.transform.parent != null ? playerMonster.transform.parent : playerMonster.transform, SummonVFXType.Special); }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Impacto (Nativo)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.specialSummonSettings.impact.useNativePulse = true; DuelFXManager.Instance.specialSummonSettings.impact.usePrefab = false; DuelFXManager.Instance.PlaySummonImpact(playerMonster, SummonVFXType.Special); }
        if (GUILayout.Button("Impacto (Prefab)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.specialSummonSettings.impact.useNativePulse = false; DuelFXManager.Instance.specialSummonSettings.impact.usePrefab = true; DuelFXManager.Instance.PlaySummonImpact(playerMonster, SummonVFXType.Special); }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Cinemática Geral", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.PlaySummonCinematic(playerMonster, SummonVFXType.Special, () => DuelFXManager.Instance.PlayPlacementAura(playerMonster)); }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.Label("<color=cyan><b>INVOCAÇÃO: TRIBUTO</b></color>");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Selection Icon (Nativo)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.tributeSummonSettings.selectionIcon.useNative = true; DuelFXManager.Instance.tributeSummonSettings.selectionIcon.usePrefab = false; DuelFXManager.Instance.SetSelectionIcon(playerMonster, SummonVFXType.Tribute, SelectionState.Available); }
        if (GUILayout.Button("Selection Icon (Prefab)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.tributeSummonSettings.selectionIcon.useNative = false; DuelFXManager.Instance.tributeSummonSettings.selectionIcon.usePrefab = true; DuelFXManager.Instance.SetSelectionIcon(playerMonster, SummonVFXType.Tribute, SelectionState.Available); }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Field Marker (Nativo)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.tributeSummonSettings.fieldMarker.useNative = true; DuelFXManager.Instance.tributeSummonSettings.fieldMarker.usePrefab = false; DuelFXManager.Instance.SpawnFieldMarker(playerMonster.transform.parent != null ? playerMonster.transform.parent : playerMonster.transform, SummonVFXType.Tribute); }
        if (GUILayout.Button("Field Marker (Prefab)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.tributeSummonSettings.fieldMarker.useNative = false; DuelFXManager.Instance.tributeSummonSettings.fieldMarker.usePrefab = true; DuelFXManager.Instance.SpawnFieldMarker(playerMonster.transform.parent != null ? playerMonster.transform.parent : playerMonster.transform, SummonVFXType.Tribute); }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Impacto (Nativo)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.tributeSummonSettings.impact.useNativePulse = true; DuelFXManager.Instance.tributeSummonSettings.impact.usePrefab = false; DuelFXManager.Instance.PlaySummonImpact(playerMonster, SummonVFXType.Tribute); }
        if (GUILayout.Button("Impacto (Prefab)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.tributeSummonSettings.impact.useNativePulse = false; DuelFXManager.Instance.tributeSummonSettings.impact.usePrefab = true; DuelFXManager.Instance.PlaySummonImpact(playerMonster, SummonVFXType.Tribute); }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Cinemática Geral", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.PlaySummonCinematic(playerMonster, SummonVFXType.Tribute, () => DuelFXManager.Instance.PlayPlacementAura(playerMonster)); }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.Label("<color=cyan><b>INVOCAÇÃO: RITUAL</b></color>");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Selection Icon (Nativo)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.ritualSummonSettings.selectionIcon.useNative = true; DuelFXManager.Instance.ritualSummonSettings.selectionIcon.usePrefab = false; DuelFXManager.Instance.SetSelectionIcon(playerMonster, SummonVFXType.Ritual, SelectionState.Available); }
        if (GUILayout.Button("Selection Icon (Prefab)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.ritualSummonSettings.selectionIcon.useNative = false; DuelFXManager.Instance.ritualSummonSettings.selectionIcon.usePrefab = true; DuelFXManager.Instance.SetSelectionIcon(playerMonster, SummonVFXType.Ritual, SelectionState.Available); }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Field Marker (Nativo)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.ritualSummonSettings.fieldMarker.useNative = true; DuelFXManager.Instance.ritualSummonSettings.fieldMarker.usePrefab = false; DuelFXManager.Instance.SpawnFieldMarker(playerMonster.transform.parent != null ? playerMonster.transform.parent : playerMonster.transform, SummonVFXType.Ritual); }
        if (GUILayout.Button("Field Marker (Prefab)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.ritualSummonSettings.fieldMarker.useNative = false; DuelFXManager.Instance.ritualSummonSettings.fieldMarker.usePrefab = true; DuelFXManager.Instance.SpawnFieldMarker(playerMonster.transform.parent != null ? playerMonster.transform.parent : playerMonster.transform, SummonVFXType.Ritual); }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Impacto (Nativo)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.ritualSummonSettings.impact.useNativePulse = true; DuelFXManager.Instance.ritualSummonSettings.impact.usePrefab = false; DuelFXManager.Instance.PlaySummonImpact(playerMonster, SummonVFXType.Ritual); }
        if (GUILayout.Button("Impacto (Prefab)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.ritualSummonSettings.impact.useNativePulse = false; DuelFXManager.Instance.ritualSummonSettings.impact.usePrefab = true; DuelFXManager.Instance.PlaySummonImpact(playerMonster, SummonVFXType.Ritual); }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Cinemática Geral", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.PlayRitualCinematic(playerMonster, SummonVFXType.Ritual, () => DuelFXManager.Instance.PlayPlacementAura(playerMonster)); }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.Label("<color=cyan><b>INVOCAÇÃO: FUSÃO</b></color>");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Selection Icon (Nativo)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.fusionSummonSettings.selectionIcon.useNative = true; DuelFXManager.Instance.fusionSummonSettings.selectionIcon.usePrefab = false; DuelFXManager.Instance.SetSelectionIcon(playerMonster, SummonVFXType.Fusion, SelectionState.Available); }
        if (GUILayout.Button("Selection Icon (Prefab)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.fusionSummonSettings.selectionIcon.useNative = false; DuelFXManager.Instance.fusionSummonSettings.selectionIcon.usePrefab = true; DuelFXManager.Instance.SetSelectionIcon(playerMonster, SummonVFXType.Fusion, SelectionState.Available); }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Field Marker (Nativo)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.fusionSummonSettings.fieldMarker.useNative = true; DuelFXManager.Instance.fusionSummonSettings.fieldMarker.usePrefab = false; DuelFXManager.Instance.SpawnFieldMarker(playerMonster.transform.parent != null ? playerMonster.transform.parent : playerMonster.transform, SummonVFXType.Fusion); }
        if (GUILayout.Button("Field Marker (Prefab)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.fusionSummonSettings.fieldMarker.useNative = false; DuelFXManager.Instance.fusionSummonSettings.fieldMarker.usePrefab = true; DuelFXManager.Instance.SpawnFieldMarker(playerMonster.transform.parent != null ? playerMonster.transform.parent : playerMonster.transform, SummonVFXType.Fusion); }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Impacto (Nativo)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.fusionSummonSettings.impact.useNativePulse = true; DuelFXManager.Instance.fusionSummonSettings.impact.usePrefab = false; DuelFXManager.Instance.PlaySummonImpact(playerMonster, SummonVFXType.Fusion); }
        if (GUILayout.Button("Impacto (Prefab)", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.fusionSummonSettings.impact.useNativePulse = false; DuelFXManager.Instance.fusionSummonSettings.impact.usePrefab = true; DuelFXManager.Instance.PlaySummonImpact(playerMonster, SummonVFXType.Fusion); }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Cinemática Geral", btnStyle)) { ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.PlayFusionCinematic(playerMonster, GetFakeMaterials(), SummonVFXType.Fusion, () => DuelFXManager.Instance.PlayPlacementAura(playerMonster)); }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.Label("<color=cyan><b>EXTRAÇÃO (POP-OUT CINEMÁTICO)</b></color>");
        GUILayout.BeginHorizontal();
        bool useExt = DuelFXManager.Instance.extractionCinematic.enableExtraction;
        if (GUILayout.Button(useExt ? "[X] Ativado" : "[ ] Ativado", btnStyle)) DuelFXManager.Instance.extractionCinematic.enableExtraction = !useExt;
        bool useScale = DuelFXManager.Instance.extractionCinematic.useScaleEffect;
        if (GUILayout.Button(useScale ? "[X] Scale (Pulse)" : "[ ] Scale (Pulse)", btnStyle)) DuelFXManager.Instance.extractionCinematic.useScaleEffect = !useScale;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<", btnStyle, GUILayout.Width(35))) { selectedExtIndex--; if(selectedExtIndex < 0) selectedExtIndex = extNames.Count - 1; }
        GUILayout.Label(extNames[selectedExtIndex], titleStyle, GUILayout.Height(26));
        if (GUILayout.Button(">", btnStyle, GUILayout.Width(35))) { selectedExtIndex++; if(selectedExtIndex >= extNames.Count) selectedExtIndex = 0; }
        GUILayout.EndHorizontal();
        
        if (GUILayout.Button("► INICIAR EXTRAÇÃO", btnStyle)) { 
            extActions[selectedExtIndex]?.Invoke(); 
        }

        GUILayout.Space(5);
        GUILayout.Label("<color=cyan><b>VOO DE CARTAS (FLIGHTS)</b></color>");
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<", btnStyle, GUILayout.Width(35))) { selectedFlightIndex--; if(selectedFlightIndex < 0) selectedFlightIndex = flightNames.Count - 1; }
        GUILayout.Label(flightNames[selectedFlightIndex], titleStyle, GUILayout.Height(26));
        if (GUILayout.Button(">", btnStyle, GUILayout.Width(35))) { selectedFlightIndex++; if(selectedFlightIndex >= flightNames.Count) selectedFlightIndex = 0; }
        GUILayout.EndHorizontal();
        
        if (GUILayout.Button("► INICIAR ANIMAÇÃO", btnStyle)) { 
            flightActions[selectedFlightIndex]?.Invoke(); 
        }

        GUILayout.Label("<color=cyan><b>TOKEN E INVOCAÇÕES EXTRAS</b></color>");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Token (Nativo)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.useTokenSummonRoutine = true; DuelFXManager.Instance.useTokenSummonPrefab = false; DuelFXManager.Instance.PlayTokenSummonEffect(playerMonster); 
        }
        if (GUILayout.Button("Token (Prefab)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.useTokenSummonRoutine = false; DuelFXManager.Instance.useTokenSummonPrefab = true; DuelFXManager.Instance.PlayTokenSummonEffect(playerMonster); 
        }
        GUILayout.EndHorizontal();

        // --- OUTRAS AÇÕES ---
        GUILayout.Space(5);
        GUILayout.Label("<color=cyan><b>AÇÕES DE CARTA</b></color>");
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Flip (Nativo Pulse)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); 
            DuelFXManager.Instance.useFlipRoutine = true; 
            DuelFXManager.Instance.useFlipPrefab = false; 
            DuelFXManager.Instance.PlayFlipEffect(playerMonster); 
        }
        if (GUILayout.Button("Flip (Prefab)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); 
            DuelFXManager.Instance.useFlipRoutine = false; 
            DuelFXManager.Instance.useFlipPrefab = true; 
            DuelFXManager.Instance.PlayFlipEffect(playerMonster); 
        }
        GUILayout.EndHorizontal();

        TestAction("Virar Carta (Flip 3D/2D)", () => { EnsurePlayerMonster(); playerMonster.FlipCard(); }, false);
        TestAction("Mudar Posição (Atk <-> Def)", () => { EnsurePlayerMonster(); playerMonster.ChangePosition(); }, false);
                 
        GUILayout.Space(5);
        GUILayout.Label("<color=cyan><b>MAGIAS E EFEITOS</b></color>");
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Chain Link (Nativo)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerSpell(); 
            DuelFXManager.Instance.useChainLinkRoutine = true; 
            DuelFXManager.Instance.useChainLinkPrefab = false; 
            DuelFXManager.Instance.PlayChainLinkEffect(playerSpell, 2); 
        }
        if (GUILayout.Button("Chain Link (Prefab)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerSpell(); 
            DuelFXManager.Instance.useChainLinkRoutine = false; 
            DuelFXManager.Instance.useChainLinkPrefab = true; 
            DuelFXManager.Instance.PlayChainLinkEffect(playerSpell, 2); 
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Monstro Efeito (Nativo)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); 
            DuelFXManager.Instance.useMonsterEffectPulse = true; 
            DuelFXManager.Instance.useMonsterEffectPrefab = false; 
            DuelFXManager.Instance.PlayMonsterEffect(playerMonster); 
        }
        if (GUILayout.Button("Monstro Efeito (Prefab)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); 
            DuelFXManager.Instance.useMonsterEffectPulse = false; 
            DuelFXManager.Instance.useMonsterEffectPrefab = true; 
            DuelFXManager.Instance.PlayMonsterEffect(playerMonster); 
        }
        GUILayout.EndHorizontal();

        TestAction("Ativar Magia (Spell)", () => { EnsurePlayerSpell(); DuelFXManager.Instance.PlayCardActivation(playerSpell, false); });
        TestAction("Ativar Armadilha (Trap)", () => { EnsurePlayerSpell(); DuelFXManager.Instance.PlayCardActivation(playerSpell, true); });
        TestAction("Ativar Field Spell", () => { EnsurePlayerField(); DuelFXManager.Instance.PlayCardActivation(playerField, false); });
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Equipar (Ghost/Squeeze)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerSpell(); EnsurePlayerMonster(); 
            DuelFXManager.Instance.useEquipGhost = true; 
            DuelFXManager.Instance.useEquipPrefab = false; 
            DuelFXManager.Instance.PlayEquipEffect(playerSpell, playerMonster); 
        }
        if (GUILayout.Button("Equipar (Prefab)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerSpell(); EnsurePlayerMonster(); 
            DuelFXManager.Instance.useEquipGhost = false; 
            DuelFXManager.Instance.useEquipPrefab = true; 
            DuelFXManager.Instance.PlayEquipEffect(playerSpell, playerMonster); 
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Sacrifício GY (Nativo)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster();
            DuelFXManager.Instance.PlayTributeEffect(playerMonster); 
        }
        if (GUILayout.Button("Selecionado (Nativo)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster();
            DuelFXManager.Instance.tributeSummonSettings.selectionIcon.useNative = true;
            DuelFXManager.Instance.tributeSummonSettings.selectionIcon.usePrefab = false;
            DuelFXManager.Instance.SetSelectionIcon(playerMonster, SummonVFXType.Tribute, SelectionState.Selected); 
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.Label("<color=cyan><b>BATALHA E REMOÇÃO</b></color>");
        
        GUILayout.Label("<size=12><i>Ataque (Voo da Espada):</i></size>");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Voo Nativo + Shadows", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); EnsureOpponentMonster(); 
            DuelFXManager.Instance.flightSwordIndicator.useNative = true; 
            DuelFXManager.Instance.flightSwordIndicator.usePrefab = false; 
            DuelFXManager.Instance.useAttackTrail = true;
            DuelFXManager.Instance.attackTrailType = AttackTrailType.Shadows; 
            DuelFXManager.Instance.PlayAttack(playerMonster, opponentMonster, null); 
        }
        if (GUILayout.Button("Voo Nativo + Contínuo", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); EnsureOpponentMonster(); 
            DuelFXManager.Instance.flightSwordIndicator.useNative = true; 
            DuelFXManager.Instance.flightSwordIndicator.usePrefab = false; 
            DuelFXManager.Instance.useAttackTrail = true;
            DuelFXManager.Instance.attackTrailType = AttackTrailType.ContinuousLine; 
            DuelFXManager.Instance.PlayAttack(playerMonster, opponentMonster, null); 
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Voo Prefab + Shadows", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); EnsureOpponentMonster(); 
            DuelFXManager.Instance.flightSwordIndicator.useNative = false; 
            DuelFXManager.Instance.flightSwordIndicator.usePrefab = true; 
            DuelFXManager.Instance.useAttackTrail = true;
            DuelFXManager.Instance.attackTrailType = AttackTrailType.Shadows; 
            DuelFXManager.Instance.PlayAttack(playerMonster, opponentMonster, null); 
        }
        if (GUILayout.Button("Voo Prefab (Sem Rastro)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); EnsureOpponentMonster(); 
            DuelFXManager.Instance.flightSwordIndicator.useNative = false; 
            DuelFXManager.Instance.flightSwordIndicator.usePrefab = true; 
            DuelFXManager.Instance.useAttackTrail = false;
            DuelFXManager.Instance.PlayAttack(playerMonster, opponentMonster, null); 
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Space(2);
        GUILayout.Label("<size=12><i>Troca de Controle (Change of Heart):</i></size>");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Squeeze + Shadows", btnStyle)) { 
            ClearFieldForTesting(); EnsureOpponentMonster(); 
            DuelFXManager.Instance.controlSwapImpactType = ControlSwapImpactType.Squeeze; 
            DuelFXManager.Instance.controlSwapTrailType = AttackTrailType.Shadows; 
            GameManager.Instance.SwitchControl(opponentMonster); 
        }
        if (GUILayout.Button("Pulse + Contínuo", btnStyle)) { 
            ClearFieldForTesting(); EnsureOpponentMonster(); 
            DuelFXManager.Instance.controlSwapImpactType = ControlSwapImpactType.Pulse; 
            DuelFXManager.Instance.controlSwapTrailType = AttackTrailType.ContinuousLine; 
            GameManager.Instance.SwitchControl(opponentMonster); 
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Hit/Corte (Nativo)", btnStyle)) { 
            ClearFieldForTesting(); EnsureOpponentMonster(); DuelFXManager.Instance.useAttackImpactRoutine = true; DuelFXManager.Instance.useAttackImpactPrefab = false; DuelFXManager.Instance.PlayHitEffect(opponentMonster); 
        }
        if (GUILayout.Button("Hit/Corte (Prefab)", btnStyle)) { 
            ClearFieldForTesting(); EnsureOpponentMonster(); DuelFXManager.Instance.useAttackImpactRoutine = false; DuelFXManager.Instance.useAttackImpactPrefab = true; DuelFXManager.Instance.PlayHitEffect(opponentMonster); 
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Defesa/Block (Nativo)", btnStyle)) { 
            ClearFieldForTesting(); EnsureOpponentMonster(); DuelFXManager.Instance.useDefenseRoutine = true; DuelFXManager.Instance.useNativeDefenseShield = true; DuelFXManager.Instance.useDefensePrefab = false; DuelFXManager.Instance.PlayDefenseSuccessEffect(opponentMonster); 
        }
        if (GUILayout.Button("Defesa/Block (Prefab)", btnStyle)) { 
            ClearFieldForTesting(); EnsureOpponentMonster(); DuelFXManager.Instance.useDefenseRoutine = false; DuelFXManager.Instance.useDefensePrefab = true; DuelFXManager.Instance.PlayDefenseSuccessEffect(opponentMonster); 
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Ricochete/Reflect (Nativo)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.useReflectAsStandardDamage = false; DuelFXManager.Instance.useReflectRoutine = true; DuelFXManager.Instance.useReflectPrefab = false; DuelFXManager.Instance.PlayAttackFail(playerMonster); 
        }
        if (GUILayout.Button("Ricochete/Reflect (Prefab)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); DuelFXManager.Instance.useReflectRoutine = false; DuelFXManager.Instance.useReflectPrefab = true; DuelFXManager.Instance.PlayAttackFail(playerMonster); 
        }
        GUILayout.EndHorizontal();

        
        GUILayout.Space(5);
        GUILayout.Label("<color=cyan><b>DESTRUIÇÃO E REMOÇÃO</b></color>");
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Explosão (Nativo)", btnStyle)) { 
            ClearFieldForTesting(); EnsureOpponentMonster(); DuelFXManager.Instance.useDestructionRoutine = true; DuelFXManager.Instance.useExplosionPrefab = false; DuelFXManager.Instance.PlayDestruction(opponentMonster); 
        }
        if (GUILayout.Button("Explosão (Prefab)", btnStyle)) { 
            ClearFieldForTesting(); EnsureOpponentMonster(); DuelFXManager.Instance.useDestructionRoutine = false; DuelFXManager.Instance.useExplosionPrefab = true; DuelFXManager.Instance.PlayDestruction(opponentMonster); 
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Banish (Nativo)", btnStyle)) { 
            ClearFieldForTesting(); EnsureOpponentMonster(); DuelFXManager.Instance.useBanishRoutine = true; DuelFXManager.Instance.useBanishPrefab = false; DuelFXManager.Instance.PlayBanishEffect(opponentMonster); 
        }
        if (GUILayout.Button("Banish (Prefab)", btnStyle)) { 
            ClearFieldForTesting(); EnsureOpponentMonster(); DuelFXManager.Instance.useBanishRoutine = false; DuelFXManager.Instance.useBanishPrefab = true; DuelFXManager.Instance.PlayBanishEffect(opponentMonster); 
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.Label("<color=cyan><b>INDICADORES DE STATUS (UI)</b></color>");
        
        GUILayout.Label("<size=12><i>Pode Atacar (Can Attack):</i></size>");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Nativo", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); 
            DuelFXManager.Instance.canAttackIndicator.useNative = true;
            DuelFXManager.Instance.canAttackIndicator.usePrefab = false;
            if (DuelFXManager.Instance != null) DuelFXManager.Instance.SetCanAttackIndicator(playerMonster, true);
        }
        if (GUILayout.Button("Prefab", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); 
            DuelFXManager.Instance.canAttackIndicator.useNative = false;
            DuelFXManager.Instance.canAttackIndicator.usePrefab = true;
            if (DuelFXManager.Instance != null) DuelFXManager.Instance.SetCanAttackIndicator(playerMonster, true);
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("<size=12><i>Mira de Alvo (Targeting Sword):</i></size>");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Nativa", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); EnsureOpponentMonster();
            DuelFXManager.Instance.useTargetingSwordNative = true;
            DuelFXManager.Instance.targetingSwordIndicator.useNative = true;
            DuelFXManager.Instance.targetingSwordIndicator.usePrefab = false;
            if (DuelFXManager.Instance != null) DuelFXManager.Instance.SetCanAttackIndicator(playerMonster, true);
            StartCoroutine(DelaySetAttacker(playerMonster));
            if (TargetingSwordUI.Instance != null) TargetingSwordUI.Instance.ShowAndFollowMouse(playerMonster.transform);
        }
        if (GUILayout.Button("Prefab", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); EnsureOpponentMonster();
            DuelFXManager.Instance.useTargetingSwordNative = true;
            DuelFXManager.Instance.targetingSwordIndicator.useNative = false;
            DuelFXManager.Instance.targetingSwordIndicator.usePrefab = true;
            if (DuelFXManager.Instance != null) DuelFXManager.Instance.SetCanAttackIndicator(playerMonster, true);
            StartCoroutine(DelaySetAttacker(playerMonster));
            if (TargetingSwordUI.Instance != null) TargetingSwordUI.Instance.ShowAndFollowMouse(playerMonster.transform);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.Label("<color=cyan><b>FEEDBACK VISUAL TÁTIL</b></color>");
        
        GUILayout.Label("<size=12><i>Hover de Ativação (Main Phase e Corrente):</i></size>");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Configurar: Nativo (Sprite)", btnStyle)) { 
            DuelFXManager.Instance.effectActivationIcon.useNative = true;
            DuelFXManager.Instance.effectActivationIcon.usePrefab = false;
            Debug.Log("[Teste] Configurado para Sprite Nativo. Passe o mouse sobre a carta para ver o balão.");
        }
        if (GUILayout.Button("Configurar: Prefab (Balão 3D)", btnStyle)) { 
            DuelFXManager.Instance.effectActivationIcon.useNative = false;
            DuelFXManager.Instance.effectActivationIcon.usePrefab = true;
            Debug.Log("[Teste] Configurado para Prefab. Passe o mouse sobre a carta para ver o balão.");
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("<size=12><i>Bloqueios (Blocks):</i></size>");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Cannot Attack", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); 
            DuelFXManager.Instance.SetCannotAttackIndicator(playerMonster, true); 
        }
        if (GUILayout.Button("Testar Cannot Change Pos", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); 
            DuelFXManager.Instance.SetCannotChangePosIndicator(playerMonster, true); 
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.Label("<color=cyan><b>GLOBAIS</b></color>");

        TestAction("Tremor de Dano na Tela", () => DuelFXManager.Instance.PlayDamageEffect(Vector3.zero));
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Dano Números (-1000)", btnStyle)) { 
            if (DamagePopupManager.Instance != null) DamagePopupManager.Instance.ShowPopup(1000, false, true); 
        }
        if (GUILayout.Button("Cura Números (+500)", btnStyle)) { 
            if (DamagePopupManager.Instance != null) DamagePopupManager.Instance.ShowPopup(500, true, true); 
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Shuffle (3D Nativo)", btnStyle)) { 
            DuelFXManager.Instance.useShuffleRoutine = true; 
            DuelFXManager.Instance.useSimple2DShuffle = false; 
            DuelFXManager.Instance.useCustomHinduShuffle = false;
            DuelFXManager.Instance.useShufflePrefab = false; 
            if (GameManager.Instance.duelFieldUI != null) {
                if (GameManager.Instance.duelFieldUI.playerDeck != null) DuelFXManager.Instance.PlayShuffleEffect(GameManager.Instance.duelFieldUI.playerDeck);
                if (GameManager.Instance.duelFieldUI.opponentDeck != null) DuelFXManager.Instance.PlayShuffleEffect(GameManager.Instance.duelFieldUI.opponentDeck);
            }
        }
        if (GUILayout.Button("Shuffle (2D Lateral)", btnStyle)) { 
            DuelFXManager.Instance.useShuffleRoutine = true; 
            DuelFXManager.Instance.useSimple2DShuffle = true; 
            DuelFXManager.Instance.useCustomHinduShuffle = false;
            DuelFXManager.Instance.useShufflePrefab = false; 
            if (GameManager.Instance.duelFieldUI != null) {
                if (GameManager.Instance.duelFieldUI.playerDeck != null) DuelFXManager.Instance.PlayShuffleEffect(GameManager.Instance.duelFieldUI.playerDeck);
                if (GameManager.Instance.duelFieldUI.opponentDeck != null) DuelFXManager.Instance.PlayShuffleEffect(GameManager.Instance.duelFieldUI.opponentDeck);
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Shuffle (Hindu Custom)", btnStyle)) { 
            DuelFXManager.Instance.useShuffleRoutine = true; 
            DuelFXManager.Instance.useSimple2DShuffle = false; 
            DuelFXManager.Instance.useCustomHinduShuffle = true;
            DuelFXManager.Instance.useShufflePrefab = false; 
            if (GameManager.Instance.duelFieldUI != null) {
                if (GameManager.Instance.duelFieldUI.playerDeck != null) DuelFXManager.Instance.PlayShuffleEffect(GameManager.Instance.duelFieldUI.playerDeck);
                if (GameManager.Instance.duelFieldUI.opponentDeck != null) DuelFXManager.Instance.PlayShuffleEffect(GameManager.Instance.duelFieldUI.opponentDeck);
            }
        }
        if (GUILayout.Button("Shuffle (Prefab)", btnStyle)) { 
            DuelFXManager.Instance.useShuffleRoutine = false; 
            DuelFXManager.Instance.useShufflePrefab = true; 
            if (GameManager.Instance.duelFieldUI != null) {
                if (GameManager.Instance.duelFieldUI.playerDeck != null) DuelFXManager.Instance.PlayShuffleEffect(GameManager.Instance.duelFieldUI.playerDeck);
                if (GameManager.Instance.duelFieldUI.opponentDeck != null) DuelFXManager.Instance.PlayShuffleEffect(GameManager.Instance.duelFieldUI.opponentDeck);
            }
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Hand Shuffle (Cross)", btnStyle)) { 
            EnsureHandCards();
            DuelFXManager.Instance.useHandShuffleAnimation = true; 
            DuelFXManager.Instance.handShuffleType = HandShuffleType.CrossSwap; 
            GameManager.Instance.ShuffleHand(true); 
        }
        if (GUILayout.Button("Hand Shuffle (Collapse)", btnStyle)) { 
            EnsureHandCards();
            DuelFXManager.Instance.useHandShuffleAnimation = true; 
            DuelFXManager.Instance.handShuffleType = HandShuffleType.CollapseAndFan; 
            GameManager.Instance.ShuffleHand(true); 
        }
        GUILayout.EndHorizontal();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
        
        GUI.DragWindow(new Rect(0, 0, 10000, 30)); // Torna a janela arrastável pelo topo
    }

    // Wrapper para executar uma ação de teste, limpando o campo antes.
    private void TestAction(string buttonText, System.Action testAction, bool clearField = true)
    {
        if (GUILayout.Button(buttonText, btnStyle))
        {
            if (clearField) ClearFieldForTesting();
            testAction?.Invoke();
        }
    }

    private void TestColoredAura(string mockType, bool isFaceDown)
    {
        EnsurePlayerMonster();
        string originalType = playerMonster.CurrentCardData.type;
        bool originalFlipped = playerMonster.isFlipped;

        playerMonster.CurrentCardData.type = mockType;
        playerMonster.isFlipped = isFaceDown;

        DuelFXManager.Instance.PlayPlacementAura(playerMonster);

        playerMonster.CurrentCardData.type = originalType;
        playerMonster.isFlipped = originalFlipped;
    }

    void ClearFieldForTesting()
    {
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel != null) {
            CardEffectManager.Instance.luaDuel.currentAttacker = null;
        }

        if (TargetingSwordUI.Instance != null) TargetingSwordUI.Instance.Hide();

        // Destrói os GameObjects e anula as referências
        if (playerMonster != null && playerMonster.gameObject != null) Destroy(playerMonster.gameObject);
        if (opponentMonster != null && opponentMonster.gameObject != null) Destroy(opponentMonster.gameObject);
        if (playerSpell != null && playerSpell.gameObject != null) Destroy(playerSpell.gameObject);
        if (playerField != null && playerField.gameObject != null) Destroy(playerField.gameObject);

        playerMonster = null;
        opponentMonster = null;
        playerSpell = null;
        playerField = null;
    }

    // Helpers de auto-geração para facilitar o teste em um clique
    void EnsurePlayerMonster() { if (playerMonster == null || playerMonster.gameObject == null) playerMonster = CreateDummyCard(true, "0001", true); }
    void EnsureOpponentMonster() { if (opponentMonster == null || opponentMonster.gameObject == null) opponentMonster = CreateDummyCard(false, "0002", true); }
    void EnsurePlayerSpell() { if (playerSpell == null || playerSpell.gameObject == null) playerSpell = CreateDummyCard(true, "0336", false); }
    void EnsurePlayerField() { if (playerField == null || playerField.gameObject == null) playerField = CreateDummyCard(true, "0013", false, true); }

    void EnsureContextMonster(bool isPlayer)
    {
        if (isPlayer) EnsurePlayerMonster();
        else EnsureOpponentMonster();
    }

    CardDisplay CreateDummyCard(bool isPlayer, string cardId, bool isMonster, bool isField = false)
    {
        if (GameManager.Instance.duelFieldUI == null) return null;

        Transform zone = null;
        if (isMonster)
            zone = isPlayer ? GameManager.Instance.duelFieldUI.playerMonsterZones[2] : GameManager.Instance.duelFieldUI.opponentMonsterZones[2];
        else if (isField)
            zone = isPlayer ? GameManager.Instance.duelFieldUI.playerFieldSpell : GameManager.Instance.duelFieldUI.opponentFieldSpell;
        else
            zone = isPlayer ? GameManager.Instance.duelFieldUI.playerSpellZones[2] : GameManager.Instance.duelFieldUI.opponentSpellZones[2];

        // Limpa a zona se tiver algo
        if (zone.childCount > 0) Destroy(zone.GetChild(0).gameObject);

        GameObject cardGO = Instantiate(GameManager.Instance.cardPrefab, zone);
        CardDisplay display = cardGO.GetComponent<CardDisplay>();
        
        CardData data = GameManager.Instance.cardDatabase.GetCardById(cardId);
        if (data == null && GameManager.Instance.cardDatabase.cardDatabase.Count > 0) 
            data = GameManager.Instance.cardDatabase.cardDatabase[0]; // Fallback

        // Força a propriedade Field se for na Field Zone para o VFX de cinemática reconhecer
        if (isField && data != null) data.property = "Field";

        display.isPlayerCard = isPlayer;
        display.isOnField = true;
        
        // Precisamos da textura do verso
        Texture2D backTex = GameManager.Instance.GetCardBackTexture();
        
        display.SetCard(data, backTex, true);
        
        // Oculta a carta se o toggle estiver ativo (útil para testar Field Markers e Auras)
        if (hideDummyCards) display.SetVisibility(false);

        cardGO.transform.localPosition = Vector3.zero;
        cardGO.transform.localScale = GameManager.Instance.fieldCardScale;
        
        if (!isPlayer)
        {
            cardGO.transform.localRotation = Quaternion.Euler(0, 0, 180);
        }

        return display;
    }

    void EnsureHandCards()
    {
        if (GameManager.Instance.playerHand.Count < 3)
        {
            for (int i = GameManager.Instance.playerHand.Count; i < 5; i++)
            {
                GameManager.Instance.AddCardToHand(GameManager.Instance.cardDatabase.cardDatabase[i], true);
            }
        }
    }

    // Helper para extrair 3 cartas do Banco de Dados e simular os Materiais no vórtice da Fusão
    private List<CardData> GetFakeMaterials()
    {
        List<CardData> mats = new List<CardData>();
        if (GameManager.Instance != null && GameManager.Instance.cardDatabase != null && GameManager.Instance.cardDatabase.cardDatabase.Count > 2)
        {
            mats.Add(GameManager.Instance.cardDatabase.cardDatabase[0]);
            mats.Add(GameManager.Instance.cardDatabase.cardDatabase[1]);
            mats.Add(GameManager.Instance.cardDatabase.cardDatabase[2]);
        }
        return mats;
    }

    private IEnumerator DelaySetAttacker(CardDisplay attacker)
    {
        // Espera um frame para o clique do mouse não ativar o Tabuleiro e golpear na mesma hora
        yield return new WaitForEndOfFrame();
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel != null) {
            CardEffectManager.Instance.luaDuel.currentAttacker = new LuaCard(attacker);
            attacker.SetAttackSelectionVisual(true);
            if (GameManager.Instance != null) { GameManager.Instance.RefreshAttackIndicators(); GameManager.Instance.HandleAttackIndicatorHover(attacker, true); }
        }
    }

    private void InitFlights()
    {
        if (flightsInitialized) return;
        flightNames.Clear();
        flightActions.Clear();
        bool p = !testAsOpponent;
        
        AddFlight("Hand -> Field (Attack)", () => { ClearFieldForTesting(); EnsureContextMonster(p); var m = p ? playerMonster : opponentMonster; Vector3 sPos = p ? GameManager.Instance.playerHandLayoutGroup.position : GameManager.Instance.opponentHandLayoutGroup.position; DuelFXManager.Instance.PlayCardFlight(m.CurrentCardData, GameManager.Instance.GetCardBackTexture(), true, true, sPos, m.transform.position, GameManager.Instance.handCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightHandToField, false, null); });
        AddFlight("Hand -> Field (Set)", () => { ClearFieldForTesting(); EnsureContextMonster(p); var m = p ? playerMonster : opponentMonster; Vector3 sPos = p ? GameManager.Instance.playerHandLayoutGroup.position : GameManager.Instance.opponentHandLayoutGroup.position; DuelFXManager.Instance.PlayCardFlight(m.CurrentCardData, GameManager.Instance.GetCardBackTexture(), true, false, sPos, m.transform.position, GameManager.Instance.handCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.Euler(0,0,90), DuelFXManager.Instance.flightHandToField, false, null); });
        AddFlight("Hand -> Deck", () => { Vector3 sPos = p ? GameManager.Instance.playerHandLayoutGroup.position : GameManager.Instance.opponentHandLayoutGroup.position; Vector3 ePos = p ? GameManager.Instance.playerDeckDisplay.transform.position : GameManager.Instance.opponentDeckDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), true, false, sPos, ePos, GameManager.Instance.handCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightHandToDeck, false, null); });
        AddFlight("Hand -> GY (Discard)", () => { Vector3 sPos = p ? GameManager.Instance.playerHandLayoutGroup.position : GameManager.Instance.opponentHandLayoutGroup.position; Vector3 ePos = p ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), true, true, sPos, ePos, GameManager.Instance.handCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightHandToGraveyard, false, null); });
        AddFlight("Hand -> Banish", () => { Vector3 sPos = p ? GameManager.Instance.playerHandLayoutGroup.position : GameManager.Instance.opponentHandLayoutGroup.position; Vector3 ePos = p ? GameManager.Instance.playerRemovedDisplay.transform.position : GameManager.Instance.opponentRemovedDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), true, true, sPos, ePos, GameManager.Instance.handCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightHandToBanished, false, null); });
        AddFlight("Hand -> Comparison (UI)", () => { Vector3 sPos = GameManager.Instance.playerHandLayoutGroup.position; Vector3 ePos = new Vector3(Screen.width / 3f, Screen.height / 2f, 0); DuelFXManager.Instance.PlayCardFlight(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), true, true, sPos, ePos, GameManager.Instance.handCardScale, GameManager.Instance.fieldCardScale * 1.5f, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightCardComparisonUI, false, null); });
        
        AddFlight("Hand -> SpellZone", () => { ClearFieldForTesting(); EnsurePlayerSpell(); Vector3 sPos = GameManager.Instance.playerHandLayoutGroup.position; DuelFXManager.Instance.PlayCardFlight(playerSpell.CurrentCardData, GameManager.Instance.GetCardBackTexture(), true, true, sPos, playerSpell.transform.position, GameManager.Instance.handCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightHandToSpellZone, false, null); });
        AddFlight("Hand -> SpellZone (Set)", () => { ClearFieldForTesting(); EnsurePlayerSpell(); Vector3 sPos = GameManager.Instance.playerHandLayoutGroup.position; DuelFXManager.Instance.PlayCardFlight(playerSpell.CurrentCardData, GameManager.Instance.GetCardBackTexture(), true, false, sPos, playerSpell.transform.position, GameManager.Instance.handCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightHandToSpellZone, false, null); });
        AddFlight("Hand -> FieldSpellZone", () => { ClearFieldForTesting(); EnsurePlayerField(); Vector3 sPos = GameManager.Instance.playerHandLayoutGroup.position; DuelFXManager.Instance.PlayCardFlight(playerField.CurrentCardData, GameManager.Instance.GetCardBackTexture(), true, true, sPos, playerField.transform.position, GameManager.Instance.handCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightHandToFieldSpellZone, false, null); });
        AddFlight("Hand -> FieldSpellZone (Set)", () => { ClearFieldForTesting(); EnsurePlayerField(); Vector3 sPos = GameManager.Instance.playerHandLayoutGroup.position; DuelFXManager.Instance.PlayCardFlight(playerField.CurrentCardData, GameManager.Instance.GetCardBackTexture(), true, false, sPos, playerField.transform.position, GameManager.Instance.handCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightHandToFieldSpellZone, false, null); });
        
        AddFlight("Field -> Hand (Bounce)", () => { ClearFieldForTesting(); EnsureContextMonster(p); var m = p ? playerMonster : opponentMonster; Vector3 ePos = p ? GameManager.Instance.playerHandLayoutGroup.position : GameManager.Instance.opponentHandLayoutGroup.position; DuelFXManager.Instance.PlayCardFlight(m.CurrentCardData, GameManager.Instance.GetCardBackTexture(), true, true, m.transform.position, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.handCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightFieldToHand, false, null); });
        AddFlight("Field -> Deck (Spin)", () => { ClearFieldForTesting(); EnsureContextMonster(p); var m = p ? playerMonster : opponentMonster; Vector3 ePos = p ? GameManager.Instance.playerDeckDisplay.transform.position : GameManager.Instance.opponentDeckDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(m.CurrentCardData, GameManager.Instance.GetCardBackTexture(), true, false, m.transform.position, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightFieldToDeck, false, null); });
        AddFlight("Field -> GY (Destroy)", () => { ClearFieldForTesting(); EnsureContextMonster(p); var m = p ? playerMonster : opponentMonster; Vector3 ePos = p ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(m.CurrentCardData, GameManager.Instance.GetCardBackTexture(), true, true, m.transform.position, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightFieldToGraveyard, false, null); });
        AddFlight("Field -> Banish", () => { ClearFieldForTesting(); EnsureContextMonster(p); var m = p ? playerMonster : opponentMonster; Vector3 ePos = p ? GameManager.Instance.playerRemovedDisplay.transform.position : GameManager.Instance.opponentRemovedDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(m.CurrentCardData, GameManager.Instance.GetCardBackTexture(), true, true, m.transform.position, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightFieldToBanished, false, null); });
        AddFlight("Field -> Extra Deck", () => { ClearFieldForTesting(); EnsureContextMonster(p); var m = p ? playerMonster : opponentMonster; Vector3 ePos = p ? GameManager.Instance.playerExtraDeckDisplay.transform.position : GameManager.Instance.opponentExtraDeckDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(m.CurrentCardData, GameManager.Instance.GetCardBackTexture(), true, false, m.transform.position, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightFieldToExtraDeck, false, null); });
        
        AddFlight("FieldSpellZone -> Hand", () => { ClearFieldForTesting(); EnsurePlayerField(); Vector3 ePos = GameManager.Instance.playerHandLayoutGroup.position; DuelFXManager.Instance.PlayCardFlight(playerField.CurrentCardData, GameManager.Instance.GetCardBackTexture(), true, true, playerField.transform.position, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.handCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightFieldSpellZoneToHand, false, null); });
        AddFlight("FieldSpellZone -> Deck", () => { ClearFieldForTesting(); EnsurePlayerField(); Vector3 ePos = GameManager.Instance.playerDeckDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(playerField.CurrentCardData, GameManager.Instance.GetCardBackTexture(), true, false, playerField.transform.position, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightFieldSpellZoneToDeck, false, null); });
        AddFlight("FieldSpellZone -> GY", () => { ClearFieldForTesting(); EnsurePlayerField(); Vector3 ePos = GameManager.Instance.playerGraveyardDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(playerField.CurrentCardData, GameManager.Instance.GetCardBackTexture(), true, true, playerField.transform.position, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightFieldSpellZoneToGraveyard, false, null); });
        AddFlight("FieldSpellZone -> Banish", () => { ClearFieldForTesting(); EnsurePlayerField(); Vector3 ePos = GameManager.Instance.playerRemovedDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(playerField.CurrentCardData, GameManager.Instance.GetCardBackTexture(), true, true, playerField.transform.position, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightFieldSpellZoneToBanished, false, null); });

        AddFlight("Deck -> Hand (Draw)", () => { Vector3 sPos = p ? GameManager.Instance.playerDeckDisplay.transform.position : GameManager.Instance.opponentDeckDisplay.transform.position; Vector3 ePos = p ? GameManager.Instance.playerHandLayoutGroup.position : GameManager.Instance.opponentHandLayoutGroup.position; DuelFXManager.Instance.PlayCardFlight(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), false, true, sPos, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.handCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightDeckToHand, true, null); });
        AddFlight("Deck -> Field", () => { ClearFieldForTesting(); EnsureContextMonster(p); var m = p ? playerMonster : opponentMonster; Vector3 sPos = p ? GameManager.Instance.playerDeckDisplay.transform.position : GameManager.Instance.opponentDeckDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(m.CurrentCardData, GameManager.Instance.GetCardBackTexture(), false, true, sPos, m.transform.position, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightDeckToField, true, null); });
        AddFlight("Deck -> Field (Set)", () => { ClearFieldForTesting(); EnsureContextMonster(p); var m = p ? playerMonster : opponentMonster; Vector3 sPos = p ? GameManager.Instance.playerDeckDisplay.transform.position : GameManager.Instance.opponentDeckDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(m.CurrentCardData, GameManager.Instance.GetCardBackTexture(), false, false, sPos, m.transform.position, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.Euler(0,0,90), DuelFXManager.Instance.flightDeckToField, true, null); });
        AddFlight("Deck -> GY (Mill)", () => { Vector3 sPos = p ? GameManager.Instance.playerDeckDisplay.transform.position : GameManager.Instance.opponentDeckDisplay.transform.position; Vector3 ePos = p ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), false, true, sPos, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightDeckToGraveyard, true, null); });
        AddFlight("Deck -> Banish", () => { Vector3 sPos = p ? GameManager.Instance.playerDeckDisplay.transform.position : GameManager.Instance.opponentDeckDisplay.transform.position; Vector3 ePos = p ? GameManager.Instance.playerRemovedDisplay.transform.position : GameManager.Instance.opponentRemovedDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), false, true, sPos, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightDeckToBanished, true, null); });
        AddFlight("Deck -> View (UI Modal)", () => { Vector3 sPos = GameManager.Instance.playerDeckDisplay.transform.position; Vector3 ePos = new Vector3(Screen.width / 2f, Screen.height / 2f, 0); DuelFXManager.Instance.PlayCardFlight(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), false, true, sPos, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightCardSelectionUI, true, null); });

        AddFlight("GY -> Hand (Salvage)", () => { Vector3 sPos = p ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position; Vector3 ePos = p ? GameManager.Instance.playerHandLayoutGroup.position : GameManager.Instance.opponentHandLayoutGroup.position; DuelFXManager.Instance.PlayCardFlight(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), true, true, sPos, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.handCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightGraveyardToHand, true, null); });
        AddFlight("GY -> Field (Reborn)", () => { ClearFieldForTesting(); EnsureContextMonster(p); var m = p ? playerMonster : opponentMonster; Vector3 sPos = p ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(m.CurrentCardData, GameManager.Instance.GetCardBackTexture(), true, true, sPos, m.transform.position, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightGraveyardToField, true, null); });
        AddFlight("GY -> Field (Set)", () => { ClearFieldForTesting(); EnsureContextMonster(p); var m = p ? playerMonster : opponentMonster; Vector3 sPos = p ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(m.CurrentCardData, GameManager.Instance.GetCardBackTexture(), true, false, sPos, m.transform.position, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.Euler(0,0,90), DuelFXManager.Instance.flightGraveyardToField, true, null); });
        AddFlight("GY -> Deck (Avarice)", () => { Vector3 sPos = p ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position; Vector3 ePos = p ? GameManager.Instance.playerDeckDisplay.transform.position : GameManager.Instance.opponentDeckDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), true, false, sPos, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightGraveyardToDeck, true, null); });
        AddFlight("GY -> Extra Deck", () => { Vector3 sPos = p ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position; Vector3 ePos = p ? GameManager.Instance.playerExtraDeckDisplay.transform.position : GameManager.Instance.opponentExtraDeckDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), true, false, sPos, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightGraveyardToExtraDeck, true, null); });
        AddFlight("GY -> Banish", () => { Vector3 sPos = p ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position; Vector3 ePos = p ? GameManager.Instance.playerRemovedDisplay.transform.position : GameManager.Instance.opponentRemovedDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), true, true, sPos, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightGraveyardToBanished, true, null); });

        AddFlight("Extra -> Field", () => { ClearFieldForTesting(); EnsureContextMonster(p); var m = p ? playerMonster : opponentMonster; Vector3 sPos = p ? GameManager.Instance.playerExtraDeckDisplay.transform.position : GameManager.Instance.opponentExtraDeckDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(m.CurrentCardData, GameManager.Instance.GetCardBackTexture(), false, true, sPos, m.transform.position, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightExtraToField, true, null); });
        AddFlight("Extra -> Field (Set)", () => { ClearFieldForTesting(); EnsureContextMonster(p); var m = p ? playerMonster : opponentMonster; Vector3 sPos = p ? GameManager.Instance.playerExtraDeckDisplay.transform.position : GameManager.Instance.opponentExtraDeckDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(m.CurrentCardData, GameManager.Instance.GetCardBackTexture(), false, false, sPos, m.transform.position, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.Euler(0,0,90), DuelFXManager.Instance.flightExtraToField, true, null); });
        AddFlight("Extra -> GY", () => { Vector3 sPos = p ? GameManager.Instance.playerExtraDeckDisplay.transform.position : GameManager.Instance.opponentExtraDeckDisplay.transform.position; Vector3 ePos = p ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), false, true, sPos, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightExtraToGraveyard, true, null); });
        AddFlight("Extra -> Banish", () => { Vector3 sPos = p ? GameManager.Instance.playerExtraDeckDisplay.transform.position : GameManager.Instance.opponentExtraDeckDisplay.transform.position; Vector3 ePos = p ? GameManager.Instance.playerRemovedDisplay.transform.position : GameManager.Instance.opponentRemovedDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), false, true, sPos, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightExtraToBanished, true, null); });

        AddFlight("Banish -> Hand", () => { Vector3 sPos = p ? GameManager.Instance.playerRemovedDisplay.transform.position : GameManager.Instance.opponentRemovedDisplay.transform.position; Vector3 ePos = p ? GameManager.Instance.playerHandLayoutGroup.position : GameManager.Instance.opponentHandLayoutGroup.position; DuelFXManager.Instance.PlayCardFlight(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), true, true, sPos, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.handCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightBanishToHand, true, null); });
        AddFlight("Banish -> Field", () => { ClearFieldForTesting(); EnsureContextMonster(p); var m = p ? playerMonster : opponentMonster; Vector3 sPos = p ? GameManager.Instance.playerRemovedDisplay.transform.position : GameManager.Instance.opponentRemovedDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(m.CurrentCardData, GameManager.Instance.GetCardBackTexture(), true, true, sPos, m.transform.position, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightBanishToField, true, null); });
        AddFlight("Banish -> Field (Set)", () => { ClearFieldForTesting(); EnsureContextMonster(p); var m = p ? playerMonster : opponentMonster; Vector3 sPos = p ? GameManager.Instance.playerRemovedDisplay.transform.position : GameManager.Instance.opponentRemovedDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(m.CurrentCardData, GameManager.Instance.GetCardBackTexture(), true, false, sPos, m.transform.position, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.Euler(0,0,90), DuelFXManager.Instance.flightBanishToField, true, null); });
        AddFlight("Banish -> Deck", () => { Vector3 sPos = p ? GameManager.Instance.playerRemovedDisplay.transform.position : GameManager.Instance.opponentRemovedDisplay.transform.position; Vector3 ePos = p ? GameManager.Instance.playerDeckDisplay.transform.position : GameManager.Instance.opponentDeckDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), true, false, sPos, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightBanishToDeck, true, null); });
        AddFlight("Banish -> GY", () => { Vector3 sPos = p ? GameManager.Instance.playerRemovedDisplay.transform.position : GameManager.Instance.opponentRemovedDisplay.transform.position; Vector3 ePos = p ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), true, true, sPos, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightBanishToGraveyard, true, null); });
        AddFlight("Banish -> Extra Deck", () => { Vector3 sPos = p ? GameManager.Instance.playerRemovedDisplay.transform.position : GameManager.Instance.opponentRemovedDisplay.transform.position; Vector3 ePos = p ? GameManager.Instance.playerExtraDeckDisplay.transform.position : GameManager.Instance.opponentExtraDeckDisplay.transform.position; DuelFXManager.Instance.PlayCardFlight(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), true, false, sPos, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightBanishToExtraDeck, true, null); });


        flightsInitialized = true;
    }
    
    private void InitExtractions()
    {
        if (extInitialized) return;
        extNames.Clear();
        extActions.Clear();
        bool p = !testAsOpponent;

        AddExt("Deck -> Hand (Flip)", () => {
            Vector3 sPos = p ? GameManager.Instance.playerDeckDisplay.transform.position : GameManager.Instance.opponentDeckDisplay.transform.position;
            Vector3 ePos = p ? GameManager.Instance.playerHandLayoutGroup.position : GameManager.Instance.opponentHandLayoutGroup.position;
            float m = DuelFXManager.Instance.extractionCinematic.useScaleEffect ? DuelFXManager.Instance.extractionCinematic.deck.peakScaleMult : DuelFXManager.Instance.extractionCinematic.deck.startScaleMult;
            DuelFXManager.Instance.PlayExtractionCinematic(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), p, CardLocation.Deck, sPos, false, true, (ghost, pos) => {
                DuelFXManager.Instance.PlayCardFlight(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), true, true, pos, ePos, GameManager.Instance.fieldCardScale * m, GameManager.Instance.handCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightDeckToHand, false, null, ghost);
            });
        });

        AddExt("Deck -> Hand (Cega)", () => {
            Vector3 sPos = p ? GameManager.Instance.playerDeckDisplay.transform.position : GameManager.Instance.opponentDeckDisplay.transform.position;
            Vector3 ePos = p ? GameManager.Instance.playerHandLayoutGroup.position : GameManager.Instance.opponentHandLayoutGroup.position;
            float m = DuelFXManager.Instance.extractionCinematic.useScaleEffect ? DuelFXManager.Instance.extractionCinematic.deck.peakScaleMult : DuelFXManager.Instance.extractionCinematic.deck.startScaleMult;
            DuelFXManager.Instance.PlayExtractionCinematic(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), p, CardLocation.Deck, sPos, false, false, (ghost, pos) => {
                DuelFXManager.Instance.PlayCardFlight(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), false, false, pos, ePos, GameManager.Instance.fieldCardScale * m, GameManager.Instance.handCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightDeckToHand, false, null, ghost);
            });
        });

        AddExt("Extra -> Field (Sincro)", () => {
            Vector3 sPos = p ? GameManager.Instance.playerExtraDeckDisplay.transform.position : GameManager.Instance.opponentExtraDeckDisplay.transform.position;
            ClearFieldForTesting(); EnsureContextMonster(p); var m = p ? playerMonster : opponentMonster;
            float mult = DuelFXManager.Instance.extractionCinematic.useScaleEffect ? DuelFXManager.Instance.extractionCinematic.extraDeck.peakScaleMult : DuelFXManager.Instance.extractionCinematic.extraDeck.startScaleMult;
            DuelFXManager.Instance.PlayExtractionCinematic(m.CurrentCardData, GameManager.Instance.GetCardBackTexture(), p, CardLocation.ExtraDeck, sPos, false, true, (ghost, pos) => {
                DuelFXManager.Instance.PlayCardFlight(m.CurrentCardData, GameManager.Instance.GetCardBackTexture(), true, true, pos, m.transform.position, GameManager.Instance.fieldCardScale * mult, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightExtraToField, false, null, ghost);
            });
        });

        AddExt("Graveyard -> Field", () => {
            Vector3 sPos = p ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position;
            ClearFieldForTesting(); EnsureContextMonster(p); var m = p ? playerMonster : opponentMonster;
            float mult = DuelFXManager.Instance.extractionCinematic.useScaleEffect ? DuelFXManager.Instance.extractionCinematic.graveyard.peakScaleMult : DuelFXManager.Instance.extractionCinematic.graveyard.startScaleMult;
            DuelFXManager.Instance.PlayExtractionCinematic(m.CurrentCardData, GameManager.Instance.GetCardBackTexture(), p, CardLocation.Graveyard, sPos, true, true, (ghost, pos) => {
                DuelFXManager.Instance.PlayCardFlight(m.CurrentCardData, GameManager.Instance.GetCardBackTexture(), true, true, pos, m.transform.position, GameManager.Instance.fieldCardScale * mult, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightGraveyardToField, false, null, ghost);
            });
        });

        AddExt("Graveyard -> Deck", () => {
            Vector3 sPos = p ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position;
            Vector3 ePos = p ? GameManager.Instance.playerDeckDisplay.transform.position : GameManager.Instance.opponentDeckDisplay.transform.position;
            float mult = DuelFXManager.Instance.extractionCinematic.useScaleEffect ? DuelFXManager.Instance.extractionCinematic.graveyard.peakScaleMult : DuelFXManager.Instance.extractionCinematic.graveyard.startScaleMult;
            DuelFXManager.Instance.PlayExtractionCinematic(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), p, CardLocation.Graveyard, sPos, true, false, (ghost, pos) => {
                DuelFXManager.Instance.PlayCardFlight(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), true, false, pos, ePos, GameManager.Instance.fieldCardScale * mult, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightGraveyardToDeck, false, null, ghost);
            });
        });

        AddExt("Banish -> Deck", () => {
            Vector3 sPos = p ? GameManager.Instance.playerRemovedDisplay.transform.position : GameManager.Instance.opponentRemovedDisplay.transform.position;
            Vector3 ePos = p ? GameManager.Instance.playerDeckDisplay.transform.position : GameManager.Instance.opponentDeckDisplay.transform.position;
            float mult = DuelFXManager.Instance.extractionCinematic.useScaleEffect ? DuelFXManager.Instance.extractionCinematic.banished.peakScaleMult : DuelFXManager.Instance.extractionCinematic.banished.startScaleMult;
            DuelFXManager.Instance.PlayExtractionCinematic(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), p, CardLocation.Banished, sPos, true, false, (ghost, pos) => {
                DuelFXManager.Instance.PlayCardFlight(GameManager.Instance.cardDatabase.cardDatabase[0], GameManager.Instance.GetCardBackTexture(), true, false, pos, ePos, GameManager.Instance.fieldCardScale * mult, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, DuelFXManager.Instance.flightBanishToDeck, false, null, ghost);
            });
        });

        extInitialized = true;
    }

    private void AddFlight(string name, System.Action action)
    {
        flightNames.Add(name);
        flightActions.Add(action);
    }

    private void AddExt(string name, System.Action action)
    {
        extNames.Add(name);
        extActions.Add(action);
    }
}
