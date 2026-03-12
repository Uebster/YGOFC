# Sistema de Construção de Decks (Deck Builder System)

## Visão Geral
O **Deck Builder** é a interface onde o jogador gerencia sua coleção de cartas do Baú (`Trunk`) e constrói os baralhos que usará nos duelos. O sistema permite filtrar, ordenar, pesquisar, importar/exportar e validar a legalidade dos decks em tempo real.

## Estrutura de UI (Hierarquia)

Abaixo está a estrutura hierárquica recomendada dos objetos na cena Unity para o `Panel_DeckBuilder`.

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
        *   **Panel_CardChest** `[Image]` - *Container da lista de cartas.*
            *   **Scroll View** `[ScrollRect]`
                *   **Viewport** `[Mask]`
                    *   **Content** `[VerticalLayoutGroup, ContentSizeFitter, DeckDropZone]` - *Onde os prefabs das cartas do baú são instanciados.*
                *   **Scrollbar Vertical** `[Scrollbar]`
        *   **ChestTitle** `[Image]`
            *   **Card List** `[TMP]`
    *   **Panel_Deck** `[Image]` - *Área dos Decks Ativos.*
        *   **Panel_MainDeck** `[Image]`
            *   **Scroll View** `[ScrollRect]`
                *   **Viewport** `[Mask]`
                    *   **Content** `[DeckDropZone, CustomDeckLayout]` - *Usa o layout customizado.*
            *   **Main Deck Count Text** `[TMP]`
        *   **Panel_MainDeckTitle** `[Image]`
            *   **Text** `[TMP]` "Main Deck"
        *   **Panel_SideDeck** `[Image]`
            *   **Scroll View** `[ScrollRect]`
                *   **Viewport** `[Mask]`
                    *   **Content** `[DeckDropZone]`
            *   **Side Deck Count Text** `[TMP]` - *Exibe "X / 15".*
        *   **Panel_SideDeckTitle** `[Image]`
            *   **Text** `[TMP]` "Side Deck"
        *   **Panel_ExtraDeck** `[Image]`
            *   **Scroll View** `[ScrollRect]`
                *   **Viewport** `[Mask]`
                    *   **Content** `[DeckDropZone]`
            *   **Extra Deck Count Text** `[TMP]`
            *   **Panel_ExtraDeckTitle** `[Image]` - *Exibe "X / 15".*
            *   **Text** `[TMP]` "Extra Deck"
        *   **InputDeckName** `[TMP_InputField]` - *Nome para exportação.*
            *   **Text Area** `[RectMask2D]`
                *   **Placeholder** `[TMP]`
                *   **Text** `[TMP]`

## Funcionalidades

### Paginação do Baú (Chest Pagination)
Para lidar com a grande quantidade de cartas do jogo (2147+), a lista do baú é paginada.
*   **`itemsPerPage`**: Variável no `DeckBuilderManager` que define quantas cartas aparecem por página (padrão: 50).
*   **`btnPrevPage` / `btnNextPage`**: Botões na UI que permitem ao jogador navegar entre as páginas de sua coleção.
*   **`txtPageInfo`**: Um campo de texto (`TextMeshProUGUI`) que exibe a informação da página atual, como "Page 1 / 43".

Isso garante que o jogo não tente renderizar milhares de itens de UI de uma vez, o que causaria lentidão e alto consumo de memória.

### 1. Filtragem e Ordenação
O `DeckBuilderManager` gerencia uma lista de cartas do baú (`currentTrunk`) e a re-exibe conforme os filtros e ordenações são aplicados.
*   **Filtros de Tipo:** Botões como *Normal, Effect, Spell, Trap, Ritual, Fusion* funcionam como "toggles". Se ativos, mostram aquele tipo de carta. Múltiplos filtros podem estar ativos simultaneamente.
*   **Ordenação (Sort):**
    *   **ABC:** Ordena alfabeticamente pelo nome. Clicar novamente inverte a ordem (A-Z <-> Z-A).
    *   **ATK:** Ordena por pontos de ataque. Clicar novamente inverte (Maior <-> Menor).
    *   **DEF:** Ordena por pontos de defesa. Clicar novamente inverte (Maior <-> Menor).
    *   *Nota:* A ordenação respeita os filtros ativos.
*   **Pesquisa por Texto:** O campo `Input_SearchCard` filtra a lista de cartas em tempo real, buscando o texto no nome da carta.

### 2. Sistema de Ícones (4 Campos)
Para organizar a exibição dos ícones de forma clara, o `DeckBuilderManager` agora possui 4 listas no Inspector, que correspondem diretamente aos `Image` no prefab `Card_PrefabChestList`:

*   **`attributeIcons`**: Mapeia o atributo do monstro (ex: "DARK", "LIGHT"). Usado pelo `AttributeIcon`.
*   **`raceIcons`**: Mapeia a raça do monstro (ex: "Warrior", "Dragon"). Usado pelo `RaceIcon`.
*   **`typeIcons`**: Mapeia o tipo principal da carta (ex: "Spell", "Trap"). Usado pelo `TypeIcon`.
*   **`subTypeIcons`**: Mapeia a propriedade da Magia/Armadilha (ex: "Equip", "Continuous", "Counter"). Usado pelo `SubTypeIcon`.

A lógica de exibição é a seguinte:
*   **Se a carta for um Monstro:**
    *   `AttributeIcon` e `RaceIcon` são ativados.
    *   `TypeIcon` e `SubTypeIcon` são desativados.
*   **Se a carta for uma Magia ou Armadilha:**
    *   `TypeIcon` e `SubTypeIcon` são ativados.
    *   `AttributeIcon` e `RaceIcon` são desativados.

Isso garante que cada tipo de carta mostre apenas os ícones relevantes, mantendo a interface limpa e informativa.

### 2. Drag and Drop (Arrastar e Soltar)
A lógica é dividida entre `DeckDragHandler` (na carta) e `DeckDropZone` (nas áreas de deck).
*   Cartas podem ser arrastadas do Baú (Trunk) para qualquer zona de deck (Main, Side, Extra).
*   Cartas podem ser movidas entre zonas de deck.
*   Arrastar uma carta de um deck para o "vazio" ou de volta para o Baú a remove do deck.
*   **Validação de Zona:** O `DeckBuilderManager.AddCardToDeck` impede, por exemplo, colocar Monstros de Fusão no Main Deck.
*   **Feedback Visual:** Se uma jogada for inválida (ex: 4ª cópia, zona errada, deck cheio), a área do deck alvo piscará em vermelho.

### 3. Regras de Construção (Limites)
A validação é feita em tempo real pelo `DeckBuilderManager.AddCardToDeck` e verificada novamente ao salvar.
*   **Main Deck:** Mínimo 40, Máximo 60 cartas.
*   **Side Deck:** Máximo 15 cartas.
*   **Extra Deck:** Máximo 15 cartas.
*   **Cópias:** Máximo de 3 cópias da mesma carta (soma de Main + Side + Extra).
*   **Ban List:** Suporte para limitar cartas a 0 (Forbidden), 1 (Limited) ou 2 (Semi-Limited).
    *   **Opção Global:** Se `GameManager.allowForbiddenCards` estiver ativo, cartas Proibidas são tratadas como Limitadas (1).
    *   **Opção Sem Limites:** Se `GameManager.disableBanlist` estiver ativo, a Banlist é ignorada e todas as cartas podem ter até 3 cópias.

### 4. Persistência
*   **Save Deck:** O botão `Btn_SaveDeck` chama `SaveDeck()`. Este método primeiro valida o deck. Se for válido, ele atualiza o deck ativo no `GameManager` e marca que não há mais alterações pendentes (`hasUnsavedChanges = false`).
*   **Sair (Back):** O botão `Btn_BackToMenu` chama `ExitDeckBuilder()`. Se `hasUnsavedChanges` for `true`, ele exibe um pop-up de confirmação antes de sair, para evitar a perda de alterações.
*   **Export/Import Deck:** (A ser implementado) Salva/carrega a lista de IDs das cartas em um arquivo JSON. A importação deve verificar se o jogador possui as cartas no Baú.

### 5. Indicador de Cartas "New"
*   Ao popular a lista do baú, o `DeckBuilderManager` verifica cada carta com `SaveLoadSystem.Instance.IsCardNew(card.id)`.
*   Se for `true`, ele instancia o `newTagPrefab` como filho do item da carta, criando o indicador visual.
*   Ao adicionar uma carta a qualquer deck, o `DeckBuilderManager` chama `SaveLoadSystem.Instance.MarkCardAsUsed(card.id)`, garantindo que a tag "New" desapareça na próxima vez que a biblioteca ou o construtor de decks for aberto.

## Layout Customizado do Deck (CustomDeckLayout.cs)
O comportamento de organização das cartas no Main Deck (distribuição em linhas e empilhamento) é muito específico e não pode ser alcançado com os componentes de Layout padrão do Unity. Para isso, foi criado o script `CustomDeckLayout.cs`.

### Explicação
Este script deve ser adicionado ao objeto `Content` do `Panel_MainDeck`. Ele remove qualquer outro componente de layout e assume o controle total do posicionamento de seus filhos (as cartas).

A cada atualização (`UpdateLayout`), ele:
1.  Conta o número total de cartas no deck.
2.  Distribui esse total entre o número de linhas (`numberOfRows`) de forma equilibrada. Ex: 41 cartas em 4 linhas resulta em uma linha com 11 e três com 10.
3.  Para cada linha, calcula o espaçamento horizontal necessário para que as cartas preencham o espaço disponível. Se o número de cartas exceder o espaço, o espaçamento se torna negativo, criando o efeito de sobreposição.
4.  Posiciona cada carta em sua coordenada `(x, y)` calculada.

### Parâmetros (Inspector)
O script expõe várias propriedades públicas para que você possa ajustar o layout diretamente no Unity Editor:
*   **`numberOfRows`**: O número de linhas fixas na grade (ex: 4).
*   **`cardWidth` / `cardHeight`**: As dimensões exatas do seu prefab de carta.
*   **`verticalSpacing`**: O espaço vertical entre as linhas.
*   **`minHorizontalSpacing`**: O espaçamento horizontal mínimo, mesmo quando as cartas estão sobrepostas. Use um valor negativo grande (ex: -80) para permitir bastante sobreposição.
*   **`horizontalPadding` / `verticalPadding`**: O preenchimento interno nas bordas do painel `Content`.

## Estrutura do Prefab do Baú (Card_PrefabChestList)
Este prefab representa um único item na lista rolável do baú. Sua estrutura é projetada para exibir um resumo completo da carta.

*   **Card_PrefabChestList** `[Image, LayoutElement]` - O objeto raiz. O `LayoutElement` é crucial para que o `ContentSizeFitter` do ScrollView funcione corretamente.
    *   **Card2D** `[EventTrigger, CardDisplay, Mask, Image]` - A imagem da carta em si. `CardDisplay` renderiza a arte, e `EventTrigger` detecta o mouse para o `CardViewer`.
    *   **CardNameText** `[TMP]` - Exibe o nome da carta.
    *   **CardStatsText** `[TMP]` - Exibe a quantidade disponível para uso (ex: "x2").
    *   **MonsterLvl** `[TMP]` - Exibe o nível do monstro.
    *   **AttributeIcon** `[Image]` - Ícone de Atributo (ex: FIRE).
    *   **RaceIcon** `[Image]` - Ícone de Raça (ex: Warrior).
    *   **TypeIcon** `[Image]` - Ícone de Tipo (ex: Spell).
    *   **SubTypeIcon** `[Image]` - Ícone de Subtipo (ex: Equip).
    *   **Star01 a Star12** `[Image]` - As estrelas de nível, que são ativadas ou desativadas conforme o nível do monstro.

## Banlist (Lista de Restrições)

A lista abaixo reflete um formato clássico (baseado em 2005/Goat Format), adaptado para o equilíbrio da campanha.
*(Estas restrições são aplicadas a menos que `disableBanlist` esteja ativo no GameManager)*

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
    *   Black Luster Soldier (Ritual)
    *   Chaos Sorcerer
    *   Breaker the Magical Warrior
    *   Jinzo
    *   Tribe-Infecting Virus
    *   Sinister Serpent
    *   Exiled Force
    *   D.D. Assailant
    *   D.D. Warrior Lady
    *   Ancient Gear Beast
    *   Armed Dragon LV5 / LV7
    *   Behemoth the King of All Animals
    *   Chaos Command Magician
    *   Injection Fairy Lily
    *   Reflect Bounder
    *   Twin-Headed Behemoth
    *   Vampire Lord
    *   Morphing Jar
    *   Dark Magician of Chaos
    *   Relinquished
    *   Summoner Monk
    *   Rescue Cat
    *   Exodia the Forbidden One (e todas as 4 partes)
*   **Magias:**
    *   Pot of Greed
    *   Heavy Storm
    *   Snatch Steal
    *   Premature Burial
    *   Swords of Revealing Light
    *   Nobleman of Crossout
    *   Book of Moon
    *   Mystical Space Typhoon
    *   Giant Trunade
    *   Mage Power
    *   Painful Choice
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