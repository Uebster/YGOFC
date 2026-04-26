# 5. A Bíblia de Programação de Cartas (Card Programming API)

## Visão Geral
Este documento é o guia absoluto e exaustivo para programar e interagir com os efeitos das 2147 cartas do jogo. Ele unifica a arquitetura do sistema, os Gatilhos (Hooks), Funções Auxiliares (Helpers), o Dicionário de Variáveis de Estado (Flags), as lógicas de interrupções de UI e subsistemas complexos. 

**[REVOLUÇÃO DE ARQUITETURA]:** O projeto migrou de um modelo engessado de métodos C# Hardcoded (antigo `CardEffectManager_Impl`) para um motor dinâmico e flexível baseado em scripts **LUA (MoonSharp)**, emulando perfeitamente a API oficial do YGOPro (OCGCore).

---

## 5.1 Arquitetura do Sistema de Efeitos (`CardEffectManager` e MoonSharp)

O `CardEffectManager` é a Máquina Virtual central do jogo. Ele hospeda o interpretador Lua (MoonSharp) e atua como o regente da orquestra, delegando a inteligência da carta para o script de texto e executando os gráficos/status no C#.

### 5.1.1 Estrutura de Arquivos e Componentes
*   **`CardEffectManager.cs` (O Singleton Hub):** Delega e une o motor Lua com as Corrotinas do Unity. Guarda o cache de `activeLuaCards` e despacha requisições entre a Lógica C# e os scripts de cartas.
*   **`LuaScriptLoader.cs` (O Compilador):** Classe estática responsável por ler os arquivos `.lua` no disco e realizar o `SanitizeOCGScript`. É ele que transforma a sintaxe moderna de Lua 5.3 (YGOPro) em algo que o nosso interpretador MoonSharp consiga rodar sem travar.
*   **`LuaEngineCore.cs` (A Fundação e o Dicionário):** Carrega a Máquina Virtual MoonSharp e mapeia a API do OCGCore. É o cofre que guarda todas as **Constantes de Jogo** (`LOCATION_DECK`, `TYPE_SPELL`, `EVENT_SUMMON_SUCCESS`).
*   **`ChainManager.cs` (O Motor de Pilhas LIFO):** Extensão modular instanciada pelo CardEffectManager. Cuida exclusivamente das Janelas de Corrente, orquestrando as validações, a limpeza de Mágicas/Traps após o fim da corrente e emitindo os Callbacks da interface gráfica para o oponente responder (`ResponseWindowRoutine`).
*   **`LuaEventManager.cs` (Os Olhos e Ouvidos):** Extrai todos os gatilhos e escutas do motor. Funções como `OnSummon` e `OnCardLeavesField` moram aqui, vigiando o jogo e ativando as cartas que estavam escutando silenciosamente (`TriggerLuaEvent`).
*   **Pasta `LuaAPI/` (A Ponte C# <-> LUA):** Diretório contendo os arquivos independentes que expõem as lógicas do C# para os scripts de cartas:
    *   **O Ecossistema `LuaDuel`** (`Duel.`): Devido ao seu tamanho massivo, a classe principal foi dividida usando `partial class` em 5 pilares funcionais:
        *   **`LuaDuel_Core.cs`:** A calculadora da partida. Contém variáveis de memória (`currentTargetGroup`), lógicas vitais de matemática de batalha (`CalculateDamage`) e vida (`Recover`).
        *   **`LuaDuel_Queries.cs`:** Os "Radares". Reúne consultas passivas que escaneiam o tabuleiro em 1 frame sem travar o jogo (`GetMatchingGroup`, `GetFieldCard`, `IsExistingTarget`).
        *   **`LuaDuel_Actions.cs`:** A "Mão" de tabuleiro. Funções que manipulam as cartas fisicamente (`SendtoGrave`, `SpecialSummon`, `Destroy`, `Draw`).
        *   **`LuaDuel_UI.cs`:** As "Interrupções". Funções que obrigam a engine Lua a se suspender (`YieldReq`) para abrir modais interativos para o jogador (`SelectTarget`, `SelectOption`, `ConfirmCards`). Integrado ao **`LuaHints.cs`** para traduções.
        *   **`LuaDuel_Stubs.cs`:** O "Lixão Organizado". Arquivo destinado a receber declarações vazias de funções `Duel.` que não usamos na nossa engine, mas que o YGOPro exige que existam para não crachar a compilação. Pode ficar vazio por longos períodos.
    *   **`LuaCard.cs`** (`c:`): A representação física da carta (`c:GetAttack()`).
    *   **`LuaEffect.cs`** (`Effect.`): Contêiner que monta a habilidade (`Condition`, `Cost`).
    *   **`LuaGroup.cs`** (`Group` ou `eg`): Listas dinâmicas usadas em invocações e destruições em massa.
    *   **`LuaUtilityBridge.cs`:** A "Ponte de Atalhos". Um tradutor elegante que permite ao C# chamar rotinas puras de LUA (Tabelas `aux.` e `bit.`) que vivem dentro do MoonSharp. Permite reciclar filtros complexos (como `Auxiliary.IsMonster`) diretamente em lógicas C#, poupando centenas de linhas de código duplicado.
    *   **`LuaProcsAndStubs.cs`**: Diferente do *Duel_Stubs*, este arquivo simula **Classes Inteiras** do OCGCore (`Fusion`, `Synchro`, `Xyz`, `Spirit`). Ele impede que a Unity entre em pânico quando uma carta tenta invocar `Fusion.AddProcMix`. Também hospeda a `PythonAnalyzerStubs`, uma classe vazia projetada unicamente para cegar a regex do nosso Validador LUA Estático (Python) e evitar alarmes falsos.
*   **`Assets/Scripts/LuaScripts/`:** O diretório que contém os scripts de lógica para cada carta, nomeados por sua ID (ex: `c0618.lua` para o *Exodia*).

### 5.1.3 A Supremacia do `LuaEngineCore` e as Constantes
A estabilidade do simulador depende de uma lei imutável: **O C# deve falar o mesmo idioma do YGOPro.**
*   Quando uma carta no campo é destruída por batalha, o script Lua dela espera ouvir o grito do evento `EVENT_BATTLE_DESTROYED`. O C# não pode simplesmente inventar um número aleatório ou string para esse evento. Ele **deve** usar o número exato atrelado a esse evento na tabela do OCGCore.
*   O `LuaEngineCore.cs` hospeda a função `InjectVitalConstants()`. Este é o santuário onde definimos, por exemplo, que `LOCATION_GRAVE = 0x10` e `EVENT_SUMMON_SUCCESS = 1100`.
*   **Sempre que for programar uma ação nova no C#** (Ex: "Quero avisar as cartas que um monstro mudou de posição"), consulte o `LuaEngineCore.cs` ou o arquivo `constant.lua` original. Encontre o ID correto (ex: `1016` para `EVENT_CHANGE_POS`) e dispare o `TriggerLuaEvent(1016, carta)`. O respeito a essa regra é o que garante que 100% dos scripts de cartas oficiais funcionem em nossa Engine sem precisar de uma única reescrita!

### 5.1.2 O Fluxo de Execução e o Sistema `chk`
O jogo obedece rigorosamente às janelas de ativação de um simulador autêntico. A ação foi dividida entre o momento de pagar/escolher alvos (Ativação) e a explosão do efeito (Resolução na Corrente):
1.  **Gatilho Inicial:** O jogador clica em "Activate" ou um evento de jogo ocorre. O `CardEffectManager` chama `ActivateCard()` ou `ExecuteCardEffect()`.
2.  **Carregamento e Cache:** O script `.lua` da carta é lido do disco, sanitizado e carregado na memória. Uma `LuaCard` é criada e associada ao `CardDisplay`, sendo cacheada em `activeLuaCards`.
3.  **Modo de Validação Silenciosa (`chk=0`):** Antes de entrar na corrente, o C# chama `CanActivateEffect()`. Esta função faz um "Dry-run", executando as funções `condition`, `cost` e `target` do script Lua com o argumento `chk=0`. Se qualquer uma delas retornar `false`, a ativação é abortada antes de qualquer custo ser pago ou UI ser mostrada.
4.  **Fase de Ativação (`chk=1`):** Se a validação passar, `BuildAndResolveChainRoutine` é iniciado. As mesmas funções são chamadas com `chk=1`, agora abrindo os modais de UI para o jogador selecionar alvos ou pagar custos.
5.  **A Entrada na Corrente:** Um `ChainLink` é criado e adicionado à pilha `currentChain`.
6.  **Janela de Resposta:** `ResponseWindowRoutine` é chamado, permitindo que o oponente responda acorrentando outro efeito.
7.  **A Resolução (`Operation`):** Quando a corrente fecha, o `CardEffectManager` resolve a pilha em ordem LIFO, executando a função `operation` de cada `ChainLink`.

---

## 5.2 Referência de Gatilhos C# e Escutas LUA (Event Listeners)
Os Hooks são os "radares" do C# chamados automaticamente pela Engine. No novo sistema, ao invés de o C# ter o código da carta, ele atua como um "megafone". Quando um evento de jogo ocorre, ele chama `TriggerLuaEvent(EventCode, args)`, e as cartas cujos scripts `.lua` se registraram para ouvir aquele evento (`e:SetCode(EVENTO)`) são ativadas.

### 5.2.1 Hooks de Fases e Turno
*   **`OnPhaseStart(GamePhase phase)`**
    *   *Momento:* Logo após o `PhaseManager` alterar a fase atual.
    *   *LUA:* Dispara o `EVENT_PHASE (0x1000)`.
    *   *Uso Frequente:* Custos de manutenção na Standby Phase e limpeza de efeitos temporários na End Phase.
*   **`OnPreDrawPhase(bool isPlayerTurn, Action onContinue)`**
    *   *Momento:* Ocorre *antes* da compra normal da Draw Phase.
    *   *LUA:* Dispara o `EVENT_PREDRAW (1118)`.
    *   *Uso Frequente:* Utilizado por cartas "Pule sua Draw Phase para...". Em modo assíncrono, obriga a execução do delegate `onContinue?.Invoke()` se a compra não for negada.

### 5.2.2 Hooks de Batalha (Iniciados pela UI, resolvidos em Lua)
*   **O fluxo de ataque não usa mais hooks C# como `OnAttackDeclared`**. Em vez disso:
    1.  O clique do jogador em `CardDisplay` inicia a lógica de ataque.
    2.  `CardEffectManager` chama a corrotina `Core.Attack` em Lua.
    3.  O script Lua dispara o evento `EVENT_ATTACK_ANNOUNCE (1102)` internamente.
    4.  Isso permite que cartas no campo ou na mão (com efeitos rápidos) respondam à declaração de ataque.
*   **Cálculo de Dano:** O script `Core.Attack` chega ao passo de cálculo e dispara o `EVENT_DAMAGE_CALCULATING (1127)`. É a janela para efeitos que alteram ATK/DEF apenas durante o cálculo.
*   **Fim da Batalha:** Após o cálculo, o script Lua dispara `EVENT_BATTLE_END` e `EVENT_BATTLED`. Se um monstro foi destruído, `EVENT_BATTLE_DESTROYED (1131)` é disparado.

### 5.2.3 Hooks de Dano e Pontos de Vida (LP)
*   **`OnDamageTaken(bool isPlayer, int amount)`**
    *   *Momento:* Assim que um jogador sofre dano (batalha ou efeito).
    *   *LUA:* Dispara `EVENT_DAMAGE (1025)`.
*   **`OnLifePointsGained(bool isPlayer, int amount)`**
    *   *Momento:* Sempre que um jogador cura LPs.
    *   *LUA:* Dispara `EVENT_RECOVER (1026)`.

### 5.2.4 Hooks de Movimentação e Destruição de Cartas
*   **`OnCardSentToGraveyard(CardData, isOwnerPlayer, fromLocation, reason)`**
    *   *Momento:* Assim que uma carta aterrissa no GY.
    *   *LUA:* Dispara `EVENT_TO_GRAVE (1014)`.
    *   *Contexto:* Os parâmetros `fromLocation` e `reason` são passados para o evento Lua, permitindo que os scripts verifiquem "se esta carta foi enviada do campo para o cemitério por batalha".
*   **`OnCardLeavesField(CardDisplay card)`**
    *   *Momento:* Milissegundos antes da carta física ser destruída/banida/retornada.
    *   *LUA:* Dispara `EVENT_LEAVE_FIELD (1015)`.
    *   *Limpeza Vital:* Remove o `LuaCard` do cache `activeLuaCards`, libera zonas bloqueadas e destrói `CardLink`s de equipamento associados.
*   **`OnCardDiscarded(CardDisplay card, bool causedByOpponent)`**
    *   *Momento:* Quando uma carta é descartada da mão.
    *   *LUA:* Dispara `EVENT_DISCARD (1018)`.
*   **`OnCardDrawn(CardData card, bool isPlayer)`**
    *   *Momento:* Assim que a carta vai do Deck para a Mão.
    *   *LUA:* Dispara `EVENT_DRAW (1027)`.

### 5.2.5 Hooks de Estado de Campo e Magias
*   **`OnSummon(CardDisplay card)` / `OnSpecialSummon(CardDisplay card)`**
    *   *LUA:* Dispara `EVENT_SUMMON_SUCCESS (1104)` e `EVENT_SPSUMMON_SUCCESS (1105)`. Essencial para *Trap Hole*.
*   **`OnSet(CardDisplay card)`**
    *   *LUA:* Dispara `EVENT_MSET_SUCCESS (1107)` ou `EVENT_SSET_SUCCESS (1108)`.
*   **`OnBattlePositionChanged(CardDisplay card)`**
    *   *LUA:* Dispara `EVENT_CHANGE_POS (1016)`.
*   **`OnSpellActivated(CardDisplay spell)`**
    *   *LUA:* Dispara `EVENT_CHAIN_ACTIVATING (1021)`.
---

## 5.3 API de Helpers, Conversão Lua e Caixa de Ferramentas C#

Apesar da lógica ter sido movida para os scripts `.lua`, a Engine Unity não enxerga scripts, ela enxerga objetos 3D. Os métodos C# abaixo, a maioria no `GameManager`, atuam como os **motores reais** que movem as peças no tabuleiro. A `LuaAPI` chama esses métodos sob a camada (Under the hood) para criar a mágica que você vê na tela. Sempre que você chamar `Duel.Damage()` no arquivo Lua, ele transborda para o helper `Effect_DirectDamage()` documentado aqui.

### 5.3.1 Vida e Dano (LP)
*   **`Effect_DirectDamage(source, amount)`:** Causa dano de efeito. Implementado no `CardEffectManager`.
*   **`Effect_GainLP(source, amount)`:** Cura o jogador. Implementado no `CardEffectManager`.
*   **`Effect_PayLP(source, amount)`:** Paga um custo em LP. Implementado no `CardEffectManager`.

### 5.3.2 Buscas e Filtros no Tabuleiro
*   **`GetEquippedCards(CardDisplay target)`:** Retorna uma `List<CardDisplay>` de todas as cartas equipadas no monstro alvo.

### 5.3.3 Alteração de Status (ATK / DEF)
*   A lógica de modificadores foi transferida para o `CardDisplay` e não é mais exposta diretamente como um Helper de API. Os scripts Lua agora usam `Effect.CreateEffect` com `EFFECT_UPDATE_ATTACK`, e a engine C# aplica a matemática.

### 5.3.4 Manipulação de Deck, Mão e Cemitério
*   **`GameManager.Instance.MillCards(isPlayer, amount)`**
*   **`GameManager.Instance.BanishCard(CardDisplay card)`**
*   **`GameManager.Instance.ReturnToHand(CardDisplay card)`**
*   **`GameManager.Instance.ReturnToDeck(CardDisplay card, bool toTop)`**
*   **`Duel.Destroy(group)`:** O comando Lua é interpetado pela `LuaAPI` que então chama `GameManager.SendToGraveyard` para cada carta no grupo.

### 5.3.5 Lógicas Empacotadas (Subtipos)
*   **Fusão (`BeginFusionSummon`):** Abre a `FusionUI`.
*   **Ritual (`BeginRitualSummon`):** Abre a `RitualUI`.

### 5.3.6 Interações de Contra-Ataque e Negação (Counter Traps)
*   A negação agora é feita diretamente no `ChainLink` (`link.isActivationNegated = true`), não havendo mais helpers C# como `GetLinkToNegate` ou `NegateAndDestroy`.

### 5.3.7 Fichas, Armadilhas e Miscelânea
*   **`GameManager.Instance.SpawnToken(...)`**
*   **`GameManager.Instance.ConvertTrapToMonster(...)`** (Lógica agora implementada em Lua)

---

## 5.4 Dicionário Global de Estados e Variáveis de Memória

Consulte este dicionário antes de criar variáveis novas. O jogo já rastreia o estado da partida exaustivamente.

### 5.4.1 Em `CardDisplay` (Estado Local da Instância)
Acesse via `cardDisplay.nomeDaVariavel`. Pertence individualmente a cada carta instanciada.
*   **Batalha:**
    *   `hasAttackedThisTurn`: Marca se concluiu 1 ataque.
    *   `position` (`BattlePosition` enum): Armazena se a carta está em `Attack` ou `Defense`.
*   **Restrições e Estado:**
    *   `isFlipped`: `true` se a carta está virada para baixo.
    *   `isInteractable`: `true` para cartas na mão do jogador, permitindo o efeito de hover e clique.
    *   `isOnField`: `true` se a carta está em uma zona de Monstro ou Magia/Armadilha.
*   **Stats Dinâmicos:**
    *   `originalAtk` / `originalDef`: Os stats base da carta.
    *   `currentAtk` / `currentDef`: Os stats atuais após modificadores.
*   **Timers e Contadores:**
    *   `turnCounter` / `maxTurnCounter`: O relógio regressivo/progressivo da carta que integra com a UI do `TurnClockUI`.
*   **Histórico (para Efeitos de Warp):**
    *   `previousLocation`, `previousOwner`, `previousPreviousLocation`: Rastreiam as zonas e o dono anterior da carta, para efeitos que dependem de onde a carta veio.

### 5.4.2 Em `CardEffectManager` (Memória Global e Condições de Campo)
Acesse via `CardEffectManager.Instance.nomeDaVariavel`. Ditam leis e interceptações temporárias, muitas são para compatibilidade com a API Lua.
*   **Estado da Corrente:**
    *   `currentChain` (`List<ChainLink>`): A pilha de efeitos que está sendo construída ou resolvida.
    *   `isChainResolving` (bool): `true` se a corrente está na fase de resolução (LIFO).
*   **Dicionários e Listas Pendentes:**
    *   `blockedZonesByCard` (`Dictionary<CardDisplay, List<Transform>>`): Zonas físicas trancadas e inutilizáveis (*Ojama King*).
    *   `activeLuaCards` (`Dictionary<CardDisplay, LuaCard>`): Cache de scripts Lua já carregados para as cartas em campo, evitando releitura do disco.

### 5.4.3 Em `GameManager` (Estado Central da Partida)
*   **Dados do Duelo:**
    *   `playerLP`, `opponentLP`: Pontos de vida.
    *   `isPlayerTurn` (bool): `true` se for o turno do jogador.
    *   `turnCount`: Número de turnos passados.
    *   `isDuelOver` (bool): `true` quando o duelo acaba, bloqueando a maioria das ações.
*   **Contadores de Turno:**
    *   `lpPaidThisTurnPlayer`, `lpPaidLastTurnPlayer`: Usado para cálculos de *Life Absorbing Machine*.
    *   `normalSummonsThisTurnPlayer`: Conta quantas invocações normais foram feitas no turno.
*   **Listas e Pilhas:**
    *   `playerHand`, `opponentHand`: `List<GameObject>` com as cartas na mão.
    *   `playerGraveyard`, `opponentGraveyard`, etc.: `List<CardData>` para as pilhas.
*   **Estado de UI:**
    *   `isSelectingFromHand`: Bloqueio de UI durante a "Seleção Direta" de cartas.

---

## 5.5 Sistemas Complexos e Efeitos Contínuos

### 5.5.1 Efeitos Contínuos e Restrições Globais (Locks)
*   **Implementação:** Efeitos contínuos são implementados em Lua com `EFFECT_TYPE_FIELD`. Eles são aplicados uma vez quando a carta entra em campo e seus efeitos (ex: `EFFECT_CANNOT_ATTACK`) são registrados na engine. A verificação da restrição é feita no momento da ação (ex: `CardDisplay.OnPointerClick` verifica se pode atacar).
*   **Exemplos:**
    *   *Skill Drain*: Seu script Lua registra um efeito que nega os efeitos de outros monstros.
    *   *Gravity Bind / Level Limit:* Seus scripts Lua registram um efeito `EFFECT_CANNOT_ATTACK`. A verificação ocorre antes de um ataque ser declarado.

### 5.5.2 Efeitos Retardados (Delayed Effects)
Gerencia efeitos que não acontecem imediatamente.
*   **Implementação:** Geralmente usam um gatilho de fase (`EVENT_PHASE + PHASE_END`). Quando a `End Phase` começa, o `CardEffectManager` dispara o evento, e as cartas que estavam esperando por ele ativam seus `operation`.

### 5.5.3 Buffs Dinâmicos (Dynamic Stat Buffs)
Cartas cujos stats mudam a cada jogada (ex: *Buster Blader*, *Chaos Necromancer*).
*   **Lógica de Renovação:** O script Lua da carta registra um efeito contínuo que recalcula os stats. A função `operation` do efeito busca as cartas relevantes no campo/cemitério (`Duel.GetMatchingGroup`), faz a matemática, e usa `c:SetAttack` para atualizar o valor.

---

## 5.6 Interações de UI, Modais e Minigames

A Engine do jogo roda na Thread principal da Unity. Como as decisões exigem input humano, o jogo utiliza estritamente o padrão assíncrono de corrotinas.

### 5.6.1 O Padrão Assíncrono e a Mágica da Engine LUA (`YieldReq`)
Quando o Lua precisa de uma informação do jogador, ele não pode simplesmente parar e esperar. Ele "pede" para o C# e se suspende.
*   **Fluxo de `Duel.SelectTarget()`:**
    1.  O script Lua chama `Duel.SelectTarget(tp, filter, 1, 1, nil)`.
    2.  A `LuaAPI` em C# recebe essa chamada. Ela abre a UI de seleção de alvo para o jogador.
    3.  A `LuaAPI` retorna um `YieldReq`, que sinaliza ao `CardEffectManager` para suspender a corrotina Lua.
    4.  O `CardEffectManager` entra em um estado de espera (`isWaitingForLuaYield = true`).
    5.  Quando o jogador clica em um alvo válido, a UI chama um `callback`.
    6.  O C# armazena a carta selecionada em `yieldReturnValue` e define `isWaitingForLuaYield = false`.
    7.  O `CardEffectManager` detecta a mudança, resume a corrotina Lua, passando `yieldReturnValue` como o resultado da função que foi chamada. O script Lua então continua sua execução com o alvo que o jogador escolheu.

### 5.6.2 Tipos de Modais Oficiais da Engine
O programador de cartas não precisa criar UIs. Ele chama funções Lua (`Duel.SelectYesNo`, `Duel.SelectOption`, `Duel.AnnounceRace`) que são traduzidas pela `LuaAPI` para os modais C# correspondentes:
*   **Confirmação Simples (Sim/Não):** `UIManager.Instance.ShowConfirmation(...)`
*   **Seleção em Listas (Busca, GY, Mão):** `UIManager.Instance.ShowCardSelection(...)`
*   **Digitação de Nome de Carta:** `GlobalCardSearchUI.Instance.Show(...)`
*   **Textos e Escolhas Arbitrárias:** `MultipleChoiceUI.Instance.Show(...)`
*   **Teclado Numérico Interativo (Numpad):** `NumericSelectionUI.Instance.Show(...)`
*   **Painéis Visuais (Atributos, Raças, ATK/DEF):** `AnnounceSelectionUI.Instance.Show...(...)` (Substitui UIs genéricas por painéis iconográficos ricos).

### 5.6.3 O Bypass de IA e Simulação
*   Se o jogador que precisa fazer uma escolha for a IA (`player == 1`) ou o jogo estiver em simulação (`isSimulating`), a `LuaAPI` não abre a UI. Em vez disso, ela chama um método da IA (`OpponentAI.Instance.SelectLuaTargets`) ou escolhe a primeira opção válida para que o duelo prossiga sem interrupção.

### 5.6.4 Minigames de Sorte e Controle Visual
*   As chamadas Lua `Duel.TossCoin` e `Duel.RollDice` são mapeadas para os métodos do `GameManager`, que abrem as UIs interativas dos minigames.

### 5.6.5 Tabela de Valores de Declaração (Announce)
Quando o motor LUA utiliza funções como `Duel.AnnounceAttribute` ou `Duel.AnnounceRace`, a Unity abre o `AnnounceSelectionUI`. Os botões da interface não retornam strings, mas sim valores numéricos (potências de 2, lógicas Bitwise) que o LUA compreende perfeitamente.

**Valores de Atributo (Attribute):**
*   Terra (EARTH): `1` | Água (WATER): `2` | Fogo (FIRE): `4` | Vento (WIND): `8`
*   Trevas (DARK): `16` | Luz (LIGHT): `32` | Divino (DIVINE): `64`

**Valores de Raça / Tipo (Race):**
*   Warrior: `1` | Spellcaster: `2` | Fairy: `4` | Fiend: `8`
*   Zombie: `16` | Machine: `32` | Aqua: `64` | Pyro: `128`
*   Rock: `256` | Winged Beast: `512` | Plant: `1024` | Insect: `2048`
*   Thunder: `4096` | Dragon: `8192` | Beast: `16384` | Beast-Warrior: `32768`
*   Dinosaur: `65536` | Fish: `131072` | Sea Serpent: `262144` | Reptile: `524288`

---

## 5.7 Ferramenta de Validação em Massa (Mass Validator)

Para garantir a estabilidade da Engine Lua, o `CardEffectManager` possui uma ferramenta de validação acessível pelo menu de contexto no Inspector da Unity.

### 5.7.1 Como Executar
1. Com o jogo rodando (Play Mode), selecione o `CardEffectManager`.
2. Clique com o botão direito no script e escolha **"DEV: Validar Todos os Scripts LUA"**.

### 5.7.2 As Duas Fases de Validação
O script processa todas as cartas com efeito do banco de dados em duas barreiras:

1. **Validação de Compile-Time (Sintaxe e Tradução):**
   A Engine lê o arquivo `.lua`, passa pelo `SanitizeOCGScript` e tenta compilar na Máquina Virtual. Isso garante que a sintaxe seja compatível.
2. **Validação de Runtime (Dry-Run / Teste Profundo):**
   Após carregar, o validador executa um "Dry-Run" em todos os Efeitos registrados, chamando `condition`, `cost` e `target` com `chk = 0`. Isso força a carta a fazer perguntas à `LuaAPI` sem abrir UIs, revelando chamadas para funções que não existem ou que possuem a assinatura errada.

O resultado é impresso no Console, com um resumo de sucessos/falhas e um log detalhado dos erros.

---

## 5.8 Casos de Estudo e Soluções Arquiteturais (Breakthroughs)
Esta seção documenta como os desafios mais complexos de integração entre o C# e o LUA foram resolvidos, servindo como modelo para desenvolver novas mecânicas.

### 5.8.1 A Integração Híbrida (Ex: Carta Mágica "7")
Algumas cartas exigem UIs cinemáticas massivas na Unity que o LUA nativo não compreende.
*   **O Problema:** A carta "7" precisava invocar a UI do "Caça-Níqueis" e, se bem sucedida, destruir a si mesma e curar o jogador.
*   **A Solução Híbrida:** O C# possui um "Bypass" rígido na função `ActivateCard()`. Quando a carta "7" é ativada, a Unity "sequestra" a ação e toca a cinemática, destruindo as cartas. Como o C# manda essas cartas para o Cemitério usando `MoveCard`, o motor LUA global capta o `EVENT_TO_GRAVE` e a própria carta "7" roda a parte final do seu script Lua de forma autônoma para curar os 700 LP.

### 5.8.2 A Visão do LUA sobre Rituais, Fusões e o "Extra Deck"
O LUA do YGOPro geralmente invoca Fusões forçando uma interface fria. Nós queríamos que o jogador clicasse taticamente nos materiais no tabuleiro 3D.
*   **O Problema:** O Extra Deck é uma pilha invisível, o jogador não pode clicar nela taticamente. E o LUA dava "crash" (`attempt to call a nil value`) se não recebesse a assinatura exata ao verificar monstros.
*   **A Solução (Fusão/Ritual):** O `GameManager` intercepta a ativação da *Polymerization*. O C# abre a interface Clássica de Grade (`CardSelectionUI`) **apenas** mostrando o Extra Deck, permitindo ao jogador escolher o Monstro de Fusão primeiro. Depois de confirmado, o C# filtra a mão/campo e acende a *Seleção Tática Direta* nos materiais. 
*   **A Ponte LUA:** Para o LUA confirmar que um monstro era "Invocável" do Extra Deck sem crachar, a função base `IsCanBeSpecialSummoned` no `LuaCard.cs` foi atualizada de forma agressiva utilizando o parâmetro C# `params object[] extraArgs`. Isso fez com que a Unity absorvesse qualquer quantidade infinita de parâmetros extras que o LUA resolvesse enviar, silenciando exceções.

### 5.8.3 Memória de Turno, Preload e Efeitos Globais (Ex: "A Deal with Dark Ruler")
Cartas que dependem de ações passadas no turno (Ex: "Se um Nível 8 morreu neste turno, invoque o Berserk Dragon") precisam estar constantemente vigiando a partida.
*   **O Problema:** O Unity só carregava o script `.lua` da magia no momento em que o jogador clicava nela. Como ela estava inativa quando o dragão morreu, ela perdia o momento e recusava a ativação.
*   **O Preload:** Foi introduzido o `PreloadScriptsForDecks()`. Assim que o duelo inicia, a Unity lê todas as cartas em ambos os decks e registra discretamente seus `EFFECT_TYPE_FIELD` (Efeitos Globais Invisíveis) em uma lista `globalEffects` no `LuaDuel`.
*   **As Flags (`playerFlags`):** A API agora suporta `RegisterFlagEffect` e `GetFlagEffect`. O motor LUA carimba um "selo" temporário invisível no jogador (ex: "ID 6850209 ativado") assim que um Level 8 morre. Quando a magia é ativada, ela verifica a Flag e a encontra lá. A lista de Flags é zerada na `End Phase`!

### 5.8.4 Monstros Normais e a Identidade "Dummy"
Monstros Normais (como *Blue-Eyes White Dragon*) não possuem arquivo de script `.lua`.
*   **O Problema:** Quando o Blue-Eyes morria, o `LuaEventManager` tentava carregar seu script, falhava e retornava `null`. O sistema ignorava o evento, e as *Flags Globais* do LUA não ficavam sabendo que um monstro Nível 8 morreu.
*   **O Fallback de Identidade:** Agora, no `LuaEventManager.cs`, se um script não for encontrado, a Unity constrói e veste o monstro com um `new LuaCard(card)`. O monstro não tem efeito nenhum, mas ele adquire uma casca virtual contendo seu Level, ATK, e ID. Ele é submetido aos gatilhos (como `EVENT_TO_GRAVE`), permitindo que a engrenagem do LUA global rastreie mortes de cartas normais impecavelmente!

### 5.8.5 Injeção Inteligente de Dependências (Quality Assurance)
O motor precisa ser rápido de testar, sem o desenvolvedor precisar criar Decks de 40 cartas meticulosos.
*   **O Problema:** No `FullTestManager`, ao forçarmos o *A Deal with Dark Ruler* na mão para testar, ele falhava silenciosamente porque a Unity abria o Deck do jogador e percebia que havia `0` *Berserk Dragons* lá dentro para invocar (bloqueando a ativação por respeito à regra).
*   **Injeção Dinâmica (Regex):** O `FullTestManager` agora intercepta o evento de *Spawnar* cartas. Ele usa Expressões Regulares (`Regex`) para varrer a string `description` da carta em busca de nomes entre aspas duplas (Ex: `"Berserk Dragon"`). Se ele acha uma dependência declarada, o C# procura essa carta no Banco de Dados e adiciona discretamente 1 cópia dentro do Deck Principal ou Extra Deck do jogador, garantindo que os motores LUA tenham a peça exata necessária para a carta funcionar!

### 5.8.6 Interceptação de UI Customizada pelo LUA (Ex: "7 Completed")
Algumas cartas exigem escolhas de jogador (Ex: Escolher entre aplicar o buff em ATK ou DEF). O LUA nativo lida com isso chamando a função de listas de texto simples (`Duel.SelectOption`).
*   **Filtragem do Tabuleiro:** Para garantir que "7 Completed" só seja equipada em Máquinas, o script `.lua` usa a rotina padrão `aux.AddEquipProcedure`, passando `c:IsRace(RACE_MACHINE)` como filtro. A Engine Unity cuida de acender o `Outline` e permitir o clique apenas em máquinas válidas no campo através da comunicação natural com `IsExistingTarget`.
*   **O "Sequestro" da UI (`LuaDuel.cs`):** Quando o LUA tenta chamar `Duel.SelectOption` passando os IDs de string que representam as opções do "7 Completed" (usando a matemática do ID da carta `86198326`), o método C# identifica essa assinatura específica através de um `if (isAtkDefChoice)`.
*   **O Painel Tático Unificado:** Ao invés de abrir a UI de múltipla escolha padrão (`MultipleChoiceUI`), a Unity intercepta a chamada e abre o nosso gerenciador universal `AnnounceSelectionUI` no modo ATK/DEF.
*   **O Retorno LUA e a Aplicação do Buff:** O painel pausa a corrotina do LUA (`isWaitingForLuaYield = true`) e aguarda o clique. Quando o jogador escolhe, a Unity devolve `0` (ATK) ou `1` (DEF) via `yieldReturnValue`. O script LUA recebe esse número e registra, de fato, um `EFFECT_UPDATE_ATTACK` ou `EFFECT_UPDATE_DEFENSE` na carta alvo. Por debaixo dos panos, o evento dispara o método C# `CardEffectManager.RecalculateStats()`, que escaneia a carta, lê o novo modificador invisível do LUA e aplica os `+700` no número de Status flutuante do monstro 3D!

### 5.8.8 O Desafio das Duplicatas (Seleção e Ativação Múltipla)
Cartas como "A Deal with Dark Ruler" podem ser ativadas várias vezes no mesmo turno, mas o sistema apresentava dois bugs críticos com cópias múltiplas.
*   **O Problema da Seleção:** Ao injetar 3 "Berserk Dragon" no deck, a UI de seleção (`CardSelectionUI`) mostrava apenas 1. Isso ocorria porque a UI usava uma lista de `CardData` e o método `Contains()` para verificar a seleção. Como as 3 cópias eram referências ao mesmo objeto `CardData` do banco de dados, o sistema as via como uma única entidade.
*   **A Solução da Seleção:** A `CardSelectionUI` foi refatorada para usar uma lista de `CardDisplay` (as cartas visuais) em vez de `CardData`. Como cada carta na UI é um `GameObject` único, o sistema agora consegue diferenciar as cópias perfeitamente, exibindo todas as 3 na tela.
*   **O Problema da Ativação:** A segunda e terceira cópia da magia falhavam. O `LuaScriptLoader` estava recarregando o script e resetando a "memória de turno" (`s[0]=false`) toda vez que uma nova cópia era puxada para a mão.
*   **A Solução da Ativação (Cache LUA):** O `LuaScriptLoader` foi transformado em um sistema de Cache. Agora, ele verifica se o script (`cXXXXX.lua`) já foi carregado na memória do LUA. Se sim, ele reutiliza a tabela de funções existente em vez de recriá-la. Isso garante que todas as 3 cópias da magia compartilhem o mesmo "cérebro" e a mesma memória de turno, permitindo ativações múltiplas.

### 5.8.9 O Problema de Identidade (IDs Customizados vs. Oficiais)
O motor LUA depende estritamente dos IDs numéricos oficiais do YGOPro (ex: `85605684` para Berserk Dragon), enquanto nosso banco de dados usa IDs customizados (ex: `DM0165`).
*   **O Problema:** A função `c:IsCode(85605684)` no LUA falhava porque a nossa implementação C# de `GetCode()` retornava o ID do nosso banco (`165`), e `165` não é igual a `85605684`.
*   **A Solução (Mapeamento Reverso):** A função `IsCode()` no `LuaCard.cs` foi blindada. Agora, além de comparar os IDs numéricos, ela faz uma busca reversa: pega o código oficial (`85605684`), procura no `CardDatabase` qual carta possui essa `password`, pega o **nome** dessa carta ("Berserk Dragon") e compara com o nome da carta que está sendo avaliada no deck. Isso cria uma ponte universal e à prova de falhas entre nossos IDs e os do OCGCore.

### 5.8.10 O Iterador Silencioso e a Limpeza de Memória (`aux.Next` e `aux.AddValuesReset`)
O script de "A Deal with Dark Ruler" usa duas funções auxiliares (`aux`) cruciais que não estavam implementadas.
*   **O Problema:** A função `checkop` do script usa um loop `for tc in aux.Next(eg) do` para verificar cada carta que foi para o cemitério. Nossa implementação de `aux.Next` era um *stub* vazio que retornava `nil`, fazendo o loop nunca executar. Consequentemente, a "flag" de que um Nível 8 morreu nunca era registrada.
*   **A Solução do Iterador:** O método `aux.Next` em `LuaEngineCore.cs` foi implementado para retornar um iterador real (`g.Iter()`) compatível com o MoonSharp, permitindo que o LUA percorra os grupos de cartas corretamente.
*   **O Problema da Limpeza:** O script também chama `aux.AddValuesReset` para registrar uma função que deve ser executada no final do turno (para resetar a flag `s[tp]=false`). Sem isso, a magia ficaria ativável para sempre após a primeira morte de um Nível 8.
*   **A Solução da Limpeza:** Implementamos o `AddValuesReset` para adicionar a função LUA a uma lista `endTurnCallbacks` no `LuaDuel`. O `LuaEventManager`, no gancho `OnPhaseStart(GamePhase.End)`, agora percorre e executa todas as funções registradas nessa lista, garantindo que a memória de turno seja limpa corretamente.

### 5.8.11 O Desafio das Auras Globais e Nível Dinâmico (Ex: A Legendary Ocean)
Magias de Campo e Efeitos Contínuos que afetam a Mão e o Campo simultaneamente traziam dessincronização entre UI e a Engine LUA.
*   **O Problema do Nível e do Enum:** Cartas de Água na mão não recebiam a redução de Nível. O motivo era o `CardLocation.Hand` ter valor `0` no C#, o que quebrava a matemática Bitwise `(location & affectedLocation)`. Transformamos o Enum `CardLocation` em `[System.Flags]`, permitindo que as Auras localizassem cartas na mão perfeitamente.
*   **A "Burrice" do Action Menu:** A interface de ações (`DuelActionMenu`) lia o Nível Base do banco de dados (5) em vez do Nível Dinâmico (4). Isso fazia a UI esconder o botão "Summon" para monstros que não precisavam mais de sacrifício. A UI foi atualizada para consultar o `GlobalAuraManager` antes de renderizar os botões.
*   **Injeção do Nível no Lua:** Mesmo com a UI corrigida, a função `Core.NormalSummon` no Lua exigia tributo porque lia a propriedade dura da carta. O GameManager foi alterado para passar o `dynamicLevel` processado no C# como um argumento extra (injetado) direto para a função LUA, evitando dupla interpretação.
*   **Filtros de Alvo Originais:** A magia usava funções auxiliares do YGOPro (`aux.TargetBoolFunction`) que não existiam no nosso `LuaEngineCore`. O C# foi atualizado com as Closures em texto dessas funções para permitir que os scripts LUA nativos executem a lógica de "Quem deve receber o buff".
*   **A Sobrevivência da Corrente (Crash Yield):** Durante testes de tributo interrompidos (cancelamento da caixa), o C# devolvia `null` para o LUA, causando crash fatal de referência. Blindamos o `RunGenericLuaCoroutine` com `yieldReturnValue ?? DynValue.Nil` para garantir que um valor vazio caia suavemente no interpretador MoonSharp.

### 5.8.7 Stubs de Compatibilidade e Efeitos de Arquétipo (Ex: Monstros "Spirit")
O motor OCGCore utiliza funções auxiliares para registrar efeitos comuns a um arquétipo (Ex: `Spirit.AddProcedure(c)` para o efeito de retornar à mão).
*   **O Problema:** Durante o `PreloadScriptsForDecks`, o motor LUA tentava chamar `Spirit.AddProcedure` para cartas como "Inaba White Rabbit", mas a classe C# `Spirit` estava vazia, causando um crash de "método não encontrado".
*   **A Solução (Stubs Funcionais):** Foi criado o arquivo `LuaProcsAndStubs.cs` para abrigar essas funções auxiliares. A função `Spirit.AddProcedure` agora existe e, em vez de conter a lógica de retorno (que já é tratada pelo C# no `PhaseManager`), ela simplesmente registra um efeito com um código customizado (`511002963`) na carta. Isso serve como um "carimbo" para que a engine C# saiba que aquela carta é um monstro Spirit e deve aplicar a regra de retorno na End Phase, prevenindo o crash e garantindo a funcionalidade correta.

### 5.8.12 A Referência Viva e a Reciclagem de Objetos 3D (Ex: A Wingbeat of Giant Dragon)
Muitas cartas exigem devolver uma carta para a mão como custo e, *logo em seguida*, testam se a carta realmente chegou lá antes de prosseguir com a destruição em massa.
*   **O Problema (O Clone Morto):** O script LUA possuía a condição exata `if tc:IsLocation(LOCATION_HAND) then`. Na arquitetura antiga, o método `ReturnToHand` no C# pegava a carta da mesa, disparava `Destroy(gameObject)` e instanciava um "clone" 3D idêntico na mão. Como o objeto original foi apagado da RAM, o LUA perdia a referência na memória (`tc` virava um ponteiro morto/nulo) e respondia `False` para a checagem, fazendo o furacão falhar silenciosamente.
*   **A Solução (Reciclagem Física):** A arquitetura do `GameManager` foi revolucionada. Métodos como `ReturnToHand` (e o unificado `MoveCard`) pararam de destruir GameObjects. Agora, eles apenas alteram a árvore hierárquica (`SetParent`) da carta para o Layout da Mão, desligam a flag `isOnField` e acionam a corrotina de voo no *mesmo objeto físico*.
*   **O Impacto:** Essa sacada genial preservou as referências vivas do motor LUA (`userdata`), garantindo que scripts complexos originais do OCGCore validem suas condições 100% corretamente. De bônus, reduziu drasticamente os picos de processamento (Garbage Collection) ao erradicar a necessidade de Instanciar/Destruir prefabs constantemente durante partidas longas.

### 5.8.13 A Regra de Ouro do "Juiz Imparcial" e o Auto-Shuffle (Ex: Abyssal Designator, Reinforcement of the Army)
Em simuladores digitais modernos (Master Duel, YGOPro), a Engine atua como um Juiz Imparcial inquestionável.
*   **O Problema da Auditoria:** No jogo físico, ao procurar uma carta ou declarar um alvo no deck, o oponente tem o direito de "auditar" o baralho para garantir que não há trapaça. Nos videogames, a Engine vasculha a lista de cartas nas sombras (sem abrir UI). Cartas como *Abyssal Designator* forçam a IA a descartar alvos válidos passivamente e, se a IA não tiver a carta, a engine apenas encerra o efeito (pois o computador não mente).
*   **A Regra do Embaralhamento:** Sempre que um Deck é vasculhado, pesquisado ou tem uma carta extraída, ele DEVE ser embaralhado logo após a resolução.
*   **A Solução (Gatilho de Baixo Nível):** Para não precisarmos alterar centenas de scripts `.lua` adicionando o comando de embaralhar manualmente, implementamos o **Auto-Shuffle** direto no núcleo do C# (`RemoveDataFromAllPiles` na `LuaDuel.cs`). Se qualquer `CardData` for removido de `GetPlayerDeck()` ou `GetOpponentDeck()`, a Engine dispara `GameManager.Instance.ShuffleDeck(isPlayer)` imediatamente após a extração.
*   **O Impacto:** O jogo simula perfeitamente as regras rigorosas de integridade de torneios do TCG sem corromper a base de dados mundial dos scripts LUA originais do OCGCore.

### 5.8.15 O Desafio dos Arquétipos (IsSetCard) e a Injeção JSON
No formato original, a verificação de Arquétipos (Ex: *Amazoness Spellcaster* buscando monstros "Amazoness" no tabuleiro) era feita através da função Lua de consulta de catálogo `c:IsSetCard(0x04)`. 
*   **O Problema:** Como nosso banco de dados clássico inicialmente não possuía mapeamento formal de Arquétipos, o método `IsSetCard` no C# tinha um fallback engessado: `return true;`. Isso "enganava" a engine Lua dizendo que *todas* as cartas pertenciam a *todos* os arquétipos, causando bugs de mira cruzada bizarros (como uma magia suportar o campo do oponente inteiro por acidente).
*   **A Solução (Typeline e Archetype):** O extrator Python (`download_cards_ultimate.py`) foi atualizado para raspar a propriedade `archetype` crua da API oficial e injetá-la nas propriedades nativas do `CardData`. 
*   **O Mapeamento Seguro:** O C# foi reescrito. Agora `IsSetCard` traduz o código hexadecimal que o Lua pede (ex: `0x04`) comparando a restrição de maneira robusta com a string salva na memória da Unity (`if (code == 0x04 && arch.Contains("Amazoness")) return true;`).
*   **Stubs de Retrocompatibilidade:** Para garantir suporte inquebrável a scripts Lua da comunidade escritos em épocas diferentes (OCGCore antigo vs. moderno), os métodos C# auxiliares `IsHasSetcode` e `IsHasSetCard` foram criados na `LuaCard.cs` como *Stubs*. Eles redirecionam silenciosamente as chamadas duplicadas para o `IsSetCard` principal, evitando crashes mortais (`attempt to call a nil value`).

### 5.8.14 A Memória Interna dos Efeitos (SetLabel e GetLabel)
Muitos scripts LUA precisam armazenar escolhas feitas pelo jogador (como declarar um Nível, Raça ou Atributo em uma janela da UI) para usá-las milissegundos depois dentro de uma função de filtro (`s.filter`).
*   **O Problema (Amnésia):** Cartas como *Abyssal Designator* usavam `e:SetLabel(att)` para guardar o atributo escolhido. Como a implementação C# de `LuaEffect.cs` possuía apenas Stubs vazios para esses métodos, a variável era descartada, e o filtro recebia `0`, falhando silenciosamente na hora de procurar a carta no deck.
*   **A Solução:** Implementação das propriedades internas `_label` e `_labelObject` no `LuaEffect.cs` e a correção do despachante `GetChainInfo` no `LuaDuel.cs` para transportar o `targetParam` e `targetPlayer` corretamente.
*   **O Impacto:** A Engine tornou-se capaz de sustentar o "Contexto" (Context State) de uma carta, permitindo que a IA ou o jogador retenham escolhas arbitrárias na RAM e apliquem filtros precisos sem modificar a estrutura OCGCore original.

---

## 5.9 Core Pillars e A Regra Brutal das Constantes (Metatable Interceptor)

A estabilidade do motor LUA depende de uma regra de design inquebrável: **Jamais assuma que a matemática de um script falhou sem antes verificar se a Unity conhece as palavras contidas nela.** O LUA não avisa naturalmente quando uma variável global não existe; ele a trata como nula (`nil`). Tentar fazer operações matemáticas com nulos (ex: `STATUS_SUMMON_TURN + nil`) causa um erro fatal silencioso na Engine, abortando o script por completo.

### 5.9.1 O Detector Universal (A Regra Brutal)
Para erradicar a caça a fantasmas (onde um script LUA trava e o desenvolvedor passa horas depurando funções C# em vão), injetamos um interceptador implacável na metatabela global (`_G`) através do script `LuaEngineCore.cs`:

```lua
setmetatable(_G, {
    __index = function(t, k)
        if type(k) == 'string' and string.match(k, '^[A-Z0-9_]+$') then 
            Log('<color=red>[LUA MISSING CONSTANT]</color> A constante ' .. k .. ' não está definida na Engine C#! Retornando 0 (NIL) como Fallback.')
            return 0 
        end
        if type(k) == 'string' and string.match(k, '^[A-Z][a-zA-Z0-9_]*$') then 
            Log('<color=red>[LUA MISSING STUB]</color> A Classe ' .. k .. ' não está definida na Engine C#! Retornando Dummy.')
            return dummyTable 
        end
        return nil
    end
})
```

### 5.9.2 Protocolo de Resolução e Mitigação
*   **Monitoramento Implacável:** Se o Console da Unity gritar `<color=red>[LUA MISSING CONSTANT] NOME_DA_VARIAVEL</color>`, a prioridade máxima e imediata do desenvolvedor é abrir o arquivo original `constant.lua` do YGOPro (incluído no projeto como referência/cheat sheet), descobrir o valor numérico ou hexadecimal daquela constante, e injetá-la imediatamente no método `InjectVitalConstants()` da classe `LuaEngineCore.cs`.
*   **Mitigação de Stubs (Funções Vazias):** Da mesma forma, se a engine alertar `<color=red>[LUA MISSING STUB] NomeDaClasse</color>` (ex: uma carta chamou `Coin.Toss()`), significa que uma classe de procedimento nativa em C# não foi exportada para o Lua. A Engine C# agora devolve uma tabela inofensiva (`dummyTable`) blindada contra crasches. No entanto, a prioridade do projeto é **diminuir a quantidade de Stubs Vazios**. Verifique o que a classe ausente estava tentando fazer e mapeie o comportamento visual ou lógico corretamente na Unity!

---

## 5.10 Dicionário OCGCore: A Anatomia das Variáveis LUA

Ao ler ou portar scripts nativos do YGOPro/EDOPro, você encontrará abreviações padronizadas exaustivamente utilizadas pela comunidade. Para intervir no LUA com precisão, a equipe deve compreender o significado exato de cada sigla no ecossistema:

*   **`e` (Effect):** A classe do Efeito atual que está sendo avaliado, ativado ou resolvido (`LuaEffect`).
*   **`tp` (Trigger Player / Turn Player):** O índice do jogador (0 = Você, 1 = Oponente) que ativou o efeito ou de quem é o turno ativo.
*   **`eg` (Event Group):** O Grupo de cartas envolvidas no evento que disparou este efeito (`LuaGroup`). Ex: a lista de monstros destruídos por um *Dark Hole*.
*   **`ep` (Event Player):** O jogador envolvido no evento disparador. Ex: o jogador que tomou o dano de batalha.
*   **`ev` (Event Value):** O valor numérico atrelado ao evento. Ex: a quantidade exata de dano recebido ou os LPs ganhos.
*   **`re` (Reason Effect):** O Efeito (a Carta) que causou o evento.
*   **`r` (Reason):** O motivo (código Bitwise) pelo qual o evento ocorreu (Ex: `REASON_BATTLE`, `REASON_EFFECT`, `REASON_COST`).
*   **`rp` (Reason Player):** O jogador responsável por causar o evento.
*   **`c` (Card):** A própria carta que é "dona" do script (equivalente ao `this` no C# ou o `GetHandler()`).
*   **`tc` (Target Card):** Uma carta alvo específica, tipicamente nomeada assim dentro de loops de iteração ou resgatada de um grupo (`GetFirst()`).
*   **`g` (Group):** Um grupo genérico de cartas recém instanciado ou filtrado (`LuaGroup`).
*   **`sg` (Selected Group / Sub Group):** O grupo final e irreversível de cartas que foi selecionado pelo jogador após um filtro ou UI (`SelectTarget`).
*   **`mg` (Material Group):** O grupo de cartas disponíveis ou já utilizadas como Materiais (Fusões, Rituais).
*   **`chk` (Check):** A flag binária mais crítica da Engine, definindo a Dry-Run de uma Corrente:
    *   `chk == 0`: "Validação Silenciosa". A Engine C# apenas quer saber se o efeito **pode** ser ativado (Verifica custos, alvos no deck). **Não modifique a partida nem abra UIs sob esta flag.**
    *   `chk == 1`: "Ativação Real". A Engine já entrou na Corrente e está ordenando que o jogador abra UIs, selecione alvos e pague custos.
*   **`val` (Value):** Variável temporária de Valor Numérico gerada sob demanda.
*   **`id` (ID):** O código numérico (Password) da carta.
*   **`s` (Script):** Uma referência à tabela estática do próprio script LUA. No design moderno (YGOPro), substitui a injeção via `cXXXXX` para hospedar funções locais (`s.filter`).