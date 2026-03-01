# Sistema de Personagens e Decks

## Visão Geral
Os oponentes são definidos no `CharacterDatabase`. Cada personagem possui uma identidade visual (Avatar), dados de dificuldade e até 3 variações de baralho para oferecer desafios diferentes.

## Convenção de IDs
Os IDs dos personagens seguem o formato `XXX_nome_variacao` para facilitar a ordenação e busca.
*   **001-010:** Ato 1 (Ex: `001_novice`, `010_duke`)
*   **011-020:** Ato 2 (Ex: `020_pegasus`)
*   ...
*   **091-100:** Ato 10 (Ex: `100_marik`)

## Estrutura de Dados (`CharacterData`)
Cada oponente possui:
*   **ID:** Identificador único.
*   **Name:** Nome de exibição.
*   **Avatar:** Imagem do rosto (Sprite).
*   **Difficulty:** Valor numérico (1-10) para UI.
*   **DropPool:** Lista de IDs de cartas que este oponente pode dropar ao ser vencido.

## Sistema de 3 Decks
Para aumentar a rejogabilidade e permitir ajustes de dificuldade, cada personagem pode ter até 3 listas de deck:

### Deck A (Padrão)
*   O deck usado na primeira vez que você enfrenta o oponente na Campanha.
*   Balanceado para o nível do Ato correspondente.

### Deck B (Avançado/Hard)
*   Usado em "New Game+" ou modos de desafio.
*   Contém cartas mais fortes, combos melhores e menos monstros normais fracos.

### Deck C (Expert/Cheat)
*   Reservado para eventos especiais ou "God Mode".
*   Pode conter cartas proibidas, múltiplos cópias de cartas limitadas ou estratégias injustas (ex: Exodia turno 1).

## Inteligência Artificial (Planejado)
O sistema de IA usará o deck selecionado para tomar decisões.
*   **IA Básica:** Joga cartas se puder. Ataca se tiver ATK maior.
*   **IA Avançada:** Guarda cartas para combos, entende prioridade de ameaça.