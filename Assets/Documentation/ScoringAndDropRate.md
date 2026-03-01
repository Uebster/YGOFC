# Sistema de Pontuação e Drop Rate

## Visão Geral
O sistema de pontuação (`DuelScoreManager.cs`) avalia o desempenho do jogador em cada duelo. O objetivo é recompensar vitórias técnicas e rápidas, incentivando o uso de mecânicas avançadas (Fusão, Armadilhas) em vez de apenas força bruta. A pontuação define o **Rank**, que por sua vez define a qualidade das recompensas (**Drop Rate**).

## Cálculo de Pontuação (Duel Score)

### Pontuação Base
*   **Vitória:** +2500 pontos.
*   **Derrota:** A pontuação final é dividida por 4.

### Bônus de Ação (Técnica)
*   **Magia Ativada:** +100 pts
*   **Armadilha Ativada:** +100 pts
*   **Fusão Realizada:** +400 pts (Incentivo alto)
*   **Ritual Realizado:** +400 pts
*   **Tributo Realizado:** +150 pts
*   **Monstro Inimigo Destruído:** +300 pts
*   **Dano Máximo:** +100 pts a cada 1000 de dano em um único ataque.
*   **LP Restante:** +50 pts a cada 1000 LP sobrando.
*   **Vitória Perfeita (Sem Dano):** +1000 pts.

### Penalidades
*   **Tempo:** -3 pts por segundo (aprox. -180 pts/min). Duelos lentos diminuem o Rank.
*   **Cartas no Cemitério:** -20 pts por carta (desencoraja descarte excessivo).
*   **Dano Recebido:** -100 pts a cada 1000 de dano tomado.

## Ranks de Duelo
A pontuação final determina o Rank:
*   **S+:** Vitória por Deck Out (Oponente sem cartas). Independente da pontuação.
*   **S:** > 9000 pts (Vitória Perfeita ou muito rápida).
*   **A+:** 8000 - 8999 pts.
*   **A:** 7000 - 7999 pts.
*   **B+:** 6000 - 6999 pts.
*   **B:** 5000 - 5999 pts.
*   **C+:** 4000 - 4999 pts.
*   **C:** 3000 - 3999 pts.
*   **D:** < 3000 pts (Vitória pobre ou Derrota).

## Sistema de Drop (Planejado)
Cada oponente na `CampaignDatabase` possui uma lista de `rewards` (IDs de cartas).
Ao vencer:
1.  O jogo calcula o Rank.
2.  O Rank define a **probabilidade** e o **pool** de saque.
    *   **Rank S:** Alta chance de cartas Raras/Super Raras do oponente. Pode dropar até 3 cartas.
    *   **Rank A:** Chance média de cartas Raras. Dropa 1-2 cartas.
    *   **Rank B/C/D:** Apenas cartas comuns ou "lixo" (Fodder).

*Nota: O sistema de entrega de cartas (adicionar ao Trunk) é acionado pelo `GameManager` ao final do duelo, usando os dados do `DuelScoreManager`.*
