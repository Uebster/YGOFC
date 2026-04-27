# O Motor Principal e Fluxo de Turno (Engine & Game Loop)

Este documento centraliza a arquitetura principal do motor do jogo, descrevendo as responsabilidades de todos os Gerenciadores (Managers), as funções vitais e flags do `GameManager`, a máquina de estados (Phases) e o sistema assíncrono de interrupções de UI.

---

## 3.1 Visão Geral dos Gerenciadores (Managers)

O jogo utiliza uma arquitetura de múltiplos Managers (Singletons) para separar responsabilidades. O `GameManager` atua como o orquestrador central, com o `CardEffectManager` e `PhaseManager` como seus principais assistentes.

### 1. GameManager
**Responsabilidade:** Orquestrador central do duelo. Gerencia o estado do jogo (LP, turnos), as listas de cartas (mão, cemitério, etc.), e inicia as principais ações de jogo como invocações e ativação de cartas, servindo como a principal interface para a UI. Valida regras de alto nível antes de delegar a lógica de efeitos.
*   **Funções Principais:** `StartDuel`, `EndDuel`, `SwitchTurn`, `TrySummonMonster`, `PlaySpellTrap`.

### 2. PhaseManager
**Responsabilidade:** Controlar o fluxo de tempo e as fases do turno.
*   **Fases:** Draw -> Standby -> Main 1 -> Battle -> Main 2 -> End.
*   **Funções:** `ChangePhase(GamePhase)`, `StartTurn()`.
*   Atua como uma máquina de estados, chamando `hooks` no `GameManager` (`OnDrawPhaseStart`, etc.) quando as fases mudam.

### 3. CardEffectManager
**Responsabilidade:** Hub central para execução de lógica de cartas (via scripts Lua), gerenciamento de correntes (Chains) e escuta de eventos globais.
*   **Estrutura:** Usa a engine **MoonSharp** para interpretar scripts Lua associados a cada carta.
*   **Hooks:** `OnSummon`, `OnAttack`, `OnCardSentToGraveyard`, `OnPhaseStart`, etc. É notificado pelo `GameManager` e `PhaseManager` sobre os eventos do jogo.
*   **Correntes (Chains):** Gerencia a pilha de efeitos LIFO (Last-In, First-Out) quando múltiplos efeitos são ativados em resposta.

### 4. DuelFXManager
**Responsabilidade:** Feedback visual e sonoro.
*   **Funções:** Toca sons (SFX) e instancia partículas (VFX) para ações como Invocação, Ataque, Dano, etc. Também gerencia a música de fundo dinâmica.

*(Nota: Gerenciadores como `SummonManager`, `BattleManager` e `ChainManager` foram consolidados no `GameManager` e `CardEffectManager` para centralizar o fluxo de controle).*

---

## 3.2 O Grande Cérebro Dividido (A Arquitetura Modular do `GameManager`)

O `GameManager` é um Singleton (`GameManager.Instance`) acessível globalmente. Devido à imensa quantidade de responsabilidades, ele foi refatorado utilizando o padrão `partial class` do C#. Isso significa que, para a engine da Unity e os scripts Lua, ele age como um único objeto monolítico, mas o seu código-fonte é organizado cirurgicamente em 7 arquivos diferentes para facilitar a manutenção e escalabilidade.

### 3.2.1 `GameManager.cs` (O Core, Configurações e Variáveis)
É o arquivo base e hub de memória. Abriga **exclusivamente** as variáveis globais, enumeradores (`GamePhase`, `StatDisplayMode`), referências de Inspector para UI, e os métodos nativos da Unity (`Awake`, `Start`, `Update`, `OnValidate`, `OnDestroy`).
*   **Hooks de Inicialização:** `EnsureCoreManagers()`, `CreateManager()`, `GetUIParent()`, `SetPlayerProfile()`.
*   **Modos de Jogo e Debug (Flags):** `devMode`, `fullTestMode`, `testDuelDirectly`, `infiniteLP`, `disableBanlist`, `unlockAllCards`, `alwaysCoinHead`.
*   **UX e Configurações de UI:** `useMouseTooltipUI`, `quickSummonFromHand`, `confirmAttackTarget`, `useDirectHandSelection`, `playerDrawSpeed`.

### 3.2.2 `GameManager_Phases.cs` (Orquestração de Fases, Turnos e Fim de Jogo)
Gerencia o fluxo de tempo e a transição do duelo do começo ao fim. É o parceiro direto do `PhaseManager`.
*   **Início e Fim:** 
    *   `StartDuel()`, `StartDuel(opponent, duelIndex)`: Ponto de entrada, limpa o campo e delega a criação dos decks.
    *   `DuelStartSequence()`: Rotina de abertura cinematográfica (compra as 5 cartas iniciais após o shuffle).
    *   `EndDuel(playerWon, isDeckOut)`: Mostra a mensagem de vitória, escurece a tela e processa os drops.
    *   `CalculateDrop(rank, opponent)`: Lógica percentual baseada no rank e nas pools (S+ a D).
    *   `CheckExodiaWin()`: Condição de vitória instantânea varrendo a mão do jogador.
*   **Ciclo de Turnos:** `SwitchTurn()`, `TurnTransitionRoutine()` (pausa a engine para o texto "YOUR TURN").
*   **Gatilhos de Fase (Hooks C#):** `OnDrawPhaseStart()`, `OnStandbyPhaseStart()`, `OnMainPhase1Start()`, `OnEndPhaseStart()`.
*   **Limites e UI Dinâmica:** `HandleHandLimitSequence()` (regra das 6 cartas) e `AnnounceText(string text)` (cria o banner de fase voador no HUD).

### 3.2.3 `GameManager_BoardActions.cs` (Movimentação Física e Magias/Armadilhas)
Lida puramente com a manipulação espacial dos GameObjects, zonas e vínculos no tabuleiro.
*   **Movimento Base:** 
    *   `MoveCard(card, destination, reason)`: O método universal e seguro para mover uma carta entre zonas (Graveyard, Hand, Deck, Banished, ExtraDeck).
    *   `SendToGraveyard(card, isPlayer, from, reason)`: Insere logicamente na pilha de cemitério.
    *   `BanishCard(card)`, `RemoveFromPlay(card, isPlayer)`: Exilação do jogo.
    *   `ReturnToHand(card)`, `AnimateCardToHand(...)`: Retorno e Bounce com animação ou fantasma.
*   **Spells/Traps:** `PlaySpellTrap(cardGO, data, isSet)`, `ActivateFieldSpellTrap(cardGO)`, `SetSpellTrapFromData(...)`.
*   **Interações e Vínculos:** `EquipMonsterToMonster(equip, target)`, `SwitchControl(card)`, `CreateCardLink(source, target, type)`.
*   **Helpers:** `ClearFieldZonesOnly()`, `GetFieldCardCount()`, `GetMonsterCount()`, `FindCardOnField()`, `IsCardActiveOnField(cardId)`.

### 3.2.4 `GameManager_Decks.cs` (Pilhas, Setup, Saque e Visualizadores)
Focado estritamente na mecânica de baralhos, compra de cartas, embaralhamento e exibição visual de pilhas.
*   **Setup e Gestão:** `CleanupDuelState()` (Limpeza hard de tela/listas), `InitializePlayerDeck()`, `InitializeOpponentDeck()`.
*   **Compras (Draw) e Deck:** `DrawCard(ignoreLimit)`, `DrawOpponentCard()`, `DrawInitialHandRoutine(count)`, `MillCards(...)`.
*   **Embaralhamento:** `ShuffleDeck(isPlayer)`, `ShuffleHand(isPlayer)` e a corrotina `HandShuffleRoutine(...)`.
*   **Visuais e Modais (Viewers):** `ViewGraveyard(isPlayer)`, `ViewExtraDeck(isPlayer)`, `ViewDeck(isPlayer)`, `ViewRemovedCards(isPlayer)`, `UpdatePileVisuals()`.

### 3.2.5 `GameManager_Stats.cs` (Life Points, Status Visuais e Dev Tools)
Isola os cálculos matemáticos de pontos de vida, os recálculos visuais de status sobre as cartas renderizadas e ferramentas de depuração em tempo real.
*   **Life Points (LP):** `DamagePlayer(amount)`, `DamageOpponent(amount)`, `UpdateLPUI()`.
*   **Renderização Dinâmica (Card Viewer):** `UpdateCardViewer(hoveredCard, isFaceUp)`, `ClearCardViewer()`.
*   **Motor de Status (`RefreshAllCardsVisuals`):** Varre a mesa inteira injetando os efeitos contínuos (`EFFECT_TYPE_SINGLE`) processados pelo Lua direto nos textos da UI.
*   **Buscas Espaciais:** `GetFreeMonsterZone(isPlayer)`, `GetFreeSpellZone(isPlayer)`.
*   **Ferramentas Dev/QA:** `Dev_InjectDependencies(CardData)` (Lê aspas no texto da carta e injeta dependências no Deck em runtime para não dar soft-lock em testes), `Dev_NextOpponent()`, `ToggleOpponentHandVisibility()`.

### 3.2.6 `GameManager_Summons.cs` (O Motor de Invocações)
Arquivo dedicado única e exclusivamente à complexa mecânica de trazer monstros para o jogo e suas regras atreladas.
*   **Invocação Normal/Tributo:** `TrySummonMonster(cardGO, cardData, isSet, ignoreLimit)` e `PerformTributeSummon(...)`.
*   **Invocação Especial:** `PerformSpecialSummon(cardGO, cardData)` (abre escolha Atk/Def), `SpecialSummonFromData(...)` (puxa cartas direto do JSON, usado no cemitério), `DefaultSpecialSummon(...)`.
*   **A Ponte Final:** `FinalizeSummon(...)` (a função crítica que ancora a carta na mesa, calcula rotações de Atk/Def e dispara as cinemáticas do `DuelFXManager`).
*   **Gatilhos de LUA:** `OnSummon(card)`, `OnFlipSummon(card)`.
*   **Mecânicas Dedicadas:**
    *   **Fusão:** `BeginFusionSummon(sourceCard)`, `SelectMaterialsForFusion(...)`.
    *   **Ritual:** `BeginRitualSummon(sourceCard)`, `SelectTributesForRitual(...)`, `PerformRitualSummon(...)`.
    *   **Fichas:** `SpawnToken(forPlayer, atk, def, name, level, race, attribute)`.

### 3.2.7 `GameManager_Selections.cs` (UI Tátil, Miras, Espadas e Minigames)
Acomoda tudo o que exige Input interativo humano no tabuleiro para satisfazer requisições e "Yields" do LUA.
*   **Miras e Combate:** 
    *   `HandleAttackIndicatorHover(card, isHovering)` e `RefreshAttackIndicators()` (Mostram os ícones de cruzamento de espadas sobre cartas aptas).
    *   `CancelAttackTargeting()` (Aborta o alvo e destrói as espadas em voo).
*   **Seleção Tátil Inteligente:** O clique direto no campo/mão usa a lógica *Power-Set* (Matemática Discreta). O método `HandleHandCardClick(card)` calcula todos os subconjuntos de uma seleção para preencher os Rituais (ex: achar a soma 8 cravada descartando o excesso). Permite encerramento antecipado com o Botão Direito (`Right-Click to Finish`) em seleções abertas ("Escolha 1 ou 2 monstros").
*   **Seleção Tradicional (Modal UI):** `OpenCardMultiSelection(...)`, `OpenCardSelection(...)`.
*   **Janela de Corrente:** `StartResponseSelection(...)`, `HandleResponseSelection(card)`, `CancelResponseSelection()`, `IsResponseCandidate(card)`.
*   **Minigames Matemáticos:** `TossCoin(numberOfCoins, onResult)`, `CoinTossRoutine(...)`, `RollDice(count, requireChoice, callback)`.

---

## 3.3 O Sistema de Fases e Turnos (`PhaseManager.cs`)

O controle de tempo do duelo é gerenciado pelo `PhaseManager.cs`, que atua como uma máquina de estados. O `GameManager` escuta as mudanças de fase para disparar hooks do `CardEffectManager`.

### 3.3.1 O Ciclo de Fases (`GamePhase` Enum)
1.  **Draw Phase:** O `PhaseManager` chama `GameManager.OnDrawPhaseStart()`. As flags restritivas de turno (`hasDrawnThisTurn`, `hasPerformedNormalSummon`) são limpas. 
    *   **Modo Automático (`canPlayerDrawFromDeck = false`):** O jogo saca 1 carta automaticamente e avança para a Standby Phase.
    *   **Modo Manual (`canPlayerDrawFromDeck = true`):** O jogo **pausa** na Draw Phase. O jogador deve clicar fisicamente no Deck para sacar e só então avança de fase.
2.  **Standby Phase:** Fase de manutenção. O `PhaseManager` lê o `displayDuration` das configurações e assegura que a fase demore o tempo necessário para o jogador ler o texto na tela antes de pular para a Main Phase 1. Útil para pagar custos ou disparar efeitos retroativos.
3.  **Main Phase 1:** Ações principais permitidas: Normal Summon / Set (1x), Special Summons (Ilimitados), Ativar/Setar Mágicas e Armadilhas, Mudar posições de batalha (1x por monstro, caso não tenha entrado neste turno).
4.  **Battle Phase:** Acessível apenas se for o turno do jogador (e após o Turno 1). Subdividida em Start Step, Battle Step, Damage Step e End Step. Totalmente orquestrada pelo `BattleManager`.
5.  **Main Phase 2:** Ações idênticas à MP1 para se preparar para o turno inimigo. Se o jogador entrou na Battle Phase, ele **deve** passar pela MP2 antes de encerrar.
6.  **End Phase:** Dispara a limpeza de efeitos temporários (ex: "until the end of this turn"). O controle é passado para o oponente (`SwitchTurn`). Verifica o limite de mão (6 cartas); se exceder, invoca `HandleHandLimitSequence`.

### 3.3.2 UI de Fases
*   A barra superior contém botões clicáveis para avançar fases (ex: Pular Battle Phase).
*   **Neon Effect:** A fase ativa recebe um brilho intenso colorido para leitura instantânea do estado.

---

## 3.4 O Sistema Assíncrono e Modais (Interrupções e Escolhas)

Como as lógicas rodam na Thread principal da Unity, a engine **nunca** utiliza funções do tipo `Thread.Sleep()`. Toda tomada de decisão requer um padrão estrito de **Corrotinas e Callbacks** para pausar a lógica sem travar a renderização da Engine.

### 3.4.1 A Regra de Ouro (O Padrão `While-Yield`)
Sempre que uma carta em `CardEffectManager_Impl` precisar de input do jogador no meio de uma resolução, ela **deve** congelar a si mesma:
```csharp
bool isWaiting = true;
UIManager.Instance.ShowConfirmation("Deseja ativar o efeito?", 
    () => { /* Ação Sim */ isWaiting = false; }, 
    () => { /* Ação Não */ isWaiting = false; }
);

// Pausa a execução de fundo (Corrotina) até o callback disparar
while(isWaiting) yield return null;
```

### Tipos de Modais Oficiais
*   **Confirmação Simples:** `UIManager.Instance.ShowConfirmation(msg, onYes, onNo)`
*   **Miras Físicas (Targeting):** `SpellTrapManager.Instance.StartTargetSelection(filter, callback)` - Exige clique no tabuleiro.
*   **Buscas em Lista (Hand/Deck/GY):** `GameManager.Instance.OpenCardMultiSelection(lista, min, max, callback)`. Se for na Mão e `min == max`, ativa o clique direto nos objetos 3D da mão.
*   **Busca Global (Digitação):** `GlobalCardSearchUI.Instance.Show(msg, callback)` - Filtra o banco de 2147 cartas para efeitos de "Declare 1 nome".
*   **Decisões em Texto:** `MultipleChoiceUI.Instance.Show(optionsList, title, min, max, callback)` - Ex: Declarar Tipos, Atributos.
*   **Teclado Numérico (LP/Nível):** `NumericSelectionUI.Instance.Show(..., callback)` - Ex: Custo do *Wall of Revealing Light*.
*   **Reordenação de Deck:** `ReorderCardsUI.Instance.Show(...)` - Abre sistema de Drag & Drop para o Topo do Deck.

### O Bypass de Simulação
Se `GameManager.Instance.isSimulating == true` (Chaos Simulator rodando), todos esses modais são ignorados/bypassed. A engine automaticamente escolhe a 1ª opção válida (ou usa `Random`) e invoca o callback instantaneamente para permitir que o jogo rode em `50x` speed.