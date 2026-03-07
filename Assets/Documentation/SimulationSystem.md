# Sistema de Simulação de Caos (Chaos Simulator)

O `SimulationManager` é uma ferramenta de desenvolvimento projetada para testar a estabilidade do jogo, encontrar bugs de lógica e verificar o balanceamento de decks rodando duelos automaticamente em alta velocidade.

## Como Usar

1.  Na cena do Unity, certifique-se de que existe um GameObject com o script `SimulationManager`.
2.  Inicie o jogo (Play Mode).
3.  No canto direito da tela, você verá o painel "SIMULADOR DE CAOS".

## Modos de Operação

### 1. Modo Visual (Assistir)
*   **Botão:** `Iniciar Visual`
*   **Velocidade:** 1.5x (Ajustável em `visualTimeScale`).
*   **Descrição:** O jogo roda sozinho, mas mantém as animações de batalha, movimento de cartas e efeitos visuais ligados.
*   **Uso:** Ideal para assistir a IA jogando contra si mesma e verificar se as animações estão sincronizadas ou se há erros visuais.

### 2. Modo Rápido (Log)
*   **Botão:** `Iniciar Rápido`
*   **Velocidade:** 50x (Ajustável em `fastTimeScale`).
*   **Descrição:** Desliga todas as animações (`DuelFXManager`) e tenta rodar os turnos o mais rápido possível.
*   **Uso:** Teste de estresse. Verifica se o jogo trava (soft-lock), se ocorrem erros de referência nula ou loops infinitos em interações complexas de cartas.

## Funcionalidades Automáticas

Quando a simulação está ativa (`GameManager.isSimulating = true`), o jogo altera seu comportamento padrão:

*   **Bypass de UI:** Janelas de confirmação, seleção de alvo e seleção de posição são respondidas automaticamente (geralmente escolhendo a primeira opção válida ou aleatória) para não travar o fluxo.
*   **Tributo Automático:** Se um monstro de nível alto for invocado, o sistema sacrifica automaticamente os monstros mais fracos disponíveis.
*   **Ignorar Travas de Turno:** O sistema permite que o "Oponente" jogue cartas mesmo que o estado do turno esteja em transição, evitando avisos de `canPlaceOpponentCards`.

## Logs e Depuração

O painel na tela mostra um log em tempo real das ações tomadas:
*   `[SIM] P tenta Invocar...`: O Jogador (Player) está agindo.
*   `[SIM] O tenta Ativar...`: O Oponente está agindo.
*   `[SIM] Ataque declarado...`: Ações de batalha.

## Configuração (Inspector)

No componente `SimulationManager`:
*   `Duels To Run`: Quantos duelos rodar em sequência antes de parar.
*   `Auto Start Simulation`: Se marcado, começa a simular assim que o jogo abre (útil para builds de teste).
*   `Max Turns Per Duel`: Limite de segurança para forçar empate se o duelo durar muito (evita loops infinitos de decks de stall).

## Solução de Problemas Comuns

*   **O jogo "congela" mas não trava:** Geralmente significa que uma janela de UI (como "Deseja ativar o efeito?") abriu e o Simulador não tem lógica para fechá-la.
    *   *Solução:* Verifique `UIManager.cs` e adicione um bloco `if (GameManager.Instance.isSimulating)` para pular essa janela.
*   **Erros de "Ação bloqueada":** Ocorrem se a simulação tentar jogar cartas do oponente muito rápido durante a troca de turno.
    *   *Solução:* A correção no `GameManager` (ignorar travas se `isSimulating`) resolve isso.