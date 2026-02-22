using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class CardLibraryManager : MonoBehaviour
{
    [Header("Referências")]
    public Transform gridContent; // O objeto 'Content' dentro do ScrollView
    public GameObject cardItemPrefab; // O mesmo prefab usado no DeckBuilder (pequeno)
    public CardViewerUI cardViewer; // O visualizador grande à esquerda

    [Header("Paginação")]
    public TextMeshProUGUI pageText; // Texto "Page 1/21"
    public Button prevButton;
    public Button nextButton;
    public int itemsPerPage = 50;

    private List<CardData> unlockedCards = new List<CardData>();
    private int currentPage = 1;
    private int totalPages = 1;

    void Start()
    {
        if (prevButton) prevButton.onClick.AddListener(PrevPage);
        if (nextButton) nextButton.onClick.AddListener(NextPage);
    }

    void OnEnable()
    {
        LoadLibrary();
    }

    public void LoadLibrary()
    {
        if (GameManager.Instance == null || GameManager.Instance.cardDatabase == null) return;
        
        unlockedCards.Clear();
        
        // Filtra apenas cartas que o jogador possui (Trunk)
        // Usa HashSet para evitar duplicatas visuais se o jogador tiver 3 cópias
        HashSet<string> ownedIDs = new HashSet<string>(GameManager.Instance.playerTrunk);
        
        foreach (CardData card in GameManager.Instance.cardDatabase.cardDatabase)
        {
            if (ownedIDs.Contains(card.id))
            {
                unlockedCards.Add(card);
            }
        }

        // Calcula total de páginas
        totalPages = Mathf.CeilToInt((float)unlockedCards.Count / itemsPerPage);
        if (totalPages < 1) totalPages = 1;
        
        currentPage = 1;
        UpdatePage();
    }

    void UpdatePage()
    {
        if (gridContent == null || cardItemPrefab == null) return;

        // Limpa grid atual
        foreach (Transform child in gridContent) Destroy(child.gameObject);

        int startIndex = (currentPage - 1) * itemsPerPage;
        int count = Mathf.Min(itemsPerPage, unlockedCards.Count - startIndex);

        for (int i = 0; i < count; i++)
        {
            CardData card = unlockedCards[startIndex + i];
            GameObject go = Instantiate(cardItemPrefab, gridContent);
            
            // Configura a imagem
            CardDisplay display = go.GetComponent<CardDisplay>();
            if (display == null) display = go.AddComponent<CardDisplay>();
            
            // Carrega a carta (apenas frente, sem lógica de jogo)
            display.SetCard(card, null, true);
            display.isInteractable = true; // Permite hover

            // Adiciona evento de Hover para atualizar o visualizador
            // (O CardDisplay já tem lógica de hover, mas precisamos garantir que ele chame ESTE visualizador)
            // Uma forma simples é adicionar um componente que ponteia para cá
            LibraryCardInteraction interaction = go.AddComponent<LibraryCardInteraction>();
            interaction.manager = this;
            interaction.myCard = card;
        }

        // Atualiza UI
        if (pageText) pageText.text = $"Page {currentPage}/{totalPages}";
        if (prevButton) prevButton.interactable = currentPage > 1;
        if (nextButton) nextButton.interactable = currentPage < totalPages;
    }

    public void NextPage()
    {
        if (currentPage < totalPages)
        {
            currentPage++;
            UpdatePage();
        }
    }

    public void PrevPage()
    {
        if (currentPage > 1)
        {
            currentPage--;
            UpdatePage();
        }
    }

    public void OnCardHover(CardData card)
    {
        if (cardViewer != null)
        {
            cardViewer.DisplayCardData(card);
        }
    }
}

// Pequena classe auxiliar para detectar o mouse na biblioteca
public class LibraryCardInteraction : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler
{
    public CardLibraryManager manager;
    public CardData myCard;

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (manager != null) manager.OnCardHover(myCard);
    }
}