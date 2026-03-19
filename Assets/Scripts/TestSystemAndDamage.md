# Sistemas de Teste, Debug e Efeitos Visuais (UI/UX)

## 1. Sistema de Popup de Dano (Damage Screen)
O `DamagePopupManager` substitui textos chatos de log por números estilo MMO flutuando sobre o campo.

### Como Funciona:
*   Quando `GameManager.DamagePlayer` ou `GainLifePoints` são chamados, o GameManager checa a flag `enableDamagePopups`.
*   O `DamagePopupManager` é invocado, pegando um Prefab vazio (`DamagePopup`).
*   **Construção Dinâmica:** Se `useSpritesForNumbers` estiver ativado, o script lê o valor (ex: 2500) e cria `Images` filhas no Layout Group usando os Sprites configurados no Inspector (índices de 0 a 9). O índice 10 é reservado para o sinal de Operador (`+` verde ou `-` vermelho).
*   **Animação:** O número flutua para cima e perde opacidade (Fade Out) antes de se destruir sozinho.

## 2. Tooltip Dinâmico de Mouse
*   **Componente:** `MouseTooltipUI` (Ativado por `GameManager.useMouseTooltipUI`).
*   Segue a posição do Cursor lendo o novo Input System da Unity.
*   **Matemática de Pivot Inteligente:** Evita que o balão saia da tela cortado. Se o mouse for para o canto direito, o `Pivot X` vira `1` e o balão projeta para a esquerda.

## 3. Full Test Mode (Painel de Desenvolvedor)
A ferramenta de QA suprema do jogo, ativada no `GameManager.fullTestMode` ou pelo atalho **Ctrl + T** durante um duelo.

### Funcionalidades do Painel (`FullTestManager`):
*   **Arrastável:** O painel usa `DraggableWindow` (IDragHandler) para não atrapalhar a visão do campo.
*   **Toggles Embutidos:**
    *   `IA (On/Off)`: Ao desligar, o script `OpponentAI` é desativado e o desenvolvedor ganha controle total das cartas do oponente (`canPlaceOpponentCards = true`).
    *   `Show Opponent Hand`: Vira as cartas da IA para cima.
    *   `Auto Phases`: Trava o pulo de fases se você quiser ficar preso na Battle Phase para testar algo repetidamente.
    *   `Infinite LP`: O dano é processado e os popups aparecem, mas o Game Over nunca ocorre.
    *   `VFX e SFX`: Desativa luzes e sons para debug limpo.
*   **Seletores de Cenário (Dropdowns):**
    *   `Ato`: Força o carregamento do tema e músicas de um Ato específico (1 a 10).
    *   `Opponent`: Força a IA a usar o deck e avatar do personagem escolhido na lista.
    *   `Deck Variant`: Define qual versão do deck o oponente utilizará (Aleatório, Deck A, Deck B ou Deck C).
*   **Botões de Ação:**
    *   `Spawn Card`: Abre a Busca Global (Teclado) e coloca qualquer carta do banco de dados das 2147 cartas direto na sua mão durante o duelo!
    *   `Simular Ataque`: Pede que você clique em 1 monstro inimigo, e depois 1 monstro seu. O sistema força a corrente (Chain) a abrir pedindo armadilhas de resposta e roda o combate.
    *   `Forçar Ativação (Trap)`: Clica numa armadilha setada e ela resolve imediatamente sem precisar de gatilho.

### Dev Card Menu (Super Menu)
*   Com o *Full Test Mode* ativo, segure **Shift e clique com o Botão Direito** em qualquer carta do campo/mão.
*   Abre um modal de múltipla escolha com opções absolutas de código:
    1.  Mandar pro Cemitério.
    2.  Banir.
    3.  Voltar pra Mão.
    4.  Voltar pro Topo do Deck.
    5.  Mudar a Posição forçada.
    6.  Flip / Face-Down forçado.