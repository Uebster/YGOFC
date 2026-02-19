using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class PileDisplay : MonoBehaviour, IPointerClickHandler
{
    public enum PileType { Deck, Graveyard }
    [Header("Configuração")]
    public PileType pileType;
    public bool isPlayerPile = true; // Define se é do jogador ou oponente
    public GameObject cardPrefab; // O mesmo prefab de carta usado na mão
    public Transform contentParent; // O objeto pai onde as cartas serão instanciadas (pode ser o próprio transform deste objeto)

    [Header("Visual")]
    public Vector2 stackOffset = new Vector2(0f, 1.5f); // Deslocamento (x, y) por carta para criar o efeito de pilha
    public int maxVisualCards = 60; // Limite visual para não sobrecarregar (opcional)

    private List<GameObject> activeCards = new List<GameObject>();
    private Texture2D currentBackTexture;

    public void UpdatePile(List<CardData> cards, Texture2D backTexture)
    {
        if (contentParent == null || cardPrefab == null) return;
        
        currentBackTexture = backTexture;
        int targetCount = cards.Count;

        // 1. Ajusta o número de objetos visuais (Pool simples: cria ou destrói conforme necessário)
        while (activeCards.Count < targetCount && activeCards.Count < maxVisualCards)
        {
            GameObject newCard = Instantiate(cardPrefab, contentParent);
            // Removemos componentes de layout que possam interferir no posicionamento manual
            LayoutElement le = newCard.GetComponent<LayoutElement>();
            if (le != null) le.ignoreLayout = true;
            
            activeCards.Add(newCard);
        }
        
        while (activeCards.Count > targetCount)
        {
            GameObject toRemove = activeCards[activeCards.Count - 1];
            activeCards.RemoveAt(activeCards.Count - 1);
            Destroy(toRemove);
        }

        // 2. Atualiza posições e dados das cartas
        for (int i = 0; i < activeCards.Count; i++)
        {
            GameObject cardObj = activeCards[i];
            RectTransform rect = cardObj.GetComponent<RectTransform>();
            
            // Reset básico
            cardObj.transform.localScale = Vector3.one;
            cardObj.transform.localRotation = Quaternion.identity;

            // Lógica de Índice:
            // Visual 0 = Fundo da pilha (renderizado primeiro)
            // Visual N = Topo da pilha (renderizado por último)
            
            int dataIndex = 0;

            if (pileType == PileType.Deck)
            {
                // No Deck (GameManager), o índice 0 é o TOPO (próxima carta a comprar).
                // Visualmente, o topo deve ser o último filho (renderizado por cima).
                // Portanto, invertemos a ordem para visualização.
                // Visual 0 (Fundo) -> Dados [Count-1]
                // Visual Topo -> Dados [0]
                dataIndex = (targetCount - 1) - i;
            }
            else // Graveyard
            {
                // No Cemitério, adicionamos cartas ao final da lista.
                // Índice 0 = Primeira carta morta (Fundo).
                // Índice Count-1 = Última carta morta (Topo).
                // A ordem visual segue a ordem da lista.
                dataIndex = i;
            }

            // Proteção contra índice fora do limite se limitarmos visualmente
            if (dataIndex >= 0 && dataIndex < cards.Count)
            {
                CardData data = cards[dataIndex];
                CardDisplay display = cardObj.GetComponent<CardDisplay>();

                if (display != null)
                {
                    bool isFaceUp = (pileType == PileType.Graveyard);

                    if (!isFaceUp)
                    {
                        // Deck: Mostra apenas o verso (otimizado)
                        display.SetCardBackOnly(currentBackTexture);
                    }
                    else
                    {
                        // Cemitério: Mostra a carta virada para cima
                        display.SetCard(data, currentBackTexture, true);
                    }
                }
            }

            // Aplica o efeito de "montinho" deslocando a posição
            if (rect != null)
            {
                rect.anchoredPosition = stackOffset * i;
                cardObj.transform.SetSiblingIndex(i); // Garante a ordem de renderização
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (GameManager.Instance == null) return;

        if (pileType == PileType.Deck)
        {
            if (isPlayerPile)
            {
                // Jogador saca carta clicando no deck (se habilitado)
                if (GameManager.Instance.enableDeckClickDraw)
                    GameManager.Instance.DrawCard();
            }
            else if (GameManager.Instance.devMode)
            {
                // Desenvolvedor saca carta para o oponente clicando no deck dele
                GameManager.Instance.DrawOpponentCard();
            }
        }
    }
}
