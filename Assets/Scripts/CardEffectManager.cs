// Assets/Scripts/CardEffectManager.cs
using UnityEngine;
using System.Collections.Generic;

public enum CardLocation { Hand, Deck, Field, ExtraDeck, Graveyard, Banished, Unknown }
public enum SendReason { Battle, Effect, Cost, Tribute, Destroyed, Discarded, Mill, Return, Rule, Unknown }

public class CardEffectManager : MonoBehaviour
{
    public static CardEffectManager Instance;

    public enum TargetType { Monster, Spell, Trap, Any }

    // Dicionário de zonas físicas bloqueadas (ex: Ojama King)
    public Dictionary<CardDisplay, List<Transform>> blockedZonesByCard = new Dictionary<CardDisplay, List<Transform>>();

    void Awake()
    {
        Instance = this;
        // O Interpretador MoonSharp LUA será inicializado aqui no futuro!
    }

    public bool ExecuteCardEffect(CardDisplay card)
    {
        if (card == null || card.CurrentCardData == null) return false;
        
        Debug.Log($"[API] Preparando para executar script LUA para: {card.CurrentCardData.name}");
        // A lógica de ler o .lua da pasta e rodar virá no próximo passo!
        return false;
    }

    // --- HOOKS DA ENGINE (O LUA VAI SE INSCREVER NELES DEPOIS) ---
    public void OnSummon(CardDisplay card) { }
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
    public void OnPhaseStart(GamePhase phase) { if (phase == GamePhase.End) CleanAllExpiredModifiers(); }
    public void OnPreDrawPhase(bool isPlayerTurn, System.Action onContinue) { onContinue?.Invoke(); }
    public void OnCardSentToGraveyard(CardData card, bool isOwnerPlayer, CardLocation fromLocation, SendReason reason) { }
    public void OnDamageTaken(bool isPlayer, int amount) { }
    public void OnLifePointsGained(bool isPlayer, int amount) { }
    public void OnCardEquipped(CardDisplay equip, CardDisplay target) { }
    public void OnSpellActivated(CardDisplay spell) { }

    public void OnCardLeavesField(CardDisplay card)
    {
        // Limpeza Genérica Obrigatória (Isso continua sendo C# nativo para não bugar a engine)
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<Transform> allZones = new List<Transform>();
            allZones.AddRange(GameManager.Instance.duelFieldUI.playerMonsterZones);
            allZones.AddRange(GameManager.Instance.duelFieldUI.opponentMonsterZones);
            foreach (var zone in allZones)
            {
                if (zone.childCount > 0)
                {
                    CardDisplay target = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (target != null) target.RemoveModifiersFromSource(card);
                }
            }
        }

        // Destrói equipamentos físicos que dependem desta carta
        CardLink[] links = Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);
        foreach (var link in links)
        {
            if (link.target == card && link.type == CardLink.LinkType.Equipment && link.source != null && link.source.isOnField)
            {
                GameManager.Instance.SendToGraveyard(link.source.CurrentCardData, link.source.isPlayerCard);
                Destroy(link.source.gameObject);
            }
        }

        // Libera zonas bloqueadas por cartas contínuas
        if (blockedZonesByCard.ContainsKey(card))
        {
            if (GameManager.Instance.duelFieldUI != null)
            {
                foreach (var z in blockedZonesByCard[card]) GameManager.Instance.duelFieldUI.UnblockZone(z);
            }
            blockedZonesByCard.Remove(card);
        }
    }

    // --- HOOKS DE BATALHA ---
    public void OnAttackDeclared(CardDisplay attacker, CardDisplay target, System.Action onContinue) { onContinue?.Invoke(); }
    public void OnDamageCalculation(CardDisplay attacker, CardDisplay target, System.Action onContinue) { onContinue?.Invoke(); }
    public void OnBattleEnd(CardDisplay attacker, CardDisplay target) { }
    public bool CanDeclareAttack(CardDisplay attacker) { return true; }
    public bool IsAttackPreventedByContinuousEffect(CardDisplay attacker) { return false; }
    public bool IsAttackRestricted(CardDisplay attacker) { return false; }

    // --- REGRAS CONTÍNUAS GLOBAIS ---
    public bool IsSummonRestricted(bool isSpecialSummon) { return false; }
    public bool ShouldBanishInsteadOfGraveyard() { return false; }
    public string GetEffectiveRace(CardDisplay card) { return !string.IsNullOrEmpty(card.temporaryRace) ? card.temporaryRace : (card.isTrapMonster ? card.trapMonsterRace : card.CurrentCardData.race); }
    public string GetEffectiveAttribute(CardDisplay card) { return !string.IsNullOrEmpty(card.temporaryAttribute) ? card.temporaryAttribute : (card.isTrapMonster ? card.trapMonsterAttribute : card.CurrentCardData.attribute); }
    public bool HasAttribute(CardDisplay card, string attribute) { return GetEffectiveAttribute(card).Equals(attribute, System.StringComparison.OrdinalIgnoreCase); }
    public bool CheckChainEnergy(bool isPlayer) { return true; }
    public bool CheckSpatialCollapse(bool isPlayer) { return true; }
    public bool CheckRivalryOfWarlords(bool isPlayer, string newRace) { return true; }
    public bool IsFusionSubstitute(string cardIdOrName) { return false; }

    // --- HELPERS E UTILITÁRIOS (A PONTE LUA VAI CHAMÁ-LOS) ---

    public void Effect_DirectDamage(CardDisplay source, int amount)
    {
        if (source.isPlayerCard) GameManager.Instance.DamageOpponent(amount);
        else GameManager.Instance.DamagePlayer(amount);
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDamageEffect(Vector3.zero);
    }

    public void Effect_GainLP(CardDisplay source, int amount)
    {
        GameManager.Instance.GainLifePoints(source.isPlayerCard, amount);
    }

    public bool Effect_PayLP(CardDisplay source, int amount)
    {
        return GameManager.Instance.PayLifePoints(source.isPlayerCard, amount);
    }

    public List<CardDisplay> GetEquippedCards(CardDisplay target)
    {
        List<CardDisplay> equipped = new List<CardDisplay>();
        CardLink[] links = Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);
        foreach (var link in links)
            if (link.target == target && link.type == CardLink.LinkType.Equipment && link.source != null)
                equipped.Add(link.source);
        return equipped;
    }

    public void CleanAllExpiredModifiers()
    {
        if (GameManager.Instance.duelFieldUI == null) return;
        List<Transform> allZones = new List<Transform>();
        allZones.AddRange(GameManager.Instance.duelFieldUI.playerMonsterZones);
        allZones.AddRange(GameManager.Instance.duelFieldUI.opponentMonsterZones);

        foreach (var zone in allZones)
        {
            if (zone.childCount > 0)
            {
                CardDisplay cd = zone.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null) cd.CleanExpiredModifiers();
            }
        }
    }

    public void RollDice(int amount, System.Action<List<int>> onResult)
    {
        List<int> results = new List<int>();
        for (int i = 0; i < amount; i++) results.Add(Random.Range(1, 7));
        onResult?.Invoke(results);
    }

    public void SetClockCounter(CardDisplay target, int turns)
    {
        if (target != null) target.turnCounter = turns; 
    }

    public void TickClock(CardDisplay card, System.Action onComplete)
    {
        if (card == null || card.turnCounter <= 0) { onComplete?.Invoke(); return; }
        int oldTurns = card.turnCounter;
        card.turnCounter--; 
        if (GameManager.Instance != null && GameManager.Instance.turnClockUI != null && GameManager.Instance.enableTurnClockVisuals)
            GameManager.Instance.turnClockUI.AnimateTick(card.CurrentCardData.name, oldTurns, card.turnCounter, card.maxTurnCounter, onComplete);
        else onComplete?.Invoke(); 
    }
}