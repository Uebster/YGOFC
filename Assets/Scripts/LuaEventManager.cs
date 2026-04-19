using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LuaEventManager
{
    private CardEffectManager core;

    public LuaEventManager(CardEffectManager coreManager)
    {
        this.core = coreManager;
    }

    public void TriggerLuaEvent(int eventCode, object triggerArgs)
    {
        if (GameManager.Instance == null || GameManager.Instance.duelFieldUI == null) return;

        // Identifica a carta que originou o evento para evitar re-trigger
        LuaCard sourceCard = null;
        if (triggerArgs is LuaCard lc) sourceCard = lc;
        else if (triggerArgs is CardDisplay cd) sourceCard = core.EnsureCardScriptLoaded(cd);

        // 1. Processa efeitos globais e gatilhos passivos registrados na memória
        if (core.luaDuel != null && core.luaDuel.globalEffects != null)
        {
            var matchingGlobal = core.luaDuel.globalEffects.FindAll(e => e.code == eventCode);
            foreach (var effect in matchingGlobal)
            {
                if (core.CanActivateEffect(effect.owner, effect, 0, triggerArgs))
                {
                    if (effect.operationFunc != null)
                        core.StartCoroutine(core.RunLuaCoroutine(effect.operationFunc, effect, 0, triggerArgs, -1));
                }
            }
        }

        // 2. Processa efeitos das cartas físicas no campo
        List<CardDisplay> allField = new List<CardDisplay>();
        CollectCards(GameManager.Instance.duelFieldUI.playerMonsterZones, allField);
        CollectCards(GameManager.Instance.duelFieldUI.opponentMonsterZones, allField);
        CollectCards(GameManager.Instance.duelFieldUI.playerSpellZones, allField);
        CollectCards(GameManager.Instance.duelFieldUI.opponentSpellZones, allField);

        // 3. Inclui a mão para processar efeitos reativos de cartas na mão (ex: Kuriboh, Deal with Dark Ruler)
        foreach(var go in GameManager.Instance.playerHand) { if (go != null) { var cd = go.GetComponent<CardDisplay>(); if (cd != null) allField.Add(cd); } }
        foreach(var go in GameManager.Instance.opponentHand) { if (go != null) { var cd = go.GetComponent<CardDisplay>(); if (cd != null) allField.Add(cd); } }

        foreach(var c in allField)
        {
            // NOVO: Pula a carta que originou o evento para não duplicar a corrente
            if (sourceCard != null && c == sourceCard.unityCard) continue;

            LuaCard lc_loop = core.EnsureCardScriptLoaded(c);
            if (lc_loop == null) continue;

            // Impede a Engine de auto-ativar cartas Manuais (Spells/Traps) ou Quick Effects. Eles devem ser ativados pelo jogador na Response Window!
            var matchingEffects = lc_loop.registeredEffects.FindAll(e => e.code == eventCode && e.type != 0x0010 && e.type != 0x0100 && e.type != 0x0080);
            foreach(var effect in matchingEffects)
            {
                if (core.CanActivateEffect(lc_loop, effect, lc_loop.GetControler(), triggerArgs))
                {
                    core.StartCoroutine(core.chainManager.BuildAndResolveChainRoutine(lc_loop, effect, triggerArgs, lc_loop.GetControler(), null));
                }
            }
        }
    }

    private void CollectCards(Transform[] zones, List<CardDisplay> list)
    {
        if (zones == null) return;
        foreach (var z in zones)
        {
            if (z != null && z.childCount > 0)
            {
                var cd = z.GetComponentInChildren<CardDisplay>();
                if (cd != null) list.Add(cd);
            }
        }
    }

    public void OnSummon(CardDisplay card) { 
        LuaCard lc = core.EnsureCardScriptLoaded(card);
        if (lc == null) lc = new LuaCard(card);

        // Adiciona efeitos contínuos ao entrar em campo
        var fieldEffects = lc.registeredEffects.FindAll(e => (e.type & 0x0002) != 0);
        core.continuousFieldEffects.AddRange(fieldEffects.Where(e => !core.continuousFieldEffects.Contains(e)));
        if (fieldEffects.Count > 0)
            core.ApplyAllContinuousEffects();
        
        var singleEffects = lc.registeredEffects.FindAll(e => e.code == 1100 && (e.type & 0x0001) != 0); // EFFECT_TYPE_SINGLE
        foreach(var e in singleEffects) 
        {
            if (core.CanActivateEffect(lc, e, lc.GetControler(), null))
                core.StartCoroutine(core.chainManager.BuildAndResolveChainRoutine(lc, e, null, lc.GetControler(), null));
        }
        TriggerLuaEvent(1100, lc); // EVENT_SUMMON_SUCCESS
    }
    
    public void OnSet(CardDisplay card) { }
    public void OnBattlePositionChanged(CardDisplay card) { }
    public void OnDamageDealt(CardDisplay attacker, CardDisplay target, int amount) { }
    public void OnCounterTrapResolved(CardDisplay trap) { }
    public void OnCardAddedToHand(CardDisplay card) { 
        core.EnsureCardScriptLoaded(card);
        // Recalcula stats caso uma carta como A Legendary Ocean esteja em campo
        core.ApplyAllContinuousEffects();
    }
    public void OnTribute(CardDisplay card) { }
    public void OnCardDiscarded(CardDisplay card, bool causedByOpponent)
    {
        // Recalcula stats caso uma carta na mão tenha sido descartada
        core.ApplyAllContinuousEffects();
    }
    public void OnCardDrawn(CardData card, bool isPlayer) { }
    public void OnSpecialSummon(CardDisplay card) { }
    public void OnControlSwitched(CardDisplay card) { }
    
    public void OnPhaseStart(GamePhase phase) { 
        if (phase == GamePhase.End) {
            core.CleanAllExpiredModifiers(); 
            core.luaDuel.playerFlags.Clear(); // Limpa as flags temporárias de turno
            if (core.luaDuel.endTurnCallbacks != null) {
                var callbacksToRun = new List<MoonSharp.Interpreter.Closure>(core.luaDuel.endTurnCallbacks);
                foreach (var cb in callbacksToRun) {
                    try { core.luaEngine.Call(cb); } catch (System.Exception e) { Debug.LogWarning($"Erro no AddValuesReset: {e.Message}"); }
                }
            }
        }
        TriggerLuaEvent(4096, null); // EVENT_PHASE
        core.StartCoroutine(core.OpenFastEffectWindow($"Início da {phase}", 4096, null));
    }
    
    public void OnPreDrawPhase(bool isPlayerTurn, System.Action onContinue) { onContinue?.Invoke(); }
    
    public void OnCardSentToGraveyard(CardData card, bool isOwnerPlayer, CardLocation fromLocation, SendReason reason) 
    { 
        if (card == null) return;

        LuaCard lc = LuaScriptLoader.LoadScriptForData(card, core.luaEngine);
        if (lc == null)
        {
            lc = new LuaCard(card);
        }

        lc.previousLocation = fromLocation; // Memoriza de onde veio para o IsPreviousLocation do LUA!
        lc.ownerPlayerIndex = isOwnerPlayer ? 0 : 1;
        int tp = lc.GetControler();

        Debug.Log($"[LuaEventManager] {card.name} enviado ao GY. PreviousLocation: {fromLocation}, PreviousController: {tp}");

        var gyEffects = lc.registeredEffects.FindAll(e => e.code == 1014 && (e.type & 0x0001) != 0); // EVENT_TO_GRAVE
        Debug.Log($"[Surgical Log] Carta '{card.name}' caiu no GY. Efeitos EVENT_TO_GRAVE (1014) encontrados: {gyEffects.Count}");
        
        foreach(var e in gyEffects)
        {
            bool canAct = core.CanActivateEffect(lc, e, tp, lc);
            Debug.Log($"[Surgical Log] CanActivateEffect para {card.name} (chk=0) retornou: {canAct}");
            
            if (canAct)
                core.StartCoroutine(core.chainManager.BuildAndResolveChainRoutine(lc, e, lc, tp, null));
        }
        TriggerLuaEvent(1014, lc); 
        core.StartCoroutine(core.OpenFastEffectWindow($"Queda de {card.name}", 1014, lc));
    }

    public void OnDamageTaken(bool isPlayer, int amount) { }
    public void OnLifePointsGained(bool isPlayer, int amount) { }
    
    public void OnCardEquipped(CardDisplay equip, CardDisplay target) {
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayEquipEffect(equip, target);
        core.RecalculateStats(target);
    }
    
    public void OnSpellActivated(CardDisplay spell) { }

    public void OnCardLeavesField(CardDisplay card) {
        CardLink[] allLinks = UnityEngine.Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);

        // Remove efeitos contínuos
        LuaCard lc = core.activeLuaCards.ContainsKey(card) ? core.activeLuaCards[card] : null;
        if (lc != null && core.continuousFieldEffects.RemoveAll(e => e.owner == lc) > 0)
        {
            core.ApplyAllContinuousEffects();
        }

        foreach (var link in allLinks) if (link.source == card && link.type == CardLink.LinkType.Equipment && link.target != null) core.StartCoroutine(core.RecalculateStatsNextFrame(link.target));
        if (core.activeLuaCards.ContainsKey(card)) core.activeLuaCards.Remove(card);
        foreach (var link in allLinks) if (link.target == card && link.type == CardLink.LinkType.Equipment && link.source != null && link.source.isOnField) {
            GameManager.Instance.SendToGraveyard(link.source.CurrentCardData, link.source.isPlayerCard);
            GameObject.Destroy(link.source.gameObject);
        }
        if (core.blockedZonesByCard.ContainsKey(card)) {
            if (GameManager.Instance.duelFieldUI != null) foreach (var z in core.blockedZonesByCard[card]) GameManager.Instance.duelFieldUI.UnblockZone(z);
            core.blockedZonesByCard.Remove(card);
        }
    }
    
    public void OnBattleEnd(CardDisplay attacker, CardDisplay target) { 
        if (target != null && target.CurrentCardData != null && (GameManager.Instance.GetPlayerGraveyard().Contains(target.CurrentCardData) || GameManager.Instance.GetOpponentGraveyard().Contains(target.CurrentCardData))) {
            LuaCard deadCard = core.EnsureCardScriptLoaded(target);
            if (deadCard == null) deadCard = new LuaCard(target);

            var singleEffects = deadCard.registeredEffects.FindAll(e => e.code == 1010 && (e.type & 0x0001) != 0); 
            foreach(var e in singleEffects) { 
                if (core.CanActivateEffect(deadCard, e, deadCard.GetControler(), deadCard)) 
                    core.StartCoroutine(core.chainManager.BuildAndResolveChainRoutine(deadCard, e, deadCard, deadCard.GetControler(), null)); 
            }
            TriggerLuaEvent(1010, deadCard);
        }
    }
}