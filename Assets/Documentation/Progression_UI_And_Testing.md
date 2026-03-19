# 9. Progressão, Recompensas e Visuais (Progression, UI & Testing)

Este documento unifica todos os sistemas responsáveis pelo acabamento do jogo, incluindo a lógica de pontuação, o museu de colecionáveis (Library), as ferramentas de depuração avançadas e os gerenciadores estéticos de tema, partículas e áudio.

---

## 9.1 Sistema de Pontuação e Drop Rate (`DuelScoreManager.cs` e `RewardPanelUI.cs`)

### Visão Geral
O sistema de pontuação (`DuelScoreManager.cs`) avalia o desempenho do jogador em cada duelo. O objetivo é recompensar vitórias técnicas e rápidas, incentivando o uso de mecânicas avançadas (Fusão, Armadilhas) em vez de apenas força bruta. A pontuação define o **Rank**, que por sua vez define a qualidade das recompensas (**Drop Rate**).

### Cálculo de Pontuação (Duel Score)

#### Pontuação Base
*   **Vitória:** +2500 pontos.
*   **Derrota:** A pontuação final é dividida por 4.

#### Bônus de Ação (Técnica)
*   **Magia Ativada:** +100 pts
*   **Armadilha Ativada:** +100 pts
*   **Fusão Realizada:** +400 pts (Incentivo alto)
*   **Ritual Realizado:** +400 pts
*   **Tributo Realizado:** +150 pts
*   **Monstro Inimigo Destruído:** +300 pts
*   **Dano Máximo:** +100 pts a cada 1000 de dano em um único ataque.
*   **LP Restante:** +50 pts a cada 1000 LP sobrando.
*   **Vitória Perfeita (Sem Dano):** +1000 pts.

#### Penalidades
*   **Tempo:** -3 pts por segundo (aprox. -180 pts/min). Duelos lentos diminuem o Rank.
*   **Cartas no Cemitério:** -20 pts por carta própria no GY (desencoraja descarte excessivo).
*   **Dano Recebido:** -100 pts a cada 1000 de dano tomado.

### Ranks de Duelo
A pontuação final determina o Rank:
*   **S+:** Vitória por Deck Out (Oponente sem cartas). Independente da pontuação.
*   **S:** > 9000 pts (Vitória Perfeita ou muito rápida).
*   **A+:** 8000 - 8999 pts.
*   **A:** 7000 - 7999 pts.
*   **B+:** 6000 - 6999 pts.
*   **B:** 5000 - 5999 pts.
*   **C+:** 4000 - 4999 pts.
*   **C:** 3000 - 3999 pts.
*   **D+:** 2000 - 2999 pts.
*   **D:** < 2000 pts (Vitória pobre).
*   **F:** Derrota. Recompensas não são concedidas.

### Sistema de Drop
Cada oponente na `CampaignDatabase` possui um objeto `rewards` estruturado por Tiers no arquivo `characters.json`. Ao vencer:
1.  O jogo calcula o Rank.
2.  O Rank define as probabilidades de saque de cada pool de cartas:

| Rank Obtido | S+ (Única) | Pool S (Top) | Pool B (Mid) | Pool C (Low) | Pool D (Fodder) |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **S+ (Deck Out)** | **15%** | 34% | 25.5% | 17% | 8.5% |
| **S / A+ / A** | 0% | **40%** | 30% | 20% | 10% |
| **B+ / B** | 0% | 10% | **40%** | 30% | 20% |
| **C+ / C** | 0% | 3% | 17% | **40%** | 40% |
| **D+ / D** | 0% | 1% | 4% | 25% | **70%** |
| **F** | 0% | 0% | 0% | 0% | 0% |

*Nota: No Rank S+, se o sorteio de 15% da carta única falhar, o jogo distribui os 85% restantes proporcionalmente entre os pools S, B, C e D seguindo a distribuição do Rank S.*

### Estrutura do JSON
O campo `rewards` no `characters.json` contém:
*   `s_plus`: ID único da carta especial.
*   `s`, `b`, `c`, `d`: Listas (arrays) de IDs de cartas classificadas por poder.

---

## 9.2 Sistema de Troféus e Conquistas (`TrophyManager.cs`)

O sistema visa recompensar tanto a progressão na história quanto o "grind" de longo prazo na Arena, além de encorajar a exploração de diferentes mecânicas de jogo.

### As 7 Categorias (100 Troféus)
*   **Categoria 1: Campanha e História (1-10):** Conclusão dos Atos de 1 a 10 e zerar o jogo.
*   **Categoria 2: Arena e Vitórias (11-25):** Acumular até 25.000 vitórias na Arena e desbloquear todos os 100 oponentes em *Free Duel*.
*   **Categoria 3: Coleção e Construção (26-35):** Obter a primeira carta "New", completar a Card Library (2147 cartas), salvar 10 Decks e desbloquear Arenas visuais.
*   **Categoria 4: Dano e Batalha (36-50):** Hit킬 de 8000+ dano em um turno, Overkill de 10.000+, causar 100 Milhões de dano acumulado e vencer sem sofrer dano (*Perfect Win*).
*   **Categoria 5: Magia, Armadilha e Efeitos (51-70):** Ativar magias e armadilhas até 10.000 vezes. Invocações Especiais massivas e mecânicas específicas (Counter Traps e Quick-Plays).
*   **Categoria 6: Ranks e Resultados (71-80):** Obter todos os Ranks de F a S+. Vencer e Perder por *Deck Out*. Vencer reunindo as peças do *Exodia*.
*   **Categoria 7: Desafios Específicos (81-100):** Derrotar chefes X vezes (Ex: Bandit Keith 20x). Vencer usando *Destiny Board* ou *Final Countdown*. Terminar com monstro de 5000+ ATK ou 5 S/T no campo. Acertar 3 moedas seguidas.

---

## 9.3 Sistema de Biblioteca (Library System)

A **Biblioteca (Library)** é o museu pessoal do jogador, onde todo o progresso, colecionáveis e informações desbloqueadas são armazenados. O acesso é feito através do Menu Principal.

### 9.3.1 Menu Principal da Biblioteca (`Panel_Library`)
*   **Estrutura:** `Panel_Library` -> `Panel_LibraryButtons`.
*   **Botões:** `Btn_LibCards`, `Btn_LibDuelists`, `Btn_LibArenas`, `Btn_LibDecks`, `Btn_BackToMenu`. Utilizam o componente customizado `MillenniumButton`.

### 9.3.2 Biblioteca de Duelistas (`Panel_LibDuelists` / `DuelistLibraryManager.cs`)
*   **Lógica de Desbloqueio:**
    1. Bloqueado: Não aparece.
    2. Arena (Free Duel): Após vencer na Campanha, fica disponível para duelos livres.
    3. Biblioteca Completa: Requer **1 vitória na Arena** contra este oponente para revelar Avatar, Lore e Descrição.
*   **Hierarquia:** `Panel_DuelistsViewer` -> `DuelistViewer` (Reusa `CardViewerUI` para Avatar e Descrição). Grade em `Panel_DuelistLibrary` com Virtual Scrolling/Paginação.

### 9.3.3 Biblioteca de Cartas (`Panel_LibCards` / `CardLibraryManager.cs`)
*   **Visualização Completa:** Exibe todas as 2147 cartas do banco de dados em páginas.
*   **Cartas Obtidas vs Não Obtidas:** Cartas não obtidas exibem o verso (`Back`) da carta e omitem os dados no `CardViewerUI` para manter o mistério.
*   **Hierarquia:** `Panel_CardViewer` à esquerda. `Panel_CardLibrary` à direita em `GridLayoutGroup` (10 colunas x 5 linhas, 50 por página).

### 9.3.4 Biblioteca de Decks (`Panel_LibDecks` / `DeckLibraryManager.cs`)
*   **Grind System (Desbloqueio de Variantes):** Permite ver receitas de inimigos. Requer *grind* na Arena contra o oponente específico:
    *   **Deck A:** 50 Vitórias.
    *   **Deck B:** 100 Vitórias.
    *   **Deck C:** 200 Vitórias.
    *   **Extra Deck:** 250 Vitórias.
*   **Hierarquia:** `Panel_DeckViewer` (lista a receita) e `Panel_VariantButtons` (A/B/C).

### 9.3.5 Biblioteca de Arenas (`Panel_LibArenas` / `ArenaLibraryManager.cs`)
*   **Lógica:** O desbloqueio é vinculado à Campanha (`CampaignManager.maxUnlockedLevel`). Ao derrotar o chefe de um Ato, o tema (`DuelTheme`) inteiro é liberado na galeria.
*   **Filtros:** `Btn_Backgrounds`, `Btn_Frames`, `Btn_UIElements`.

### 9.3.6 Scripts de Dados (`LibraryDataTypes.cs`)
*   `DuelistWinRecord`: Estrutura serializável para IDs e vitórias.
*   `LibrarySaveData`: Armazenado no `SaveLoadSystem`, guarda as listas de cartas vistas (remoção da Tag NEW) e vitórias.

---

## 9.4 Mecânica Efêmera da Tag "NEW"

### Visão Geral e Exibição
Notifica o jogador sobre cartas recém-adquiridas nas recompensas (`RewardPanelUI`), na Biblioteca (`CardLibraryManager`) e no Baú (`DeckBuilderManager`).
*   **Ocultação Temporária:** Se o jogador adicionar uma cópia da carta ao Deck (Principal, Extra ou Side) durante a sessão de construção, a tag some do Baú temporariamente. Se remover a carta, a tag reaparece.

### Lógica de Persistência (`SaveLoadSystem`)
Uma carta deixa de ser "Nova" quando o jogador demonstra intenção de uso:
*   `SaveLoadSystem.IsCardNew(string cardId)`: Retorna `true` se a carta não estiver no array de cartas "usadas".
*   `SaveLoadSystem.MarkCardAsUsed(string cardId)`: Registra o uso. Disparado nos seguintes eventos:
    1. Ao clicar em **Save Deck** no `DeckBuilderManager` (itera por todo o deck e marca).
    2. Ao gerar o **Deck Inicial** (`InitialDeckBuilder`), para que as primeiras 40 cartas não fiquem piscando na biblioteca.
    3. Ao ativar o cheat `unlockAllCards` no GameManager, para não poluir a biblioteca visualmente.

---

## 9.5 Sistemas de Teste, Debug e UI Dinâmica

### 9.5.1 Popup de Dano (`DamagePopupManager.cs`)
*   Substitui logs chatos por números flutuantes na tela. Ativado via `GameManager.enableDamagePopups`.
*   **Renderização Dinâmica:** Se `useSpritesForNumbers` estiver ativado, o script gera `Images` filhas num `HorizontalLayoutGroup` lendo sprites de 0 a 9 (o índice 10 é o operador `+` ou `-`).
*   **Animação:** O Prefab `DamagePopup` sobe verticalmente e perde opacidade (Fade Out) com o tempo, e depois chama `Destroy()`.

### 9.5.2 Tooltip de Mouse Dinâmico (`MouseTooltipUI.cs`)
*   Ativado via `GameManager.useMouseTooltipUI`. Segue o cursor usando o Input System.
*   Substitui o Menu de Ação clássico informando ações de clique. Ex: Na mão -> (Esq: Summon, Dir: Set).
*   **Pivot Inteligente:** Se o mouse for para o canto direito da tela, o Pivot X vira 1, projetando o balão para a esquerda para não cortar a interface.

### 9.5.3 Painel de Desenvolvedor (`FullTestManager.cs`)
A ferramenta de QA suprema, ativada no Inspector (`fullTestMode`) ou por **Ctrl + T** in-game. É uma janela arrastável (`IDragHandler`).
*   **Toggles:**
    *   `IA (On/Off)`: Desativa `OpponentAI` e aciona `canPlaceOpponentCards = true`.
    *   `Show Opponent Hand`: Vira as cartas do inimigo.
    *   `Auto Phases`: Congela as fases para testes repetidos de combate.
    *   `Infinite LP`: Impede Game Over para testar dano constante.
    *   `VFX / SFX`: Desativa luzes e som.
*   **Dropdowns de Cenário:** Força a troca de `DuelTheme` (Ato 1 a 10), carrega baralhos específicos do `CharacterDatabase` e seleciona `Deck Variant` (A, B, C).
*   **Botões de Ação:**
    *   `Spawn Card`: Abre o `GlobalCardSearchUI` para injetar qualquer carta na mão.
    *   `Simular Ataque` e `Forçar Ativação (Trap)`.
*   **Dev Card Menu (Super Menu):** Com o modo ativo, **Shift + Botão Direito** em qualquer carta abre um modal para: Mandar pro GY, Banir, Bounce pra mão, Topo do Deck, Mudar Posição Forçada ou Flip Forçado.

### 9.5.4 In-Game Debug Console (`InGameDebugConsole.cs`)
*   **Atalho:** Ctrl + Shift + D.
*   Painel `DontDestroyOnLoad` que coleta os últimos 1000 `Debug.Log`, `Warning` e `Error`.
*   Botão **Copy** extrai tudo formatado para a área de transferência, embutindo o `StackTrace` em caso de erros para relatórios de QA fáceis fora do Editor Unity.

---

## 9.6 Simulador de Caos (`SimulationManager.cs`)

Ferramenta de automação para testes de estresse, balanço de deck e procura por soft-locks na engine. Fica ativa com `GameManager.isSimulating = true`.

### Modos de Operação
*   **Modo Visual (Assistir):** Velocidade em 1.5x (`visualTimeScale`). Roda com animações e efeitos (`DuelFXManager`) ligados para checar sincronia gráfica.
*   **Modo Rápido (Log):** Velocidade em 50x (`fastTimeScale`). Desliga gráficos e voa pela lógica matemática. Testa loops infinitos e Null References.

### Comportamentos de Bypass (`isSimulating`)
*   A flag muda o roteamento de métodos assíncronos. Modais de Confirmação, Seleções de Alvo e Posição não abrem janelas. O sistema escolhe a primeira opção válida imediatamente.
*   Ignora validações estritas de `canPlaceOpponentCards` para a máquina conseguir jogar por ambos os lados sem ser travada pela troca de turnos.
*   Executa sacrifícios automaticamente (Tributo Automático) escolhendo lacaios fracos para invocar Boss Monsters.
*   **Painel UI:** Exibe logs rápidos (`[SIM] P tenta Invocar...`). Possui travas de segurança (`Max Turns Per Duel`) para forçar empate e não travar o loop de milhares de partidas.

---

## 9.7 Sistema de Temas de Duelo (`DuelThemeManager.cs` e `DuelTheme`)

Permite alterar a identidade gráfica e sonora de toda a Interface (UI) sem tocar em código, dependendo de qual "Ato" o jogador se encontra.

### Arquitetura
1.  **`DuelTheme` (ScriptableObject):** Arquivo de dados que arquiva sprites de Background (Tabuleiro, Perfis), Painéis, Botões, Fontes, VFXs (partículas) e BGMs (Música).
2.  **`DuelThemeManager`:** Aplicado à cena `Panel_Duel`. No `Start()`, o GameManager consulta a `CampaignDatabase` para saber o Ato atual e envia o tema para este Manager através de `ApplyTheme(theme)`.
3.  **Integração Minigames:** O Theme repassa os assets visuais para injetar moedas (`CoinTossUI`) e dados (`DiceRollUI`) tematizados de acordo com a região (Ex: Moedas egípcias no Ato 8).

---

## 9.8 Efeitos Visuais e Sonoros (`DuelFXManager.cs`)

Centraliza todos os instanciadores de partículas (VFX) e áudios do duelo. Os assets utilizados podem ser sobrescritos pelas configurações dinâmicas do `DuelTheme`.

### Efeitos de Ação de Carta
| Ação | Descrição Visual |
| :--- | :--- |
| **Summon** | Partículas de luz/poeira na base da carta instanciada. |
| **Set / Flip** | Som ríspido de papel e "poeira" sutil. |
| **Activate Spell** | Brilho verde/mágico ascendente. |
| **Activate Trap** | Brilho roxo/sinistro acompanhado de som metálico. |
| **Tribute** | Efeito de "alma" ou feixe de luz azul saindo do monstro sacrificado em direção aos céus. |
| **Fusion** | Espiral giratória (Polymerization style) unindo materiais e brilhando no centro. |
| **Destruction** | Fumaça e explosão com som fragmentado (shatter). |
| **Banish** | Vórtice negro ou fenda dimensional sugando o `CardDisplay`. |

### Efeitos de Batalha
| Ação | Descrição Visual |
| :--- | :--- |
| **Attack** | Animação Tween da carta recuando e avançando violentamente (Strike). |
| **Impact** | Efeito de faíscas/corte aplicado na coordenada do alvo. |
| **Reflect** | Barreira de escudo semi-esférica protegendo o defensor quando a DEF supera o ATK. |
| **Damage** | Tremor de tela (Screen Shake) em câmera e baque pesado. |
| **Direct Attack** | Animação do atacante voando em direção à UI do avatar do oponente. |

### Música Dinâmica (BGM State Machine)
A música do duelo flutua em tempo real lendo o método `UpdateBGM(playerLP, opponentLP)`.
1.  **Normal:** BGM padrão do `DuelTheme`.
2.  **Tense (Perigo):** Transição (Fade-Cross) se o Player possuir menos de 50% dos LPs do Oponente ou estiver com LP crítico absoluto.
3.  **Winning (Vantagem):** Transição gloriosa se o Player possuir mais de 200% dos LPs do Oponente.

# Estrutura e Funcionalidades do Painel de Duelo (`Panel_Duel`)

## Visão Geral
O `Panel_Duel` é a interface principal do jogo, onde ocorre a batalha entre o jogador e o oponente. Ele é gerenciado principalmente pelos scripts `GameManager`, `DuelFieldUI` e `DuelThemeManager`.

## Hierarquia de Objetos (Unity)

Abaixo está a estrutura exata dos GameObjects na cena, com seus principais componentes entre colchetes `[]`.

*   **Panel_Duel** `[Image]`
    *   **Panel_CardViewer** `[Image, CardViewerUI]`
        *   **CardViewer** `[CardViewerUI]`
            *   **Card2D** `[EventTrigger, CardDisplay, Mask, Image]`
                *   **Art** `[RawImage]`
            *   **CardStatsText** `[TextMeshProUGUI]`
            *   **Panel_Description** `[Image]`
                *   **CardNameText** `[TextMeshProUGUI]`
                *   **CardInfoText** `[TextMeshProUGUI]`
                *   **Scroll View** `[Image, ScrollRect]`
                    *   **Viewport** `[Image, Mask]`
                        *   **Content** `[]`
                        *   **CardDescriptionText** `[TextMeshProUGUI, Scrollbar, ContentSizeFitter]`
                    *   **Scrollbar Vertical** `[Image, Scrollbar]`
                        *   **Sliding Area** `[]`
                            *   **HandleDescription** `[Image]`
    *   **DuelBoard** `[DuelFieldUI]`
        *   **BoardBackground** `[Image]`
        *   **FieldImg** `[Image]`
        *   **FieldArea** `[Image]`
        *   **OpponentArea** `[]`
            *   **OpponentHand** `[HorizontalLayoutGroup]`
            *   **RemovedCardsOpponent** `[Image, PileDisplay]`
            *   **Field** `[]`
                *   **SpellRow** `[HorizontalLayoutGroup]`
                    *   **S_Zone_1** a **S_Zone_5** `[Image]`
                *   **MonsterRow** `[HorizontalLayoutGroup]`
                    *   **M_Zone_1** a **M_Zone_5** `[Image]`
            *   **FieldSpell** `[Image]`
            *   **OpponentExtraDeck** `[Image, PileDisplay]`
            *   **OpponentGraveyard** `[Image, PileDisplay]`
            *   **OpponentDeck** `[Image, PileDisplay]`
        *   **PlayerArea** `[]`
            *   **PlayerHand** `[HorizontalLayoutGroup]`
            *   **RemovedCardsPlayer** `[Image, PileDisplay]`
            *   **Field** `[]`
                *   **MonsterRow** `[HorizontalLayoutGroup]`
                    *   **M_Zone_1** a **M_Zone_5** `[Image]`
                *   **SpellRow** `[HorizontalLayoutGroup]`
                    *   **S_Zone_1** a **S_Zone_5** `[Image]`
            *   **FieldSpell** `[Image]`
            *   **PlayerExtraDeck** `[Image, PileDisplay]`
            *   **PlayerGraveyard** `[Image, PileDisplay]`
            *   **PlayerDeck** `[Image, PileDisplay]`
    *   **EffectsCards** `[]` (Container para VFX)
    *   **StatsArea** `[Image]`
        *   **PhaseIndicator** `[Image]`
            *   **Draw Phase** `[Image]`
                *   **Btn_Draw** `[Image, Button]` -> **Text_Draw** `[TextMeshProUGUI]`
            *   **Standby Phase** `[Image]`
                *   **Btn_Standby** `[Image, Button]` -> **Text_Standby** `[TextMeshProUGUI]`
            *   **Main Phase 1** `[Image]`
                *   **Btn_Main1** `[Image, Button]` -> **Text_Main1** `[TextMeshProUGUI]`
            *   **Battle Phase** `[Image]`
                *   **Btn_Battle** `[Image, Button]` -> **Text_Battle** `[TextMeshProUGUI]`
            *   **Main Phase 2** `[Image]`
                *   **Btn_Main2** `[Image, Button]` -> **Text_Main2** `[TextMeshProUGUI]`
            *   **End Phase** `[Image]`
                *   **Btn_End** `[Image, Button]` -> **Text** `[TextMeshProUGUI]`
        *   **PlayerProfile** `[]`
            *   **Avatar** `[Image]`
            *   **PanelPlayerProfile** `[Image]`
            *   **Name** `[TextMeshProUGUI]`
            *   **LP** `[TextMeshProUGUI]`
        *   **OpponentProfile** `[]`
            *   **Avatar** `[Image]`
            *   **OpponentPlayerProfile** `[Image]`
            *   **Name** `[TextMeshProUGUI]`
            *   **LP** `[TextMeshProUGUI]`
    *   **GraveyardViewerPanel** `[Image, GraveyardViewer]`
        *   **Scroll View** `[Image, ScrollRect]`
            *   **Viewport** `[Image, Mask]`
                *   **Content** `[HorizontalLayoutGroup]`
            *   **Scrollbar Horizontal** `[Image, Scrollbar]`
                *   **Sliding Area** `[]`
                    *   **Handle** `[Image]`
        *   **CloseGraveyard** `[Image, Button]`
            *   **Close** `[TextMeshProUGUI]`
    *   **ExtraDeckViewerPanel** `[Image, GraveyardViewer]`
        *   **Scroll View** `[Image, ScrollRect]`
            *   **Viewport** `[Image, Mask]`
                *   **Content** `[HorizontalLayoutGroup]`
            *   **Scrollbar Horizontal** `[Image, Scrollbar]`
                *   **Sliding Area** `[]`
                    *   **Handle** `[Image]`
        *   **CloseExtraDeck** `[Image, Button]`
            *   **Close** `[TextMeshProUGUI]`
    *   **RemovedCardsViewerPanel** `[Image, GraveyardViewer]`
        *   **Scroll View** `[Image, ScrollRect, GridLayoutGroup]`
            *   **Viewport** `[Image, Mask]`
                *   **Content** `[]`
            *   **Scrollbar Horizontal** `[Image, Scrollbar]`
                *   **Sliding Area** `[]`
                    *   **HandleExtraDeck** `[Image]`
            *   **Scrollbar Vertical** `[Image, Scrollbar]`
                *   **Sliding Area** `[]`
                    *   **Handle** `[Image]`
        *   **CloseRemovedCards** `[Image, Button]`
            *   **Close** `[TextMeshProUGUI]`
    *   **DeckCardsViewerPanel** `[Image, GraveyardViewer]`
        *   **Scroll View** `[Image, ScrollRect, GridLayoutGroup]`
            *   **Viewport** `[Image, Mask]`
                *   **Content** `[]`
            *   **Scrollbar Horizontal** `[Image, Scrollbar]`
                *   **Sliding Area** `[]`
                    *   **HandleExtraDeck** `[Image]`
            *   **Scrollbar Vertical** `[Image, Scrollbar]`
                *   **Sliding Area** `[]`
                    *   **Handle** `[Image]`
        *   **CloseDeckCards** `[Image, Button]`
            *   **Close** `[TextMeshProUGUI]`
    *   **Panel_ActionMenu** `[Image, VerticalLayoutGroup, DuelActionMenu]`
        *   **Btn_Summon** `[Image, Button]`
            *   **Summon** `[TextMeshProUGUI]`
        *   **Btn_Set** `[Image, Button]`
            *   **Set** `[TextMeshProUGUI]`
        *   **Btn_Activate** `[Image, Button]`
            *   **Activate** `[TextMeshProUGUI]`
        *   **Btn_Cancel** `[Image, Button]`
            *   **Cancel** `[TextMeshProUGUI]`
    *   **Panel_Confirmation** `[Image]`
        *   **Text_Confirmation** `[TextMeshProUGUI]`
        *   **Btn_Yes** `[Image, Button]`
            *   **Text_Yes** `[TextMeshProUGUI]`
        *   **Btn_No** `[Image, Button]`
            *   **Text_No** `[TextMeshProUGUI]`
    *   **Panel_PositionSelection** `[Image, PositionSelectionUI]`
        *   **Text_PositionAsk** `[TextMeshProUGUI]`
        *   **Btn_SummonPosition** `[Image, Button]`
            *   **SummonPosition** `[Image]`
        *   **Btn_SetPosition** `[Image, Button]`
            *   **SetPosition** `[Image]`
    *   **Panel_CardSelection** `[Image, CardSelectionUI]`
        *   **Scroll View** `[Image, ScrollRect]`
            *   **Viewport** `[Image, Mask]`
                *   **Content** `[HorizontalLayoutGroup]`
            *   **Scrollbar Horizontal** `[Image, Scrollbar]`
                *   **Sliding Area** `[]`
                    *   **Handle** `[Image]`
        *   **CloseDeckCards** `[Image, Button]`

## Descrição dos Componentes

### 1. Tabuleiro (`DuelBoard`)
A área central onde as cartas são jogadas.
*   **Zonas de Monstro (5x):** Onde os monstros são invocados.
*   **Zonas de Magia/Armadilha (5x):** Onde Spells/Traps são ativadas ou setadas.
*   **Zona de Campo:** Para cartas de Magia de Campo.
*   **Pilhas (Piles):**
    *   **Deck:** Pilha de compra. Clique para sacar (se permitido).
    *   **Graveyard (Cemitério):** Cartas destruídas/usadas. Clique para visualizar.
    *   **Extra Deck:** Monstros de Fusão. Clique para visualizar.
    *   **Removed Cards:** Cartas banidas. Clique para visualizar.

### 2. Perfis (`PlayerProfile` / `OpponentProfile`)
Exibem as informações vitais dos duelistas.
*   **Avatar:** Imagem do personagem.
*   **Nome:** Nome do duelista.
*   **LP (Life Points):** Pontos de vida atuais. Atualizados dinamicamente com efeitos de "tique-taque" ou tremor.

### 3. Indicador de Fases (`PhaseIndicator`)
Uma barra lateral ou superior que mostra o progresso do turno.
*   **Botões:** Draw, Standby, Main 1, Battle, Main 2, End.
*   **Interação:** O jogador pode clicar para avançar de fase (ex: pular Battle Phase).
*   **Visual:** A fase atual é destacada com cor e brilho neon (`PhaseManager`).

### 4. Visualizador de Cartas (`CardViewer`)
Um painel lateral esquerdo que mostra a carta sob o mouse em detalhes.
*   **Arte Grande:** Imagem em alta resolução.
*   **Descrição:** Texto completo do efeito (com scroll se necessário).
*   **Status:** ATK/DEF, Nível, Tipo, Atributo.

### 5. Mão do Jogador (`PlayerHand`)
*   Área onde as cartas compradas aparecem.
*   **Layout:** `HorizontalLayoutGroup` que organiza as cartas automaticamente.
*   **Hover:** Ao passar o mouse, a carta sobe (`hoverYOffset`) e ganha um contorno (`Outline`) para indicar seleção.

## Painéis de Sobreposição (Modais)

### 1. Visualizadores de Pilha (`GraveyardViewer`, `ExtraDeckViewer`, `RemovedCardsViewer`, `DeckCardsViewer`)
*   Abrem ao clicar nas respectivas pilhas.
*   Mostram uma lista rolável (`ScrollRect`) de todas as cartas naquela zona. Possuem barra de rolagem formatada para facilitar a navegação por dezenas de cartas.

### 2. Menu de Ação (`Panel_ActionMenu`)
*   Abre ao clicar em uma carta na mão.
*   **Opções:** Summon (Ataque), Set (Defesa), Activate (Magia), Cancel.
*   **Inteligência:** Só mostra opções válidas (ex: esconde "Summon" se já invocou no turno).
*   **Ativação de Monstro:** Monstros de efeito no campo agora exibem a opção "Activate" neste menu.

### 3. Seleção de Posição (`Panel_PositionSelection`)
*   Abre ao realizar uma Invocação Especial (ex: *Monster Reborn*).
*   Permite escolher entre **Ataque** (Vertical) ou **Defesa** (Horizontal).

### 4. Confirmação (`Panel_Confirmation`)
*   Janela genérica para perguntas de "Sim/Não".
*   Usada para: Declarar ataque, Tributar monstros, Ativar efeitos opcionais, Render-se.

### 5. Seleção de Cartas Múltipla (`Panel_CardSelection`)
*   Exibe a listagem de cartas quando efeitos demandam escolhas difíceis e complexas (ex: Buscar carta do Deck, reviver monstro do Cemitério).
*   Renderiza o resultado horizontalmente em um `HorizontalLayoutGroup`.

### 6. Tooltip Dinâmico do Mouse (`MouseTooltipUI`)
*   **Nova Mecânica:** Um prefab com formato de mouse que segue o cursor e informa atalhos rápidos ao passar sobre uma carta (pode ser instanciado dinamicamente via script se não listado acima).
*   **Comandos Mapeados:**
    *   **Monstro na Mão:** Esquerdo = Summon, Direito = Set.
    *   **Magia/Armadilha na Mão:** Esquerdo = Set, Direito = Activate.
    *   **Monstro no Campo (Main Phase):** Esquerdo = Efeito, Direito = Mudar Posição.
    *   **Monstro no Campo (Battle Phase):** Esquerdo = Atacar, Direito = Cancelar Ataque.

## Interações e Feedback

### Hover (Mouse Over)
*   **Cartas na Mão:** Sobem e brilham.
*   **Cartas no Campo:** Brilham (Outline) e aparecem no `CardViewer`.
*   **Pilhas:** Brilham para indicar que são clicáveis.

### Cliques
*   **Clique Esquerdo:** Seleciona, abre menu de ação ou ativa.
*   **Clique Direito:**
    *   **No Monstro (Campo):** Abre modal para mudar posição de batalha (Atk <-> Def).
    *   **No Deck:** Abre modal de Rendição (Surrender).
    *   **No Vazio:** Atalho para cancelar ou avançar fase (contextual).

### Temas (`DuelThemeManager`)
Todos os elementos visuais do `Panel_Duel` (fundos, botões, molduras) são dinâmicos e trocados automaticamente pelo `DuelThemeManager` com base no Ato da campanha atual.
