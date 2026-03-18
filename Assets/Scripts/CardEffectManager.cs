using UnityEngine;
using System.Collections.Generic;

public enum CardLocation { Hand, Deck, Field, ExtraDeck, Graveyard, Banished, Unknown }
public enum SendReason { Battle, Effect, Cost, Tribute, Destroyed, Discarded, Mill, Return, Rule, Unknown }

public partial class CardEffectManager : MonoBehaviour
{
    public static CardEffectManager Instance;

    public enum TargetType { Monster, Spell, Trap, Any }

    // Mapeia ID da carta -> Função de efeito
    private Dictionary<string, System.Action<CardDisplay>> effectDatabase;

    // Lista para reviver monstros na próxima Standby Phase (Vampire Lord, etc)
    public List<CardData> reviveNextStandby = new List<CardData>();

    public List<CardData> pharaohsTreasureCards = new List<CardData>();

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

        // --- VERIFICAÇÕES DE NEGAÇÃO CONTÍNUA ---

        // 1655 - Skill Drain: Nega efeitos de monstros face-up no campo
        if (card.CurrentCardData.type.Contains("Monster") && card.isOnField && !card.isFlipped)
        {
            if (GameManager.Instance.IsCardActiveOnField("1655"))
            {
                Debug.Log($"Efeito de {card.CurrentCardData.name} negado por Skill Drain.");
                return false;
            }
        }

        // 1858 - The End of Anubis: Nega efeitos de cartas no Cemitério
        if (card.isInPile && (GameManager.Instance.GetPlayerGraveyard().Contains(card.CurrentCardData) || GameManager.Instance.GetOpponentGraveyard().Contains(card.CurrentCardData)))
        {
            if (GameManager.Instance.IsCardActiveOnField("1858"))
            {
                Debug.Log($"Efeito de {card.CurrentCardData.name} no GY negado por The End of Anubis.");
                return false;
            }
        }

        // 1460 - Prohibition
        if (GameManager.Instance.prohibitedCards.Contains(card.CurrentCardData.name))
        {
            Debug.Log($"Efeito de {card.CurrentCardData.name} negado por Prohibition.");
            return false;
        }

        // 1489 - Rare Metalmorph: Nega magia que dê alvo
        if (card.CurrentCardData.type.Contains("Spell") && ChainManager.Instance != null)
        {
            var link = ChainManager.Instance.GetLastChainLink();
            if (link != null && link.target != null)
            {
                bool protectedByMetalmorph = false;
                CardDisplay metalmorphCard = null;
                CardLink[] links = Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);
                foreach (var cl in links) {
                    if (cl.target == link.target && cl.source != null && cl.source.CurrentCardData.id == "1489" && cl.source.spellCounters == 0) {
                        protectedByMetalmorph = true;
                        metalmorphCard = cl.source;
                        break;
                    }
                }
                if (protectedByMetalmorph) {
                    Debug.Log("Rare Metalmorph: Anulando efeito de magia que deu alvo!");
                    metalmorphCard.spellCounters = 1; // Registra o uso único
                    return false;
                }
            }
        }

        // 1443 - Pole Position
        if (card.CurrentCardData.type.Contains("Spell") && ChainManager.Instance != null)
        {
            var link = ChainManager.Instance.GetLastChainLink();
            if (link != null && link.target != null && link.target.CurrentCardData.type.Contains("Monster"))
            {
                if (GameManager.Instance.IsCardActiveOnField("1443") || GameManager.Instance.IsCardActiveOnField("Pole Position"))
                {
                    int maxAtk = -1;
                    List<CardDisplay> all = new List<CardDisplay>();
                    if (GameManager.Instance.duelFieldUI != null) {
                        CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
                        CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
                    }
                    foreach(var m in all) if (!m.isFlipped && m.currentAtk > maxAtk) maxAtk = m.currentAtk;
                    if (link.target.currentAtk == maxAtk) {
                        Debug.Log("Pole Position: Magia negada pois o alvo é o monstro com maior ATK.");
                        return false;
                    }
                }
            }
        }

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

    public void OnSummon(CardDisplay card) { OnSummonImpl(card); }
    public void OnSet(CardDisplay card) { OnSetImpl(card); }

    public void OnBattlePositionChanged(CardDisplay card) { OnBattlePositionChangedImpl(card); }
    public void OnDamageDealt(CardDisplay attacker, CardDisplay target, int amount) { OnDamageDealtImpl(attacker, target, amount); }
    public void OnCounterTrapResolved(CardDisplay trap) { OnCounterTrapResolvedImpl(trap); }
    public void OnCardAddedToHand(CardDisplay card) { OnCardAddedToHandImpl(card); }
    public void OnTribute(CardDisplay card) { OnTributeImpl(card); }
    public void OnCardDiscarded(CardDisplay card, bool causedByOpponent) { OnCardDiscardedImpl(card, causedByOpponent); }
    public void OnCardDrawn(CardData card, bool isPlayer) { OnCardDrawnImpl(card, isPlayer); }
    public void OnSpecialSummon(CardDisplay card) { OnSpecialSummonImpl(card); }
    public void OnPreDrawPhase(bool isPlayerTurn, System.Action onContinue) { OnPreDrawPhaseImpl(isPlayerTurn, onContinue); }
    public void OnControlSwitched(CardDisplay card) { OnControlSwitchedImpl(card); }

    // --- VALIDAÇÕES DE REGRAS DE EFEITO ---

    public bool invertDecks = false; // Para Convulsion of Nature (0327)

    public string dnaTransplantDeclaredType = ""; // Para DNA Transplant (0391)

    public bool CheckChainEnergy(bool isPlayer)
    {
        if (GameManager.Instance.IsCardActiveOnField("0284")) // Chain Energy
        {
            if (!GameManager.Instance.PayLifePoints(isPlayer, 500))
            {
                Debug.Log("Chain Energy: LP insuficientes para realizar a ação.");
                return false;
            }
        }
        return true;
    }

    public bool CheckSpatialCollapse(bool isPlayer)
    {
        if (GameManager.Instance.IsCardActiveOnField("1716")) // Spatial Collapse
        {
            if (GameManager.Instance.GetFieldCardCount(isPlayer) >= 5)
            {
                Debug.LogWarning("Spatial Collapse: Limite de 5 cartas no campo atingido.");
                return false;
            }
        }
        return true;
    }

    public bool CheckRivalryOfWarlords(bool isPlayer, string newRace)
    {
        if (GameManager.Instance.IsCardActiveOnField("1541")) // Rivalry of Warlords
        {
            bool conflict = false;
            if (GameManager.Instance.duelFieldUI != null)
            {
                Transform[] zones = isPlayer ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
                foreach (var z in zones)
                {
                    if (z.childCount > 0)
                    {
                        var m = z.GetChild(0).GetComponent<CardDisplay>();
                        if (m != null && m.CurrentCardData.race != newRace) conflict = true;
                    }
                }
            }
            if (conflict) 
            { 
                Debug.LogWarning($"Rivalry of Warlords: Você só pode controlar 1 Tipo. Tentativa: {newRace}."); 
                return false; 
            }
        }
        return true;
    }

    /// <summary>
    /// Retorna verdadeiro se a carta possuir o dom de substituir 1 material de fusão nomeado.
    /// </summary>
    public bool IsFusionSubstitute(string cardIdOrName)
    {
        string[] subs = { "0781", "1315", "2037", "1850", "1856", "1877", "Goddess with the Third Eye", "Mystical Sheep #1", "Versago the Destroyer", "The Dark - Hex-Sealed Fusion", "The Earth - Hex-Sealed Fusion", "The Light - Hex-Sealed Fusion" };
        return System.Array.Exists(subs, element => element == cardIdOrName);
    }

    // --- HELPERS DE SISTEMAS GLOBAIS (MINIGAMES) ---

    /// <summary>
    /// Rola um ou mais dados (1 a 6) e retorna a lista de resultados no callback.
    /// Prepara o terreno para futura UI de dados 3D.
    /// </summary>
    public void RollDice(int amount, System.Action<List<int>> onResult)
    {
        List<int> results = new List<int>();
        for (int i = 0; i < amount; i++) results.Add(Random.Range(1, 7));
        // TODO: Chamar GameManager.Instance.ShowDiceUI(results, onResult) no futuro
        onResult?.Invoke(results);
    }

    /// <summary>
    /// Ativa o sistema visual de relógio e contagem regressiva em uma carta.
    /// </summary>
    public void SetClockCounter(CardDisplay target, int turns)
    {
        if (target == null) return;
        target.turnCounter = turns; // O setter do turnCounter já aciona o UpdateTurnClockVisual nativamente
    }

    /// <summary>
    /// Processa o tick de um relógio visualmente chamando o painel central, e depois aplica a lógica de continuidade.
    /// Uso recomendado dentro de OnPhaseStart para decrementar Espadas da Luz Reveladora, etc.
    /// </summary>
    public void TickClock(CardDisplay card, System.Action onComplete)
    {
        if (card == null || card.turnCounter <= 0) { onComplete?.Invoke(); return; }

        int oldTurns = card.turnCounter;
        card.turnCounter--; // Decrementa os dados

        if (GameManager.Instance != null && GameManager.Instance.turnClockUI != null && GameManager.Instance.enableTurnClockVisuals)
        {
            GameManager.Instance.turnClockUI.AnimateTick(card.CurrentCardData.name, oldTurns, card.turnCounter, card.maxTurnCounter, onComplete);
        }
        else { onComplete?.Invoke(); }
    }

    // Métodos de Eventos (Implementados em CardEffectManager_Impl.cs)
    // public void OnPhaseStart(GamePhase phase);
    // public void OnCardSentToGraveyard(CardData card, bool isOwnerPlayer);
    // public void OnSpecialSummon(CardDisplay summonedCard);
    // public void OnDamageTaken(bool isPlayer, int amount);
    // public void OnCardLeavesField(CardDisplay card);
    // public bool IsAttackRestricted(CardDisplay attacker);
    // public void OnAttackDeclared(CardDisplay attacker, CardDisplay target, System.Action onContinue);
    // public void OnDamageCalculation(CardDisplay attacker, CardDisplay target);
    // public void OnBattleEnd(CardDisplay attacker, CardDisplay target);
    // public void OnLifePointsGained(bool isPlayer, int amount);
    partial void OnSummonImpl(CardDisplay card);
    partial void OnSetImpl(CardDisplay card);
    partial void OnBattlePositionChangedImpl(CardDisplay card);
    partial void OnCounterTrapResolvedImpl(CardDisplay trap);
    partial void OnCardAddedToHandImpl(CardDisplay card);
    partial void OnTributeImpl(CardDisplay card);
    partial void OnCardDiscardedImpl(CardDisplay card, bool causedByOpponent);
    partial void OnSpecialSummonImpl(CardDisplay card);
    partial void OnCardDrawnImpl(CardData card, bool isPlayer);
    partial void OnPreDrawPhaseImpl(bool isPlayerTurn, System.Action onContinue);
    partial void OnControlSwitchedImpl(CardDisplay card);

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
        // 1401 - Pandemonium Watchbear (Proteção)
        cards.RemoveAll(c => c.CurrentCardData.name == "Pandemonium" && GameManager.Instance.IsCardActiveOnField("1401"));

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
