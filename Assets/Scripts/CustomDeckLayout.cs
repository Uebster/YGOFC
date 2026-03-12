using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Organiza os itens filhos em uma grade com um número fixo de linhas,
/// distribuindo os itens de forma equilibrada e ajustando o espaçamento
/// horizontal para caber no contêiner, permitindo sobreposição.
/// </summary>
[ExecuteInEditMode] // Permite ver as mudanças no Editor sem rodar o jogo
public class CustomDeckLayout : MonoBehaviour
{
    [Header("Configuração da Grade")]
    [Tooltip("O número de linhas fixas na grade.")]
    [Range(1, 20)]
    public int numberOfRows = 4;

    [Header("Dimensões dos Itens")]
    [Tooltip("A largura de cada carta na grade.")]
    public float cardWidth = 96f;
    [Tooltip("A altura de cada carta na grade.")]
    public float cardHeight = 135f;

    [Header("Espaçamento")]
    [Tooltip("Espaçamento vertical entre as linhas.")]
    public float verticalSpacing = 3f;
    [Tooltip("Espaçamento horizontal mínimo, mesmo quando as cartas estão sobrepostas. Use um valor negativo grande para permitir bastante sobreposição.")]
    public float minHorizontalSpacing = -80f;

    [Header("Padding (Preenchimento Interno)")]
    [Tooltip("Espaço à esquerda e à direita dentro do contêiner.")]
    public float horizontalPadding = 5f;
    [Tooltip("Espaço acima e abaixo dentro do contêiner.")]
    public float verticalPadding = 3f;


    #if UNITY_EDITOR
    // Atualiza o layout no editor sempre que uma propriedade é alterada para feedback visual imediato.
    void Update()
    {
        if (!Application.isPlaying)
        {
            UpdateLayout();
        }
    }
    #endif

    /// <summary>
    /// Calcula e aplica a posição de cada carta filha.
    /// Este método é chamado pelo DeckBuilderManager quando o deck é atualizado.
    /// </summary>
    public void UpdateLayout()
    {
        RectTransform container = GetComponent<RectTransform>();
        if (container == null) return;

        int totalCards = 0;
        // Conta apenas filhos ativos para ignorar objetos desativados
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).gameObject.activeSelf)
                totalCards++;
        }

        if (totalCards == 0) return;

        // 1. Calcula quantas cartas irão para cada linha (distribuição round-robin)
        int[] cardsInRow = new int[numberOfRows];
        for (int i = 0; i < totalCards; i++)
        {
            cardsInRow[i % numberOfRows]++;
        }

        float currentY = -verticalPadding;
        int cardIndex = 0;

        // 2. Itera sobre cada linha para posicionar as cartas
        for (int i = 0; i < numberOfRows; i++)
        {
            int cardsThisRow = cardsInRow[i];
            if (cardsThisRow == 0) continue;

            // A largura disponível para as cartas, descontando o padding
            float availableWidth = container.rect.width - (horizontalPadding * 2);
            float spacing;

            if (cardsThisRow <= 1)
            {
                spacing = 0; // Se houver apenas uma carta, não há espaçamento
            }
            else
            {
                // Calcula o espaçamento ideal para que as cartas preencham a largura disponível.
                // Se houver mais cartas, o espaçamento diminuirá, podendo ficar negativo (sobreposição).
                spacing = (availableWidth - cardWidth) / (cardsThisRow - 1);
            }

            // Limita o espaçamento para não ficar menor que o mínimo definido
            spacing = Mathf.Max(spacing, minHorizontalSpacing);

            // Calcula a largura total que a linha de cartas ocupará com o espaçamento atual
            float totalRowWidth = cardWidth + (Mathf.Max(0, cardsThisRow - 1) * spacing);
            float startX = horizontalPadding;

            // Centraliza o grupo de cartas se ele não ocupar toda a largura
            if (totalRowWidth < availableWidth)
            {
                startX = (availableWidth - totalRowWidth) / 2 + horizontalPadding;
            }

            for (int j = 0; j < cardsThisRow; j++)
            {
                if (cardIndex >= transform.childCount) break;

                RectTransform cardRect = transform.GetChild(cardIndex) as RectTransform;
                if (cardRect == null || !cardRect.gameObject.activeSelf) continue;

                float xPos = startX + (j * spacing);

                // Define a âncora no canto superior esquerdo para um posicionamento previsível
                cardRect.anchorMin = new Vector2(0, 1);
                cardRect.anchorMax = new Vector2(0, 1);
                cardRect.pivot = new Vector2(0, 1);
                cardRect.anchoredPosition = new Vector2(xPos, currentY);

                cardIndex++;
            }
            // Move para a próxima linha
            currentY -= (cardHeight + verticalSpacing);
        }
    }
}
