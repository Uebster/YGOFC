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
*   **`LuaEngineCore.cs` (A Fundação do Interpretador):** Responsável por carregar o MoonSharp, injetar as classes C# e registrar centenas de constantes LUA na memória (como IDs de eventos e zonas).
*   **`ChainManager.cs` (O Motor de Pilhas LIFO):** Extensão modular instanciada pelo CardEffectManager. Cuida exclusivamente das Janelas de Corrente, orquestrando as validações, a limpeza de Mágicas/Traps após o fim da corrente e emitindo os Callbacks da interface gráfica para o oponente responder (`ResponseWindowRoutine`).
*   **`LuaEventManager.cs` (Os Olhos e Ouvidos):** Extrai todos os gatilhos e escutas do motor. Funções como `OnSummon` e `OnCardLeavesField` moram aqui, vigiando o jogo e ativando as cartas que estavam escutando silenciosamente (`TriggerLuaEvent`).
*   **Pasta `LuaAPI/` (A Ponte C# <-> LUA):** Diretório contendo os arquivos independentes que expõem as lógicas do C# para os scripts de cartas:
    *   **`LuaDuel.cs`** (`Duel.`): Ações de tabuleiro (`Duel.Damage`, `Duel.SelectTarget`).
    *   **`LuaCard.cs`** (`c:`): A representação física da carta (`c:GetAttack()`).
    *   **`LuaEffect.cs`** (`Effect.`): Contêiner que monta a habilidade (`Condition`, `Cost`).
    *   **`LuaGroup.cs`** (`Group` ou `eg`): Listas dinâmicas usadas em invocações e destruições em massa.
    *   **`LuaProcsAndStubs.cs`**: Implementações simuladas (Stubs) de classes processuais como `Fusion`, `Synchro` e dependências exclusivas do analisador Python.
*   **`Assets/Scripts/LuaScripts/`:** O diretório que contém os scripts de lógica para cada carta, nomeados por sua ID (ex: `c0618.lua` para o *Exodia*).

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

### 5.6.3 O Bypass de IA e Simulação
*   Se o jogador que precisa fazer uma escolha for a IA (`player == 1`) ou o jogo estiver em simulação (`isSimulating`), a `LuaAPI` não abre a UI. Em vez disso, ela chama um método da IA (`OpponentAI.Instance.SelectLuaTargets`) ou escolhe a primeira opção válida para que o duelo prossiga sem interrupção.

### 5.6.4 Minigames de Sorte e Controle Visual
*   As chamadas Lua `Duel.TossCoin` e `Duel.RollDice` são mapeadas para os métodos do `GameManager`, que abrem as UIs interativas dos minigames.

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