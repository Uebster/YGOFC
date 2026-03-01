# Sistema de Fases e Turnos

## Visão Geral
O controle de tempo do duelo é gerenciado pelo `PhaseManager.cs`, que atua como uma máquina de estados. O `GameManager.cs` escuta as mudanças de fase para executar regras de jogo (como resetar contadores de invocação).

## Ciclo de Fases (`GamePhase` Enum)

### 1. Draw Phase
*   **Início:** `PhaseManager` chama `GameManager.OnDrawPhaseStart()`.
*   **Reset:** `hasDrawnThisTurn` e `hasPerformedNormalSummon` são resetados.
*   **Mecânica de Saque (Draw):**
    *   **Automático (`canPlayerDrawFromDeck = false`):** O jogo saca uma carta automaticamente e avança imediatamente para a *Standby Phase*.
    *   **Manual (`canPlayerDrawFromDeck = true`):** O jogo **pausa** na Draw Phase. O jogador deve clicar fisicamente no Deck para sacar. Após o saque, o jogo avança para a *Standby Phase*.
    *   *Nota:* Em `devMode`, o jogador pode sacar quantas vezes quiser e não avança fase automaticamente.

### 2. Standby Phase
*   **Função:** Fase de manutenção para efeitos de cartas (ex: pagar custos, aumentar contadores).
*   **Duração:** Atualmente automática (espera alguns segundos e vai para Main 1), a menos que haja efeitos encadeados (futuro).

### 3. Main Phase 1
*   **Ações Permitidas:**
    *   Normal Summon / Set (1 por turno).
    *   Special Summon (Ilimitado).
    *   Ativar/Setar Spells e Traps.
    *   Alterar posição de batalha de monstros (1x por monstro).

### 4. Battle Phase
*   **Requisito:** Só acessível se for o turno do jogador (e não for o primeiro turno do duelo).
*   **Sub-steps:** Start Step -> Battle Step (Ataques) -> Damage Step (Cálculo) -> End Step.
*   **Gerenciador:** `BattleManager.cs` controla a declaração de ataques e seleção de alvos.

### 5. Main Phase 2
*   **Função:** Idêntica à Main Phase 1. Permite baixar cartas ou monstros caso não tenha feito na MP1, para se preparar para o turno do oponente.
*   **Fluxo:** Se o jogador entrou na Battle Phase, ele deve passar pela MP2 antes de encerrar (ou pular direto para End).

### 6. End Phase
*   **Função:** Limpeza de efeitos temporários (ex: "até o final do turno").
*   **Troca de Turno:** O controle é passado para o oponente (`GameManager.ChangeTurn()`).

## UI de Fases
*   A barra de fases no topo da tela mostra a fase atual.
*   **Botões:** Permitem ao jogador avançar manualmente (ex: pular Battle Phase e ir para End Phase).
*   **Neon Effect:** A fase atual brilha para indicar o estado do jogo.