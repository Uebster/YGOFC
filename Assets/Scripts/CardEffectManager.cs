using UnityEngine;
using System.Collections.Generic;

public partial class CardEffectManager : MonoBehaviour
{
    public static CardEffectManager Instance;
    
    public enum TargetType { Monster, Spell, Trap, Any }

    // Mapeia ID da carta -> Função de efeito
    private Dictionary<string, System.Action<CardDisplay>> effectDatabase;

    void Awake()
    {
        Instance = this;
        InitializeEffects();
    }

    void AddEffect(string id, System.Action<CardDisplay> effect)
    {
        if (!effectDatabase.ContainsKey(id))
        {
            effectDatabase.Add(id, effect);
        }
        else
        {
            Debug.LogWarning($"Tentativa de registrar efeito duplicado para ID: {id}");
        }
    }

    // Método principal chamado pelo GameManager/ChainManager
    public bool ExecuteCardEffect(CardDisplay card)
    {
        if (card == null || card.CurrentCardData == null) return false;

        string id = card.CurrentCardData.id;

        if (effectDatabase.ContainsKey(id))
        {
            Debug.Log($"Executando efeito da carta: {card.CurrentCardData.name} (ID: {id})");
            effectDatabase[id].Invoke(card);
            return true;
        }
        else
        {
            // Debug.LogWarning($"Efeito não implementado para: {card.CurrentCardData.name} (ID: {id})");
            return false;
        }
    }

    // Métodos de Eventos (Implementados em CardEffectManager_Impl.cs)
    // public void OnPhaseStart(GamePhase phase);
    // public void OnCardSentToGraveyard(CardData card, bool isOwnerPlayer);
    // public void OnSpecialSummon(CardDisplay summonedCard);
    // public void OnDamageTaken(bool isPlayer, int amount);
    // public void OnCardLeavesField(CardDisplay card);

    void DestroyAllMonsters(bool targetOpponent, bool targetPlayer)
    {
        List<CardDisplay> toDestroy = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            if (targetPlayer) CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, toDestroy);
            if (targetOpponent) CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, toDestroy);
        }
        foreach (var monster in toDestroy)
        {
            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(monster);
            GameManager.Instance.SendToGraveyard(monster.CurrentCardData, monster.isPlayerCard);
            Destroy(monster.gameObject);
        }
    }

    void CollectCards(Transform[] zones, List<CardDisplay> list)
    {
        foreach (var zone in zones)
        {
            if (zone != null && zone.childCount > 0)
            {
                var cd = zone.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null) list.Add(cd);
            }
        }
    }

    void DestroyCards(List<CardDisplay> cards, bool isPlayerSource)
    {
        foreach (var card in cards)
        {
            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(card);
            GameManager.Instance.SendToGraveyard(card.CurrentCardData, card.isPlayerCard);
            Destroy(card.gameObject);
        }
    }

    void CollectMonsters(Transform[] zones, List<CardDisplay> list)
    {
        foreach (var zone in zones)
        {
            if (zone.childCount > 0)
            {
                var cd = zone.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null) list.Add(cd);
            }
        }
    }

    bool IsValidTarget(CardDisplay target, TargetType type)
    {
        if (!target.isOnField) return false;
        switch (type)
        {
            case TargetType.Monster: return target.CurrentCardData.type.Contains("Monster");
            case TargetType.Spell: return target.CurrentCardData.type.Contains("Spell");
            case TargetType.Trap: return target.CurrentCardData.type.Contains("Trap");
            case TargetType.Any: return true;
            default: return false;
        }
    }
}
