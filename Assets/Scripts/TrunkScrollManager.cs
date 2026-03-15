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

        // Força o pivot do content para o topo esquerdo, garantindo que o scroll desça corretamente
        content.pivot = new Vector2(0, 1);

        // --- Início da Lógica de Cálculo do Tamanho da Célula ---
        var layoutElement = cardItemPrefab.GetComponent<LayoutElement>();
        var prefabRect = cardItemPrefab.GetComponent<RectTransform>();
        bool sizeFound = false;

        if (layoutElement != null && layoutElement.preferredHeight > 0)
        {
            cellSize = new Vector2(layoutElement.preferredWidth, layoutElement.preferredHeight);
            Debug.Log($"[TrunkScrollManager] Tamanho da célula determinado pelo LayoutElement: {cellSize}");
            sizeFound = true;
        }
        else if (prefabRect != null && prefabRect.sizeDelta.y > 0)
        {
            cellSize = prefabRect.sizeDelta;
            Debug.Log($"[TrunkScrollManager] Tamanho da célula determinado pelo RectTransform.sizeDelta: {cellSize}");
            sizeFound = true;
        }

        if (!sizeFound)
        {
            Debug.LogError("[TrunkScrollManager] Não foi possível determinar um tamanho de célula válido a partir do prefab. Verifique o LayoutElement (preferredHeight) ou o RectTransform (Size Delta Y) do prefab. Abortando.");
            // Limpa a visualização para evitar itens antigos ou mal posicionados
            foreach (var item in itemPool) { if(item != null) item.gameObject.SetActive(false); }
            content.sizeDelta = new Vector2(content.sizeDelta.x, 0);
            return;
        }
        // --- Fim da Lógica de Cálculo ---

        // Calculate total content height based on the number of rows
        int totalRows = Mathf.CeilToInt((float)cardGroups.Count / columns);
        float contentHeight = totalRows * (cellSize.y + spacing.y) + padding.y * 2;
        content.sizeDelta = new Vector2(content.sizeDelta.x, contentHeight);
        Debug.Log($"[TrunkScrollManager] Initialize: Altura do conteúdo definida para {contentHeight} para {totalRows} linhas.");

        if (!isInitialized || PoolIsInvalid())
        {
            Debug.Log($"[TrunkScrollManager] Status da inicialização: isInitialized={isInitialized}, PoolIsInvalid={PoolIsInvalid()}. Recriando o pool.");
            DestroyPool();
            CreatePool();
            isInitialized = true;
        }

        // Reset scroll position and update view
        content.anchoredPosition = Vector2.zero;
        OnScroll(Vector2.zero);
    }

    private bool PoolIsInvalid()
    {
        // 1. Pool não existe ou está vazio quando deveria ter itens.
        if (itemPool == null || (itemPool.Count == 0 && cardGroups.Count > 0))
        {
            return true;
        }
        // 2. Itens no pool foram destruídos (referências nulas).
        foreach (var item in itemPool)
        {
            if (item == null)
            {
                return true;
            }
        }
        return false;
    }

    private void DestroyPool()
    {
        if (itemPool == null) return;
        foreach (var item in itemPool)
        {
            if (item != null && item.gameObject != null)
            {
                Destroy(item.gameObject);
            }
        }
        itemPool.Clear();
        Debug.Log("[TrunkScrollManager] Pool de itens destruído.");
    }

    /// <summary>
    /// Atualiza visualmente os itens que estão na tela (ex: escurecer carta que acabou a quantidade)
    /// sem reconstruir a lista ou perder a posição do scroll.
    /// </summary>
    public void RefreshVisibleItems()
    {
        if (cardGroups == null || itemPool.Count == 0) return;
        OnScroll(Vector2.zero); // Força os itens visíveis a chamarem UpdateContent() novamente
    }

    private void CreatePool()
    {
        if (cellSize.y <= 0)
        {
            Debug.LogError("[TrunkScrollManager] CreatePool: Altura da célula é inválida, não é possível criar o pool.");
            return;
        }
        
        // Força a atualização do canvas para garantir que o Viewport tenha a altura calculada
        Canvas.ForceUpdateCanvases();
        
        float viewportHeight = scrollRect.viewport.rect.height;
        if (viewportHeight <= 0)
        {
            // Fallback seguro caso a altura ainda seja 0 (ex: painel foi recém ativado)
            viewportHeight = 1080f; 
        }

        // Calculate how many rows are visible and add a buffer
        int visibleRows = Mathf.CeilToInt(viewportHeight / (cellSize.y + spacing.y)) + 2;
        int requiredPoolSize = visibleRows * columns;
        Debug.Log($"[TrunkScrollManager] CreatePool: Criando um pool de {requiredPoolSize} itens. ViewportHeight: {viewportHeight}");

        for (int i = 0; i < requiredPoolSize; i++)
        {
            GameObject go = Instantiate(cardItemPrefab, content);
            
            // Garante que as âncoras da carta sejam Top-Left para o cálculo matemático de posição funcionar
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);

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

        // Prevent division by zero if item height is somehow invalid
        if (itemHeightWithSpacing <= 0) return;

        // Determine the first visible row and item index
        int firstVisibleRow = Mathf.Max(0, Mathf.FloorToInt(scrollY / itemHeightWithSpacing));
        int firstVisibleIndex = firstVisibleRow * columns;

        // Recycle and update items from the pool
        for (int i = 0; i < itemPool.Count; i++)
        {
            int dataIndex = firstVisibleIndex + i;
            TrunkCardScrollItem item = itemPool[i];

            if (item == null)
            {
                Debug.LogError($"[TrunkScrollManager] Item nulo encontrado no pool no índice {i}. O pool pode estar corrompido.");
                continue;
            }

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