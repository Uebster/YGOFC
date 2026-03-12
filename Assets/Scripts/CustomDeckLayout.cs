using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Organiza as cartas em uma grade customizada.
/// Este script distribui as cartas de forma equilibrada entre um número fixo de linhas.
/// Conforme mais cartas são adicionadas, o espaçamento horizontal diminui para acomodá-las,
/// criando um efeito de "empilhamento" ou sobreposição quando necessário.
/// Todas as propriedades são ajustáveis via Inspector para fácil customização.
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
    public float verticalSpacing = 10f;
    [Tooltip("Espaçamento horizontal máximo entre as cartas.")]
    public float maxHorizontalSpacing = 15f;
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

        int totalCards = 0; // Usaremos para contar apenas os filhos ativos
        // Conta apenas filhos ativos para ignorar objetos desativados
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).gameObject.activeSelf)
                totalCards++;
        }

        if (totalCards == 0) return;

        // 1. Calcula a distribuição de cartas por linha.
        // Ex: Com 41 cartas e 4 linhas, o resultado será [11, 10, 10, 10].
        int[] cardsInRow = new int[numberOfRows];
        for (int i = 0; i < totalCards; i++)
        {
            // O operador de módulo (%) distribui as cartas uma a uma por linha.
            cardsInRow[i % numberOfRows]++;
        }

        float currentY = -verticalPadding;
        int cardIndex = 0; // Índice para percorrer os filhos do objeto

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
                spacing = (availableWidth - (cardsThisRow * cardWidth)) / (cardsThisRow - 1);
            }

            // --- FIX: Limita o espaçamento para não ficar nem muito grande, nem muito pequeno ---
            spacing = Mathf.Clamp(spacing, minHorizontalSpacing, maxHorizontalSpacing);

            // --- FIX: Alinha sempre à esquerda ---
            float startX = horizontalPadding;
            
            for (int j = 0; j < cardsThisRow; j++)
            {
                if (cardIndex >= transform.childCount) break;

                // Pega o próximo filho ativo
                RectTransform cardRect = transform.GetChild(cardIndex) as RectTransform;
                if (cardRect == null || !cardRect.gameObject.activeSelf) continue;

                float xPos = startX + (j * (cardWidth + spacing));

                // Define a âncora no canto superior esquerdo para um posicionamento previsível e consistente.
                cardRect.anchorMin = new Vector2(0, 1);
                cardRect.anchorMax = new Vector2(0, 1);
                cardRect.pivot = new Vector2(0, 1);
                cardRect.anchoredPosition = new Vector2(xPos, currentY);

                // Garante que a carta tenha o tamanho correto
                cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);

                cardIndex++;
            }
            // Move para a próxima linha
            currentY -= (cardHeight + verticalSpacing);
        }
    }
}
