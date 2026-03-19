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
*   **D+:** 2000 - 2999 pts.
*   **D:** < 2000 pts (Vitória pobre).
*   **F:** Derrota. Recompensas não são concedidas.

## Sistema de Drop (Implementado)
Cada oponente na `CampaignDatabase` possui um objeto `rewards` estruturado por Tiers.
Ao vencer:
1.  O jogo calcula o Rank.
2.  O Rank define as probabilidades de saque de cada pool de cartas:

| Rank Obtido | S+ (Única) | Pool S (Top) | Pool B (Mid) | Pool C (Low) | Pool D (Fodder) |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **S+ (Deck Out)** | **15%** | 34% | 25.5% | 17% | 8.5% |
| **S / A+ / A** | 0% | **40%** | 30% | 20% | 10% |
| **B+ / B** | 0% | 10% | **40%** | 30% | 20% |
| **C+ / C** | 0% | 3% | 17% | **40%** | 40% |
| **D+ / D** | 0% | 1% | 4% | 25% | **70%** |
| **F** | 0% | 0% | 0% | 0% | 0% |

### Estrutura do JSON
O campo `rewards` no `characters.json` contém:
*   `s_plus`: ID único da carta especial.
*   `s`, `b`, `c`, `d`: Listas (arrays) de IDs de cartas classificadas por poder.

*Nota: No Rank S+, se o sorteio de 15% da carta única falhar, o jogo distribui os 85% restantes proporcionalmente entre os pools S, B, C e D seguindo a distribuição do Rank S (Melhor cenário possível).*

### Tag "NEW" na Recompensa
Na tela de vitória (`RewardPanelUI`), a faixa **"NEW"** aparece sobre a carta ganha se, e somente se, o jogador **não possuir nenhuma cópia** daquela carta em seu Baú (Trunk).
*   Se o jogador já tiver a carta (mesmo que nunca a tenha usado em um deck), a tag "NEW" **não** aparecerá na recompensa.
*   Isso difere da Biblioteca, onde "New" significa "Nunca usada em um deck".
