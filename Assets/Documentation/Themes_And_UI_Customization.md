# Customização de Temas e UI (Themes & UI)

Este documento detalha o funcionamento do sistema de Temas (Data-Driven Design) utilizando `ScriptableObjects` e como configurar elementos dinâmicos da interface, como o Relógio de Turnos e Minigames.

## 1. A Arquitetura de Temas (O Console e os Cartuchos)
O jogo utiliza um sistema modular para permitir que cada Ato da campanha (ou duelo específico) tenha sua própria identidade visual sem sobrecarregar a memória da cena principal.

*   **`DuelThemeManager` (O Console):** Fica na cena (`Panel_Duel`). Ele possui referências vazias para os componentes de UI (Images, Texts). Ele não armazena as artes, apenas as aplica.
*   **`DuelTheme` (O Cartucho):** É um arquivo de dados (`ScriptableObject`) salvo nas pastas do projeto (`Assets/Data/Themes/`). Ele guarda todas as imagens, ícones, sons e coordenadas de um tema específico (ex: Ato 1 - Escola, Ato 8 - Egito).

### 1.1 Como o Tema é Aplicado
1.  O `GameManager` verifica em qual nível/ato o jogador está através do `CampaignDatabase`.
2.  Ele carrega o `DuelTheme` correspondente.
3.  Ele passa esse arquivo para o `DuelThemeManager.Instance.ApplyTheme(theme)`.
4.  O Manager injeta os sprites nas UIs e salva o `currentTheme` para que elementos gerados dinamicamente (como as moedas e relógios que surgem durante o duelo) saibam qual arte puxar.
*   **Fallback:** Se a campanha não retornar um tema válido (ou o tabuleiro do tema estiver vazio), o Manager utiliza o `defaultTheme` (Tema Padrão) configurado diretamente nele no momento do Start.

---

## 2. Configurando o Relógio de Turnos (`TurnClockUI`)
O Relógio Gigante que aparece para cartas como *Swords of Revealing Light* e *Destiny Board* é altamente customizável via `DuelTheme`, permitindo adaptar qualquer formato de desenho.

### 2.1 As Três Camadas do Relógio
O objeto `Panel_TurnClock` possui três imagens sobrepostas para gerar o efeito visual perfeito:
1.  **Base (`clockBaseImage`):** A imagem estática de fundo. Geralmente deve ter uma cor escurecida ou semitransparente (ex: Alpha 0.5) para representar o "tempo que já acabou".
2.  **Fill (`clockFillImage`):** A mesma imagem da base, mas configurada como `Image Type: Filled` (Radial 360). Conforme o tempo passa, a fatia diminui, revelando a base escura por baixo (Efeito de Luz Varrendo).
3.  **Hand (`clockHandImage`):** O ponteiro (Espada/Ponteiro) que gira livremente na frente de tudo.

### 2.2 Parâmetros de Customização no `DuelTheme`
No arquivo do tema (ex: `Theme_Act1`), a seção **"Minigames (Sorte/Tempo)"** possui as seguintes opções:
*   **Clock Base Sprite & Clock Hand Sprite:** As imagens brutas.
*   **Preserve Clock Aspect (bool):** Se ativo, impede que as imagens fiquem achatadas/esticadas.
*   **Clock Base Size / Clock Hand Size (X, Y):** Força um tamanho específico para o fundo e o ponteiro. Se deixado em `(0, 0)`, usa o tamanho atual do prefab.
*   **Use Clock Fill Effect (bool):** Liga ou desliga a camada `Fill` (a fatia de pizza colorida). Se desativado, o relógio exibe apenas a Base estática e a Espada.

### 2.3 O Segredo do Alinhamento Físico (Pivot e Center)
Como os desenhos variam de formato, o código ajusta a posição ancorando a espada matematicamente através de duas coordenadas base:

*   **Clock Hand Center (X, Y):** Define onde o ponteiro deve ser "pregado" em relação ao centro exato da imagem de fundo. 
    *   *Exemplo:* Se o buraco do relógio desenhado está deslocado para cima e para a direita, ajuste algo como `X: 15, Y: 20`.
*   **Clock Hand Pivot (X, Y):** Define qual é o "eixo de rotação" do próprio ponteiro. Varia de `0.0` a `1.0`.
    *   *Exemplo:* Se a espada aponta para cima, a base dela está embaixo. Configure o Pivot para `X: 0.5` e `Y: 0.0`.
    *   *Ajuste Fino:* Se o eixo de giro da espada precisar ficar um pouco "fora" do limite inferior da imagem, use valores negativos (ex: `Y: -0.15`).

---

## 3. Minigames (Moeda e Dados)
Assim como o relógio, o `CoinTossUI` e o `DiceRollUI` resgatam dinamicamente seus sprites baseados no `currentTheme`.
*   Os dados suportam um Array de 6 faces (`diceFaceSprites`).
*   Se a roleta for chamada e o tema não tiver as imagens definidas, a Unity exibirá blocos brancos, exigindo o preenchimento do Cartucho de Tema.

---

## 4. Ícones de Seleção e Feedback Visual (Selection Icons)
O sistema `DuelFXManager` permite customizar o feedback visual tátil nas cartas de forma modular, sem depender apenas dos contornos (`Outlines`). Utilizando `HighlightCategory`, é possível usar Prefabs 3D ou Sprites 2D que piscam sobre as cartas em diferentes gatilhos do jogo.

### 4.1 Resposta de Corrente (Chain Response)
*   **Contexto:** Quando a Engine detecta que você tem "Quick Effects" ou "Trap Cards" para acorrentar a uma ação inimiga, o tabuleiro não exibe mais a "Mira Gigante" genérica.
*   **Customização:** Em `DuelFXManager` -> `--- CHAIN RESPONSE (ACTIVATE) ---`, você pode definir um brilho/neon específico. Acompanhado do `MouseTooltipUI` indicando `L: Activate | R: Cancel`, ele cria a experiência de um eSports moderno.

### 4.2 Ativação Preditiva 1-Click (Effect Activation Hover)
*   **Contexto:** Ao ativar a opção `activateEffectsWithOneClick` no `GameManager`, o jogo desabilita a necessidade de abrir o submenu (`DuelActionMenu`) para ativar efeitos em campo.
*   **Customização:** Para instruir visualmente o jogador sobre o que pode ser clicado, o `DuelFXManager` -> `--- EFFECT ACTIVATION (HOVER) ---` instanciará automaticamente o Prefab configurado (Ex: um balão pulsante com o texto "Activate") sempre que o cursor passar por cima de uma carta válida. O jogador clica com o botão esquerdo e o efeito explode em velocidade máxima.