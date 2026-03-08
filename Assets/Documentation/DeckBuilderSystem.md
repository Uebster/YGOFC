# Sistema de Construção de Decks (Deck Builder System)

## Visão Geral
O **Deck Builder** é a interface onde o jogador gerencia sua coleção de cartas e constrói os baralhos que usará nos duelos. O sistema permite filtrar, ordenar, pesquisar, importar/exportar e validar a legalidade dos decks.

## Estrutura de UI (Hierarquia)

Abaixo está a estrutura hierárquica dos objetos na cena Unity para o `Panel_DeckBuilder`.

*   **Panel_DeckBuilder** `[Image, DeckBuilderManager]` - *Gerenciador principal.*
    *   **Panel_CardViewer** `[Image, CardViewerUI]` - *Visualizador de detalhes da carta.*
        *   **CardViewer** `[CardViewerUI]`
            *   **Card2D** `[RawImage, EventTrigger]`
            *   **CardNameText** `[TMP]`
            *   **CardInfoText** `[TMP]`
            *   **CardStatsText** `[TMP]`
            *   **Panel_Description** `[Image]`
                *   **Scroll View** `[ScrollRect]`
                    *   **Viewport** `[Mask]`
                        *   **Content**
                    *   **Scrollbar Vertical** `[Scrollbar]`
                        *   **Sliding Area**
                            *   **Handle** `[Image]`
                *   **CardDescriptionText** `[TMP]`
        *   **Btn_SaveDeck** `[Button]` - *Salva e define como ativo.*
            *   **Text** `[TMP]` "Save Deck"
        *   **Btn_BackToMenu** `[Button]` - *Sai do menu.*
            *   **Text** `[TMP]` "Back"
        *   **Btn_ImportDeck** `[Button]` - *Carrega de arquivo JSON.*
            *   **Text** `[TMP]` "Import Deck"
        *   **Btn_ExportDeck** `[Button]` - *Salva em arquivo JSON.*
            *   **Text** `[TMP]` "Export Deck"
    *   **Deck Construction** `[TMP]` - *Título da Tela.*
    *   **Panel_Chest** `[Image]` - *Área do Baú (Trunk).*
        *   **Panel_SearchCardInput** `[Image]`
            *   **Input_SearchCard** `[TMP_InputField]`
                *   **Text Area** `[RectMask2D]`
                    *   **Placeholder** `[TMP]`
                    *   **Text** `[TMP]`
        *   **Panel_Filters** `[GridLayoutGroup]` - *Botões de Filtro.*
            *   **Btn_FilterAtk** `[Button]` - *Ordena por ATK.*
            *   **Btn_FilterDef** `[Button]` - *Ordena por DEF.*
            *   **Btn_FilterNormal** `[Button]` - *Filtra Normais.*
            *   **Btn_FilterEffect** `[Button]` - *Filtra Efeito.*
            *   **Btn_FilterSpell** `[Button]` - *Filtra Magias.*
            *   **Btn_FilterTrap** `[Button]` - *Filtra Armadilhas.*
            *   **Btn_FilterRitual** `[Button]` - *Filtra Rituais.*
            *   **Btn_FilterFusion** `[Button]` - *Filtra Fusões.*
        *   **Btn_FilterABC** `[Button]` - *Ordena A-Z / Z-A.*
        *   **Panel_CardChest** `[Image]` - *Lista de cartas disponíveis.*
            *   **Scroll View** `[ScrollRect]`
                *   **Viewport** `[Mask]`
                    *   **Content** `[VerticalLayoutGroup, ContentSizeFitter, DeckDropZone]`
                *   **Scrollbar Vertical** `[Scrollbar]`
        *   **ChestTitle** `[Image]`
            *   **Card List** `[TMP]`
    *   **Panel_Deck** `[Image]` - *Área dos Decks Ativos.*
        *   **Panel_MainDeck** `[Image]`
            *   **Scroll View** `[ScrollRect]`
                *   **Viewport** `[Mask]`
                    *   **Content** `[DeckDropZone]`
            *   **Main Deck Count Text** `[TMP]`
        *   **Panel_MainDeckTitle** `[Image]`
            *   **Text** `[TMP]` "Main Deck"
        *   **Panel_SideDeck** `[Image]`
            *   **Scroll View** `[ScrollRect]`
                *   **Viewport** `[Mask]`
                    *   **Content** `[DeckDropZone]`
            *   **Side Deck Count Text** `[TMP]`
        *   **Panel_SideDeckTitle** `[Image]`
            *   **Text** `[TMP]` "Side Deck"
        *   **Panel_ExtraDeck** `[Image]`
            *   **Scroll View** `[ScrollRect]`
                *   **Viewport** `[Mask]`
                    *   **Content** `[DeckDropZone]`
            *   **Extra Deck Count Text** `[TMP]`
        *   **Panel_ExtraDeckTitle** `[Image]`
            *   **Text** `[TMP]` "Extra Deck"
        *   **InputDeckName** `[TMP_InputField]` - *Nome para exportação.*
            *   **Text Area** `[RectMask2D]`
                *   **Placeholder** `[TMP]`
                *   **Text** `[TMP]`

## Funcionalidades

### 1. Filtragem e Ordenação
*   **Filtros de Tipo:** Botões como *Normal, Effect, Spell, Trap, Ritual, Fusion* funcionam como "toggles". Se ativos, mostram aquele tipo de carta. Múltiplos filtros podem estar ativos simultaneamente.
*   **Ordenação (Sort):**
    *   **ABC:** Ordena alfabeticamente pelo nome. Clicar novamente inverte a ordem (A-Z <-> Z-A).
    *   **ATK:** Ordena por pontos de ataque. Clicar novamente inverte (Maior <-> Menor).
    *   **DEF:** Ordena por pontos de defesa. Clicar novamente inverte (Maior <-> Menor).
    *   *Nota:* A ordenação respeita os filtros ativos.

### 2. Drag and Drop (Arrastar e Soltar)
*   Cartas podem ser arrastadas do Baú (Trunk) para qualquer zona de deck (Main, Side, Extra).
*   Cartas podem ser movidas entre zonas de deck.
*   Arrastar uma carta de um deck para o "vazio" ou de volta para o Baú a remove do deck.
*   **Validação de Zona:** O sistema impede, por exemplo, colocar Monstros de Fusão no Main Deck.

### 3. Regras de Construção (Limites)
Para salvar e sair com um deck válido:
*   **Main Deck:** Mínimo 40, Máximo 60 cartas.
*   **Side Deck:** Máximo 15 cartas.
*   **Extra Deck:** Máximo 15 cartas.
*   **Cópias:** Máximo de 3 cópias da mesma carta (soma de Main + Side + Extra).
*   **Ban List (Esqueleto):** Suporte para limitar cartas a 0 (Forbidden), 1 (Limited) ou 2 (Semi-Limited).
    *   **Opção Global:** Se `GameManager.allowForbiddenCards` estiver ativo, cartas Proibidas são tratadas como Limitadas (1).

### 4. Persistência
*   **Save Deck:** Salva a configuração atual no perfil do jogador (`GameManager`), tornando-o o deck ativo para duelos.
*   **Export Deck:** Salva a lista de IDs das cartas em um arquivo JSON na pasta de dados do aplicativo.
*   **Import Deck:** Carrega um arquivo JSON. O sistema verifica se o jogador possui as cartas no Baú (Trunk). Se não possuir, a carta não é adicionada.

### 5. Banlist (Lista de Restrições)

A lista abaixo reflete o formato clássico (baseado em 2005/Goat Format), adaptado para o equilíbrio da campanha.

#### 🚫 Proibidas (0 Cópias)
Estas cartas não podem ser usadas no Deck (a menos que a opção `allowForbiddenCards` esteja ativa no GameManager).

*   Chaos Emperor Dragon - Envoy of the End
*   Sangan
*   Witch of the Black Forest
*   Yata-Garasu
*   Dark Hole
*   Delinquent Duo
*   Graceful Charity
*   Harpie's Feather Duster
*   Monster Reborn
*   Raigeki
*   United We Stand
*   Imperial Order
*   Mirror Force
*   Change of Heart
*   Confiscation
*   The Forceful Sentry
*   Fiber Jar
*   Cyber Jar
*   Magical Scientist
*   Cyber-Stein
*   Mirror Wall
*   Destruction Ring

#### ⚠️ Limitadas (1 Cópia)
Apenas 1 cópia permitida no Deck (Main + Side + Extra).

*   **Monstros:**
    *   Black Luster Soldier - Envoy of the Beginning
    *   Chaos Sorcerer
    *   Breaker the Magical Warrior
    *   Sangan
    *   Jinzo
    *   Tribe-Infecting Virus
    *   Sinister Serpent
    *   Exiled Force
    *   D.D. Assailant
    *   D.D. Warrior Lady
    *   Morphing Jar
    *   Dark Magician of Chaos
    *   Relinquished
    *   Summoner Monk
    *   Rescue Cat
    *   Exodia the Forbidden One (e todas as 4 partes)
*   **Magias:**
    *   Pot of Greed
    *   Graceful Charity
    *   Dark Hole
    *   Heavy Storm
    *   Snatch Steal
    *   Premature Burial
    *   Swords of Revealing Light
    *   Nobleman of Crossout
    *   Book of Moon
    *   Mystical Space Typhoon
    *   Giant Trunade
    *   United We Stand
    *   Mind Control
    *   Brain Control
    *   Limiter Removal
    *   Megamorph
    *   Card Destruction
    *   Dimension Fusion
    *   Primal Seed
*   **Armadilhas:**
    *   Call of the Haunted
    *   Ring of Destruction
    *   Torrential Tribute
    *   Magic Cylinder
    *   Ceasefire
    *   Reckless Greed
    *   Royal Decree
    *   Mask of Darkness
    *   Time Seal
    *   Wall of Revealing Light
    *   Self-Destruct Button
    *   Return from the Different Dimension
    *   Protector of the Sanctuary

#### ⚠️⚠️ Semi-Limitadas (2 Cópias)
Até 2 cópias permitidas no Deck.

*   Creature Swap
*   Last Turn
*   Manticore of Darkness
*   Marauding Captain
*   Morphing Jar #2
*   Nobleman of Crossout
*   Reinforcement of the Army
*   Upstart Goblin
*   Cyber Dragon
*   A Feint Plan
*   Enemy Controller
*   Messenger of Peace
*   Level Limit - Area B
*   Gravity Bind
*   Miracle Dig
*   Good Goblin Housekeeping
*   Needle Worm
*   Apprentice Magician
*   Magician of Faith
*   Deck Devastation Virus
*   Second Coin Toss
*   Reasoning