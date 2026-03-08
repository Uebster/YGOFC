# Estrutura e Funcionalidades do Painel de Duelo (`Panel_Duel`)

## Visão Geral
O `Panel_Duel` é a interface principal do jogo, onde ocorre a batalha entre o jogador e o oponente. Ele é gerenciado principalmente pelos scripts `GameManager`, `DuelFieldUI` e `DuelThemeManager`.

## Hierarquia de Objetos (Unity)

Abaixo está a estrutura exata dos GameObjects na cena, com seus principais componentes entre colchetes `[]`.

*   **Panel_Duel** `[Image]`
    *   **Panel_CardViewer** `[Image, CardViewerUI]`
        *   **CardViewer** `[CardViewerUI]`
            *   **Card2D** `[EventTrigger, CardDisplay, Mask, Image]`
                *   **Art** `[RawImage]`
            *   **CardStatsText** `[TextMeshProUGUI]`
            *   **Panel_Description** `[Image]`
                *   **CardNameText** `[TextMeshProUGUI]`
                *   **CardInfoText** `[TextMeshProUGUI]`
                *   **Scroll View** `[Image, ScrollRect]`
                    *   **Viewport** `[Image, Mask]`
                        *   **Content** `[]`
                        *   **CardDescriptionText** `[TextMeshProUGUI, Scrollbar, ContentSizeFitter]`
                    *   **Scrollbar Vertical** `[Image, Scrollbar]`
                        *   **Sliding Area** `[]`
                            *   **HandleDescription** `[Image]`
    *   **DuelBoard** `[DuelFieldUI]`
        *   **BoardBackground** `[Image]`
        *   **FieldImg** `[Image]`
        *   **FieldArea** `[Image]`
        *   **OpponentArea** `[]`
            *   **OpponentHand** `[HorizontalLayoutGroup]`
            *   **RemovedCardsOpponent** `[Image, PileDisplay]`
            *   **Field** `[]`
                *   **SpellRow** `[HorizontalLayoutGroup]`
                    *   **S_Zone_1** a **S_Zone_5** `[Image]`
                *   **MonsterRow** `[HorizontalLayoutGroup]`
                    *   **M_Zone_1** a **M_Zone_5** `[Image]`
            *   **FieldSpell** `[Image]`
            *   **OpponentExtraDeck** `[Image, PileDisplay]`
            *   **OpponentGraveyard** `[Image, PileDisplay]`
            *   **OpponentDeck** `[Image, PileDisplay]`
        *   **PlayerArea** `[]`
            *   **PlayerHand** `[HorizontalLayoutGroup]`
            *   **RemovedCardsPlayer** `[Image, PileDisplay]`
            *   **Field** `[]`
                *   **MonsterRow** `[HorizontalLayoutGroup]`
                    *   **M_Zone_1** a **M_Zone_5** `[Image]`
                *   **SpellRow** `[HorizontalLayoutGroup]`
                    *   **S_Zone_1** a **S_Zone_5** `[Image]`
            *   **FieldSpell** `[Image]`
            *   **PlayerExtraDeck** `[Image, PileDisplay]`
            *   **PlayerGraveyard** `[Image, PileDisplay]`
            *   **PlayerDeck** `[Image, PileDisplay]`
    *   **EffectsCards** `[]` (Container para VFX)
    *   **StatsArea** `[Image]`
        *   **PhaseIndicator** `[Image]`
            *   **Draw Phase** `[Image]` -> **Btn_Draw** `[Image, Button]` -> **Text_Draw** `[TMP]`
            *   **Standby Phase** `[Image]` -> **Btn_Standby** `[Image, Button]` -> **Text_Standby** `[TMP]`
            *   **Main Phase 1** `[Image]` -> **Btn_Main1** `[Image, Button]` -> **Text_Main1** `[TMP]`
            *   **Battle Phase** `[Image]` -> **Btn_Battle** `[Image, Button]` -> **Text_Battle** `[TMP]`
            *   **Main Phase 2** `[Image]` -> **Btn_Main2** `[Image, Button]` -> **Text_Main2** `[TMP]`
            *   **End Phase** `[Image]` -> **Btn_End** `[Image, Button]` -> **Text** `[TMP]`
        *   **PlayerProfile** `[]`
            *   **Avatar** `[Image]`
            *   **PanelPlayerProfile** `[Image]`
            *   **Name** `[TextMeshProUGUI]`
            *   **LP** `[TextMeshProUGUI]`
        *   **OpponentProfile** `[]`
            *   **Avatar** `[Image]`
            *   **OpponentPlayerProfile** `[Image]`
            *   **Name** `[TextMeshProUGUI]`
            *   **LP** `[TextMeshProUGUI]`
    *   **GraveyardViewerPanel** `[Image, GraveyardViewer]`
        *   **Scroll View** `[Image, ScrollRect]`
            *   **Viewport** `[Image, Mask]` -> **Content** `[HorizontalLayoutGroup]`
            *   **Scrollbar Horizontal** `[Image, Scrollbar]`
        *   **CloseGraveyard** `[Image, Button]` -> **Close** `[TMP]`
    *   **ExtraDeckViewerPanel** `[Image, GraveyardViewer]`
        *   (Estrutura similar ao GraveyardViewerPanel)
    *   **RemovedCardsViewerPanel** `[Image, GraveyardViewer]`
        *   (Estrutura similar, mas com GridLayoutGroup no Content)
    *   **DeckCardsViewerPanel** `[Image, GraveyardViewer]`
        *   (Estrutura similar, usada para visualizar o Deck em modo Dev)
    *   **Panel_ActionMenu** `[Image, VerticalLayoutGroup, DuelActionMenu]`
        *   **Btn_Summon** `[Image, Button]` -> **Summon** `[TMP]`
        *   **Btn_Set** `[Image, Button]` -> **Set** `[TMP]`
        *   **Btn_Activate** `[Image, Button]` -> **Activate** `[TMP]`
        *   **Btn_Cancel** `[Image, Button]` -> **Cancel** `[TMP]`
    *   **Panel_Confirmation** `[Image]`
        *   **Text_Confirmation** `[TextMeshProUGUI]`
        *   **Btn_Yes** `[Image, Button]` -> **Text_Yes** `[TMP]`
        *   **Btn_No** `[Image, Button]` -> **Text_No** `[TMP]`
    *   **Panel_PositionSelection** `[Image, PositionSelectionUI]`
        *   **Text_PositionAsk** `[TextMeshProUGUI]`
        *   **Btn_SummonPosition** `[Image, Button]` -> **SummonPosition** `[Image]`
        *   **Btn_SetPosition** `[Image, Button]` -> **SetPosition** `[Image]`
    *   **Panel_CardSelection** `[Image, CardSelectionUI]`
        *   **Scroll View** `[Image, ScrollRect]`
            *   **Viewport** `[Image, Mask]` -> **Content** `[HorizontalLayoutGroup]`
            *   **Scrollbar Horizontal** `[Image, Scrollbar]`
        *   **CloseDeckCards** `[Image, Button]`

## Descrição dos Componentes

### 1. Tabuleiro (`DuelBoard`)
A área central onde as cartas são jogadas.
*   **Zonas de Monstro (5x):** Onde os monstros são invocados.
*   **Zonas de Magia/Armadilha (5x):** Onde Spells/Traps são ativadas ou setadas.
*   **Zona de Campo:** Para cartas de Magia de Campo.
*   **Pilhas (Piles):**
    *   **Deck:** Pilha de compra. Clique para sacar (se permitido).
    *   **Graveyard (Cemitério):** Cartas destruídas/usadas. Clique para visualizar.
    *   **Extra Deck:** Monstros de Fusão. Clique para visualizar.
    *   **Removed Cards:** Cartas banidas. Clique para visualizar.

### 2. Perfis (`PlayerProfile` / `OpponentProfile`)
Exibem as informações vitais dos duelistas.
*   **Avatar:** Imagem do personagem.
*   **Nome:** Nome do duelista.
*   **LP (Life Points):** Pontos de vida atuais. Atualizados dinamicamente com efeitos de "tique-taque" ou tremor.

### 3. Indicador de Fases (`PhaseIndicator`)
Uma barra lateral ou superior que mostra o progresso do turno.
*   **Botões:** Draw, Standby, Main 1, Battle, Main 2, End.
*   **Interação:** O jogador pode clicar para avançar de fase (ex: pular Battle Phase).
*   **Visual:** A fase atual é destacada com cor e brilho neon (`PhaseManager`).

### 4. Visualizador de Cartas (`CardViewer`)
Um painel lateral esquerdo que mostra a carta sob o mouse em detalhes.
*   **Arte Grande:** Imagem em alta resolução.
*   **Descrição:** Texto completo do efeito (com scroll se necessário).
*   **Status:** ATK/DEF, Nível, Tipo, Atributo.

### 5. Mão do Jogador (`PlayerHand`)
*   Área onde as cartas compradas aparecem.
*   **Layout:** `HorizontalLayoutGroup` que organiza as cartas automaticamente.
*   **Hover:** Ao passar o mouse, a carta sobe (`hoverYOffset`) e ganha um contorno (`Outline`) para indicar seleção.

## Painéis de Sobreposição (Modais)

### 1. Visualizadores de Pilha (`GraveyardViewer`, `ExtraDeckViewer`, `RemovedCardsViewer`)
*   Abrem ao clicar nas respectivas pilhas.
*   Mostram uma lista rolável (`ScrollRect`) de todas as cartas naquela zona.

### 2. Menu de Ação (`Panel_ActionMenu`)
*   Abre ao clicar em uma carta na mão.
*   **Opções:** Summon (Ataque), Set (Defesa), Activate (Magia), Cancel.
*   **Inteligência:** Só mostra opções válidas (ex: esconde "Summon" se já invocou no turno).

### 3. Seleção de Posição (`Panel_PositionSelection`)
*   Abre ao realizar uma Invocação Especial (ex: *Monster Reborn*).
*   Permite escolher entre **Ataque** (Vertical) ou **Defesa** (Horizontal).

### 4. Confirmação (`Panel_Confirmation`)
*   Janela genérica para perguntas de "Sim/Não".
*   Usada para: Declarar ataque, Tributar monstros, Ativar efeitos opcionais, Render-se.

## Interações e Feedback

### Hover (Mouse Over)
*   **Cartas na Mão:** Sobem e brilham.
*   **Cartas no Campo:** Brilham (Outline) e aparecem no `CardViewer`.
*   **Pilhas:** Brilham para indicar que são clicáveis.

### Cliques
*   **Clique Esquerdo:** Seleciona, abre menu de ação ou ativa.
*   **Clique Direito:**
    *   **No Monstro (Campo):** Abre modal para mudar posição de batalha (Atk <-> Def).
    *   **No Deck:** Abre modal de Rendição (Surrender).
    *   **No Vazio:** Atalho para cancelar ou avançar fase (contextual).

### Temas (`DuelThemeManager`)
Todos os elementos visuais do `Panel_Duel` (fundos, botões, molduras) são dinâmicos e trocados automaticamente pelo `DuelThemeManager` com base no Ato da campanha atual.
