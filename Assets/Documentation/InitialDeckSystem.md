# Sistema de Criação de Deck Inicial

## Visão Geral
O sistema de Deck Inicial (`InitialDeckBuilder.cs`) é responsável por gerar um baralho único e balanceado para o jogador no início de um novo jogo. Diferente de jogos que dão um deck fixo, este sistema cria uma experiência "Roguelike", onde cada save começa com ferramentas ligeiramente diferentes, mas com poder equivalente.

## Estrutura do Deck (40 Cartas)
O deck é composto por 5 "Pools" (Piscinas) de cartas, definidas para garantir consistência e evitar mãos injogáveis.

### 1. Monstros Fracos ("Fodder") - 15 Cartas
*   **Critérios:** Monstros Normais, Nível 1-4, ATK < 950.
*   **Função:** Servem como defesa inicial e tributo para monstros mais fortes.

### 2. Monstros Médios ("Warriors") - 12 Cartas
*   **Critérios:** Monstros Normais, Nível 1-4, ATK entre 951 e 1300.
*   **Função:** A espinha dorsal ofensiva do deck no início do jogo.

### 3. Monstros Fortes ("Ace") - 3 Cartas
*   **Critérios:** Monstros Normais ou de Efeito (Simples), Nível 1-4, ATK entre 1301 e 1600.
*   **Função:** As cartas mais valiosas do jogador. São raras para tornar o saque delas emocionante.

### 4. Magias de Suporte - 5 Cartas
*   **Critérios:** Cartas de Magia (Equipamento, Campo, Normal).
*   **Exclusões:** Cartas de destruição em massa (*Raigeki*, *Dark Hole*) e compras excessivas (*Pot of Greed*) são banidas desta lista inicial.

### 5. Armadilhas Básicas - 5 Cartas
*   **Critérios:** Cartas de Armadilha.
*   **Exclusões:** Armadilhas de alto impacto (*Mirror Force*) são banidas.

## Regras de Filtragem
Para garantir que o deck seja válido para o início da campanha (Ato 1), o sistema aplica filtros rígidos:
*   **Nível:** Máximo Nível 4 (sem monstros que exigem tributo na mão inicial).
*   **Tipo:** Sem Monstros de Fusão, Ritual ou Tokens no Main Deck.
*   **Banlist Interna:** Uma lista de IDs (`forbiddenIds`) impede que cartas "quebradas" (OP) apareçam, mesmo que se encaixem nos critérios de ATK/DEF.

## Fluxo de Execução
1.  O jogador inicia um "New Game".
2.  `GameManager` verifica se existe um save. Se não, chama `InitialDeckBuilder`.
3.  O Builder varre o `CardDatabase` e separa as cartas candidatas para cada Pool.
4.  Seleciona aleatoriamente a quantidade necessária de cada Pool.
5.  Embaralha a lista final e salva no perfil do jogador.
