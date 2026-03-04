# Progressão da Campanha e História

## Visão Geral
A campanha é o coração do jogo, estruturada como um RPG de cartas inspirado em *Yu-Gi-Oh! Forbidden Memories*. O jogador viaja por um mapa mundi dividido em **10 Atos**, enfrentando oponentes progressivamente mais difíceis para ganhar cartas (Drop System) e fortalecer seu baralho.

## Estrutura dos Atos
O jogo possui 10 Atos principais. Cada Ato contém **10 Oponentes** (Níveis).
*   **Total de Duelos:** 100 oponentes únicos na campanha principal.
*   **Desbloqueio:** Vencer o oponente 1 desbloqueia o 2, e assim por diante.
*   **Boss de Ato:** O 10º oponente de cada ato é um "Chefe". Vencê-lo desbloqueia o próximo Ato no mapa.

### Lista de Atos (Resumo)
1.  **O Início:** Academia de Duelos (Tutoriais e rivais escolares).
2.  **Reino dos Duelistas:** Ilha de Pegasus.
3.  **Batalha na Cidade:** Kaiba Corp e Rare Hunters.
4.  **Guardiões Místicos:** Magos e templos antigos.
5.  **Mundo Virtual:** Arco do Big Five.
6.  **Ascensão das Trevas:** Vilões clássicos e versões sombrias.
7.  **O Desafio da Elite:** Revanches contra rivais em nível máximo.
8.  **O Vale dos Reis:** Viagem ao Egito Antigo.
9.  **O Labirinto Final:** Desafios de Bakura.
10. **A Batalha Suprema:** Torneio final contra Marik e os Deuses.

## O Mapa (Campaign Node System)
O mapa é composto por "Nós" (`CampaignNode`) interativos:
*   **HOME (Acampamento):** Onde o jogador salva o jogo, edita o deck, vê a biblioteca de cartas e seus troféus.
*   **ARENA (Free Duel):** Local para "grind". Permite enfrentar qualquer oponente já desbloqueado repetidamente para farmar cartas (Drop Rate) sem avançar a história.
*   **ACT NODES (1-10):** Botões que levam para a tela de seleção de oponentes daquele ato.

## Sistema de "Walkthrough"
Antes de cada duelo da campanha, uma tela de história (`Panel_Walkthrough`) é apresentada.
*   **Narrativa:** Texto estilo máquina de escrever contextualizando a batalha.
*   **Música:** Cada Ato possui um tema musical (`DuelTheme`) que toca nesta tela.
*   **Info:** Mostra o nome e a dificuldade do oponente.

## Mecânica de RPG
*   **Deck Inicial:** O jogador começa com um deck fraco gerado proceduralmente (`InitialDeckSystem`).
*   **Drops:** Ao vencer, o jogador ganha cartas do oponente (baseado no Rank S/A/B/C/D).
*   **Starchips (Futuro):** Planejado sistema de moeda para comprar cartas específicas via Password.

## Distribuição de Pools (Drops e Decks)
A qualidade das cartas (Pools 1.1 a 5.5) escala conforme o progresso da campanha. Tanto os decks dos oponentes quanto as recompensas de vitória seguem esta progressão:
*   **Ato 1 e 2:** Pools 1.x (Cartas Fracas/Iniciais)
*   **Ato 3 e 4:** Pools 2.x (Cartas Médias)
*   **Ato 5 e 6:** Pools 3.x (Cartas Fortes)
*   **Ato 7 e 8:** Pools 4.x (Cartas Elite)
*   **Ato 9 e 10:** Pools 5.x (Cartas Lendárias/God)