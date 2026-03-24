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

## 3.2 Funções Vitais e Configurações (`GameManager.cs`)

O `GameManager` é um Singleton (`GameManager.Instance`) acessível globalmente. Ele orquestra o duelo, atuando como o "mestre de cerimônias" que detém o estado do jogo e serve como a API principal para a UI e outros gerenciadores.

### 3.2.1 Orquestração do Duelo e Gerenciamento de Estado
*   **Controle do Duelo:**
    *   `StartDuel()`: Ponto de entrada para iniciar um duelo. Limpa o estado anterior, inicializa decks, LPs e chama a corrotina `DuelStartSequence` para a animação inicial.
    *   `StartDuel(opponent, duelIndex)`: Sobrecarga para iniciar um duelo de campanha contra um oponente específico.
    *   `EndDuel(bool playerWon, isDeckOut)`: Finaliza o duelo, chama o `DuelScoreManager` e exibe a tela de recompensas.
    *   `SwitchTurn()`: Realiza a troca de turno, limpa contadores (`lpPaidThisTurn`), atualiza a UI de fase e invoca a IA se for o turno do oponente.
    *   `CleanupDuelState()`: Método de limpeza pesada que destrói todos os GameObjects de cartas, limpa todas as listas de dados (mão, campo, GY, etc.) e reseta a UI.
*   **Gerenciamento de Vida (LP):**
    *   `DamagePlayer(int amount)` / `DamageOpponent(int amount)`: Reduz LP, exibe pop-up de dano, notifica o `CardEffectManager` e verifica condição de derrota.
    *   `PayLifePoints(isPlayer, amount)`: Tenta pagar um custo em LP. Retorna `false` se os LPs forem insuficientes.
    *   `GainLifePoints(isPlayer, amount)`: Aumenta os LPs, exibe pop-up e notifica o `CardEffectManager`.
*   **Eventos de Fase (Hooks):**
    *   `OnDrawPhaseStart()`: Chamado pelo `PhaseManager`. Reseta contadores de turno (Normal Summon, ataques) e inicia o saque.
    *   `OnStandbyPhaseStart()`: Chamado pelo `PhaseManager`. Dispara a verificação de custos de manutenção no `CardEffectManager`.
    *   `OnEndPhaseStart()`: Chamado pelo `PhaseManager`. Inicia a corrotina `HandleHandLimitSequence` para verificar o limite de mão.
*   **Consultas de Estado:**
    *   `IsCardActiveOnField(cardId)`: Verifica se uma carta com um ID específico está com a face para cima no campo (para efeitos contínuos como *Jinzo*).
    *   `GetFieldCardCount(isPlayer)`, `GetMonsterCount(isPlayer)`, `GetFreeMonsterZones(isPlayer)`, `GetFreeSpellTrapZones(isPlayer)`: Helpers para a IA e efeitos de cartas consultarem o estado do tabuleiro.
    *   `GetPlayerHandData()`, `GetPlayerGraveyard()`, etc.: Métodos que retornam as listas de `CardData` das zonas correspondentes.

### 3.2.2 API de Ações de Jogo (A Caixa de Ferramentas)
Esta é a API principal que a UI e o `CardEffectManager` usam para executar ações no jogo.

*   **Movimentação de Cartas (Core):**
    *   `MoveCard(card, destination, reason)`: **Método unificado** para mover uma carta entre zonas (Mão, Deck, GY, Banida). Centraliza a lógica de saída de campo.
    *   `DrawCard(ignoreLimit)` / `DrawOpponentCard()`: Delega a compra de cartas ao `DeckManager`.
    *   `SendToGraveyard(card, isPlayer, from, reason)`: Adiciona a carta à lista do cemitério e notifica o `CardEffectManager`.
    *   `BanishCard(card)`: Nova implementação para banir uma carta do jogo.
    *   `ReturnToHand(card)` / `ReturnToDeck(card, toTop)`: Efeitos de "Bounce" e "Spin".
    *   `EquipMonsterToMonster(equip, target)`: Transforma um monstro em equipamento e cria um `CardLink`.
*   **Ações de Jogo:**
    *   `TrySummonMonster(cardGO, data, isSet)`: Inicia a validação de invocação e, se aprovada, delega para `CardEffectManager` (Lua) ou `FinalizeSummon`.
    *   `FinalizeSummon(...)`: Etapa final da invocação, responsável por colocar a carta fisicamente na zona e aplicar os efeitos visuais.
    *   `PlaySpellTrap(cardGO, data, isSet)`: Inicia a validação para ativar ou baixar uma Magia/Armadilha.
    *   `ActivateFieldSpellTrap(cardGO)`: Ativa uma carta que já estava Setada no campo.
    *   `TributeCard(card)`: Envia uma carta ao cemitério como tributo (com VFX).
    *   `DiscardCard(card)` / `DiscardRandomHand(isPlayer, amount)` / `DiscardHand(isPlayer)`: Métodos para descarte.
*   **Invocações Especiais e Mecânicas:**
    *   `SpecialSummonFromData(...)`: Invoca um monstro diretamente a partir de seus dados (`CardData`), usado para reviver do cemitério ou invocar do deck.
    *   `BeginFusionSummon(source)` / `BeginRitualSummon(source)`: Abrem as UIs de Fusão/Ritual para seleção de materiais.
    *   `PerformRitualSummon(source, ritual, tributes)`: Executa a invocação ritual após a seleção.
    *   `SpawnToken(...)`: Cria um Token em uma zona de monstro livre.
    *   `SwitchControl(card)`: Troca o controle de um monstro para o oponente.
*   **Interação com UI e Minigames:**
    *   `OpenCardMultiSelection(...)`: Abre a UI para seleção de cartas de uma lista.
    *   `HandleHandCardClick(card)`: Gerencia a lógica de clique para o modo `useDirectHandSelection`.
    *   `TossCoin(...)` / `RollDice(...)`: Inicia os respectivos minigames.
    *   `ViewGraveyard(isPlayer)`, `ViewDeck(isPlayer)`, etc: Abrem os painéis de visualização das pilhas.
    *   `UpdateCardViewer(card, isFaceUp)` / `ClearCardViewer()`: Controla a janela de visualização principal.
    *   `ToggleOpponentHandVisibility()`: Ferramenta de debug para mostrar/esconder a mão do oponente.

### 3.2.3 Modos de Jogo, Debug e Ferramentas (Inspector Flags)
*   `devMode`: Habilita trapaças gerais (comprar a qualquer hora, controlar cartas do oponente).
*   `effectTestMode`: Habilita o menu de teste de VFX/Sons isolados.
*   **Hierarquia de Teste Direto (Pula o Menu Principal):** O sistema agora segue uma prioridade rígida (1 frame delay) para iniciar telas diretamente pela Unity:
    1.  `testDuelDirectly`: (Prioridade 1) Pula menus e inicia duelo livre imediatamente.
    2.  `testDeckBuilderDirectly`: (Prioridade 2) Inicia na tela de Deck.
    3.  `testLibraryDirectly`: (Prioridade 3) Inicia na página da Biblioteca de Cartas.
*   `testOpponentID` / `testPlayerID`: Substitui baralhos por perfis específicos para teste.
*   `unlockAllCards`: (Requer `devMode`) Se marcado, adiciona 3 cópias de TODAS as cartas ao Baú. As cartas virão marcadas como "Usadas" para evitar poluir a Biblioteca com a tag "New".
*   `disableDeckShuffle`: Impede o embaralhamento inicial. As cartas virão na ordem do JSON (Útil para "Stacking the Deck").
*   `forcePlayerGoingFirst`: Força o jogador a sempre ter o primeiro turno, ignorando a moeda.
*   `infiniteLP`: O jogador não toma dano (Modo Deus).
*   `infiniteNormalSummons`: Remove o limite de 1 Normal Summon por turno.
*   `disableTributeRequirements`: Permite invocar monstros Nível 5+ sem tributos.
*   `disableFusionCost` / `disableRitualCost`: Não consome materiais ou a carta mágica no processo.
*   `alwaysCoinHead`: Força o resultado das moedas a ser sempre Cara.
*   `enableHandLimit`: Ativa a regra de limite de mão (6 cartas) no End Phase.
*   `allowForbiddenCards` / `disableBanlist`: Flexibiliza ou ignora a lista de cartas proibidas.
*   `placeTributeSummonInTributeZone`: Monstros invocados ocupam a zona do primeiro sacrifício.
*   `ToggleFullscreen()`: Alterna modo janela/tela cheia (Atalho: F11 em DevMode).

### 3.2.4 Opções de Input, UX e Velocidade
*   `enableRightClickPhaseMenu`: Clicar com botão direito no campo vazio abre atalhos de fase.
*   `confirmBattlePositionChange` / `confirmAttackTarget`: Omissão de UI para gameplay ágil.
*   `useDirectHandSelection`: Permite selecionar cartas de tributo/descarte clicando fisicamente nelas na mão 3D.
*   `confirmHandSelection`: Se marcado, pede confirmação final após selecionar a carta 3D.
*   `useMouseTooltipUI`: Se ativado, usa um prefab de mouse com atalhos de Esquerdo/Direito em vez do Menu de Ação Clássico.
*   `quickSummonFromHand` / `quickSpellTrapFromHand`: Permite invocar/setar com um clique único, pulando o menu.
*   `quickAttackDirectly`: Se não houver monstros inimigos, clicar no atacante bate direto sem precisar clicar no Avatar.
*   **Velocidade (`playerDrawSpeed` / `opponentDrawSpeed`):** Tempo (em segundos) entre cada carta comprada na animação fluida inicial.

---

## 3.3 O Sistema de Fases e Turnos (`PhaseManager.cs`)

O controle de tempo do duelo é gerenciado pelo `PhaseManager.cs`, que atua como uma máquina de estados. O `GameManager` escuta as mudanças de fase para disparar hooks do `CardEffectManager`.

### 3.3.1 O Ciclo de Fases (`GamePhase` Enum)
1.  **Draw Phase:** O `PhaseManager` chama `GameManager.OnDrawPhaseStart()`. As flags restritivas de turno (`hasDrawnThisTurn`, `hasPerformedNormalSummon`) são limpas. 
    *   **Modo Automático (`canPlayerDrawFromDeck = false`):** O jogo saca 1 carta automaticamente e avança para a Standby Phase.
    *   **Modo Manual (`canPlayerDrawFromDeck = true`):** O jogo **pausa** na Draw Phase. O jogador deve clicar fisicamente no Deck para sacar e só então avança de fase.
2.  **Standby Phase:** Fase de manutenção. Atualmente automática, serve para pagar custos ou disparar efeitos retroativos através de `CardEffectManager.CheckMaintenanceCosts()`.
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