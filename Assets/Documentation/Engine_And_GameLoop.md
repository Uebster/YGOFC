# O Motor Principal e Fluxo de Turno (Engine & Game Loop)

Este documento centraliza a arquitetura principal do motor do jogo, descrevendo as responsabilidades de todos os Gerenciadores (Managers), as funções vitais e flags do `GameManager`, a máquina de estados (Phases) e o sistema assíncrono de interrupções de UI.

---

## 3.1 Visão Geral dos Gerenciadores (Managers)

O jogo utiliza uma arquitetura de múltiplos Managers (Singletons) para separar responsabilidades e facilitar a manutenção. O `GameManager` atua como o orquestrador central.

### 1. PhaseManager
**Responsabilidade:** Controlar o fluxo de tempo e fases do turno.
*   **Fases:** Draw -> Standby -> Main 1 -> Battle -> Main 2 -> End.
*   **Funções:**
    *   `ChangePhase(GamePhase)`: Avança para a próxima fase.
    *   `StartTurn()`: Reseta contadores do turno.
    *   Gerencia a UI da barra de fases (botões e brilho neon).

### 2. SummonManager
**Responsabilidade:** Validar e executar regras de invocação de monstros.
*   **Regras:** Limite de 1 Normal Summon por turno, Tributos necessários (Nível 5+).
*   **Funções:**
    *   `PerformSummon(...)`: Verifica se a invocação é válida.
    *   `GetRequiredTributes(level)`: Retorna 0, 1 ou 2.
    *   `SelectTributes(...)`: Inicia o fluxo de seleção manual de tributos com callback.
    *   Gerencia o fluxo de **Tributo Manual** (pausa o jogo para o jogador selecionar os monstros a sacrificar).

### 3. BattleManager
**Responsabilidade:** Gerenciar a Battle Phase, ataques e cálculo de dano.
*   **Funções:**
    *   `DeclareAttack(attacker)`: Inicia um ataque.
    *   `SelectTarget(target)`: Define o alvo e calcula o resultado.
    *   `ResolveDamage(...)`: Aplica a lógica de ATK vs ATK ou ATK vs DEF e destrói monstros.
    *   `IsTrapActivationBlocked(...)`: Verifica se armadilhas podem ser ativadas na Battle Phase (ex: *Mirage Dragon*).
    *   `TryChangePosition(card)`: Gerencia a mudança manual de posição (Ataque/Defesa) com limite de 1x por turno.

### 4. SpellTrapManager
**Responsabilidade:** Gerenciar regras de Magias e Armadilhas e respostas (Chains).
*   **Funções:**
    *   `CheckForTraps(...)`: Verifica se há armadilhas que podem ser ativadas em resposta a um ataque ou invocação.
    *   `CanActivateCard(...)`: Valida se uma carta pode ser usada (ex: Trap só no turno seguinte).
    *   Gerencia exceções como pular Draw Phase ou comprar cartas extras.

### 5. ChainManager
**Responsabilidade:** Gerenciar a pilha de efeitos (Corrente/Chain).
*   **Lógica:** LIFO (Last-In, First-Out). O último card ativado resolve primeiro.
*   **Funções:**
    *   `AddToChain(card)`: Adiciona um efeito à pilha.
    *   `ResolveChain()`: Executa os efeitos na ordem inversa e envia as cartas para o cemitério (se não forem contínuas).

### 6. SpellCounterManager
**Responsabilidade:** Gerenciar contadores de magia (Spell Counters) em cartas.
*   **Funções:**
    *   `AddCounter(card, amount)`: Adiciona contadores.
    *   `RemoveCounter(card, amount)`: Remove contadores.
    *   `GetCount(card)`: Retorna a quantidade atual.
    *   `RemoveCountersFromField(...)`: Remove contadores de qualquer lugar do campo (para custos).

### 7. CardEffectManager
**Responsabilidade:** Hub central para execução de lógica de cartas e escuta de eventos globais.
*   **Estrutura:** Dividido em classes parciais (`Impl`, `Registry`, `Part1`..`Part5`) para organização.
*   **Hooks:** `OnSummon`, `OnSet`, `OnBattlePositionChanged`, `OnCardSentToGraveyard`, `OnPhaseStart`, etc.
*   **Flags Globais:** Gerencia estados como `reverseStats` (Reverse Trap) e `banishInsteadOfGraveyard` (Macro Cosmos).

### 8. DuelFXManager
**Responsabilidade:** Feedback visual e sonoro.
*   **Funções:**
    *   Toca sons (SFX) e instancia partículas (VFX) para ações como Invocação, Ataque, Dano, Flip, etc.
    *   `UpdateBGM(playerLP, opponentLP)`: Altera a música de fundo dinamicamente baseada na vantagem de vida.

---

## 3.2 Funções Vitais e Configurações (`GameManager.cs`)

O `GameManager` é um Singleton (`GameManager.Instance`) acessível globalmente. Ele orquestra o duelo e abriga diversas configurações de inspeção (Inspector).

### 3.2.1 Controle de Duelo, Vida e Dados
*   **Duelo:**
    *   `StartDuel()`: Inicia um duelo (Free Duel ou Campanha). Limpa o tabuleiro, embaralha decks e compra mãos iniciais.
    *   `EndDuel(bool playerWon)`: Finaliza o duelo, calcula pontuação e rank.
    *   `CleanupDuelState()`: Reseta todas as listas e destrói objetos visuais.
*   **Vida (LP):**
    *   `DamagePlayer(int amount)` / `DamageOpponent(int amount)`: Reduz LP, atualiza UI e checa vitória/derrota.
    *   `PayLifePoints(isPlayer, amount)`: Tenta pagar LP. Retorna `false` se não tiver o suficiente.
    *   `GainLifePoints(isPlayer, amount)`: Aumenta os LP.
*   **Perfil e Dados:**
    *   `GetPlayerMainDeck()`: Retorna a lista atual do Deck.
    *   `PlayerHasCard(id)`: Verifica se o jogador possui uma carta no Trunk (Baú).
    *   `SetPlayerProfile(name, saveID)`: Atualiza dados do perfil.

### 3.2.2 Ações de Cartas e Tabuleiro
*   `DrawCard(bool ignoreLimit)`: Compra uma carta do Deck.
*   `DrawOpponentCard()`: Compra carta para IA.
*   `TrySummonMonster(cardGO, data, isSet)`: Inicia o fluxo de invocação.
*   `PerformSpecialSummon(cardGO, data)`: Abre o modal de escolha de posição (Atk/Def) e realiza Special Summon.
*   `PlaySpellTrap(cardGO, data, isSet)`: Ativa ou Seta uma M/T.
*   `ActivateFieldSpellTrap(cardGO)`: Ativa uma carta que já estava Setada.
*   `TributeCard(card)`: Envia ao GY como tributo (com VFX).
*   `SendToGraveyard(card, isPlayer)` / `RemoveFromPlay(data, isPlayer)`: Envia ao GY ou Bane a carta.
*   `DiscardCard(card)` / `DiscardHand(isPlayer)`: Regras de descarte direto.
*   `ReturnToHand(card)` / `ReturnToDeck(card, toTop)`: Bounce e Spin.
*   `ShuffleDeck(isPlayer)`: Re-embaralha o deck de um jogador.
*   `MillCards(isPlayer, amount)`: Envia cartas do topo do deck para o GY.
*   `CreateCardLink(source, target, type)`: Cria o elo invisível de Equipamento.
*   **Mecânicas Especiais:** `BeginFusionSummon`, `PerformFusionSummon`, `BeginRitualSummon`, `PerformRitualSummon`, `TossCoin`, `SpawnToken`.

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