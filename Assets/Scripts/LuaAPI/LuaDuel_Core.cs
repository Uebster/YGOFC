using UnityEngine;
using MoonSharp.Interpreter;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

// ==============================================================================
// 1. CLASSE DUEL (Ações Globais e Tabuleiro)
// Onde a Mágica Acontece: Intercepta os comandos 'Duel.' do OCGCore.
// Tratativas Críticas & Dependências: 
// - CardEffectManager.cs: Exige suspensão (Yields) e Corrotinas Assíncronas.
// - GameManager.cs: Executa a movimentação física 3D e interfaces UI.
// - LuaEngineCore.cs: Injeta Constantes Nativas (ex: CHAININFO_TARGET_CARDS).
// ==============================================================================
[MoonSharpUserData]
public partial class LuaDuel
{
    public int targetPlayer;
    public int targetParam;
    public LuaGroup currentTargetGroup;
    public LuaCard currentAttacker;
    public LuaCard currentAttackTarget;
    public LuaCard historicalAttacker; // Safety Net: Lembra quem lutou até o fim do turno
    public LuaCard historicalAttackTarget;
    public LuaEffect currentActivatingEffect;
    public LuaEffect currentContinuousEffect; // Memória do efeito passivo em andamento
    public LuaGroup lastCostGroup; // Memória de curto prazo para custos pagos
    public List<CardDisplay> pendingComparisonCards = new List<CardDisplay>(); // Buffer para Confrontos Cinemáticos
    public Dictionary<string, int> hardOncePerTurnUsages = new Dictionary<string, int>(); // Controle de Usos (Hard Once per Turn)

    public List<LuaEffect> globalEffects = new List<LuaEffect>();
    public Dictionary<string, int> playerFlags = new Dictionary<string, int>();
    public Dictionary<string, Dictionary<int, int>> cardFlags = new Dictionary<string, Dictionary<int, int>>();
    public List<Closure> endTurnCallbacks = new List<Closure>();
    public string lastHintMsg = "Select a target";
    public bool nextShuffleDisabled = false;

    private int ConvertToInt(object obj)
    {
        if (obj == null) return 0;
        if (obj is MoonSharp.Interpreter.DynValue dv)
        {
            if (dv.Type == MoonSharp.Interpreter.DataType.Number) return (int)dv.Number;
            if (dv.Type == MoonSharp.Interpreter.DataType.Boolean) return dv.Boolean ? 1 : 0;
            if (dv.Type == MoonSharp.Interpreter.DataType.String && int.TryParse(dv.String, out int res)) return res;
            return 0;
        }
        if (obj is double d) return (int)d;
        if (obj is int i) return i;
        if (obj is long l) return (int)l;
        if (obj is bool b) return b ? 1 : 0;
        if (obj is string s && int.TryParse(s, out int parsed)) return parsed;
        return 0;
    }

    // Utilitário de Conversão: 0 = Human Player, 1 = AI Opponent
    public bool IsPlayer(object playerIndex)
    {
        return ConvertToInt(playerIndex) == 0;
    }

    public void Damage(object player, object amount, object reason)
    {
        if (IsPlayer(player)) GameManager.Instance.DamagePlayer(ConvertToInt(amount));
        else GameManager.Instance.DamageOpponent(ConvertToInt(amount));
    }

    public void Recover(object player, object amount, object reason)
    {
        // Debug.Log($"[Surgical Log] Duel.Recover! Jogador_Raw: {player} | Amount_Raw: {amount}");
        int pInt = ConvertToInt(player);
        int aInt = ConvertToInt(amount);
        // Debug.Log($"[Surgical Log] Convertido para C# -> Jogador: {pInt} | Cura: {aInt}");
        if (pInt == 0) GameManager.Instance.GainLifePoints(true, aInt);
        else GameManager.Instance.GainLifePoints(false, aInt);
    }

    // --- STATUS BASE (Resgatados das requisições LUA) ---
    public bool IsTurnPlayer(object player) { return GetTurnPlayer() == ConvertToInt(player); }
    public int GetTurnCount() { return GameManager.Instance != null ? GameManager.Instance.turnCount : 0; }
    public int GetLP(object player) { return IsPlayer(player) ? GameManager.Instance.playerLP : GameManager.Instance.opponentLP; }
    public int GetBattleDamage(object player) { return 0; }
    public bool IsDuelType(object type) { return true; }

    public int GetTurnPlayer()
    {
        // Retorna 0 se for o turno do Jogador Humano, 1 se for da IA
        return GameManager.Instance.isPlayerTurn ? 0 : 1;
    }

    public int GetPhase() { return GetCurrentPhase(); }

    public int GetCurrentPhase()
    {
        if (PhaseManager.Instance == null) return 0;
        
        // Tradução direta para a tabela hexadecimal do constant.lua
        switch (PhaseManager.Instance.currentPhase)
        {
            case GamePhase.Draw: return 0x01;       // PHASE_DRAW
            case GamePhase.Standby: return 0x02;    // PHASE_STANDBY
            case GamePhase.Main1: return 0x04;      // PHASE_MAIN1
            case GamePhase.Battle: return 0x80;     // PHASE_BATTLE
            case GamePhase.Main2: return 0x100;     // PHASE_MAIN2
            case GamePhase.End: return 0x200;       // PHASE_END
            default: return 0;
        }
    }

    public bool IsPhase(object phaseObj)
    {
        int phase = 0;
        if (phaseObj is double d) phase = (int)d;
        else if (phaseObj is int i) phase = i;
        else if (phaseObj is long l) phase = (int)l;
        return GetCurrentPhase() == phase;
    }

    public bool CheckLPCost(object player, object cost)
    {
        int lp = IsPlayer(player) ? GameManager.Instance.playerLP : GameManager.Instance.opponentLP;
        return lp >= ConvertToInt(cost);
    }

    public void PayLPCost(object player, object cost)
    {
        GameManager.Instance.PayLifePoints(IsPlayer(player), ConvertToInt(cost));
    }

    public LuaCard GetAttacker()
    {
        return currentAttacker ?? SafeDummyCard();
    }

    public LuaCard GetAttackTarget()
    {
        return currentAttackTarget ?? SafeDummyCard();
    }

    public LuaCard GetHistoricalAttacker()
    {
        return historicalAttacker ?? SafeDummyCard();
    }

    public LuaCard GetHistoricalAttackTarget()
    {
        return historicalAttackTarget ?? SafeDummyCard();
    }
    
    public void CalculateDamage(object attackerObj, object defenderObj)
    {
        LuaCard attacker = attackerObj as LuaCard;
        LuaCard defender = defenderObj as LuaCard;
        
        // FIX: Preserva a memória do Atacante e Defensor para os eventos pós-dano (como After the Struggle)
        this.currentAttacker = attacker;
        this.currentAttackTarget = defender;
        
        // SAFETY NET: Histórico persistente até o fim do turno
        this.historicalAttacker = attacker;
        this.historicalAttackTarget = defender;

        if (attacker == null || attacker.unityCard == null) return;

        CardDisplay atkCard = attacker.unityCard;
        CardDisplay defCard = defender != null ? defender.unityCard : null;

        int atkPower = atkCard.currentAtk;
        bool atkIsPlayer = atkCard.isPlayerCard;
        
        if (defCard == null)
        {
            Debug.Log($"<color=orange>[DEBUG LUA BATTLE]</color> Calculando dano direto: {atkCard.CurrentCardData.name} (ATK {atkPower})...");
        }
        else
        {
            Debug.Log($"<color=orange>[DEBUG LUA BATTLE]</color> Calculando dano: {atkCard.CurrentCardData.name} (ATK {atkPower}) vs {defCard.CurrentCardData.name}...");
        }

        if (defCard == null)
        {
            // Ataque Direto
            if (atkIsPlayer) GameManager.Instance.DamageOpponent(atkPower);
            else GameManager.Instance.DamagePlayer(atkPower);
        }
        else
        {
            int defPower = defCard.position == CardDisplay.BattlePosition.Attack ? defCard.currentAtk : defCard.currentDef;
            bool defIsPlayer = defCard.isPlayerCard;

            if (defCard.position == CardDisplay.BattlePosition.Attack)
            {
                if (atkPower > defPower)
                {
                    if (defIsPlayer) GameManager.Instance.DamagePlayer(atkPower - defPower);
                    else GameManager.Instance.DamageOpponent(atkPower - defPower);
                    
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(defCard);
                    GameManager.Instance.MoveCard(defCard, CardLocation.Graveyard, SendReason.Battle);
                }
                else if (atkPower < defPower)
                {
                    if (atkIsPlayer) GameManager.Instance.DamagePlayer(defPower - atkPower);
                    else GameManager.Instance.DamageOpponent(defPower - atkPower);
                    
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(atkCard);
                    GameManager.Instance.MoveCard(atkCard, CardLocation.Graveyard, SendReason.Battle);
                }
                else
                {
                    if (DuelFXManager.Instance != null) {
                        DuelFXManager.Instance.PlayDestruction(atkCard);
                        DuelFXManager.Instance.PlayDestruction(defCard);
                    }
                    GameManager.Instance.MoveCard(atkCard, CardLocation.Graveyard, SendReason.Battle);
                    GameManager.Instance.MoveCard(defCard, CardLocation.Graveyard, SendReason.Battle);
                }
            }
            else // Defesa
            {
                if (atkPower > defPower)
                {
                    if (atkCard.hasPiercing)
                    {
                        if (defIsPlayer) GameManager.Instance.DamagePlayer(atkPower - defPower);
                        else GameManager.Instance.DamageOpponent(atkPower - defPower);
                    }
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(defCard);
                    GameManager.Instance.MoveCard(defCard, CardLocation.Graveyard, SendReason.Battle);
                }
                else if (atkPower < defPower)
                {
                    if (DuelFXManager.Instance != null) {
                        DuelFXManager.Instance.PlayAttackFail(atkCard);
                        DuelFXManager.Instance.PlayDefenseSuccessEffect(defCard);
                    }
                    if (atkIsPlayer) GameManager.Instance.DamagePlayer(defPower - atkPower);
                    else GameManager.Instance.DamageOpponent(defPower - atkPower);
                }
                else
                {
                    // Equal ATK and DEF: reflect VFX but no damage
                    if (DuelFXManager.Instance != null) {
                        DuelFXManager.Instance.PlayAttackFail(atkCard);
                        DuelFXManager.Instance.PlayDefenseSuccessEffect(defCard);
                    }
                }
            }
        }
    }

    public CardDisplay FindCardDisplayInPiles(CardData data)
    {
        if (GameManager.Instance == null || data == null) return null;
        
        var piles = new PileDisplay[] {
            GameManager.Instance.playerGraveyardDisplay,
            GameManager.Instance.opponentGraveyardDisplay,
            GameManager.Instance.playerDeckDisplay,
            GameManager.Instance.opponentDeckDisplay,
            GameManager.Instance.playerExtraDeckDisplay,
            GameManager.Instance.opponentExtraDeckDisplay,
            GameManager.Instance.playerRemovedDisplay,
            GameManager.Instance.opponentRemovedDisplay
        };

        foreach(var pile in piles) {
            if (pile != null && pile.contentParent != null) {
                // Procura do topo para o fundo, garantindo pegar a cópia que acabou de cair lá!
                for (int i = pile.contentParent.childCount - 1; i >= 0; i--) {
                    var cd = pile.contentParent.GetChild(i).GetComponent<CardDisplay>();
                    if (cd != null && cd.CurrentCardData == data) return cd;
                }
            }
        }
        return null;
    }
}