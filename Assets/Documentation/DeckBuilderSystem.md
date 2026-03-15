# Sistema de Construção de Decks (Deck Builder System)

## Visão Geral
O **Deck Builder** é a interface onde o jogador gerencia sua coleção de cartas do Baú (`Trunk`) e constrói os baralhos que usará nos duelos. O sistema permite filtrar, ordenar, pesquisar, importar/exportar e validar a legalidade dos decks em tempo real.

## Baú de Cartas (Virtual Scrolling)

Para lidar com a grande quantidade de cartas do jogo (2147+), a lista do baú (`Chest`) utiliza um sistema de **Virtual Scrolling**. A lógica foi terceirizada para scripts dedicados, mantendo o `DeckBuilderManager` focado apenas em gerenciar os dados.

### Objetivo
O objetivo principal é garantir alta performance e uma experiência de usuário fluida, mesmo ao exibir milhares de cartas. Em vez de instanciar um objeto de UI para cada carta de uma vez (o que travaria o jogo), o sistema cria e gerencia apenas os objetos que estão visíveis na tela, reciclando-os conforme o jogador rola a lista.

### Arquitetura e Scripts Envolvidos

1.  **`DeckBuilderManager.cs` (O Gerente de Dados):**
    *   Sua responsabilidade é **filtrar e ordenar** a lista completa de cartas (`currentTrunk`) com base nas ações do jogador (pesquisa de texto, filtros de tipo, ordenação).
    *   Após processar a lista, ele **entrega o resultado final** para o `TrunkScrollManager`, chamando o método `Initialize()`. Ele não se envolve mais com a criação ou posicionamento de objetos de UI do baú.

2.  **`TrunkScrollManager.cs` (O Cérebro do Scroll):**
    *   Este script é adicionado ao `Scroll View` do baú e substitui a lógica de layout padrão do Unity.
    *   **Pooling:** Ele cria um "pool" de objetos de UI (prefabs da carta) em quantidade suficiente apenas para preencher a tela.
    *   **Cálculo de Posição:** Ele escuta o evento `onValueChanged` do `ScrollRect`. A cada movimento, ele calcula quais índices da lista de cartas deveriam estar visíveis.
    *   **Reciclagem:** Pega os objetos do pool, move-os para a posição correta na grade e chama o `TrunkCardScrollItem` para que ele se atualize com os novos dados da carta. Itens que saem da tela são desativados.

3.  **`TrunkCardScrollItem.cs` (O Item da Lista):**
    *   Este script é adicionado ao prefab da carta no baú (`Card_PrefabChestList`).
    *   Sua única função é atuar como uma ponte. Ele recebe os dados de uma carta (`IGrouping<string, CardData>`) do `TrunkScrollManager`.
    *   Ele então busca informações adicionais no `DeckBuilderManager` (como o número de cópias já no deck) e passa todos os dados para o script `ChestCardItem`, que é o responsável final por atualizar os elementos visuais (nome, imagem, estrelas, etc.).

### Fluxo de Execução
1.  O jogador aplica um filtro ou busca no `DeckBuilderManager`.
2.  `DeckBuilderManager` processa a lista de 2147+ cartas e gera uma lista final `filteredCardGroups`.
3.  `DeckBuilderManager` chama `trunkScrollManager.Initialize(filteredCardGroups)`.
4.  `TrunkScrollManager` calcula a altura total do conteúdo para que a barra de rolagem funcione corretamente e cria seu pool de itens de UI.
5.  O jogador move a barra de rolagem.
6.  O método `OnScroll` do `TrunkScrollManager` é acionado.
7.  Ele calcula o primeiro item visível e, em um loop, posiciona os itens do pool e chama `item.UpdateContent(cardData)` para cada um.
8.  O `TrunkCardScrollItem` recebe os dados, consulta o `DeckBuilderManager` para obter contagens e chama o `Setup()` do `ChestCardItem` para exibir a carta na tela.

### Cuidados Especiais (Troubleshooting)
**1. Cartas Renderizando Fora da Tela:**
Para que a matemática de `xPos` e `yPos` do `TrunkScrollManager` funcione perfeitamente, as âncoras e o pivô do Content e dos itens (prefabs) **precisam** estar no canto superior esquerdo `(0, 1)`. O script agora força essa configuração via código no método `CreatePool` para evitar problemas de montagem na Unity.

**2. Bug de Imagem Piscando/Trocada (Flickering):**
Quando o jogador rola a lista muito rápido, as caixas de UI (prefabs) são recicladas e reaproveitadas antes mesmo da imagem antiga terminar de baixar. Para evitar que a imagem de uma carta "A" apareça na caixa quando ela já virou a carta "B":
*   O `ChestCardItem.Setup` agora usa `StopAllCoroutines()` no momento em que é reaproveitado.
*   A requisição atual (`UnityWebRequest`) é armazenada em uma variável e cancelada (`.Dispose()`) imediatamente caso seja reciclada ou a janela seja fechada (`OnDisable()`).
*   Isso previne *Memory Leaks* e sobreposição de imagens.

**3. Tamanho Dinâmico da Janela (Altura = 0):**
Se o painel acabou de ser ativado, a Unity ainda não tem as dimensões finais do `ScrollRect.viewport`. O script utiliza `Canvas.ForceUpdateCanvases()` para forçar a Unity a calcular o layout no mesmo frame, garantindo que o pool de itens não seja instanciado com o número errado de cartas (ex: 2 cartas em vez de 20).

## Estrutura de UI (Hierarquia)

Abaixo está a estrutura hierárquica recomendada dos objetos na cena Unity para o `Panel_DeckBuilder`.

*   **Panel_DeckBuilder** `[Image, DeckBuilderManager]`
    *   **Panel_CardViewer** `[Image, CardViewerUI]`
        *   **CardViewer** `[CardViewerUI]`
            *   **Card2D** `[RawImage, EventTrigger]`
            *   **Panel_Description** `[Image]`
                *   **Scroll View** `[Image, ScrollRect]`
                    *   **Viewport** `[Image, Mask]`
                    *   **Content** `[VerticalLayoutGroup]`
                    *   **Scrollbar Vertical** `[Image, Scrollbar]`
                        *   **Sliding Area** `[]`
                        *   **Handle** `[Image]`
                *   **CardDescriptionText** `[TextMeshProUGUI, Scrollbar]`
            *   **CardNameText** `[TextMeshProUGUI]`
            *   **CardInfoText** `[TextMeshProUGUI]`
            *   **CardStatsText** `[TextMeshProUGUI]`
    *   **Btn_SaveDeck** `[Image, Button]`
        *   **Save Deck** `[TextMeshProUGUI]`
    *   **Btn_BackToMenu** `[Image, Button]`
        *   **Back** `[TextMeshProUGUI]`
    *   **Btn_Import** `[Image, Button]`
        *   **Import** `[TextMeshProUGUI]`
    *   **Btn_Export** `[Image, Button]`
        *   **Export** `[TextMeshProUGUI]`
    *   **Deck Construction** `[TextMeshProUGUI]`
    *   **Panel_Chest** `[Image]`
        *   **Panel_SearchCardInput** `[Image]`
            *   **Text Area** `[RectMask2D]`
                *   **Placeholder** `[TextMeshProUGUI, LayoutElement]`
                *   **Text** `[TextMeshProUGUI]`
            *   **Input_SearchCard** `[Image, TMP_InputField]`
        *   **Panel_Filters** `[Image, GridLayoutGroup]`
            *   **Btn_FilterAtk** `[Image, Button]`
            *   **Btn_FilterDef** `[Image, Button]`
            *   **Btn_FilterNormal** `[Image, Button]`
            *   **Btn_FilterEffect** `[Image, Button]`
            *   **Btn_FilterSpell** `[Image, Button]`
            *   **Btn_FilterTrap** `[Image, Button]`
            *   **Btn_FilterFusion** `[Image, Button]`
            *   **Btn_FilterRitual** `[Image, Button]`
            *   **Btn_FilterABC** `[Image, Button]`
        *   **Panel_CardChest** `[Image]`
            *   **Scroll View** `[Image, ScrollRect, TrunkScrollManager]`
                *   **Viewport** `[Image, Mask]`
                *   **Content** `[RectTransform, DeckDropZone]`
                *   **Scrollbar** `[Image, Scrollbar]`
                    *   **Sliding Area** `[]`
                    *   **Handle** `[Image]`
        *   **ChestTitle** `[Image]`
            *   **Card List** `[TextMeshProUGUI]`
    *   **Panel_Deck** `[Image]`
        *   **Panel_MainDeck** `[Image]`
            *   **Scroll View** `[Image]`
                *   **Viewport** `[Image, Mask]`
                *   **Image** `[Image]`
                *   **Content** `[CustomDeckLayout, DeckDropZone]`
            *   **Main Deck Count Text** `[TextMeshProUGUI]`
            *   **NumberOfCardsType** `[Image]`
                *   **NormalQuant** `[Image]`
                *   **NormalQuantText** `[TextMeshProUGUI]`
                *   **EffectQuant** `[Image]`
                *   **EffectQuantText** `[TextMeshProUGUI]`
                *   **SpellQuant** `[Image]`
                *   **SpellQuantText** `[TextMeshProUGUI]`
                *   **TrapQuant** `[Image]`
                *   **TrapQuantText** `[TextMeshProUGUI]`
                *   **RitualQuant** `[Image]`
                *   **RitualQuantText** `[TextMeshProUGUI]`
            *   **Panel_MainDeckTitle** `[Image]`
                *   **Main Deck** `[TextMeshProUGUI]`
        *   **Panel_SideDeck** `[Image]`
            *   **Scroll View** `[Image]`
                *   **Viewport** `[Image, Mask]`
                *   **Image** `[Image, DeckDropZone]`
                *   **Content** `[CustomDeckLayout]`
            *   **NumberOfCardsType** `[Image]`
                *   **NormalQuant** `[Image]`
                *   **NormalQuantText** `[TextMeshProUGUI]`
                *   **EffectQuant** `[Image]`
                *   **EffectQuantText** `[TextMeshProUGUI]`
                *   **SpellQuant** `[Image]`
                *   **SpellQuantText** `[TextMeshProUGUI]`
                *   **TrapQuant** `[Image]`
                *   **TrapQuantText** `[TextMeshProUGUI]`
                *   **RitualQuant** `[Image]`
                *   **RitualQuantText** `[TextMeshProUGUI]`
            *   **Side Deck Count Text** `[TextMeshProUGUI]`
            *   **Panel_SideDeckTitle** `[Image]`
                *   **Side Deck** `[TextMeshProUGUI]`
        *   **Panel_ExtraDeck** `[Image]`
            *   **Scroll View** `[Image, ScrollRect]`
                *   **Viewport** `[Image, Mask]`
                *   **Image** `[Image, DeckDropZone]`
                *   **Content** `[CustomDeckLayout]`
            *   **Extra Deck Count Text** `[TextMeshProUGUI]`
            *   **Panel_ExtraDeckTitle** `[Image]`
                *   **Extra Deck** `[TextMeshProUGUI]`
            *   **NumberOfCardsType** `[Image]`
                *   **FusionQuant** `[Image]`
                *   **FusionQuantText** `[TextMeshProUGUI]`

## Funcionalidades

### 1. Filtragem e Ordenação
O `DeckBuilderManager` gerencia uma lista de cartas do baú (`currentTrunk`) e a re-exibe conforme os filtros e ordenações são aplicados.
*   **Filtros de Tipo:** Botões como *Normal, Effect, Spell, Trap, Ritual, Fusion* são mutuamente exclusivos. Apenas um filtro de tipo pode estar ativo por vez. Clicar em um filtro ativo o desativa, mostrando todas as cartas novamente.
*   **Ordenação (Sort):**
    *   **ABC:** Ordena alfabeticamente pelo nome. Clicar novamente inverte a ordem (A-Z <-> Z-A).
    *   **ATK:** Ordena por pontos de ataque. Clicar novamente inverte (Maior <-> Menor).
    *   **DEF:** Ordena por pontos de defesa. Clicar novamente inverte (Maior <-> Menor).
    *   *Nota:* A ordenação respeita os filtros ativos.
*   **Pesquisa por Texto:** O campo `Input_SearchCard` filtra a lista de cartas em tempo real (com um pequeno delay para performance), buscando o texto no nome da carta.

### Contagem Detalhada de Cartas
A UI agora fornece uma contagem detalhada dos tipos de carta em cada deck (Principal, Lateral e Extra).
*   Para o Main e Side Deck, a UI exibe contagens separadas para monstros `Normal`, `Effect`, `Ritual`, além de `Spell` e `Trap`.
*   Para o Extra Deck, a UI exibe a contagem de monstros `Fusion`.
*   Essa contagem é atualizada em tempo real conforme as cartas são adicionadas ou removidas dos decks, fornecendo feedback instantâneo ao jogador.

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

### 3. Drag and Drop (Arrastar e Soltar)
A lógica é dividida entre `DeckDragHandler` (na carta) e `DeckDropZone` (nas áreas de deck).
*   Cartas podem ser arrastadas do Baú (Trunk) para qualquer zona de deck (Main, Side, Extra).
*   Cartas podem ser movidas entre zonas de deck.
*   Arrastar uma carta de um deck para o "vazio" ou de volta para o Baú a remove do deck.
*   **Validação de Zona:** O `DeckBuilderManager.AddCardToDeck` impede, por exemplo, colocar Monstros de Fusão no Main Deck.
*   **Feedback Visual:** Se uma jogada for inválida (ex: 4ª cópia, zona errada, deck cheio), a área do deck alvo piscará em vermelho.

### 4. Regras de Construção (Limites)
A validação é feita em tempo real pelo `DeckBuilderManager.AddCardToDeck` e verificada novamente ao salvar.
*   **Main Deck:** Mínimo 40, Máximo 60 cartas.
*   **Side Deck:** Máximo 15 cartas.
*   **Extra Deck:** Máximo 15 cartas.
*   **Cópias:** Máximo de 3 cópias da mesma carta (soma de Main + Side + Extra).
*   **Ban List:** Suporte para limitar cartas a 0 (Forbidden), 1 (Limited) ou 2 (Semi-Limited).
    *   **Opção Global:** Se `GameManager.allowForbiddenCards` estiver ativo, cartas Proibidas são tratadas como Limitadas (1).
    *   **Opção Sem Limites:** Se `GameManager.disableBanlist` estiver ativo, a Banlist é ignorada e todas as cartas podem ter até 3 cópias.

### 5. Persistência
*   **Save Deck:** O botão `Btn_SaveDeck` chama `SaveDeck()`. Este método primeiro valida o deck. Se for válido, ele atualiza o deck ativo no `GameManager` e marca que não há mais alterações pendentes (`hasUnsavedChanges = false`).
*   **Sair (Back):** O botão `Btn_BackToMenu` chama `ExitDeckBuilder()`. Se `hasUnsavedChanges` for `true`, ele exibe um pop-up de confirmação antes de sair, para evitar a perda de alterações.

### 6. Importação e Exportação de Decks
O Deck Builder também permite que o jogador salve ("exporte") e carregue ("importe") receitas de decks para uso futuro. Esta funcionalidade é gerenciada por um sistema dedicado para manter a organização.

*   **Lógica de UI:** O script `DeckImportExportManager.cs` controla um painel separado que é ativado pelos botões "Import" e "Export". Ele é responsável por listar os decks salvos e capturar o nome para um novo deck.
*   **Armazenamento:** As receitas de deck são salvas diretamente no arquivo de save do jogador (`.save`) como uma lista de objetos `DeckRecipe`. O `SaveLoadSystem.cs` gerencia a leitura e escrita desses dados.
*   **Integração:** O `DeckBuilderManager` atua como uma ponte, fornecendo os métodos `ExportCurrentDeck(deckName)` e `ImportDeck(deckName)`.
    *   `ExportCurrentDeck`: Pega as listas de cartas atuais (Main, Side, Extra) e as envia para o `SaveLoadSystem` para serem salvas.
    *   `ImportDeck`: Recebe uma lista de IDs de cartas do `SaveLoadSystem` e as carrega no Deck Builder, limpando o deck anterior.

### 7. Indicador de Cartas "New"
*   Ao popular a lista do baú, o `DeckBuilderManager` verifica cada carta com `SaveLoadSystem.Instance.IsCardNew(card.id)`.
*   Se for `true`, ele instancia o `newTagPrefab` como filho do item da carta, criando o indicador visual.
*   Ao adicionar uma carta a qualquer deck, o `DeckBuilderManager` chama `SaveLoadSystem.Instance.MarkCardAsUsed(card.id)`, garantindo que a tag "New" desapareça na próxima vez que a biblioteca ou o construtor de decks for aberto.

## Layout Customizado do Deck (CustomDeckLayout.cs)
O comportamento de organização das cartas no Main Deck (distribuição em linhas e empilhamento) é muito específico e não pode ser alcançado com os componentes de Layout padrão do Unity. Para isso, foi criado o script `CustomDeckLayout.cs`.

### Regras de Arquitetura e Prevenção de Erros (Troubleshooting)
*   **Invariância de Texto:** Filtros, Tipos e Atributos sempre utilizam a conversão para minúsculas (`ToLowerInvariant()`) ou `.Contains()`. O formato da base de dados (Ex: "Fusion", "FUSION") nunca deve ser verificado com igualdade estrita (`==`), prevenindo falhas na lógica do Extra Deck.
*   **Object Pooling vs Dados:** Em listas virtuais (ex: Baú), as instâncias visuais são recicladas. É OBRIGATÓRIO garantir que o script recarregado atualize o `CardData` de todos os componentes atrelados (como o `DeckDragHandler`), do contrário o usuário pode arrastar um dado de carta "fantasma" que resultará em uma carta branca.
*   **Otimização do RefreshAllUI:** O redesenho da tela (Refresh) não destrói e recria todos os GameObjects do zero (o que causa lag/flickering). A lógica agora reaproveita os GameObjects existentes e apenas atualiza a arte através do `SetCard()` caso o ID da carta naquela posição tenha mudado.

### Explicação
Este script deve ser adicionado ao objeto `Content` do `Panel_MainDeck`. Ele remove qualquer outro componente de layout e assume o controle total do posicionamento de seus filhos (as cartas).

A cada atualização (`RefreshLayout`), ele executa um sistema de **Flow Layout** (Fluxo Contínuo):
1.  Pega o total de cartas ativas e vai enchendo a primeira linha da esquerda para a direita.
2.  Se a linha não couber todas as cartas, ele aplica o espaçamento negativo (sobreposição) até que atinja o limite de `maxCardsPerRow` (Ex: 15 cartas).
3.  A 16ª carta inicia uma nova linha de forma fluida, garantindo que o deck não forme "buracos".
4.  Posiciona cada carta em sua coordenada `(x, y)` calculada.

### Parâmetros (Inspector)
O script expõe várias propriedades públicas para que você possa ajustar o layout diretamente no Unity Editor:
*   **`maxCardsPerRow`**: O número máximo de cartas antes de quebrar a linha (ex: 15).
*   **`cardWidth` / `cardHeight`**: As dimensões exatas do seu prefab de carta.
*   **`verticalSpacing`**: O espaço vertical entre as linhas.
*   **`maxHorizontalSpacing`**: O espaço limite entre as cartas quando a linha tem poucas cartas (evita que duas cartas fiquem nas pontas extremas da tela).
*   **`minHorizontalSpacing`**: O espaçamento horizontal mínimo, mesmo quando as cartas estão sobrepostas. Use um valor negativo grande (ex: -80) para permitir bastante sobreposição.
*   **`horizontalPadding` / `verticalPadding`**: O preenchimento interno nas bordas do painel `Content`.

## Estrutura do Prefab do Baú (Card_PrefabChestList)
Este prefab representa um único item na lista rolável do baú. Sua estrutura é projetada para exibir um resumo completo da carta.

*   **Card_PrefabChestList** `[Image, LayoutElement, DeckDragHandler, ChestCardItem]` - Objeto raiz do prefab.
    *   **Card2D** `[Image, CardImageDragProxy]` - A imagem da carta em si. Deve ter `Raycast Target` ativo para o drag and drop funcionar.
        *   **Art** `[RawImage]`
    *   **QuantCard** `[TextMeshProUGUI]` - Exibe a quantidade disponível para uso (ex: "x3").
    *   *(Outros componentes de texto e ícones devem ter `Raycast Target` desativado para não interferir com o drag and drop da carta)*
    *   **CardStatsText** `[TextMeshProUGUI]` - Exibe ATK/DEF.
    *   **CardNameText** `[TextMeshProUGUI]` - Exibe o nome da carta.
    *   **MonsterLvl** `[TextMeshProUGUI]` - Exibe o nível do monstro.
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