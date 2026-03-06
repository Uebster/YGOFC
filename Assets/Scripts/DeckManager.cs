using UnityEngine;
using System.Collections.Generic;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance;

    [Header("Deck Visuals")]
    public PileDisplay playerDeckDisplay;
    public PileDisplay opponentDeckDisplay;

    private List<CardData> playerDeck = new List<CardData>();
    private List<CardData> opponentDeck = new List<CardData>();

    private bool hasDrawnThisTurn = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Auto-atribuição para robustez, caso não esteja definido no Inspector.
        if (GameManager.Instance != null && GameManager.Instance.duelFieldUI != null)
        {
            if (playerDeckDisplay == null && GameManager.Instance.duelFieldUI.playerDeck != null)
                playerDeckDisplay = GameManager.Instance.duelFieldUI.playerDeck.GetComponent<PileDisplay>();
            if (opponentDeckDisplay == null && GameManager.Instance.duelFieldUI.opponentDeck != null)
                opponentDeckDisplay = GameManager.Instance.duelFieldUI.opponentDeck.GetComponent<PileDisplay>();
        }
    }

    public void SetupDecks(List<CardData> newPlayerDeck, List<CardData> newOpponentDeck)
    {
        playerDeck = new List<CardData>(newPlayerDeck);
        opponentDeck = new List<CardData>(newOpponentDeck);
        UpdateDeckVisuals();
    }

    public List<CardData> GetPlayerDeck() { return playerDeck; }
    public List<CardData> GetOpponentDeck() { return opponentDeck; }

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
    }

    public void DrawCard(bool isPlayer, bool ignoreLimit = false)
    {
        if (isPlayer)
        {
            if (playerDeck.Count == 0)
            {
                Debug.LogWarning("Deck vazio! Não é possível comprar mais cartas.");
                return;
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

            if (!ignoreLimit && currentPhase == GamePhase.Draw && PhaseManager.Instance != null)
            {
                PhaseManager.Instance.ChangePhase(GamePhase.Standby);
            }
        }
        else
        {
            if (opponentDeck.Count == 0)
            {
                Debug.LogWarning("Deck do oponente vazio! Não é possível comprar mais cartas.");
                return;
            }

            CardData drawnCard = opponentDeck[0];
            opponentDeck.RemoveAt(0);
            UpdateDeckVisuals();

            GameManager.Instance.AddCardToHand(drawnCard, false);
            
            if (GameManager.Instance.devMode) Debug.Log($"Oponente comprou: {drawnCard.name}");
        }
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
        if (SpellCounterManager.Instance != null) SpellCounterManager.Instance.OnCardLeavesField(card);

        Destroy(card.gameObject);
        UpdateDeckVisuals();
        Debug.Log($"{data.name} retornada ao deck (Topo: {toTop}).");
    }

    public void UpdateDeckVisuals()
    {
        if (playerDeckDisplay != null) playerDeckDisplay.UpdatePile(playerDeck, GameManager.Instance.GetCardBackTexture());
        if (opponentDeckDisplay != null) opponentDeckDisplay.UpdatePile(opponentDeck, GameManager.Instance.GetCardBackTexture());
    }

    public void ResetTurnStats()
    {
        hasDrawnThisTurn = false;
    }
}