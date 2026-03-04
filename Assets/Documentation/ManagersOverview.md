# Visão Geral dos Gerenciadores (Managers)

O jogo utiliza uma arquitetura de múltiplos Managers para separar responsabilidades e facilitar a manutenção. O `GameManager` atua como o orquestrador central.

## 1. PhaseManager
**Responsabilidade:** Controlar o fluxo de tempo e fases do turno.
*   **Fases:** Draw -> Standby -> Main 1 -> Battle -> Main 2 -> End.
*   **Funções:**
    *   `ChangePhase(GamePhase)`: Avança para a próxima fase.
    *   `StartTurn()`: Reseta contadores do turno.
    *   Gerencia a UI da barra de fases (botões e brilho neon).

## 2. SummonManager
**Responsabilidade:** Validar e executar regras de invocação de monstros.
*   **Regras:** Limite de 1 Normal Summon por turno, Tributos necessários (Nível 5+).
*   **Funções:**
    *   `PerformSummon(...)`: Verifica se a invocação é válida.
    *   `GetRequiredTributes(level)`: Retorna 0, 1 ou 2.
    *   `SelectTributes(...)`: Inicia o fluxo de seleção manual de tributos com callback.
    *   Gerencia o fluxo de **Tributo Manual** (pausa o jogo para o jogador selecionar os monstros a sacrificar).

## 3. BattleManager
**Responsabilidade:** Gerenciar a Battle Phase, ataques e cálculo de dano.
*   **Funções:**
    *   `DeclareAttack(attacker)`: Inicia um ataque.
    *   `SelectTarget(target)`: Define o alvo e calcula o resultado.
    *   `ResolveDamage(...)`: Aplica a lógica de ATK vs ATK ou ATK vs DEF e destrói monstros.
    *   `IsTrapActivationBlocked(...)`: Verifica se armadilhas podem ser ativadas na Battle Phase (ex: *Mirage Dragon*).
    *   `TryChangePosition(card)`: Gerencia a mudança manual de posição (Ataque/Defesa) com limite de 1x por turno.

## 4. SpellTrapManager
**Responsabilidade:** Gerenciar regras de Magias e Armadilhas e respostas (Chains).
*   **Funções:**
    *   `CheckForTraps(...)`: Verifica se há armadilhas que podem ser ativadas em resposta a um ataque ou invocação.
    *   `CanActivateCard(...)`: Valida se uma carta pode ser usada (ex: Trap só no turno seguinte).
    *   Gerencia exceções como pular Draw Phase ou comprar cartas extras.

## 5. ChainManager
**Responsabilidade:** Gerenciar a pilha de efeitos (Corrente/Chain).
*   **Lógica:** LIFO (Last-In, First-Out). O último card ativado resolve primeiro.
*   **Funções:**
    *   `AddToChain(card)`: Adiciona um efeito à pilha.
    *   `ResolveChain()`: Executa os efeitos na ordem inversa e envia as cartas para o cemitério (se não forem contínuas).

## 6. SpellCounterManager
**Responsabilidade:** Gerenciar contadores de magia (Spell Counters) em cartas.
*   **Funções:**
    *   `AddCounter(card, amount)`: Adiciona contadores.
    *   `RemoveCounter(card, amount)`: Remove contadores.
    *   `GetCount(card)`: Retorna a quantidade atual.
    *   `RemoveCountersFromField(...)`: Remove contadores de qualquer lugar do campo (para custos).

## 7. CardEffectManager
**Responsabilidade:** Hub central para execução de lógica de cartas e escuta de eventos globais.
*   **Estrutura:** Dividido em classes parciais (`Impl`, `Registry`, `Part1`..`Part5`) para organização.
*   **Hooks:** `OnSummon`, `OnSet`, `OnBattlePositionChanged`, `OnCardSentToGraveyard`, `OnPhaseStart`, etc.
*   **Flags Globais:** Gerencia estados como `reverseStats` (Reverse Trap) e `banishInsteadOfGraveyard` (Macro Cosmos).

## 8. DuelFXManager
**Responsabilidade:** Feedback visual e sonoro.
*   **Funções:**
    *   Toca sons (SFX) e instancia partículas (VFX) para ações como Invocação, Ataque, Dano, Flip, etc.
    *   `UpdateBGM(playerLP, opponentLP)`: Altera a música de fundo dinamicamente baseada na vantagem de vida.