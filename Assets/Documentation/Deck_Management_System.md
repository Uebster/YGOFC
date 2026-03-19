# 8. Construção e Gestão de Baralhos (Deck Management System)

## 8.1 Sistema de Construção de Decks

### Visão Geral
O **Deck Builder** é a interface onde o jogador gerencia sua coleção de cartas do Baú (`Trunk`) e constrói os baralhos que usará nos duelos. O sistema permite filtrar, ordenar, pesquisar, importar/exportar e validar a legalidade dos decks em tempo real.

### Baú de Cartas (Virtual Scrolling)
Para lidar com a grande quantidade de cartas do jogo (2147+), a lista do baú (`Chest`) utiliza um sistema de **Virtual Scrolling**. A lógica foi terceirizada para scripts dedicados, mantendo o `DeckBuilderManager` focado apenas em gerenciar os dados.

#### Objetivo
O objetivo principal é garantir alta performance e uma experiência de usuário fluida, mesmo ao exibir milhares de cartas. Em vez de instanciar um objeto de UI para cada carta de uma vez (o que travaria o jogo), o sistema cria e gerencia apenas os objetos que estão visíveis na tela, reciclando-os conforme o jogador rola a lista.

#### Arquitetura e Scripts Envolvidos
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

#### Fluxo de Execução
1.  O jogador aplica um filtro ou busca no `DeckBuilderManager`.
2.  `DeckBuilderManager` processa a lista de 2147+ cartas e gera uma lista final `filteredCardGroups`.
3.  `DeckBuilderManager` chama `trunkScrollManager.Initialize(filteredCardGroups)`.
4.  `TrunkScrollManager` calcula a altura total do conteúdo para que a barra de rolagem funcione corretamente e cria seu pool de itens de UI.
5.  O jogador move a barra de rolagem.
6.  O método `OnScroll` do `TrunkScrollManager` é acionado.
7.  Ele calcula o primeiro item visível e, em um loop, posiciona os itens do pool e chama `item.UpdateContent(cardData)` para cada um.
8.  O `TrunkCardScrollItem` recebe os dados, consulta o `DeckBuilderManager` para obter contagens e chama o `Setup()` do `ChestCardItem` para exibir a carta na tela.

#### Cuidados Especiais (Troubleshooting)
**1. Cartas Renderizando Fora da Tela:**
Para que a matemática de `xPos` e `yPos` do `TrunkScrollManager` funcione perfeitamente, as âncoras e o pivô do Content e dos itens (prefabs) **precisam** estar no canto superior esquerdo `(0, 1)`. O script agora força essa configuração via código no método `CreatePool` para evitar problemas de montagem na Unity.

**2. Bug de Imagem Piscando/Trocada (Flickering):**
Quando o jogador rola a lista muito rápido, as caixas de UI (prefabs) são recicladas e reaproveitadas antes mesmo da imagem antiga terminar de baixar. Para evitar que a imagem de uma carta "A" apareça na caixa quando ela já virou a carta "B":
*   O `ChestCardItem.Setup` agora usa `StopAllCoroutines()` no momento em que é reaproveitado.
*   A requisição atual (`UnityWebRequest`) é armazenada em uma variável e cancelada (`.Dispose()`) imediatamente caso seja reciclada ou a janela seja fechada (`OnDisable()`).
*   Isso previne *Memory Leaks* e sobreposição de imagens.

**3. Tamanho Dinâmico da Janela (Altura = 0):**
Se o painel acabou de ser ativado, a Unity ainda não tem as dimensões finais do `ScrollRect.viewport`. O script utiliza `Canvas.ForceUpdateCanvases()` para forçar a Unity a calcular o layout no mesmo frame, garantindo que o pool de itens não seja instanciado com o número errado de cartas (ex: 2 cartas em vez de 20).

### Estrutura de UI (Hierarquia)
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
                *   **CardDescriptionText** `[TextMeshProUGUI, Scrollbar]`
            *   **CardNameText** `[TextMeshProUGUI]`
            *   **CardInfoText** `[TextMeshProUGUI]`
            *   **CardStatsText** `[TextMeshProUGUI]`
    *   **Btn_SaveDeck** `[Image, Button]`
    *   **Btn_BackToMenu** `[Image, Button]`
    *   **Btn_Import** `[Image, Button]`
    *   **Btn_Export** `[Image, Button]`
    *   **Panel_Chest** `[Image]`
        *   **Panel_SearchCardInput** `[Image]`
            *   **Input_SearchCard** `[Image, TMP_InputField]`
        *   **Panel_Filters** `[Image, GridLayoutGroup]`
            *   *(Botões de Filtro: Normal, Effect, Spell, Trap, etc.)*
        *   **Panel_CardChest** `[Image]`
            *   **Scroll View** `[Image, ScrollRect, TrunkScrollManager]`
                *   **Viewport** `[Image, Mask]`
                *   **Content** `[RectTransform, DeckDropZone]`
    *   **Panel_Deck** `[Image]`
        *   **Panel_MainDeck** `[Image]`
            *   **Scroll View** `[Image]`
                *   **Viewport** `[Image, Mask]`
                *   **Content** `[CustomDeckLayout, DeckDropZone]`
            *   **NumberOfCardsType** `[Image]` *(Contadores de tipos de carta)*
        *   **Panel_SideDeck** `[Image]`
            *   **Scroll View** `[Image]`
                *   **Content** `[CustomDeckLayout, DeckDropZone]`
        *   **Panel_ExtraDeck** `[Image]`
            *   **Scroll View** `[Image]`
                *   **Content** `[CustomDeckLayout, DeckDropZone]`

### Funcionalidades

#### 1. Filtragem e Ordenação
O `DeckBuilderManager` gerencia uma lista de cartas do baú (`currentTrunk`) e a re-exibe conforme os filtros e ordenações são aplicados.
*   **Filtros de Tipo:** Botões como *Normal, Effect, Spell, Trap, Ritual, Fusion* são mutuamente exclusivos. Apenas um filtro de tipo pode estar ativo por vez.
*   **Ordenação (Sort):** ABC, ATK, DEF. Clicar novamente inverte a ordem.
*   **Pesquisa por Texto:** O campo `Input_SearchCard` filtra a lista de cartas em tempo real (com um pequeno delay para performance).

#### Contagem Detalhada de Cartas
A UI fornece uma contagem detalhada dos tipos de carta em cada deck (Principal, Lateral e Extra).
*   Main e Side: Exibe contagens separadas para monstros `Normal`, `Effect`, `Ritual`, além de `Spell` e `Trap`.
*   Extra: Exibe a contagem de monstros `Fusion`.

#### 2. Sistema de Ícones (4 Campos)
Para organizar a exibição dos ícones de forma clara, o `DeckBuilderManager` mapeia o atributo, raça, tipo principal e propriedade das cartas e ativa/desativa os respectivos componentes `Image` no prefab `Card_PrefabChestList`.

#### 3. Drag and Drop (Arrastar e Soltar)
A lógica é dividida entre `DeckDragHandler` (na carta) e `DeckDropZone` (nas áreas de deck).
*   Cartas podem ser arrastadas do Baú (Trunk) para qualquer zona de deck ou entre zonas.
*   **Validação de Zona:** Impede, por exemplo, colocar Monstros de Fusão no Main Deck.
*   **Feedback Visual:** Se uma jogada for inválida, a área do deck alvo piscará em vermelho.

#### 4. Regras de Construção (Limites)
A validação é feita em tempo real e verificada novamente ao salvar.
*   **Main Deck:** Mínimo 40, Máximo 60 cartas.
*   **Side Deck:** Máximo 15 cartas.
*   **Extra Deck:** Máximo 15 cartas.
*   **Cópias:** Máximo de 3 cópias da mesma carta.
*   **Ban List:** Suporte para limitar cartas a 0 (Forbidden), 1 (Limited) ou 2 (Semi-Limited). Mensagens detalhadas orientam o jogador a consertar o erro.

#### 5. Persistência
*   **Save Deck:** O botão chama `SaveDeck()`, valida o deck e atualiza o deck ativo no `GameManager`.
*   **Sair (Back):** Chama `ExitDeckBuilder()`. Se houver alterações não salvas (`hasUnsavedChanges = true`), exibe um pop-up de confirmação.

#### 6. Indicador de Cartas "New"
*   O `DeckBuilderManager` verifica cada carta com `SaveLoadSystem.Instance.IsCardNew(card.id)`.
*   Se `true`, instancia a tag visual "NEW".
*   Ao adicionar uma carta a qualquer deck, o sistema marca a carta como usada no save, removendo a tag em aberturas futuras.

### Layout Customizado do Deck (`CustomDeckLayout.cs`)
O comportamento de organização das cartas no Main Deck é executado pelo script `CustomDeckLayout.cs`.
*   A cada atualização (`RefreshLayout`), ele executa um sistema de **Flow Layout** (Fluxo Contínuo), enchendo a primeira linha da esquerda para a direita.
*   Se a linha não couber todas as cartas, ele aplica o espaçamento negativo (sobreposição) até que atinja o limite de `maxCardsPerRow` (Ex: 15 cartas). A 16ª carta inicia uma nova linha.
*   **Invariância de Texto:** Filtros sempre utilizam conversão para minúsculas (`ToLowerInvariant()`).
*   **Otimização:** O redesenho da tela reaproveita os GameObjects existentes e apenas atualiza a arte através do `SetCard()` caso o ID mude, evitando lag.

### Estrutura do Prefab do Baú (`Card_PrefabChestList`)
*   **Card_PrefabChestList** `[DeckDragHandler, ChestCardItem]`
    *   **Card2D** `[Image, CardImageDragProxy]` (Raycast Target Ativo)
    *   **QuantCard** `[TextMeshProUGUI]`
    *   **CardStatsText** `[TextMeshProUGUI]`
    *   **CardNameText** `[TextMeshProUGUI]`
    *   **MonsterLvl** `[TextMeshProUGUI]`
    *   **AttributeIcon** / **RaceIcon** / **TypeIcon** / **SubTypeIcon** `[Image]`

### Banlist (Lista de Restrições)
A lista reflete um formato clássico (baseado em 2005/Goat Format). Estas restrições são aplicadas a menos que `GameManager.disableBanlist` esteja ativo.

#### 🚫 Proibidas (0 Cópias)
*   Chaos Emperor Dragon - Envoy of the End
*   Sangan, Witch of the Black Forest, Yata-Garasu, Cyber Jar, Fiber Jar, Magical Scientist, Cyber-Stein
*   Dark Hole, Raigeki, Harpie's Feather Duster, Monster Reborn, Delinquent Duo, Graceful Charity, Change of Heart, Confiscation, The Forceful Sentry, United We Stand
*   Imperial Order, Mirror Force, Mirror Wall, Destruction Ring

#### ⚠️ Limitadas (1 Cópia)
*   **Monstros:** BLS - Envoy, BLS (Ritual), Chaos Sorcerer, Breaker, Jinzo, Tribe-Infecting Virus, Sinister Serpent, Exiled Force, D.D. Assailant, D.D. Warrior Lady, Ancient Gear Beast, Armed Dragon LV5/LV7, Behemoth, Chaos Command Magician, Injection Fairy Lily, Reflect Bounder, Twin-Headed Behemoth, Vampire Lord, Morphing Jar, DMoC, Relinquished, Summoner Monk, Rescue Cat, Exodia the Forbidden One (e partes).
*   **Magias:** Pot of Greed, Heavy Storm, Snatch Steal, Premature Burial, Swords of Revealing Light, Nobleman of Crossout, Book of Moon, Mystical Space Typhoon, Giant Trunade, Mage Power, Painful Choice, Mind Control, Brain Control, Limiter Removal, Megamorph, Card Destruction, Dimension Fusion, Primal Seed.
*   **Armadilhas:** Call of the Haunted, Ring of Destruction, Torrential Tribute, Magic Cylinder, Ceasefire, Reckless Greed, Royal Decree, Mask of Darkness, Time Seal, Wall of Revealing Light, Self-Destruct Button, Return from the Different Dimension, Protector of the Sanctuary.

#### ⚠️⚠️ Semi-Limitadas (2 Cópias)
*   Creature Swap, Last Turn, Manticore of Darkness, Marauding Captain, Morphing Jar #2, Nobleman of Crossout, Reinforcement of the Army, Upstart Goblin, Cyber Dragon, A Feint Plan, Enemy Controller, Messenger of Peace, Level Limit - Area B, Gravity Bind, Miracle Dig, Good Goblin Housekeeping, Needle Worm, Apprentice Magician, Magician of Faith, Deck Devastation Virus, Second Coin Toss, Reasoning.

---

## 8.2 Sistema de Importação e Exportação

### Visão Geral
Este sistema permite ao jogador salvar ("exportar") e carregar ("importar") receitas de decks. Diferente de um sistema baseado em arquivos `.ydk` externos, este mecanismo salva as listas de decks diretamente no arquivo de save do jogador (`.save`), garantindo que os decks estejam vinculados ao perfil e progresso do jogador.

### Estrutura da UI
O sistema utiliza um painel principal (`Panel_ImportExport`) que é ativado a partir do Deck Builder. Sua aparência e funcionalidade são alteradas dinamicamente pelo `DeckImportExportManager`.
*   **Scroll View:** Exibe uma lista dos decks já salvos.
*   **Botão de Ação Principal:** Muda de "Import" para "Export" dependendo do modo.
*   **Campo de Nome (`Input_DeckName`):** Visível apenas no modo Exportação. Clicar em um deck existente preenche o campo.
*   **Botão de Deletar:** Permite apagar um deck selecionado da lista.

#### Hierarquia de UI (Panel_Export / Panel_Import)
*   **Panel_Export** `[Image, DeckImportExportManager]`
    *   **Scroll View** `[ScrollRect]` -> **Viewport** -> **Content** `[VerticalLayoutGroup]`
    *   **InputDeckName** `[TMP_InputField]`
    *   **Btn_ExportDeck** `[Button]`
    *   **Btn_DeleteDeck** `[Button]`
    *   **ConfirmationExport** / **WarningExport** `[Image]`

### Arquitetura e Scripts
1.  **`DeckImportExportManager.cs`:** O cérebro do painel. Configura o layout como `Import` ou `Export` e popula o ScrollView com a lista de receitas salvas.
2.  **`SaveLoadSystem.cs`:** A classe principal `GameSaveData` armazena uma `List<DeckRecipe> savedDecks`. O sistema fornece métodos como `SaveDeckRecipe`, `LoadDeckFromRecipe` e `DeleteDeckRecipe`. **Importante:** Após modificar uma receita, o `SaveGame()` deve ser chamado para persistir as alterações.
3.  **`DeckBuilderManager.cs`:** Atua como ponte lógica.
    *   `ExportCurrentDeck`: Coleta o Deck atual (Main, Side, Extra) e o envia ao Save System.
    *   `ImportDeck`: Carrega as IDs do Save System no construtor visual, limpando o deck anterior.
4.  **`DeckSlotUI.cs`:** O prefab de cada slot salvo na UI, responsável por exibir o nome e contagem de cartas, e enviar o callback de clique. No modo Export, inclui um slot "Create New Deck".

---

## 8.3 Sistema de Deck Inicial

### Visão Geral
O sistema de Deck Inicial (`InitialDeckBuilder.cs`) é responsável por gerar um baralho único e balanceado para o jogador no início de um novo jogo. Diferente de jogos que dão um deck fixo, este sistema cria uma experiência "Roguelike", onde cada save começa com ferramentas ligeiramente diferentes, mas com poder equivalente.

### Estrutura do Deck (40 Cartas)
O deck é composto por 5 "Pools" (Piscinas) de cartas, definidas para garantir consistência e evitar mãos injogáveis.

1.  **Monstros Fracos ("Fodder") - 15 Cartas:** Monstros Normais, Nível 1-4, ATK < 950. Servem como defesa inicial e tributos.
2.  **Monstros Médios ("Warriors") - 12 Cartas:** Monstros Normais, Nível 1-4, ATK entre 951 e 1300. A espinha dorsal ofensiva.
3.  **Monstros Fortes ("Ace") - 3 Cartas:** Monstros Normais ou de Efeito (Simples), Nível 1-4, ATK entre 1301 e 1600. As cartas mais valiosas do deck inicial.
4.  **Magias de Suporte - 5 Cartas:** Cartas de Magia de buff ou utilidade (Equipamento, Campo, Normal). Cartas OP como *Raigeki* e *Pot of Greed* são vetadas.
5.  **Armadilhas Básicas - 5 Cartas:** Cartas de Armadilha simples (exclusão de armadilhas absolutas como *Mirror Force*).

### Regras de Filtragem
Para garantir que o deck seja válido para o início da campanha (Ato 1), o sistema aplica filtros rígidos:
*   **Nível:** Máximo Nível 4 (sem monstros que exigem tributo logo de cara).
*   **Tipo:** Sem Monstros de Fusão, Ritual ou Tokens no Main Deck.
*   **Banlist Interna:** Uma lista de IDs (`forbiddenIds`) impede que cartas "quebradas" ou complexas demais apareçam acidentalmente.

### Fluxo de Execução
1.  O jogador inicia um "New Game".
2.  `GameManager` verifica se existe um save. Se não, chama `InitialDeckBuilder`.
3.  O Builder varre o `CardDatabase` e separa as cartas candidatas para cada Pool.
4.  Seleciona aleatoriamente a quantidade necessária de cada Pool.
5.  Embaralha a lista final e salva a coleção (Trunk) e a receita base no perfil do jogador.