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

        // Área de fundo escuro
        GUI.DrawTexture(new Rect(10, 10, 280, Screen.height - 20), darkTex);

        // Inicia o Layout
        GUILayout.BeginArea(new Rect(20, 20, 260, Screen.height - 40));
        
        GUILayout.Label("--- TESTE DE EFEITOS VFX ---", titleStyle);
        GUILayout.Space(10);

        if (GUILayout.Button("LIMPAR CENA", btnStyle)) { ClearFieldForTesting(); }


        // As cartas são geradas dinamicamente e de forma inteligente a cada clique!
        GUILayout.Label("<color=cyan><b>POUSO E INVOCAÇÕES</b></color>");
        TestAction("Aura de Pouso (Universal)", () => { EnsurePlayerMonster(); DuelFXManager.Instance.PlayPlacementAura(playerMonster); });
        TestAction("Impacto de Invocação (Monstros)", () => { EnsurePlayerMonster(); DuelFXManager.Instance.PlaySummonEffect(playerMonster); });
        TestAction("Fumaça de Ficha (Token)", () => { EnsurePlayerMonster(); DuelFXManager.Instance.PlayTokenSummonEffect(playerMonster); });
        TestAction("Tribute Summon", () => { EnsurePlayerMonster(); DuelFXManager.Instance.PlayTributeSummonEffect(playerMonster); });
        TestAction("Summon Cinemático", () => { EnsurePlayerMonster(); DuelFXManager.Instance.PlaySummonCinematic(playerMonster, false, true, null); });
        TestAction("Fusion Spin", () => { EnsurePlayerMonster(); DuelFXManager.Instance.PlayFusionEffect(playerMonster); });
        TestAction("Efeito de Poeira (Set)", () => { EnsurePlayerMonster(); DuelFXManager.Instance.PlayFlipEffect(playerMonster); });
        TestAction("Virar Carta (Flip 3D/2D)", () => { EnsurePlayerMonster(); playerMonster.FlipCard(); });
        TestAction("Mudar Posição (Atk <-> Def)", () => { EnsurePlayerMonster(); playerMonster.ChangePosition(); });
         
        GUILayout.Space(5);
        GUILayout.Label("<color=cyan><b>MAGIAS E EFEITOS</b></color>");
        TestAction("Ativar Magia (Spell)", () => { EnsurePlayerSpell(); DuelFXManager.Instance.PlayCardActivation(playerSpell, false); });
        TestAction("Ativar Armadilha (Trap)", () => { EnsurePlayerSpell(); DuelFXManager.Instance.PlayCardActivation(playerSpell, true); });
        TestAction("Ativar Field Spell", () => { EnsurePlayerField(); DuelFXManager.Instance.PlayCardActivation(playerField, false); });
        TestAction("Equipar Magia (Ghost)", () => { EnsurePlayerSpell(); EnsurePlayerMonster(); DuelFXManager.Instance.PlayEquipEffect(playerSpell, playerMonster); });
        TestAction("Chain Link (Corrente)", () => { EnsurePlayerSpell(); DuelFXManager.Instance.PlayChainLinkEffect(playerSpell, 2); });
        TestAction("Efeito de Monstro (Brilho)", () => { EnsurePlayerMonster(); DuelFXManager.Instance.PlayMonsterEffect(playerMonster); });
        TestAction("Tributar Carta (Alma)", () => { EnsurePlayerMonster(); DuelFXManager.Instance.PlayTributeEffect(playerMonster); });

        GUILayout.Space(5);
        GUILayout.Label("<color=cyan><b>BATALHA E REMOÇÃO</b></color>");
        
        TestAction("Ataque (Voo da Espada)", () => {
             EnsurePlayerMonster(); EnsureOpponentMonster();
             if (GameManager.Instance != null) GameManager.Instance.enableAttackAnimation = true;
             DuelFXManager.Instance.PlayAttack(playerMonster, opponentMonster, null);
        });
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
        TestAction("Trocar Controle (Change of Heart)", () => { EnsureOpponentMonster(); GameManager.Instance.SwitchControl(opponentMonster); });

        GUILayout.FlexibleSpace(); // Empurra pro fundo
        
        GUILayout.Label("<color=cyan><b>GLOBAIS</b></color>");
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
        TestAction("Embaralhar Deck (Shuffle)", () => {
             if (GameManager.Instance.duelFieldUI != null) DuelFXManager.Instance.PlayShuffleEffect(GameManager.Instance.duelFieldUI.playerDeck);
        });

        GUILayout.EndArea();
    }

    // Wrapper para executar uma ação de teste, limpando o campo antes.
    private void TestAction(string buttonText, System.Action testAction)
    {
        if (GUILayout.Button(buttonText, btnStyle))
        {
            ClearFieldForTesting();
            testAction?.Invoke();
        }
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
