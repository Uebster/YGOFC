using UnityEngine;
using System.Collections.Generic;

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

        List<CardDisplay> allField = new List<CardDisplay>();
        CollectCards(GameManager.Instance.duelFieldUI.playerMonsterZones, allField);
        CollectCards(GameManager.Instance.duelFieldUI.opponentMonsterZones, allField);
        CollectCards(GameManager.Instance.duelFieldUI.playerSpellZones, allField);
        CollectCards(GameManager.Instance.duelFieldUI.opponentSpellZones, allField);

        foreach(var c in allField)
        {
            LuaCard lc = core.EnsureCardScriptLoaded(c);
            if (lc == null) continue;

            var matchingEffects = lc.registeredEffects.FindAll(e => e.code == eventCode && (e.type & 0x0001) == 0);
            foreach(var effect in matchingEffects)
            {
                if (core.CanActivateEffect(lc, effect, lc.GetControler(), triggerArgs))
                {
                    core.StartCoroutine(core.chainManager.BuildAndResolveChainRoutine(lc, effect, triggerArgs, lc.GetControler(), null));
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
                var cd = z.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null) list.Add(cd);
            }
        }
    }

    public void OnSummon(CardDisplay card) { 
        LuaCard lc = core.EnsureCardScriptLoaded(card);
        if (lc != null) {
            var singleEffects = lc.registeredEffects.FindAll(e => e.code == 11 && (e.type & 0x0001) != 0); // EFFECT_TYPE_SINGLE
            foreach(var e in singleEffects) 
            {
                if (core.CanActivateEffect(lc, e, lc.GetControler(), null))
                    core.StartCoroutine(core.chainManager.BuildAndResolveChainRoutine(lc, e, null, lc.GetControler(), null));
            }
        }
        TriggerLuaEvent(11, lc); // EVENT_SUMMON_SUCCESS
    }
    
    public void OnSet(CardDisplay card) { }
    public void OnBattlePositionChanged(CardDisplay card) { }
    public void OnDamageDealt(CardDisplay attacker, CardDisplay target, int amount) { }
    public void OnCounterTrapResolved(CardDisplay trap) { }
    public void OnCardAddedToHand(CardDisplay card) { }
    public void OnTribute(CardDisplay card) { }
    public void OnCardDiscarded(CardDisplay card, bool causedByOpponent) { }
    public void OnCardDrawn(CardData card, bool isPlayer) { }
    public void OnSpecialSummon(CardDisplay card) { }
    public void OnControlSwitched(CardDisplay card) { }
    
    public void OnPhaseStart(GamePhase phase) { 
        if (phase == GamePhase.End) core.CleanAllExpiredModifiers(); 
        TriggerLuaEvent(4096, null); // EVENT_PHASE
    }
    
    public void OnPreDrawPhase(bool isPlayerTurn, System.Action onContinue) { onContinue?.Invoke(); }
    
    public void OnCardSentToGraveyard(CardData card, bool isOwnerPlayer, CardLocation fromLocation, SendReason reason) 
    { 
        if (card == null) return;

        LuaCard lc = LuaScriptLoader.LoadScriptForData(card, core.luaEngine);
        if (lc != null)
        {
            lc.previousLocation = fromLocation; // Memoriza de onde veio para o IsPreviousLocation do LUA!
            int tp = isOwnerPlayer ? 0 : 1;

            var gyEffects = lc.registeredEffects.FindAll(e => e.code == 1014 && (e.type & 0x0001) != 0); // EVENT_TO_GRAVE
            foreach(var e in gyEffects)
            {
                if (core.CanActivateEffect(lc, e, tp, lc))
                    core.StartCoroutine(core.chainManager.BuildAndResolveChainRoutine(lc, e, lc, tp, null));
            }
        }
        TriggerLuaEvent(1014, lc); 
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
            if (deadCard != null) { var singleEffects = deadCard.registeredEffects.FindAll(e => e.code == 1010 && (e.type & 0x0001) != 0); foreach(var e in singleEffects) { if (core.CanActivateEffect(deadCard, e, deadCard.GetControler(), deadCard)) core.StartCoroutine(core.chainManager.BuildAndResolveChainRoutine(deadCard, e, deadCard, deadCard.GetControler(), null)); } }
            TriggerLuaEvent(1010, deadCard);
        }
    }
}