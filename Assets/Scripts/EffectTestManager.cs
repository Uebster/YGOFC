using UnityEngine;
using System.Collections.Generic;

public class EffectTestManager : MonoBehaviour
{
    // Referências para as cartas de teste (podem ser atribuídas manualmente ou criadas)
    public CardDisplay playerMonster;
    public CardDisplay opponentMonster;
    public CardDisplay playerSpell;
    public CardDisplay playerField;

    private Texture2D darkTex;
    private GUIStyle btnStyle;
    private GUIStyle titleStyle;
    private bool styleInitialized = false;
    private Vector2 scrollPos;

    void Update()
    {
        // Abre ou fecha o painel de testes de VFX ao pressionar Ctrl + E
        if (Input.GetKeyDown(KeyCode.E) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
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

        // Área de fundo escuro (Aumentada para caber a barra de rolagem)
        GUI.DrawTexture(new Rect(10, 10, 320, Screen.height - 20), darkTex);

        // Inicia o Layout
        GUILayout.BeginArea(new Rect(20, 20, 300, Screen.height - 40));
        
        GUILayout.Label("--- TESTE DE EFEITOS VFX ---", titleStyle);
        GUILayout.Space(10);

        if (GUILayout.Button("LIMPAR CENA", btnStyle)) { ClearFieldForTesting(); }

        // Inicia a área de Rolagem (importante para caber todas as opções)
        scrollPos = GUILayout.BeginScrollView(scrollPos, false, true, GUILayout.Width(280));

        GUILayout.Label("<color=cyan><b>AURAS DE POUSO COLORIDAS</b></color>");
        TestAction("Aura: Monstro Normal (Amarelo)", () => TestColoredAura("Monster (Normal)", false));
        TestAction("Aura: Monstro Efeito (Marrom)", () => TestColoredAura("Monster (Effect)", false));
        TestAction("Aura: Monstro Fusão (Roxo)", () => TestColoredAura("Monster (Fusion)", false));
        TestAction("Aura: Monstro Ritual (Azul)", () => TestColoredAura("Monster (Ritual)", false));
        TestAction("Aura: Magia (Verde)", () => TestColoredAura("Spell", false));
        TestAction("Aura: Armadilha (Rosa)", () => TestColoredAura("Trap", false));
        TestAction("Aura: Carta Baixada (Cinza)", () => TestColoredAura("Monster (Normal)", true));

        GUILayout.Space(5);
        GUILayout.Label("<color=cyan><b>POUSO E INVOCAÇÕES</b></color>");
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Aura Pouso (Nativo)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); 
            DuelFXManager.Instance.usePlacementAuraRoutine = true; 
            DuelFXManager.Instance.usePlacementAuraPrefab = false; 
            DuelFXManager.Instance.PlayPlacementAura(playerMonster); 
        }
        if (GUILayout.Button("Aura Pouso (Prefab)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); 
            DuelFXManager.Instance.usePlacementAuraRoutine = false; 
            DuelFXManager.Instance.usePlacementAuraPrefab = true; 
            DuelFXManager.Instance.PlayPlacementAura(playerMonster); 
        }
        GUILayout.EndHorizontal();

        TestAction("Impacto de Invocação (Monstros)", () => { EnsurePlayerMonster(); DuelFXManager.Instance.PlaySummonEffect(playerMonster); });
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Token (Nativo)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); 
            DuelFXManager.Instance.useTokenSummonRoutine = true; 
            DuelFXManager.Instance.useTokenSummonPrefab = false; 
            DuelFXManager.Instance.PlayTokenSummonEffect(playerMonster); 
        }
        if (GUILayout.Button("Token (Prefab)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); 
            DuelFXManager.Instance.useTokenSummonRoutine = false; 
            DuelFXManager.Instance.useTokenSummonPrefab = true; 
            DuelFXManager.Instance.PlayTokenSummonEffect(playerMonster); 
        }
        GUILayout.EndHorizontal();

        TestAction("Tribute Summon", () => { EnsurePlayerMonster(); DuelFXManager.Instance.PlayTributeSummonEffect(playerMonster); });
        
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
        if (GUILayout.Button("Tributo Ícone (Nativo)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); 
            DuelFXManager.Instance.useNativeTributeIcon = true; 
            DuelFXManager.Instance.useTributeIconPrefab = false; 
            DuelFXManager.Instance.PlayTributeEffect(playerMonster); 
        }
        if (GUILayout.Button("Tributo Ícone (Prefab)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); 
            DuelFXManager.Instance.useNativeTributeIcon = false; 
            DuelFXManager.Instance.useTributeIconPrefab = true; 
            DuelFXManager.Instance.PlayTributeEffect(playerMonster); 
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.Label("<color=cyan><b>BATALHA E REMOÇÃO</b></color>");
        
        GUILayout.Label("<size=12><i>Ataque (Voo da Espada):</i></size>");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Espada + Shadows", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); EnsureOpponentMonster(); 
            DuelFXManager.Instance.useAttackSword = true; 
            DuelFXManager.Instance.useAttackProjectilePrefab = false; 
            DuelFXManager.Instance.attackTrailType = AttackTrailType.Shadows; 
            DuelFXManager.Instance.PlayAttack(playerMonster, opponentMonster, null); 
        }
        if (GUILayout.Button("Espada + Contínuo", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); EnsureOpponentMonster(); 
            DuelFXManager.Instance.useAttackSword = true; 
            DuelFXManager.Instance.useAttackProjectilePrefab = false; 
            DuelFXManager.Instance.attackTrailType = AttackTrailType.ContinuousLine; 
            DuelFXManager.Instance.PlayAttack(playerMonster, opponentMonster, null); 
        }
        GUILayout.EndHorizontal();
        
        if (GUILayout.Button("Ataque (Projétil Prefab)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); EnsureOpponentMonster(); 
            DuelFXManager.Instance.useAttackSword = false; 
            DuelFXManager.Instance.useAttackProjectilePrefab = true; 
            DuelFXManager.Instance.PlayAttack(playerMonster, opponentMonster, null); 
        }
        
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

        TestAction("Hit / Corte (VFX)", () => {
            EnsureOpponentMonster();
            DuelFXManager.Instance.SpawnVFXPublic(DuelFXManager.Instance.attackVFX, opponentMonster.transform.position);
            DuelFXManager.Instance.PlayCardShake(opponentMonster);
        });
        TestAction("Defesa / Bloqueio (VFX)", () => {
            EnsurePlayerMonster(); EnsureOpponentMonster();
            DuelFXManager.Instance.PlayAttackFail(playerMonster);
            DuelFXManager.Instance.PlayDefenseSuccessEffect(opponentMonster);
        });
        TestAction("Destruição (Explosão VFX)", () => {
            EnsureOpponentMonster();
            DuelFXManager.Instance.PlayDestruction(opponentMonster);
        });
        TestAction("Apenas Banir (Vórtice)", () => { EnsureOpponentMonster(); DuelFXManager.Instance.PlayBanishEffect(opponentMonster); });

        
        GUILayout.Space(5);
        GUILayout.Label("<color=cyan><b>GLOBAIS</b></color>");
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Field Marker (Nativo)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); 
            DuelFXManager.Instance.useNativeFieldMarker = true; 
            DuelFXManager.Instance.useFieldMarkerPrefabs = false; 
            DuelFXManager.Instance.PlaySummonCinematic(playerMonster, false, true, null); 
        }
        if (GUILayout.Button("Field Marker (Prefab)", btnStyle)) { 
            ClearFieldForTesting(); EnsurePlayerMonster(); 
            DuelFXManager.Instance.useNativeFieldMarker = false; 
            DuelFXManager.Instance.useFieldMarkerPrefabs = true; 
            DuelFXManager.Instance.PlaySummonCinematic(playerMonster, false, true, null); 
        }
        GUILayout.EndHorizontal();
        
        TestAction("Tremor de Dano na Tela", () => DuelFXManager.Instance.PlayDamageEffect(Vector3.zero));
        TestAction("Teste: Cinemática de Ritual", () => {
             EnsurePlayerMonster(); EnsurePlayerSpell();
             DuelFXManager.Instance.PlayRitualCinematic(playerMonster, playerSpell.CurrentCardData, null);
        });
        TestAction("Teste: Cinemática de Fusão", () => {
             EnsurePlayerMonster(); EnsureOpponentMonster(); EnsurePlayerSpell();
             List<CardData> mats = new List<CardData> { playerMonster.CurrentCardData, opponentMonster.CurrentCardData };
             DuelFXManager.Instance.PlayFusionCinematic(playerMonster, mats, playerSpell.CurrentCardData, null);
        });
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Shuffle (Nativo)", btnStyle)) { 
            DuelFXManager.Instance.useShuffleRoutine = true; 
            DuelFXManager.Instance.useShufflePrefab = false; 
            if (GameManager.Instance.duelFieldUI != null && GameManager.Instance.duelFieldUI.playerDeck != null) DuelFXManager.Instance.PlayShuffleEffect(GameManager.Instance.duelFieldUI.playerDeck);
        }
        if (GUILayout.Button("Shuffle (Prefab)", btnStyle)) { 
            DuelFXManager.Instance.useShuffleRoutine = false; 
            DuelFXManager.Instance.useShufflePrefab = true; 
            if (GameManager.Instance.duelFieldUI != null && GameManager.Instance.duelFieldUI.playerDeck != null) DuelFXManager.Instance.PlayShuffleEffect(GameManager.Instance.duelFieldUI.playerDeck);
        }
        GUILayout.EndHorizontal();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
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
        
        cardGO.transform.localPosition = Vector3.zero;
        cardGO.transform.localScale = GameManager.Instance.fieldCardScale;
        
        if (!isPlayer)
        {
            cardGO.transform.localRotation = Quaternion.Euler(0, 0, 180);
        }

        return display;
    }
}
