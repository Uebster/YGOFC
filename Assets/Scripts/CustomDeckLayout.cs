using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq; // <<<< ADICIONADO para o método Select

/// <summary>
/// Organiza as cartas em uma grade com distribuição circular por linhas.
/// As cartas são distribuídas sequencialmente: Linha 1, 2, 3, 4, 1, 2, 3, 4...
/// Alinhamento sempre à esquerda com espaçamento máximo controlado.
/// </summary>
// [ExecuteInEditMode]  // Removido temporariamente para testar se causa erro no editor
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
    public float verticalSpacing = 10f;
    [Tooltip("Espaçamento horizontal MÁXIMO entre as cartas. Se o cálculo ultrapassar este valor, usa este valor.")]
    public float maxHorizontalSpacing = 15f;
    [Tooltip("Espaçamento horizontal mínimo. Use valores negativos para permitir sobreposição.")]
    public float minHorizontalSpacing = -10f;
    
    [Header("Padding (Preenchimento Interno)")]
    [Tooltip("Espaço à esquerda e à direita dentro do contêiner.")]
    public float horizontalPadding = 5f;
    [Tooltip("Espaço acima e abaixo dentro do contêiner.")]
    public float verticalPadding = 3f;

    [Header("Limite por Linha")]
    [Tooltip("Número máximo de cartas por linha antes de forçar sobreposição.")]
    public int maxCardsPerRow = 15;

    #if UNITY_EDITOR
    void Update()
    {
        if (!Application.isPlaying)
        {
            UpdateLayout();
        }
    }
    #endif

    void Start()
    {
        UpdateLayout();
    }

    public void UpdateLayout()
    {
        RectTransform container = GetComponent<RectTransform>();
        if (container == null) return;

        // Força o container a ter âncora bottom-left para compatibilidade com ScrollRect
        container.anchorMin = new Vector2(0, 0);
        container.anchorMax = new Vector2(1, 1);
        container.pivot = new Vector2(0, 0);
        container.anchoredPosition = Vector2.zero; // Reseta posição

        // Desabilita VerticalLayoutGroup para evitar conflito
        VerticalLayoutGroup vlg = GetComponent<VerticalLayoutGroup>();
        if (vlg != null) vlg.enabled = false;

        // Coleta apenas filhos ativos que são cartas (têm CardDisplay), excluindo o DropTarget
        List<RectTransform> activeCards = new List<RectTransform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.gameObject.activeSelf)
            {
                RectTransform rect = child as RectTransform;
                if (rect != null && rect.GetComponent<CardDisplay>() != null)
                    activeCards.Add(rect);
            }
        }

        int totalCards = activeCards.Count;
        if (totalCards == 0) return;

        // Distribuição circular: cada carta vai para a próxima linha sequencialmente
        // Ex: Carta 1 -> Linha 0, Carta 2 -> Linha 1, Carta 3 -> Linha 2, Carta 4 -> Linha 3, Carta 5 -> Linha 0, etc.
        Dictionary<int, List<RectTransform>> cardsPerRow = new Dictionary<int, List<RectTransform>>();
        for (int i = 0; i < numberOfRows; i++)
        {
            cardsPerRow[i] = new List<RectTransform>();
        }

        for (int i = 0; i < totalCards; i++)
        {
            int rowIndex = i % numberOfRows; // Distribuição circular
            cardsPerRow[rowIndex].Add(activeCards[i]);
        }

        float availableWidth = container.rect.width - (horizontalPadding * 2);
        float currentY = container.rect.height - verticalPadding - cardHeight; // Começa do topo (altura total menos padding e altura da carta)

        // Processa cada linha
        for (int row = 0; row < numberOfRows; row++)
        {
            List<RectTransform> cardsInRow = cardsPerRow[row];
            int cardsThisRow = cardsInRow.Count;

            if (cardsThisRow == 0) continue;

            // --- CÁLCULO DO ESPAÇAMENTO ---
            float spacing;
            
            if (cardsThisRow <= 1)
            {
                spacing = 0;
            }
            else
            {
                // Calcula o espaçamento necessário para preencher a largura disponível
                float totalCardsWidth = cardsThisRow * cardWidth;
                float remainingSpace = availableWidth - totalCardsWidth;
                
                if (remainingSpace <= 0)
                {
                    // Já está estourado: usa espaçamento negativo (sobreposição)
                    // Distribui o espaço negativo igualmente entre os espaços
                    spacing = remainingSpace / (cardsThisRow - 1);
                }
                else
                {
                    // Espaço positivo: calcula o espaçamento ideal
                    spacing = remainingSpace / (cardsThisRow - 1);
                    
                    // SE O ESPAÇAMENTO ULTRAPASSAR O MÁXIMO, USA O MÁXIMO
                    if (spacing > maxHorizontalSpacing)
                    {
                        spacing = maxHorizontalSpacing;
                    }
                }
            }

            // Limita ao mínimo (permite negativo para sobreposição)
            spacing = Mathf.Max(spacing, minHorizontalSpacing);

            // --- POSICIONAMENTO À ESQUERDA ---
            float startX = horizontalPadding;
            
            for (int i = 0; i < cardsThisRow; i++)
            {
                RectTransform cardRect = cardsInRow[i];
                
                float xPos = startX + (i * (cardWidth + spacing));

                // Configura âncora no canto inferior esquerdo para posicionamento previsível
                cardRect.anchorMin = new Vector2(0, 0);
                cardRect.anchorMax = new Vector2(0, 0);
                cardRect.pivot = new Vector2(0, 0);
                cardRect.anchoredPosition = new Vector2(xPos, currentY);
                cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);
            }

            // Move para a próxima linha (para baixo)
            currentY -= (cardHeight + verticalSpacing);
        }

        // Define a altura do container baseada no layout
        float totalHeight = (numberOfRows * cardHeight) + ((numberOfRows - 1) * verticalSpacing) + (verticalPadding * 2);
        container.sizeDelta = new Vector2(container.sizeDelta.x, totalHeight);
    }

    /// <summary>
    /// Método utilitário para forçar atualização do layout (chamado pelo DeckBuilderManager após mudanças)
    /// </summary>
    public void RefreshLayout()
    {
        UpdateLayout();
    }
}