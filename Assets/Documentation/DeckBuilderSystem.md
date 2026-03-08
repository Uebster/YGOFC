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

### 5. Lista de Cartas

        // Forbidden (0)
        { "0289", 0 }, // Chaos Emperor Dragon - Envoy of the End
        { "2128", 0 }, // Yata-Garasu
        { "2097", 0 }, // Witch of the Black Forest
        { "0932", 0 }, // Imperial Order
        { "0872", 0 }, // Harpie's Feather Duster
        { "0321", 0 }, // Confiscation
        { "1863", 0 }, // The Forceful Sentry
        { "0287", 0 }, // Change of Heart
        { "1055", 0 }, // Last Turn
        { "0639", 0 }, // Fiber Jar
        { "0363", 0 }, // Cyber Jar
        { "1134", 0 }, // Magical Scientist
        { "0370", 0 }, // Cyber-Stein
        { "1252", 0 }, // Mirror Wall
        { "1480", 0 }, // Raigeki
        { "1268", 0 }, // Monster Reborn
        { "0485", 0 }, // Destruction Ring

        // Limited (1)
        { "0189", 1 }, // Black Luster Soldier - Envoy of the Beginning
        { "0293", 1 }, // Chaos Sorcerer
        { "0240", 1 }, // Breaker the Magical Warrior
        { "1587", 1 }, // Sangan
        { "0975", 1 }, // Jinzo
        { "1973", 1 }, // Tribe-Infecting Virus
        { "1651", 1 }, // Sinister Serpent
        { "0616", 1 }, // Exiled Force
        { "0378", 1 }, // D.D. Assailant
        { "0388", 1 }, // D.D. Warrior Lady
        { "1277", 1 }, // Morphing Jar
        { "1457", 1 }, // Primal Seed
        { "0422", 1 }, // Dark Magician of Chaos
        { "1513", 1 }, // Relinquished
        { "1790", 1 }, // Summoner Monk
        { "1517", 1 }, // Rescue Cat
        { "1447", 1 }, // Pot of Greed
        { "0791", 1 }, // Graceful Charity
        { "0414", 1 }, // Dark Hole
        { "0881", 1 }, // Heavy Storm
        { "1683", 1 }, // Snatch Steal
        { "1453", 1 }, // Premature Burial
        { "0259", 1 }, // Call of the Haunted
        { "1251", 1 }, // Mirror Force
        { "1533", 1 }, // Ring of Destruction
        { "1955", 1 }, // Torrential Tribute
        { "1120", 1 }, // Magic Cylinder
        { "0275", 1 }, // Ceasefire
        { "1499", 1 }, // Reckless Greed
        { "1811", 1 }, // Swords of Revealing Light
        { "1353", 1 }, // Nobleman of Crossout
        { "0228", 1 }, // Book of Moon
        { "1318", 1 }, // Mystical Space Typhoon
        { "0757", 1 }, // Giant Trunade
        { "1563", 1 }, // Royal Decree
        { "2020", 1 }, // United We Stand
        { "1138", 1 }, // Magician of Faith
        { "1170", 1 }, // Mask of Darkness
        { "1236", 1 }, // Mind Control
        { "0237", 1 }, // Brain Control
        { "1088", 1 }, // Limiter Removal
        { "1200", 1 }, // Megamorph
        { "1929", 1 }, // Time Seal
        { "2050", 1 }, // Wall of Revealing Light
        { "1610", 1 }, // Self-Destruct Button
        { "0264", 1 }, // Card Destruction
        { "0497", 1 }, // Dimension Fusion
        { "1523", 1 }, // Return from the Different Dimension
        
        // Exodia Pieces (Limited)
        { "0618", 1 }, // Exodia Head
        { "1061", 1 }, // Left Arm
        { "1062", 1 }, // Left Leg
        { "1530", 1 }, // Right Arm
        { "1531", 1 }, // Right Leg

        // Semi-Limited (2)
        { "0338", 2 }, // Creature Swap
        { "2024", 2 }, // Upstart Goblin
        { "1509", 2 }, // Reinforcement of the Army
        { "0359", 2 }, // Cyber Dragon
        { "0011", 2 }, // A Feint Plan
        { "0602", 2 }, // Enemy Controller
        { "1209", 2 }, // Messenger of Peace
        { "1077", 2 }, // Level Limit - Area B
        { "0817", 2 }, // Gravity Bind
        { "1245", 2 }, // Miracle Dig
        { "0786", 2 }, // Good Goblin Housekeeping
        { "1329", 2 }, // Needle Worm
        { "1163", 2 }, // Marauding Captain
        { "0077", 2 }, // Apprentice Magician
        { "1138", 2 }, // Magician of Faith
        { "1162", 2 }, // Manticore of Darkness
        { "0460", 2 }, // Deck Devastation Virus
        { "1604", 2 }, // Second Coin Toss
        { "1498", 2 }  // Reasoning