using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages a virtualized scroll view for the card trunk.
/// This script creates a pool of UI items and recycles them as the user scrolls,
/// ensuring high performance even with thousands of cards.
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class TrunkScrollManager : MonoBehaviour
{
    [Header("UI Configuration")]
    [Tooltip("The prefab for a single card item in the grid.")]
    [SerializeField] private GameObject cardItemPrefab;
    [Tooltip("The number of columns in the grid.")]
    [SerializeField] private int columns = 4;
    [Tooltip("The spacing between items in the grid.")]
    [SerializeField] private Vector2 spacing = new Vector2(10, 10);
    [Tooltip("The padding inside the content area.")]
    [SerializeField] private Vector2 padding = new Vector2(10, 10);

    private RectTransform content;
    private ScrollRect scrollRect;
    private List<TrunkCardScrollItem> itemPool = new List<TrunkCardScrollItem>();
    private List<IGrouping<string, CardData>> cardGroups;
    private Vector2 cellSize;
    private bool isInitialized = false;

    void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        content = scrollRect.content;
        scrollRect.onValueChanged.AddListener(OnScroll);
    }

    /// <summary>
    /// Initializes the scroll view with a new set of cards.
    /// </summary>
    /// <param name="filteredCards">The filtered and sorted list of cards to display.</param>
    public void Initialize(List<IGrouping<string, CardData>> filteredCards)
    {
        Debug.Log($"[TrunkScrollManager] Initialize: Recebeu {filteredCards.Count} grupos de cartas.");
        cardGroups = filteredCards;

        if (cardItemPrefab == null)
        {
            Debug.LogError("TrunkScrollManager: cardItemPrefab is not assigned!", this);
            return;
        }

        // Calculate cell size from prefab's RectTransform
        var prefabRect = cardItemPrefab.GetComponent<RectTransform>();
        if (prefabRect != null)
        {
            cellSize = prefabRect.sizeDelta;
        }
        else
        {
            Debug.LogError("TrunkScrollManager: cardItemPrefab must have a RectTransform component to determine its size!", this);
            return;
        }

        // Calculate total content height based on the number of rows
        int totalRows = Mathf.CeilToInt((float)cardGroups.Count / columns);
        float contentHeight = totalRows * (cellSize.y + spacing.y) + padding.y * 2;
        content.sizeDelta = new Vector2(content.sizeDelta.x, contentHeight);
        Debug.Log($"[TrunkScrollManager] Initialize: Altura do conteúdo definida para {contentHeight} para {totalRows} linhas.");

        if (!isInitialized)
        {
            CreatePool();
            isInitialized = true;
        }

        // Reset scroll position and update view
        content.anchoredPosition = Vector2.zero;
        OnScroll(Vector2.zero);
    }

    private void CreatePool()
    {
        float viewportHeight = scrollRect.viewport.rect.height;
        // Calculate how many rows are visible and add a buffer
        int visibleRows = Mathf.CeilToInt(viewportHeight / (cellSize.y + spacing.y)) + 2;
        int requiredPoolSize = visibleRows * columns;
        Debug.Log($"[TrunkScrollManager] CreatePool: Criando um pool de {requiredPoolSize} itens.");

        for (int i = 0; i < requiredPoolSize; i++)
        {
            GameObject go = Instantiate(cardItemPrefab, content);
            // Ensure the item has the necessary script
            TrunkCardScrollItem item = go.GetComponent<TrunkCardScrollItem>() ?? go.AddComponent<TrunkCardScrollItem>();
            go.SetActive(false);
            itemPool.Add(item);
        }
    }

    private void OnScroll(Vector2 position)
    {
        if (cardGroups == null || itemPool.Count == 0) return;

        float scrollY = content.anchoredPosition.y;
        float itemHeightWithSpacing = cellSize.y + spacing.y;

        // Determine the first visible row and item index
        int firstVisibleRow = Mathf.Max(0, Mathf.FloorToInt(scrollY / itemHeightWithSpacing));
        int firstVisibleIndex = firstVisibleRow * columns;

        // Recycle and update items from the pool
        for (int i = 0; i < itemPool.Count; i++)
        {
            int dataIndex = firstVisibleIndex + i;
            TrunkCardScrollItem item = itemPool[i];

            if (dataIndex < cardGroups.Count)
            {
                item.gameObject.SetActive(true);
                int row = dataIndex / columns;
                int col = dataIndex % columns;
                float xPos = padding.x + col * (cellSize.x + spacing.x);
                float yPos = -padding.y - row * itemHeightWithSpacing;
                item.GetComponent<RectTransform>().anchoredPosition = new Vector2(xPos, yPos);
                item.UpdateContent(cardGroups[dataIndex]);
            }
            else
            {
                item.gameObject.SetActive(false);
            }
        }
    }
}