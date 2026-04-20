using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance;

    [Header("Deck Visuals")]
    public PileDisplay playerDeckDisplay;
    public PileDisplay opponentDeckDisplay;

    public PileDisplay playerExtraDeckDisplay; // Novo
    public PileDisplay opponentExtraDeckDisplay; // Novo

    private List<CardData> playerDeck = new List<CardData>();
    private List<CardData> opponentDeck = new List<CardData>();
    private List<CardData> playerExtraDeck = new List<CardData>(); // Novo
    private List<CardData> opponentExtraDeck = new List<CardData>(); // Novo


    private bool hasDrawnThisTurn = false;

    void Awake()
    {
        Instance = this;
    }

    public void SetupDecks(List<CardData> newPlayerMainDeck, List<CardData> newPlayerExtraDeck, List<CardData> newOpponentMainDeck, List<CardData> newOpponentExtraDeck)
    {
        // Auto-atribuição para robustez, caso não esteja definido no Inspector.
        if (GameManager.Instance != null && GameManager.Instance.duelFieldUI != null)
        {
            if (playerDeckDisplay == null && GameManager.Instance.duelFieldUI.playerDeck != null)
                playerDeckDisplay = GameManager.Instance.duelFieldUI.playerDeck.GetComponent<PileDisplay>();
            if (opponentDeckDisplay == null && GameManager.Instance.duelFieldUI.opponentDeck != null)
                opponentDeckDisplay = GameManager.Instance.duelFieldUI.opponentDeck.GetComponent<PileDisplay>();
            if (playerExtraDeckDisplay == null && GameManager.Instance.duelFieldUI.playerExtraDeck != null)
                playerExtraDeckDisplay = GameManager.Instance.duelFieldUI.playerExtraDeck.GetComponent<PileDisplay>();
            if (opponentExtraDeckDisplay == null && GameManager.Instance.duelFieldUI.opponentExtraDeck != null)
                opponentExtraDeckDisplay = GameManager.Instance.duelFieldUI.opponentExtraDeck.GetComponent<PileDisplay>();        
        }
        playerDeck = new List<CardData>(newPlayerMainDeck);
        playerExtraDeck = new List<CardData>(newPlayerExtraDeck); // Novo
        opponentDeck = new List<CardData>(newOpponentMainDeck);
        opponentExtraDeck = new List<CardData>(newOpponentExtraDeck); // Novo
        UpdateDeckVisuals();
    }

    public List<CardData> GetPlayerDeck() { return playerDeck; }
    public List<CardData> GetOpponentDeck() { return opponentDeck; }
    public List<CardData> GetPlayerExtraDeck() { return playerExtraDeck; } // Novo
    public List<CardData> GetOpponentExtraDeck() { return opponentExtraDeck; } // Novo


    public void ShuffleDeck(bool isPlayer)
    {
        List<CardData> deck = isPlayer ? playerDeck : opponentDeck;
        for (int i = 0; i < deck.Count; i++)
        {
            CardData temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
        UpdateDeckVisuals();
        // Animação visual de Shuffle na tela
        PileDisplay display = isPlayer ? playerDeckDisplay : opponentDeckDisplay;
        if (display != null)
        {
            display.PlayShuffleAnimation();
        }
    }

    public void DrawCard(bool isPlayer, bool ignoreLimit = false)
    {
        // 1462 - Protector of the Sanctuary
        if (GameManager.Instance.duelFieldUI != null) {
            Transform[] enemyZones = isPlayer ? GameManager.Instance.duelFieldUI.opponentMonsterZones : GameManager.Instance.duelFieldUI.playerMonsterZones;
            bool hasProtector = false;
            
            if (enemyZones != null) {
                foreach(var z in enemyZones) {
                    if (z != null && z.childCount > 0) {
                        var cd = z.GetChild(0).GetComponent<CardDisplay>();
                        if (cd != null && cd.CurrentCardData != null && cd.CurrentCardData.id == "1462" && !cd.isFlipped) hasProtector = true;
                    }
                }
            }
            
            if (hasProtector && PhaseManager.Instance != null && PhaseManager.Instance.currentPhase != GamePhase.Draw) {
                Debug.Log("Protector of the Sanctuary: Compra fora da Draw Phase bloqueada.");
                return;
            }
        }
        if (isPlayer)
        {
            if (playerDeck.Count == 0)
            {
                bool qaActive = GameManager.Instance.devMode || GameManager.Instance.fullTestMode || !string.IsNullOrEmpty(QAAutoSpawner.currentTestCardId);
                if (qaActive && GameManager.Instance.cardDatabase != null)
                {
                    var fillerCards = GameManager.Instance.cardDatabase.cardDatabase.Where(c => !c.type.Contains("Fusion") && !c.type.Contains("Synchro") && !c.type.Contains("Token")).ToList();
                    playerDeck.AddRange(fillerCards.OrderBy(x => Random.value).Take(10));
                    Debug.Log("<color=yellow>🔄 [QA/DEV] Deck do jogador reabastecido automaticamente (Deck Infinito)!</color>");
                    UpdateDeckVisuals();
                }
                else
                {
                    Debug.LogWarning("Deck vazio! Não é possível comprar mais cartas.");
                    return;
                }
            }

            GamePhase currentPhase = PhaseManager.Instance != null ? PhaseManager.Instance.currentPhase : GamePhase.Draw;

            if (!ignoreLimit && !GameManager.Instance.devMode)
            {
                if (currentPhase != GamePhase.Draw)
                {
                    Debug.LogWarning("Você só pode comprar cartas na Draw Phase!");
                    return;
                }
                if (hasDrawnThisTurn)
                {
                    Debug.LogWarning("Você já comprou uma carta neste turno!");
                    return;
                }
            }

            CardData drawnCard = playerDeck[0];
            playerDeck.RemoveAt(0);
            UpdateDeckVisuals();

            Debug.Log($"Carta comprada: {drawnCard.name}. Cartas restantes no deck: {playerDeck.Count}");

            if (!ignoreLimit) hasDrawnThisTurn = true;

            // Exibe a carta comprada na área de visualização principal
            if (GameManager.Instance.cardViewerDisplay != null)
            {
                GameManager.Instance.cardViewerDisplay.SetCard(drawnCard, GameManager.Instance.GetCardBackTexture());
            }

            GameManager.Instance.AddCardToHand(drawnCard, true);
            GameManager.Instance.CheckExodiaWin();

            if (CardEffectManager.Instance != null) 
                CardEffectManager.Instance.OnCardDrawn(drawnCard, true);

            if (!ignoreLimit && currentPhase == GamePhase.Draw && PhaseManager.Instance != null)
            {
                StartCoroutine(DelayedPhaseChange(GamePhase.Standby, GameManager.Instance.phaseAnnouncements.drawToStandbyDelay * GameManager.Instance.phaseAnnouncements.masterDelayMultiplier));
            }
        }
        else
        {
            if (opponentDeck.Count == 0)
            {
                bool qaActive = GameManager.Instance.devMode || GameManager.Instance.fullTestMode || !string.IsNullOrEmpty(QAAutoSpawner.currentTestCardId);
                if (qaActive && GameManager.Instance.cardDatabase != null)
                {
                    var fillerCards = GameManager.Instance.cardDatabase.cardDatabase.Where(c => !c.type.Contains("Fusion") && !c.type.Contains("Synchro") && !c.type.Contains("Token")).ToList();
                    opponentDeck.AddRange(fillerCards.OrderBy(x => Random.value).Take(10));
                    Debug.Log("<color=yellow>🔄 [QA/DEV] Deck do oponente reabastecido automaticamente (Deck Infinito)!</color>");
                    UpdateDeckVisuals();
                }
                else
                {
                    Debug.LogWarning("Deck do oponente vazio! Não é possível comprar mais cartas.");
                    return;
                }
            }

            CardData drawnCard = opponentDeck[0];
            opponentDeck.RemoveAt(0);
            UpdateDeckVisuals();

            GameManager.Instance.AddCardToHand(drawnCard, false);
            
            if (GameManager.Instance.devMode) Debug.Log($"Oponente comprou: {drawnCard.name}");

            if (CardEffectManager.Instance != null) 
                CardEffectManager.Instance.OnCardDrawn(drawnCard, false);
                
            if (!ignoreLimit && PhaseManager.Instance != null && PhaseManager.Instance.currentPhase == GamePhase.Draw)
            {
                StartCoroutine(DelayedPhaseChange(GamePhase.Standby, GameManager.Instance.phaseAnnouncements.drawToStandbyDelay * GameManager.Instance.phaseAnnouncements.masterDelayMultiplier));
            }
        }
    }

    private System.Collections.IEnumerator DelayedPhaseChange(GamePhase phase, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (PhaseManager.Instance != null) PhaseManager.Instance.ChangePhase(phase);
    }

    public void MillCards(bool isPlayer, int amount)
    {
        List<CardData> deck = isPlayer ? playerDeck : opponentDeck;
        int count = Mathf.Min(amount, deck.Count);

        for (int i = 0; i < count; i++)
        {
            CardData card = deck[0];
            deck.RemoveAt(0);
            GameManager.Instance.SendToGraveyard(card, isPlayer, CardLocation.Deck, SendReason.Mill);
            Debug.Log($"Mill: {card.name}");
        }
        UpdateDeckVisuals();
    }

    public void ReturnToDeck(CardDisplay card, bool toTop)
    {
        if (card == null) return;

        CardData data = card.CurrentCardData;
        bool isPlayer = card.isPlayerCard;
        List<CardData> deck = isPlayer ? playerDeck : opponentDeck;

        if (toTop) deck.Insert(0, data);
        else deck.Add(data);

        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(card);

        card.transform.SetParent(null);
        Destroy(card.gameObject);

        // Se for um Token (ex: foi alvo de efeito que volta pro deck), ele evapora e não suja o deck.
        if (data.id == "TOKEN") return;

        UpdateDeckVisuals();
        Debug.Log($"{data.name} retornada ao deck (Topo: {toTop}).");
    }

    public void UpdateDeckVisuals()
    {
        if (playerDeckDisplay != null) playerDeckDisplay.UpdatePile(playerDeck, GameManager.Instance.GetCardBackTexture());
        if (opponentDeckDisplay != null) opponentDeckDisplay.UpdatePile(opponentDeck, GameManager.Instance.GetCardBackTexture());
        if (playerExtraDeckDisplay != null) playerExtraDeckDisplay.UpdatePile(playerExtraDeck, GameManager.Instance.GetCardBackTexture()); // Novo
        if (opponentExtraDeckDisplay != null) opponentExtraDeckDisplay.UpdatePile(opponentExtraDeck, GameManager.Instance.GetCardBackTexture()); // Novo
    }

    public void ResetTurnStats()
    {
        hasDrawnThisTurn = false;
    }
}