# Sistema de Temas de Duelo (DuelTheme)

## Visão Geral
O sistema de temas permite alterar completamente a aparência da interface de duelo (tabuleiro, botões, painéis) sem modificar a lógica do jogo. Isso é usado para dar uma identidade visual única a cada Ato da campanha (ex: Ato 1 = Escola, Ato 2 = Reino dos Duelistas).

## Componentes

### 1. `DuelTheme` (ScriptableObject)
É um arquivo de dados que contém as referências para todos os Sprites, Cores e Sons de um tema.
*   **Localização:** `Assets/Data/Themes/` (sugerido).
*   **Criação:** Botão Direito no Project -> Create -> YuGiOh -> Duel Theme.
*   **Campos:**
    *   Backgrounds (Tabuleiro, Perfis).
    *   Painéis (Card Viewer, Cemitério, Extra Deck, Removidas).
    *   Botões de Ação (Summon, Set, Activate).
    *   Modais (Confirmação, Seleção de Posição).
    *   Estilo de Texto (Fonte, Cor).
    *   Minigames (Sorte/Tempo - Moedas, etc).
    *   Efeitos Visuais (VFX) e Sonoros (BGM).

### 2. `DuelThemeManager` (Script)
É o script presente na cena `Panel_Duel` responsável por aplicar o tema.
*   Ele possui referências para os componentes `Image` e `Text` da UI.
*   Quando o duelo começa, o `GameManager` chama `DuelThemeManager.Instance.ApplyTheme(theme)`.
*   O Manager então substitui os sprites da UI pelos sprites definidos no arquivo `DuelTheme`.
*   Ele também repassa os assets específicos (como sprites de moedas e faces de dados) para os painéis isolados de minigames (`CoinTossUI` e `DiceRollUI`).

## Como Criar um Novo Tema
1.  Crie um novo `DuelTheme` no Project.
2.  Arraste suas texturas customizadas para os campos do ScriptableObject.
3.  Associe este tema a um Ato no `CampaignDatabase`.

## Elementos Customizáveis
*   **Tabuleiro:** Fundo do campo e imagem central.
*   **Fases:** Botões e fundo da barra de fases.
*   **Perfis:** Molduras dos avatares.
*   **Janelas:** Cemitério, Extra Deck, Cartas Removidas.
*   **Menus:** Menu de Ação (clique na carta), Modal de Confirmação, Modal de Posição (Ataque/Defesa).
*   **Minigames:** Sprites de Cara/Coroa para o `CoinTossUI` e os 6 lados do dado para o `DiceRollUI`, permitindo temática total.
*   **Efeitos:** Partículas de invocação, ataque, destruição, etc.
*   **Música:** BGM Normal, Tensa e de Vitória.

## Integração com Campanha
Cada `ActData` no `CampaignDatabase` possui um campo `theme`. Ao iniciar um duelo de campanha, o jogo carrega automaticamente o tema correspondente ao Ato atual.