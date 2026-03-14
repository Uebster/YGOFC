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
    [Header("Configuração da Grade")]
    [Tooltip("O número de linhas fixas na grade (padrão: 4 para Main Deck).")]
    [Range(1, 10)]
    public int numberOfRows = 4;
    
    [Header("Dimensões dos Itens")]
    [Tooltip("A largura de cada carta na grade.")]
    public float cardWidth = 96f;
    [Tooltip("A altura de cada carta na grade.")]
    public float cardHeight = 135f;
    
    [Header("Espaçamento")]
    [Tooltip("Espaçamento vertical entre as linhas.")]
    public float verticalSpacing = 5f;
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
            // Garante que só pegamos cartas, e não outros objetos como o DropZone
            if (child.gameObject.activeSelf && child.GetComponent<CardDisplay>() != null)
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

        // --- Lógica de Distribuição ---
        // Tenta distribuir as cartas igualmente, mas se uma linha ficar muito cheia, move para a próxima.
        int[] cardsInEachRow = new int[numberOfRows];
        int baseCardsPerRow = totalCards / numberOfRows;
        int remainder = totalCards % numberOfRows;

        for (int i = 0; i < numberOfRows; i++)
            cardsInEachRow[i] = baseCardsPerRow + (i < remainder ? 1 : 0);

        float currentY = -verticalPadding;
        int cardIndex = 0;

        for (int row = 0; row < numberOfRows; row++)
        {
            int cardsThisRow = cardsInEachRow[row];
            if (cardsThisRow == 0) continue;

            float availableWidth = container.rect.width - (horizontalPadding * 2);
            float spacing;

            if (cardsThisRow <= 1)
            {
                spacing = 0;
            }
            else
            {
                // Calcula o espaçamento ideal para preencher a largura
                spacing = (availableWidth - (cardsThisRow * cardWidth)) / (cardsThisRow - 1);
            }

            // Garante que o espaçamento não seja menor que o mínimo permitido
            spacing = Mathf.Max(spacing, minHorizontalSpacing);

            float startX = horizontalPadding;

            for (int i = 0; i < cardsThisRow; i++)
            {
                if (cardIndex >= activeCards.Count) break;

                RectTransform cardRect = activeCards[cardIndex];
                float xPos = startX + (i * (cardWidth + spacing));

                // Configura âncora e pivô para posicionamento a partir do canto superior esquerdo
                cardRect.anchorMin = new Vector2(0, 1);
                cardRect.anchorMax = new Vector2(0, 1);
                cardRect.pivot = new Vector2(0, 1);
                cardRect.anchoredPosition = new Vector2(xPos, currentY);

                cardIndex++;
            }

            currentY -= (cardHeight + verticalSpacing);
        }

        // Ajusta a altura do container para que o ScrollRect funcione
        float requiredHeight = Mathf.Abs(currentY) + verticalPadding;
        container.sizeDelta = new Vector2(container.sizeDelta.x, requiredHeight);
    }
}