using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Organiza as cartas em uma grade flexível.
/// As cartas são distribuídas em linhas, e se uma linha ficar cheia,
/// o espaçamento diminui para criar sobreposição.
/// </summary>
[ExecuteInEditMode]
public class CustomDeckLayout : MonoBehaviour
{
    [Header("Configuração da Grade (Flow)")]
    [Tooltip("Ativa a quebra dinâmica de linhas (10 cartas até 40, 11 até 44...). Ideal para o Main Deck.")]
    public bool dynamicPerRow = false;
    
    [Tooltip("O número máximo de cartas por linha (usado como padrão ou se a opção dinâmica estiver desligada).")]
    [Range(1, 60)]
    public int maxCardsPerRow = 15;
    
    [Header("Dimensões dos Itens")]
    [Tooltip("A largura de cada carta na grade.")]
    public float cardWidth = 96f;
    [Tooltip("A altura de cada carta na grade.")]
    public float cardHeight = 135f;
    
    [Header("Espaçamento")]
    [Tooltip("Espaçamento vertical entre as linhas.")]
    public float verticalSpacing = 5f;
    [Tooltip("Espaçamento horizontal máximo quando há poucas cartas na linha.")]
    public float maxHorizontalSpacing = 10f;
    [Tooltip("Espaçamento horizontal mínimo. Use valores negativos para permitir sobreposição.")]
    public float minHorizontalSpacing = -80f;
    
    [Header("Padding (Preenchimento Interno)")]
    [Tooltip("Espaço à esquerda e à direita dentro do contêiner.")]
    public float horizontalPadding = 5f;
    [Tooltip("Espaço acima e abaixo dentro do contêiner.")]
    public float verticalPadding = 3f;

    #if UNITY_EDITOR
    void Update()
    {
        if (!Application.isPlaying)
        {
            // Atualiza no editor para feedback visual
            RefreshLayout();
        }
    }
    #endif

    public void RefreshLayout()
    {
        RectTransform container = GetComponent<RectTransform>();
        if (container == null) return;

        // Coleta apenas filhos ativos que são cartas (têm CardDisplay), excluindo o DropTarget
        List<RectTransform> activeCards = new List<RectTransform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null) continue; // Evita exceção se o objeto foi destruído pela Unity neste frame
            
            // Garante que só pegamos cartas, e não outros objetos como o DropZone
            if (child.gameObject.activeSelf && child.GetComponentInChildren<CardDisplay>() != null)
            {
                RectTransform rect = child as RectTransform;
                if (rect != null)
                    activeCards.Add(rect);
            }
        }

        int totalCards = activeCards.Count;
        if (totalCards == 0) 
        {
            container.sizeDelta = new Vector2(container.sizeDelta.x, 0);
            return;
        }

        int currentMaxPerRow = maxCardsPerRow;
        if (dynamicPerRow)
        {
            if (totalCards <= 40) currentMaxPerRow = 10;
            else if (totalCards <= 44) currentMaxPerRow = 11;
            else if (totalCards <= 48) currentMaxPerRow = 12;
            else if (totalCards <= 52) currentMaxPerRow = 13;
            else if (totalCards <= 56) currentMaxPerRow = 14;
            else currentMaxPerRow = 15;
        }

        // --- Lógica de Flow Layout ---
        // Calcula quantas linhas reais teremos baseado no maxCardsPerRow
        int actualRows = Mathf.CeilToInt((float)totalCards / currentMaxPerRow);
        if (actualRows == 0) actualRows = 1;

        float currentY = -verticalPadding;

        for (int row = 0; row < actualRows; row++)
        {
            int startIndex = row * currentMaxPerRow;
            int cardsThisRow = Mathf.Min(currentMaxPerRow, totalCards - startIndex);

            float availableWidth = container.rect.width - (horizontalPadding * 2);
            float spacing = 0;

            if (cardsThisRow > 1)
            {
                // Calcula o espaçamento ideal para distribuir na tela
                spacing = (availableWidth - (cardsThisRow * cardWidth)) / (cardsThisRow - 1);
                // Previne que fiquem muito longe e permite sobreposição natural
                spacing = Mathf.Clamp(spacing, minHorizontalSpacing, maxHorizontalSpacing);
            }

            float startX = horizontalPadding;

            for (int i = 0; i < cardsThisRow; i++)
            {
                int cardIndex = startIndex + i;
                RectTransform cardRect = activeCards[cardIndex];
                
                // Sempre alinha da esquerda para a direita fluidamente
                float xPos = startX + (i * (cardWidth + spacing));

                // Configura âncora e pivô para posicionamento a partir do canto superior esquerdo
                cardRect.anchorMin = new Vector2(0, 1);
                cardRect.anchorMax = new Vector2(0, 1);
                cardRect.pivot = new Vector2(0, 1);
                cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);
                cardRect.anchoredPosition = new Vector2(xPos, currentY);
            }

            currentY -= (cardHeight + verticalSpacing);
        }

        // Ajusta a altura do container para que o ScrollRect funcione
        float requiredHeight = Mathf.Abs(currentY) + verticalPadding;
        container.sizeDelta = new Vector2(container.sizeDelta.x, requiredHeight);
    }
}