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
    public Dictionary<string, int> oathUsages = new Dictionary<string, int>(); // Controle de Usos (Once per Duel)

    public int globalFlags = 0;
    public Dictionary<CardData, LuaCard> persistentCards = new Dictionary<CardData, LuaCard>(); // Memória para cartas no Deck/GY
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
            if (dv.Type == MoonSharp.Interpreter.DataType.Number) return unchecked((int)(long)dv.Number);
            if (dv.Type == MoonSharp.Interpreter.DataType.Boolean) return dv.Boolean ? 1 : 0;
            if (dv.Type == MoonSharp.Interpreter.DataType.String && int.TryParse(dv.String, out int res)) return res;
            return 0;
        }
        if (obj is double d) return unchecked((int)(long)d);
        if (obj is int i) return i;
        if (obj is long l) return unchecked((int)l);
        if (obj is bool b) return b ? 1 : 0;
        if (obj is string s && int.TryParse(s, out int parsed)) return parsed;
        return 0;
    }

    // --- CARREGADOR DE BIBLIOTECAS OCGCORE (LoadScript) ---
    public bool LoadScript(object filename)
    {
        string name = filename.ToString();
        
        // Procura a biblioteca na pasta SupportLua dentro de StreamingAssets ou DataPath
        string path1 = System.IO.Path.Combine(Application.dataPath, "SupportLua", name);
        string path2 = System.IO.Path.Combine(Application.streamingAssetsPath, "SupportLua", name);
        
        string finalPath = System.IO.File.Exists(path1) ? path1 : (System.IO.File.Exists(path2) ? path2 : null);

        if (finalPath != null)
        {
            try
            {
                string scriptContent = System.IO.File.ReadAllText(finalPath);
                
                // OCGCore Standard Libraries (utility.lua, proc_xyz.lua, etc) usam sintaxe Lua 5.3 (Operadores Bitwise como | e &).
                // MoonSharp roda Lua 5.2. Precisamos passar o conteúdo da lib pelo mesmo sanitizador que usamos para as cartas!
                scriptContent = LuaScriptLoader.SanitizeOCGScript(scriptContent);

                if (CardEffectManager.Instance != null && CardEffectManager.Instance.luaEngine != null)
                {
                    CardEffectManager.Instance.luaEngine.DoString(scriptContent);
                    Debug.Log($"<color=green>[LuaDuel]</color> Biblioteca Oficial Carregada: {name}");
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"<color=red>[LuaDuel]</color> Erro ao compilar biblioteca {name}: {ex.Message}");
                return false;
            }
        }
        Debug.LogWarning($"<color=yellow>[LuaDuel]</color> Biblioteca solicitada não encontrada: {name}");
        return false;
    }

    // Utilitário de Conversão: 0 = Human Player, 1 = AI Opponent
    public bool IsPlayer(object playerIndex)
    {
        return ConvertToInt(playerIndex) == 0;
    }

    public DynValue Damage(object player, object amount, object reason)
    {
        int pInt = ConvertToInt(player);
        int aInt = ConvertToInt(amount);
        
        // EFFECT_REVERSE_DAMAGE (80)
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel != null) {
            if (pInt == 0 && CardEffectManager.Instance.luaDuel.IsPlayerAffectedByEffect(0, 80)) { return Recover(player, amount, reason); }
            if (pInt == 1 && CardEffectManager.Instance.luaDuel.IsPlayerAffectedByEffect(1, 80)) { return Recover(player, amount, reason); }
        }

        if (pInt == 0) {
            GameManager.Instance.DamagePlayer(aInt);
            if (DamagePopupManager.Instance != null && !GameManager.Instance.isSimulating) DamagePopupManager.Instance.ShowPopup(aInt, false, true);
        }
        else if (pInt == 1) {
            GameManager.Instance.DamageOpponent(aInt);
            if (DamagePopupManager.Instance != null && !GameManager.Instance.isSimulating) DamagePopupManager.Instance.ShowPopup(aInt, false, false);
        }
        else if (pInt == 3) {
            GameManager.Instance.DamagePlayer(aInt);
            GameManager.Instance.DamageOpponent(aInt);
            if (DamagePopupManager.Instance != null && !GameManager.Instance.isSimulating) {
                DamagePopupManager.Instance.ShowPopup(aInt, false, true);
                DamagePopupManager.Instance.ShowPopup(aInt, false, false);
            }
        }
        else return DynValue.Nil; // PLAYER_NONE (2) ou inválido

        // Feedback Visual e Sonoro de Dano de Efeito (Burn)
        if (DuelFXManager.Instance != null && GameManager.Instance != null && !GameManager.Instance.isSimulating && aInt > 0)
        {
            Vector3 vfxPos = Vector3.zero; // Padrão: Centro da tela
            if (CardEffectManager.Instance != null && CardEffectManager.Instance.chainManager != null && CardEffectManager.Instance.chainManager.resolvingLink != null)
            {
                var source = CardEffectManager.Instance.chainManager.resolvingLink.card;
                if (source != null)
                {
                    if (source.unityCard != null && source.unityCard.gameObject.activeInHierarchy) vfxPos = source.unityCard.transform.position;
                    else {
                        var pileCard = FindCardDisplayInPiles(source.unityData);
                        if (pileCard != null) vfxPos = pileCard.transform.position;
                    }
                }
            }
            DuelFXManager.Instance.PlayDamageEffect(vfxPos);
        }
        
        if (CardEffectManager.Instance != null && GameManager.Instance != null && !GameManager.Instance.isSimulating)
        {
            CardEffectManager.Instance.isWaitingForLuaYield = true;
            CardEffectManager.Instance.yieldReturnValue = null;
            CardEffectManager.Instance.StartCoroutine(WaitVisualTasksRoutine());
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("UI_Wait") });
        }
        return DynValue.Nil;
    }

    public DynValue Recover(object player, object amount, object reason)
    {
        // Debug.Log($"[Surgical Log] Duel.Recover! Jogador_Raw: {player} | Amount_Raw: {amount}");
        int pInt = ConvertToInt(player);
        int aInt = ConvertToInt(amount);
        // Debug.Log($"[Surgical Log] Convertido para C# -> Jogador: {pInt} | Cura: {aInt}");
        if (pInt == 0) {
            GameManager.Instance.GainLifePoints(true, aInt);
            if (DamagePopupManager.Instance != null && !GameManager.Instance.isSimulating) DamagePopupManager.Instance.ShowPopup(aInt, true, true);
        }
        else if (pInt == 1) {
            GameManager.Instance.GainLifePoints(false, aInt);
            if (DamagePopupManager.Instance != null && !GameManager.Instance.isSimulating) DamagePopupManager.Instance.ShowPopup(aInt, true, false);
        }
        else if (pInt == 3) {
            GameManager.Instance.GainLifePoints(true, aInt);
            GameManager.Instance.GainLifePoints(false, aInt);
            if (DamagePopupManager.Instance != null && !GameManager.Instance.isSimulating) {
                DamagePopupManager.Instance.ShowPopup(aInt, true, true);
                DamagePopupManager.Instance.ShowPopup(aInt, true, false);
            }
        }
        
        if (CardEffectManager.Instance != null && GameManager.Instance != null && !GameManager.Instance.isSimulating)
        {
            CardEffectManager.Instance.isWaitingForLuaYield = true;
            CardEffectManager.Instance.yieldReturnValue = null;
            CardEffectManager.Instance.StartCoroutine(WaitVisualTasksRoutine());
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("UI_Wait") });
        }
        return DynValue.Nil;
    }

    // --- STATUS BASE (Resgatados das requisições LUA) ---
    public bool IsTurnPlayer(object player) { return GetTurnPlayer() == ConvertToInt(player); }
    public int GetTurnCount() { return GameManager.Instance != null ? GameManager.Instance.turnCount : 0; }
    public int GetLP(object player) { 
        int pInt = ConvertToInt(player);
        if (pInt == 0) return GameManager.Instance.playerLP;
        if (pInt == 1) return GameManager.Instance.opponentLP;
        return 0; // Fallback para PLAYER_ALL/PLAYER_NONE
    }
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
        
        int phaseCode = 0;
        // Tradução direta para a tabela hexadecimal do constant.lua
        switch (PhaseManager.Instance.currentPhase)
        {
            case GamePhase.Draw: phaseCode = 0x01; break; // PHASE_DRAW
            case GamePhase.Standby: phaseCode = 0x02; break; // PHASE_STANDBY
            case GamePhase.Main1: phaseCode = 0x04; break; // PHASE_MAIN1
            case GamePhase.Battle: phaseCode = 0x80; break; // PHASE_BATTLE
            case GamePhase.Main2: phaseCode = 0x100; break; // PHASE_MAIN2
            case GamePhase.End: phaseCode = 0x200; break; // PHASE_END
        }

        if (phaseCode == 0x80)
        {
            phaseCode |= 0x20; // PHASE_DAMAGE
            phaseCode |= 0x40; // PHASE_DAMAGE_CAL
        }
        return phaseCode;
    }

    public bool IsPhase(object phaseObj)
    {
        return (GetCurrentPhase() & ConvertToInt(phaseObj)) != 0;
    }

    public bool CheckLPCost(object player, object cost)
    {
        int pInt = ConvertToInt(player);
        int cInt = ConvertToInt(cost);
        
        // EFFECT_LPCOST_REPLACE
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel.IsPlayerAffectedByEffect(pInt, 171)) return true;
        
        if (pInt == 0) return GameManager.Instance.playerLP >= cInt;
        if (pInt == 1) return GameManager.Instance.opponentLP >= cInt;
        return false;
    }

    public DynValue PayLPCost(object player, object cost)
    {
        int pInt = ConvertToInt(player);
        int cInt = ConvertToInt(cost);
        
        // EFFECT_LPCOST_REPLACE (A dedução mágica será feita direto no código LUA da Aura!)
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel.IsPlayerAffectedByEffect(pInt, 171)) return DynValue.Nil;
        
        if (pInt == 0) {
            if (GameManager.Instance.PayLifePoints(true, cInt) && DamagePopupManager.Instance != null && !GameManager.Instance.isSimulating)
                DamagePopupManager.Instance.ShowPopup(cInt, false, true);
        }
        else if (pInt == 1) {
            if (GameManager.Instance.PayLifePoints(false, cInt) && DamagePopupManager.Instance != null && !GameManager.Instance.isSimulating)
                DamagePopupManager.Instance.ShowPopup(cInt, false, false);
        }
        else if (pInt == 3) {
            bool s1 = GameManager.Instance.PayLifePoints(true, cInt);
            bool s2 = GameManager.Instance.PayLifePoints(false, cInt);
            if (s1 && DamagePopupManager.Instance != null && !GameManager.Instance.isSimulating) DamagePopupManager.Instance.ShowPopup(cInt, false, true);
            if (s2 && DamagePopupManager.Instance != null && !GameManager.Instance.isSimulating) DamagePopupManager.Instance.ShowPopup(cInt, false, false);
        }
        
        if (CardEffectManager.Instance != null && GameManager.Instance != null && !GameManager.Instance.isSimulating)
        {
            CardEffectManager.Instance.isWaitingForLuaYield = true;
            CardEffectManager.Instance.yieldReturnValue = null;
            CardEffectManager.Instance.StartCoroutine(WaitVisualTasksRoutine());
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("UI_Wait") });
        }
        return DynValue.Nil;
    }

    public IEnumerator WaitVisualTasksRoutine()
    {
        if (GameManager.Instance != null)
            yield return new WaitWhile(() => GameManager.Instance.pendingVisualTasks > 0);
            
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.yieldReturnValue = DynValue.Nil;
            CardEffectManager.Instance.isWaitingForLuaYield = false;
        }
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
    
    private void ApplyBattleDamage(int takingPlayer, int amount, LuaCard takerCard, LuaCard dealerCard)
    {
        if (amount <= 0) return;
        
        // EFFECT_NO_BATTLE_DAMAGE (ex: Waboku)
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel.IsPlayerAffectedByEffect(takingPlayer, 200)) return;
        
        // EFFECT_AVOID_BATTLE_DAMAGE (ex: Kuriboh, invulnerabilidade do monstro)
        if (takerCard != null && takerCard.IsHasEffect(201).Type != DataType.Nil) return;
        
        // EFFECT_REFLECT_BATTLE_DAMAGE (ex: Amazoness Swords Woman)
        if (takerCard != null && takerCard.IsHasEffect(202).Type != DataType.Nil) {
            if (takingPlayer == 0) GameManager.Instance.DamageOpponent(amount); else GameManager.Instance.DamagePlayer(amount);
            return;
        }
        
        // EFFECT_BOTH_BATTLE_DAMAGE (ex: Double-Edged Sword)
        if ((takerCard != null && takerCard.IsHasEffect(206).Type != DataType.Nil) || (dealerCard != null && dealerCard.IsHasEffect(206).Type != DataType.Nil)) {
            GameManager.Instance.DamagePlayer(amount); GameManager.Instance.DamageOpponent(amount); return;
        }
        
        if (takingPlayer == 0) GameManager.Instance.DamagePlayer(amount); else GameManager.Instance.DamageOpponent(amount);
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

        // EFFECT_DEFENSE_ATTACK (ex: Total Defense Shogun, Superheavy Samurai)
        var atkDefAttEff = attacker.registeredEffects.Find(e => e.code == 190);
        if (atkDefAttEff != null && atkCard.position == CardDisplay.BattlePosition.Defense) 
        {
            object val = atkDefAttEff.GetValue();
            // No OCG, se o valor for 1, o monstro aplica sua DEF como Dano em vez do ATK.
            if (val is double d && d == 1) atkPower = atkCard.currentDef;
        }
        
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
            ApplyBattleDamage(atkIsPlayer ? 1 : 0, atkPower, null, attacker);
        }
        else
        {
            int defPower = defCard.position == CardDisplay.BattlePosition.Attack ? defCard.currentAtk : defCard.currentDef;
            bool defIsPlayer = defCard.isPlayerCard;

            if (defCard.position == CardDisplay.BattlePosition.Attack)
            {
                if (atkPower > defPower)
                {
                    ApplyBattleDamage(defIsPlayer ? 0 : 1, atkPower - defPower, defender, attacker);
                    
                    if (!defender.IsIndestructableByBattle())
                    {
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(defCard);
                        defender.currentReason = 0x20; // REASON_BATTLE
                        if (defender.unityCard != null) defender.unityCard.AddStatus(0x4000); // STATUS_BATTLE_DESTROYED
                        GameManager.Instance.MoveCard(defCard, CardLocation.Graveyard, 0x20);
                    }
                }
                else if (atkPower < defPower)
                {
                    ApplyBattleDamage(atkIsPlayer ? 0 : 1, defPower - atkPower, attacker, defender);
                    
                    if (!attacker.IsIndestructableByBattle())
                    {
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(atkCard);
                        attacker.currentReason = 0x20;
                        if (attacker.unityCard != null) attacker.unityCard.AddStatus(0x4000); // STATUS_BATTLE_DESTROYED
                        GameManager.Instance.MoveCard(atkCard, CardLocation.Graveyard, 0x20);
                    }
                }
                else
                {
                    bool atkDies = !attacker.IsIndestructableByBattle();
                    bool defDies = !defender.IsIndestructableByBattle();

                    if (atkDies && DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(atkCard);
                    if (defDies && DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(defCard);

                    if (atkDies) {
                        attacker.currentReason = 0x20; 
                        if (attacker.unityCard != null) attacker.unityCard.AddStatus(0x4000);
                        GameManager.Instance.MoveCard(atkCard, CardLocation.Graveyard, 0x20);
                    }
                    if (defDies) {
                        defender.currentReason = 0x20;
                        if (defender.unityCard != null) defender.unityCard.AddStatus(0x4000);
                        GameManager.Instance.MoveCard(defCard, CardLocation.Graveyard, 0x20);
                    }
                }
            }
            else // Defesa
            {
                if (atkPower > defPower)
                {
                    if (atkCard.hasPiercing)
                    {
                        ApplyBattleDamage(defIsPlayer ? 0 : 1, atkPower - defPower, defender, attacker);
                    }
                    
                    if (!defender.IsIndestructableByBattle())
                    {
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(defCard);
                        defender.currentReason = 0x20;
                        if (defender.unityCard != null) defender.unityCard.AddStatus(0x4000); // STATUS_BATTLE_DESTROYED
                        GameManager.Instance.MoveCard(defCard, CardLocation.Graveyard, 0x20);
                    }
                }
                else if (atkPower < defPower)
                {
                    if (DuelFXManager.Instance != null) {
                        DuelFXManager.Instance.PlayAttackFail(atkCard);
                        DuelFXManager.Instance.PlayDefenseSuccessEffect(defCard);
                    }
                    ApplyBattleDamage(atkIsPlayer ? 0 : 1, defPower - atkPower, attacker, defender);
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

    public LuaEffect GetReasonEffect()
    {
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.chainManager != null && CardEffectManager.Instance.chainManager.resolvingLink != null)
            return CardEffectManager.Instance.chainManager.resolvingLink.effect;
        return currentActivatingEffect;
    }

    public int GetReasonPlayer()
    {
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.chainManager != null && CardEffectManager.Instance.chainManager.resolvingLink != null)
            return CardEffectManager.Instance.chainManager.resolvingLink.player;
        return GetTurnPlayer();
    }

    public void AssumeReset()
    {
        if (CardEffectManager.Instance != null)
        {
            foreach (var lc in CardEffectManager.Instance.activeLuaCards.Values)
            {
                lc.ClearAssumptions();
            }
        }
    }
}