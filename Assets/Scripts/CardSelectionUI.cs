using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

    void Awake()
    {
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
        
        UpdateCardVisual(card, display);
        UpdateConfirmButton();
    }

    void UpdateCardVisual(CardData card, CardDisplay display)
    {
        bool isSelected = selectedCards.Contains(card);
        // Usa o efeito de "Tribute Highlight" (azul/ciano) para indicar seleção
        display.SetTributeHighlight(isSelected);
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
