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

        // Botão opcional para limpar a tela se ficar muito bagunçado
        if (GUILayout.Button("LIMPAR CENA", btnStyle))
        {
            if (playerMonster != null) Destroy(playerMonster.gameObject);
            if (opponentMonster != null) Destroy(opponentMonster.gameObject);
            if (playerSpell != null) Destroy(playerSpell.gameObject);
            if (playerField != null) Destroy(playerField.gameObject);
        }

        GUILayout.Space(10);

        // As cartas são geradas dinamicamente e de forma inteligente a cada clique!
        GUILayout.Label("<color=cyan><b>INVOCAÇÕES</b></color>");
        if (GUILayout.Button("Summon Comum", btnStyle)) { EnsurePlayerMonster(); DuelFXManager.Instance.PlaySummonEffect(playerMonster); }
        if (GUILayout.Button("Tribute Summon", btnStyle)) { EnsurePlayerMonster(); DuelFXManager.Instance.PlayTributeSummonEffect(playerMonster); }
        if (GUILayout.Button("Summon Aura", btnStyle)) { EnsurePlayerMonster(); DuelFXManager.Instance.PlaySummonAura(playerMonster); }
        if (GUILayout.Button("Summon Cinemático", btnStyle)) { EnsurePlayerMonster(); DuelFXManager.Instance.PlaySummonCinematic(playerMonster, false, true, null); }
        if (GUILayout.Button("Fusion Spin", btnStyle)) { EnsurePlayerMonster(); DuelFXManager.Instance.PlayFusionEffect(playerMonster); }
        if (GUILayout.Button("Efeito de Poeira (Set)", btnStyle)) { EnsurePlayerMonster(); DuelFXManager.Instance.PlayFlipEffect(playerMonster); }
        if (GUILayout.Button("Virar Carta (Flip 3D/2D)", btnStyle)) { EnsurePlayerMonster(); playerMonster.FlipCard(); }
        if (GUILayout.Button("Mudar Posição (Atk <-> Def)", btnStyle)) { EnsurePlayerMonster(); playerMonster.ChangePosition(); }
        
        GUILayout.Space(5);
        GUILayout.Label("<color=cyan><b>MAGIAS E EFEITOS</b></color>");
        if (GUILayout.Button("Ativar Magia (Spell)", btnStyle)) { EnsurePlayerSpell(); DuelFXManager.Instance.PlayCardActivation(playerSpell, false); }
        if (GUILayout.Button("Ativar Armadilha (Trap)", btnStyle)) { EnsurePlayerSpell(); DuelFXManager.Instance.PlayCardActivation(playerSpell, true); }
        if (GUILayout.Button("Ativar Field Spell", btnStyle)) { EnsurePlayerField(); DuelFXManager.Instance.PlayCardActivation(playerField, false); }
        if (GUILayout.Button("Equipar Magia (Ghost)", btnStyle)) { EnsurePlayerSpell(); EnsurePlayerMonster(); DuelFXManager.Instance.PlayEquipEffect(playerSpell, playerMonster); }
        if (GUILayout.Button("Chain Link (Corrente)", btnStyle)) { EnsurePlayerSpell(); DuelFXManager.Instance.PlayChainLinkEffect(playerSpell, 2); }
        if (GUILayout.Button("Efeito de Monstro (Brilho)", btnStyle)) { EnsurePlayerMonster(); DuelFXManager.Instance.PlayMonsterEffect(playerMonster); }
        if (GUILayout.Button("Tributar Carta (Alma)", btnStyle)) { EnsurePlayerMonster(); DuelFXManager.Instance.PlayTributeEffect(playerMonster); }

        GUILayout.Space(5);
        GUILayout.Label("<color=cyan><b>BATALHA E REMOÇÃO</b></color>");
        
        if (GUILayout.Button("Ataque Direto Completo (Dano Tela)", btnStyle)) { 
            EnsurePlayerMonster();
            if (GameManager.Instance != null) GameManager.Instance.enableAttackAnimation = true; // Força a espada
            DuelFXManager.Instance.PlayAttack(playerMonster, null, () => {
                DuelFXManager.Instance.PlayDamageEffect(Vector3.zero);
            }); 
        }
        if (GUILayout.Button("Ataque e Destruir (Alvo Inimigo)", btnStyle)) { 
            EnsurePlayerMonster(); EnsureOpponentMonster();
            if (GameManager.Instance != null) GameManager.Instance.enableAttackAnimation = true; // Força a espada
            DuelFXManager.Instance.PlayAttack(playerMonster, opponentMonster, () => {
                DuelFXManager.Instance.PlayDestruction(opponentMonster);
            }); 
        }
        if (GUILayout.Button("Ataque Bloqueado (Bate e Defende)", btnStyle)) { 
            EnsurePlayerMonster(); EnsureOpponentMonster();
            if (GameManager.Instance != null) GameManager.Instance.enableAttackAnimation = true; // Força a espada
            DuelFXManager.Instance.PlayAttack(playerMonster, opponentMonster, () => {
                DuelFXManager.Instance.PlayAttackFail(playerMonster);
                DuelFXManager.Instance.PlayDefenseSuccessEffect(opponentMonster);
            }); 
        }
        
        if (GUILayout.Button("Apenas Banir (Vórtice)", btnStyle)) { EnsureOpponentMonster(); DuelFXManager.Instance.PlayBanishEffect(opponentMonster); }
        if (GUILayout.Button("Trocar Controle (Change of Heart)", btnStyle)) { 
            EnsureOpponentMonster(); 
            GameManager.Instance.SwitchControl(opponentMonster); 
            opponentMonster = null; 
        }

        GUILayout.FlexibleSpace(); // Empurra pro fundo
        
        GUILayout.Label("<color=cyan><b>GLOBAIS</b></color>");
        if (GUILayout.Button("Tremor de Dano na Tela", btnStyle)) DuelFXManager.Instance.PlayDamageEffect(Vector3.zero);
        if (GUILayout.Button("Teste: Cinemática de Ritual", btnStyle)) { 
            EnsurePlayerMonster(); EnsurePlayerSpell(); 
            DuelFXManager.Instance.PlayRitualCinematic(playerMonster, playerSpell.CurrentCardData, false, null); 
        }
        if (GUILayout.Button("Teste: Cinemática de Fusão", btnStyle)) { 
            EnsurePlayerMonster(); EnsureOpponentMonster(); EnsurePlayerSpell(); 
            List<CardData> mats = new List<CardData> { playerMonster.CurrentCardData, opponentMonster.CurrentCardData };
            DuelFXManager.Instance.PlayFusionCinematic(playerMonster, mats, playerSpell.CurrentCardData, false, null); 
        }
        if (GUILayout.Button("Embaralhar Deck (Shuffle)", btnStyle)) 
        {
            if (GameManager.Instance.duelFieldUI != null) DuelFXManager.Instance.PlayShuffleEffect(GameManager.Instance.duelFieldUI.playerDeck);
        }

        GUILayout.EndArea();
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
