# Sistema de Special Summon

## Visão Geral
O sistema de Invocação Especial permite colocar monstros no campo sem usar a Invocação Normal do turno. Diferente da Invocação Normal (que é restrita a Ataque Face-Up ou Defesa Face-Down "Set"), a Invocação Especial oferece flexibilidade de posição.

## Fluxo de Código

### 1. Gatilho
Uma carta ou efeito inicia o processo (ex: *Monster Reborn*, *Polymerization*, Efeito de Monstro).
O código chama `GameManager.Instance.PerformSpecialSummon(cardObject, cardData)`.

### 2. Seleção de Posição (`PositionSelectionUI`)
*   O jogo pausa e abre um modal (`Panel_PositionSelection`).
*   O jogador vê a carta e dois botões:
    *   **Ataque:** Ícone da carta na vertical.
    *   **Defesa:** Ícone da carta na horizontal.
*   Diferente do "Set", a Defesa aqui é **Face-Up** (virada para cima), permitindo que efeitos contínuos ou atributos sejam vistos imediatamente, a menos que o efeito especifique "Face-Down" (ex: *The Shallow Grave*).

### 3. Execução (`GameManager`)
*   Após a escolha, o callback aciona `FinalizeSummon`.
*   O monstro é movido para uma zona livre.
*   A rotação é aplicada (0º para Ataque, 90º para Defesa).
*   O `SummonManager` **não** incrementa o contador de `hasPerformedNormalSummon`, permitindo que o jogador ainda faça sua invocação normal no mesmo turno.

## Tipos de Special Summon Suportados
*   **Fusão:** Via *Polymerization* ou *Fusion Gate*.
*   **Ritual:** Via cartas de Magia de Ritual.
*   **Ressurreição:** Via *Monster Reborn*, *Call of the Haunted*.
*   **Invocação inerente:** Monstros que se invocam da mão (ex: *Cyber Dragon*).
