using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class CardSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public Transform contentArea; // Onde as cartas aparecem
    public GameObject cardItemPrefab; // Prefab da carta (pode ser o mesmo do DeckBuilder)
    public Button confirmButton;
    public TextMeshProUGUI confirmButtonText;
    public Button closeButton; // Para o botão CloseDeckCards da hierarquia

    private List<CardData> sourceList;
    private List<CardData> selectedCards = new List<CardData>();
    private int minSelection = 1;
    private int maxSelection = 1;
    private System.Action<List<CardData>> onConfirm;

    // Cache de objetos instanciados para performance
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private Dictionary<CardDisplay, GameObject> badges = new Dictionary<CardDisplay, GameObject>();

    void Awake()
    {
        // AUTO-CONFIGURAÇÃO: Tenta encontrar referências se não estiverem atribuídas
        if (contentArea == null)
        {
            // Busca profunda pelo Content, caso a hierarquia mude levemente
            var contentTransform = transform.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "Content");
            if (contentTransform != null) contentArea = contentTransform;
        }

        if (closeButton == null)
        {
            // Busca profunda pelo botão de fechar
            var btnTr = transform.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(b => b.name == "CloseDeckCards");
            if (btnTr != null) closeButton = btnTr;
        }

        if (confirmButton) confirmButton.onClick.AddListener(ConfirmSelection);
        if (closeButton) closeButton.onClick.AddListener(CancelSelection);
        // Garante que comece fechado
        gameObject.SetActive(false);
    }

    public void Show(List<CardData> cards, string title, int min, int max, System.Action<List<CardData>> callback)
    {
        sourceList = cards;
        minSelection = min;
        maxSelection = max;
        onConfirm = callback;
        selectedCards.Clear();

        if (titleText) titleText.text = title;
        
        // Se o prefab não estiver atribuído, tenta pegar do GameManager
        if (cardItemPrefab == null && GameManager.Instance != null)
            cardItemPrefab = GameManager.Instance.cardPrefab;

        gameObject.SetActive(true);
        RefreshUI();
    }

    void RefreshUI()
    {
        // Limpa visualização anterior
        foreach (var obj in spawnedObjects) Destroy(obj);
        spawnedObjects.Clear();
        badges.Clear();

        if (sourceList == null || cardItemPrefab == null) return;

        foreach (var card in sourceList)
        {
            GameObject go = Instantiate(cardItemPrefab, contentArea);
            spawnedObjects.Add(go);

            CardDisplay display = go.GetComponent<CardDisplay>();
            if (display == null) display = go.AddComponent<CardDisplay>();
            
            // Configura visual
            display.SetCard(card, GameManager.Instance.GetCardBackTexture(), true);
            display.isInteractable = false; // Sem menu de ação (apenas clique para selecionar)

            // Adiciona comportamento de clique
            Button btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();
            
            // Remove listeners antigos se houver (por segurança)
            btn.onClick.RemoveAllListeners();
            
            // Captura a variável para o closure
            CardData currentCard = card;
            btn.onClick.AddListener(() => ToggleSelection(currentCard, display));

            // Atualiza estado visual inicial
            UpdateCardVisual(currentCard, display);
        }

        UpdateConfirmButton();
    }

    void ToggleSelection(CardData card, CardDisplay display)
    {
        if (selectedCards.Contains(card))
        {
            selectedCards.Remove(card);
        }
        else
        {
            if (selectedCards.Count < maxSelection)
            {
                selectedCards.Add(card);
            }
            else if (maxSelection == 1)
            {
                // Se for seleção única, troca a seleção atual pela nova
                selectedCards.Clear();
                selectedCards.Add(card);
                // Precisamos atualizar visualmente todas as cartas para remover o destaque da anterior
                // Para simplificar, chamamos RefreshVisuals em todas
                RefreshAllVisuals();
                UpdateConfirmButton();
                return;
            }
        }
        
        // Se a ordem importa (seleção múltipla), atualizamos todos para garantir que os números (1, 2, 3) fiquem corretos
        // Ex: Se desmarcar o 1, o 2 vira 1.
        if (maxSelection > 1) RefreshAllVisuals();
        else UpdateCardVisual(card, display);

        UpdateConfirmButton();
    }

    void UpdateCardVisual(CardData card, CardDisplay display)
    {
        bool isSelected = selectedCards.Contains(card);
        // Usa o efeito de "Tribute Highlight" (azul/ciano) para indicar seleção
        display.SetTributeHighlight(isSelected);

        // Lógica de Ordem Visual (Badges)
        if (isSelected && maxSelection > 1)
        {
            int order = selectedCards.IndexOf(card) + 1;
            ShowSelectionBadge(display, order);
        }
        else
        {
            HideSelectionBadge(display);
        }
    }

    void ShowSelectionBadge(CardDisplay display, int order)
    {
        if (!badges.ContainsKey(display) || badges[display] == null)
        {
            // Cria o badge se não existir
            GameObject badge = new GameObject("OrderBadge", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(display.transform, false);
            
            // Configura Imagem (Fundo Azul)
            Image img = badge.GetComponent<Image>();
            img.color = new Color(0f, 0.5f, 1f, 0.9f); // Azul semitransparente
            
            RectTransform rect = badge.GetComponent<RectTransform>();
            // Posiciona no canto superior direito
            rect.anchorMin = new Vector2(0.75f, 0.75f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            // Configura Texto
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(badge.transform, false);
            
            TextMeshProUGUI txt = textObj.GetComponent<TextMeshProUGUI>();
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontSize = 14; // Tamanho base, o AutoSizing ajusta
            txt.color = Color.white;
            txt.enableAutoSizing = true;
            txt.fontStyle = FontStyles.Bold;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            badges[display] = badge;
        }
        
        GameObject b = badges[display];
        b.SetActive(true);
        b.GetComponentInChildren<TextMeshProUGUI>().text = order.ToString();
        b.transform.SetAsLastSibling(); // Garante que fique por cima da carta
    }

    void HideSelectionBadge(CardDisplay display)
    {
        if (badges.ContainsKey(display) && badges[display] != null)
        {
            badges[display].SetActive(false);
        }
    }

    void RefreshAllVisuals()
    {
        foreach (var go in spawnedObjects)
        {
            CardDisplay display = go.GetComponent<CardDisplay>();
            if (display != null)
            {
                UpdateCardVisual(display.CurrentCardData, display);
            }
        }
    }

    void UpdateConfirmButton()
    {
        if (confirmButton)
        {
            bool isValid = selectedCards.Count >= minSelection && selectedCards.Count <= maxSelection;
            confirmButton.interactable = isValid;
            
            if (confirmButtonText)
            {
                if (isValid) confirmButtonText.text = "Confirmar";
                else confirmButtonText.text = $"Selecione {minSelection}-{maxSelection}";
            }
        }
    }

    void ConfirmSelection()
    {
        gameObject.SetActive(false);
        onConfirm?.Invoke(selectedCards);
    }

    void CancelSelection()
    {
        gameObject.SetActive(false);
        // Retorna nulo ou lista vazia para indicar cancelamento
        onConfirm?.Invoke(new List<CardData>());
    }
}
