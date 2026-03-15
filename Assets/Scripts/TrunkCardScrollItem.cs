using UnityEngine;
using System.Linq;

/// <summary>
/// Represents a single item within the virtualized trunk scroll view.
/// This script acts as a bridge between the TrunkScrollManager and the ChestCardItem UI script.
/// </summary>
[RequireComponent(typeof(ChestCardItem))]
public class TrunkCardScrollItem : MonoBehaviour
{
    private ChestCardItem itemUI;

    void Awake()
    {
        itemUI = GetComponent<ChestCardItem>();
        if (itemUI == null)
        {
            Debug.LogError("TrunkCardScrollItem requires a ChestCardItem component on the same GameObject!", this);
        }
    }

    /// <summary>
    /// Updates the UI of this item with new card data.
    /// </summary>
    /// <param name="cardGroup">The group of cards to display (we only show the first one).</param>
    public void UpdateContent(IGrouping<string, CardData> cardGroup)
    {
        if (itemUI == null || cardGroup == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        CardData card = cardGroup.First();
        int limit = DeckBuilderManager.Instance.GetCardLimit(card.name);
        
        if (GameManager.Instance != null && GameManager.Instance.allowForbiddenCards && limit == 0) limit = 1;

        int ownedCopies = cardGroup.Count();
        if (GameManager.Instance != null && GameManager.Instance.devMode && GameManager.Instance.unlockAllCards) ownedCopies = 3;

        int maxAllowed = Mathf.Min(ownedCopies, limit);
        int copiesInDecks = DeckBuilderManager.Instance.GetCopiesInDecks(card.id);
        int availableCopies = maxAllowed - copiesInDecks;

        bool isNew = SaveLoadSystem.Instance != null && SaveLoadSystem.Instance.IsCardNew(card.id);
        bool isInDeck = copiesInDecks > 0;
        itemUI.Setup(card, availableCopies, isNew, isInDeck);
    }
}
