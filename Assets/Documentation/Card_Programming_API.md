# 5. A Bíblia de Programação de Cartas (Card Programming API)

## Visão Geral
Este documento é o guia absoluto e exaustivo para programar e interagir com os efeitos das 2147 cartas do jogo. Ele unifica a arquitetura do sistema, os Gatilhos (Hooks), Funções Auxiliares (Helpers), o Dicionário de Variáveis de Estado (Flags), as lógicas de interrupções de UI e subsistemas complexos. 

**[REVOLUÇÃO DE ARQUITETURA]:** O projeto migrou de um modelo engessado de métodos C# Hardcoded (antigo `CardEffectManager_Impl`) para um motor dinâmico e flexível baseado em scripts **LUA (MoonSharp)**, emulando perfeitamente a API oficial do YGOPro (OCGCore).

---

## 5.1 Arquitetura do Sistema de Efeitos (`CardEffectManager` e MoonSharp)

O `CardEffectManager` é a Máquina Virtual central do jogo. Ele hospeda o interpretador Lua e atua como o regente da orquestra, delegando a inteligência matemática da carta para o script de texto e executando os gráficos/status no C#.

### 5.1.1 Estrutura de Arquivos e Componentes
*   **`CardEffectManager.cs` (A Máquina Virtual):** Singleton principal. Inicializa a `luaEngine`, gerencia o cache de memória das cartas na mesa (`activeLuaCards`) e comanda as Corrotinas que abrigam os efeitos em resolução (`pendingResolutions`).
*   **`LuaAPI.cs` (A Ponte C# <-> LUA):** Um "Wrapper" maciço exposto para a máquina virtual. Ele mapeia os 4 pilares do sistema OCGCore:
    *   `LuaDuel` (`Duel.`): Classe global de estado. Ex: `Duel.Damage`, `Duel.Destroy`, `Duel.GetMatchingGroup`.
    *   `LuaCard` (`c:`): O invólucro do `CardDisplay` ou `CardData` real da Unity, provendo informações como `c:GetAttack()` para o script.
    *   `LuaEffect` (`Effect.`): O contêiner lógico que estrutura a anatomia de uma habilidade (`Condition`, `Cost`, `Target`, `Operation`).
    *   `LuaGroup` (`Group` ou `eg`): Tabelas de listas de cartas dinâmicas para aplicação em massa. Suporta métodos de injeção de delegates (Filtros C#) via `params object[] extraArgs`.
*   **`Assets/Scripts/LuaScripts/`:** O diretório de dados definitivo. Contém a base canônica (`constant.lua` e `utility.lua`) e milhares de arquivos textuais `.lua` batizados pela Custom ID da carta (ex: `c0217.lua`).
*   **`CardEffectManager_Impl.cs` (Sobrevivência):** Mantém apenas as funções utilitárias nativas (Helpers), flags temporárias da mesa e os `Hooks` de conexão, limpos de todas as lógicas hardcoded unitárias de cartas passadas.

### 5.1.2 O Novo Fluxo de Execução e o Sistema `chk`
O jogo agora obedece rigorosamente às janelas de ativação de um simulador autêntico. A ação foi dividida entre o momento de pagar/escolher alvos (Ativação) e a explosão do efeito (Resolução na Corrente):
1.  **Gatilho Inicial:** Jogador clica "Activate". O GameManager aciona `CardEffectManager.Instance.ActivateCard()`. O script LUA é lido da pasta e cacheado na memória RAM em `activeLuaCards`.
2.  **Modo de Validação Silenciosa (`chk=0`):** O C# vasculha o arquivo Lua. Executa `Condition()`, `Cost()` e `Target()` passando `chk=0`. Isso é uma checagem seca (Dry-run). Se a carta exigir um alvo específico e não houver, a engine retorna `false` antes mesmo de piscar a UI, blindando o jogador contra o gasto desnecessário de vida ou descarte.
3.  **Fase de Ativação (`chk=1`):** Se válido, a Engine abre as corrotinas e roda as mesmas funções com `chk=1`. Aqui os Custos são pagos matematicamente e os Modais de Seleção visual aparecem pedindo para o jogador clicar em um monstro. O pacote pronto é embalado na classe `PendingEffect` (A Gaveta de Pendências).
4.  **A Entrada na Corrente:** O `ChainManager` assume. O oponente tem a janela para ativar *Trap Holes* ou *Magic Jammers* em cima do que você acabou de fazer.
5.  **A Resolução (`Operation`):** Quando a corrente LIFO finalmente aciona `ExecuteCardEffect()`, o Manager retira a carta de sua Gaveta de Pendências e executa **apenas** a `Operation()` do arquivo LUA (ex: destruir o campo inteiro), limpando a memória imediatamente a seguir.

---

## 5.2 Referência de Gatilhos C# e Escutas LUA (Event Listeners)

Os Hooks são os radares do C# chamados automaticamente pela Engine. No novo sistema, ao invés do C# ter o código da carta aqui, ele atua como um "mega-fone". Quando algo ocorre, ele grita o ID do Evento em toda a arena através da função `TriggerLuaEvent(EventCode, args)`, e as cartas cujos scripts `.lua` pediram para ouvir aquilo (`e:SetCode(EVENTO)`) são ativadas.

### 5.2.1 Hooks de Fases e Turno
*   **`OnPhaseStart(GamePhase phase)`**
    *   *Momento:* Logo após o `PhaseManager` alterar a fase atual, antes que o jogador aja.
    *   *LUA:* O radar dispara o `EVENT_PHASE (4096)` e o `EVENT_PHASE_START (4097)`.
    *   *Uso Frequente:* Custos de manutenção obrigatórios na Standby Phase e limpeza temporária na End Phase via `CleanAllExpiredModifiers()`.
*   **`OnPreDrawPhaseImpl(bool isPlayerTurn, Action onContinue)`**
    *   *Momento:* Ocorre *antes* da compra automática normal da Draw Phase.
    *   *Uso Frequente:* Utilizado por cartas "Pule sua Draw Phase para..." (ex: *Freed the Matchless General*). Em modo assíncrono absoluto, obriga a execução do delegate `onContinue?.Invoke()` se a compra não for negada.

### 5.2.2 Hooks de Batalha (Battle Step & Damage Step)
*   **`OnAttackDeclared(CardDisplay attacker, CardDisplay target, Action onContinue)`**
    *   *Momento:* Battle Step. Assim que atacante e alvo são definidos (o ataque ainda pode ser negado). `target` pode ser nulo (Ataque Direto).
    *   *Uso Frequente:* Custos obrigatórios de ataque (*Panther Warrior*), rolagem de moedas/dados pré-dano (*Fairy Box*, *Sasuke Samurai #4*).
*   **`OnDamageCalculation(CardDisplay attacker, CardDisplay target, Action onContinue)`**
    *   *Momento:* Damage Step. A última janela onde atributos podem flutuar antes da subtração matemática de LPs.
    *   *Uso Frequente:* Injeção temporária de ATK/DEF pagando LP (*Injection Fairy Lily*), bônus de campo específicos de combate (*Skyscraper*), ou habilidades de destruição pré-dano (*Drillroid*).
*   **`OnBattleEnd(CardDisplay attacker, CardDisplay target)`**
    *   *Momento:* End of Damage Step. Os monstros derrotados já foram despachados para o cemitério e a matemática já foi aplicada.
    *   *LUA:* Dispara `EVENT_BATTLE_DESTROYED (1010)`. 
    *   **Engenharia Póstuma Avançada:** A engine C# detecta quais monstros morreram através de cruzamentos com as listas do GY no GameManager e chama a rotina `EnsureCardScriptLoaded` *postumamente* na memória. Isso permite que efeitos de ativação no túmulo (Ex: *Sangan*) consigam subir para a Corrente sem a carta existir fisicamente na tela.
*   **`IsAttackRestricted(CardDisplay attacker)`**
    *   *Verificação contínua:* Usado no BattleManager para checar se um monstro pode atacar.

### 5.2.3 Hooks de Dano e Pontos de Vida (LP)
*   **`OnDamageDealtImpl(CardDisplay attacker, CardDisplay target, int amount)`**
    *   *Momento:* Após o cálculo de batalha, se um ataque resultou em perda de LP para o oponente.
    *   *Uso Frequente:* Disparo de efeitos de descarte ao causar dano letal ou direto (*Don Zaloog*, *White Magical Hat*, *Robbin' Goblin*).
*   **`OnDamageTaken(bool isPlayer, int amount)`**
    *   *Momento:* Assim que um jogador sofre redução em seus LPs (por batalha ou Burn).
*   **`OnLifePointsGained(bool isPlayer, int amount)`**
    *   *Momento:* Sempre que um jogador cura LPs via efeito.

### 5.2.4 Hooks de Movimentação e Destruição de Cartas
*   **`OnCardSentToGraveyard(CardData card, bool isOwnerPlayer, CardLocation fromLocation, SendReason reason)`**
    *   *Momento:* Assim que uma carta aterrissa no GY.
    *   *Parâmetros de Contexto:* `fromLocation` (Hand, Deck, Field) e `reason` (Battle, Effect, Cost, Tribute).
    *   *Missing Timing (Atenção):* Efeitos opcionais ("Quando... você pode...") devem verificar se a `reason` **não é** `Cost` ou `Tribute` para não perderem a janela de ativação (ex: *Peten the Dark Clown*). Além disso, cartas enviadas como custo não ativam efeitos de "quando destruídas".
*   **`OnCardLeavesField(CardDisplay card)`**
    *   *Momento:* Milissegundos antes da carta física ser destruída, banida ou retornada à mão.
    *   *Limpeza Vital de LUA:* Aciona as deleções de cache no dicionário `activeLuaCards` e rasga pendências no `pendingResolutions` do `CardEffectManager` para prever falhas em corrotinas perdidas (Evitando brutalmente Memory Leaks na Unity). Suporta liberação autônoma das travas físicas de Zonas (*Ojama King*) e algemas nativas de união (*Call of the Haunted*).
*   **`OnCardDiscardedImpl(CardDisplay card, bool causedByOpponent)`**
    *   *Momento:* Quando uma carta é fisicamente descartada da mão para o GY.
    *   *Uso Frequente:* A flag `causedByOpponent` é vital para ativar ressurreições do Arquétipo *Dark World* e de cartas como *Regenerating Mummy*.
*   **`OnCardDrawnImpl(CardData card, bool isPlayer)`**
    *   *Momento:* Assim que a carta vai do Deck para a Mão (na Draw Phase ou por efeito).
    *   *Uso Frequente:* Invocação imediata forçada (*Parasite Paracide*) ou escaneamento de compra (*Crush Card Virus*).

### 5.2.5 Hooks de Estado de Campo e Magias
*   **`OnSummonImpl` / `OnSpecialSummonImpl` / `OnSetImpl`**
    *   *LUA:* Dispara `EVENT_SUMMON_SUCCESS (11)`. Essencial para Trap Holes e acúmulo de Spell Counters na Invocação.

---

## 5.3 API de Helpers, Conversão Lua e Caixa de Ferramentas C#

Apesar da lógica ter sido movida para os scripts `.lua`, a Engine Unity não enxerga scripts, ela enxerga objetos 3D. Os métodos C# abaixo continuam abrigados no `CardEffectManager_Impl.cs` como os **motores reais** que movem as peças no tabuleiro. A `LuaAPI` chama esses métodos sob a camada (Under the hood) para criar a mágica que você vê na tela. Sempre que você chamar `Duel.Damage()` no arquivo Lua, ele transborda para o helper `Effect_DirectDamage()` documentado aqui.

### 5.3.1 Vida e Dano (LP)
*   **`Effect_DirectDamage(CardDisplay source, int amount)`**
    *   Causa dano de efeito (Burn) ao oponente. Já verifica `trapOfBoardEraserActive` (anula e descarta), `spellOfPainActive` (reflete), `pikerusCircleActive` (anula) e `redirectSpellTarget` (*Mystical Refpanel*).
*   **`Effect_GainLP(CardDisplay source, int amount)`**
    *   Cura o dono da carta `source`. Engatilha combos com *Fire Princess* automaticamente, disparando `OnLifePointsGained`.
*   **`Effect_PayLP(CardDisplay source, int amount)`**
    *   Subtrai LP como custo. Retorna `true` se o jogador tinha saldo, ou `false` se não podia pagar.

### 5.3.2 Buscas e Filtros no Tabuleiro
*   **`CheckActiveCards(string cardId, Action<CardDisplay> action)`**
    *   Itera sobre todas as cópias válidas, ativas e face-up daquela carta no campo do jogador (monstros, magias e zonas de campo) e executa a `action`. Usado massivamente no `OnPhaseStart` para aplicar efeitos Contínuos.
*   **`GetEquippedCards(CardDisplay target)`**
    *   Retorna uma `List<CardDisplay>` de todas as cartas que estão fisicamente equipadas no monstro alvo através de um `CardLink`.

### 5.3.3 Alteração de Status (ATK / DEF)
A matemática de status no jogo é não-destrutiva.
*   **Adicionar Modificador:** `target.AddStatModifier(new StatModifier(StatType, ModifierType, Operation, valor, fonte));`
    *   *Operações:* `Add` (Soma/Subtração), `Multiply` (Multiplica), `Set` (Define valor fixo). A engine lida com a flag `reverseStats` (*Reverse Trap*) invertendo `+` para `-` automaticamente, e limita valores negativos em `0`.
*   **Remover Modificador Específico:** `target.RemoveModifiersFromSource(CardDisplay source)`
    *   **CRÍTICO:** Se uma carta recalcula status a cada turno (ex: *Chaos Necromancer*, *Buster Blader*), você deve **sempre** chamar isso antes de adicionar o novo `StatModifier`, senão o ATK vai crescer infinitamente a cada frame.
*   **Limpar Buffs Temporários:** `target.CleanExpiredModifiers()` é chamado globalmente na End Phase.

### 5.3.4 Manipulação de Deck, Mão e Cemitério
*   **`Effect_SearchDeck(CardDisplay source, string term, string typeFilter, int maxAtk, int maxLevel)`**
    *   Abre a UI para o jogador escolher uma carta do Deck que contenha `term` e respeite os filtros, adicionando-a à mão e embaralhando o deck depois.
*   **`Effect_SpecialSummonFromDeck(CardDisplay source, string race, string attribute, int maxAtk, int maxDef, int maxLevel, string nameContains, bool? isPlayerOverride)`**
    *   Exibe opções válidas na UI e invoca o monstro selecionado diretamente pro campo.
*   **`GameManager.Instance.MillCards(bool isPlayer, int amount)`**
    *   Envia N cartas do topo do Deck direto para o Cemitério (*Needle Worm*).
*   **`DestroyCards(List<CardDisplay> list, bool causedByPlayer)` / `DestroyAllMonsters(bool p, bool o)`**
    *   Remoção massiva e simultânea engatilhada pelo comando Lua `Duel.Destroy(group)`. Suporta as invocações em grupo (IEnumerator `DestroyCardsRoutine`) para destruir tabuleiros interios sem descompasso gráfico e tocando os VFXs (*Mirror Force*, *Raigeki*).
*   **`GameManager.Instance.BanishCard(CardDisplay card)`**
    *   Remove de jogo instantaneamente. Destrói o visual, limpa links e modificadores gerados, e a move para a lista `Removed`.
*   **`GameManager.Instance.ReturnToHand(CardDisplay card)`**
    *   Bounce. Devolve do campo para a mão. Toca animação/lógica de saída.
*   **`GameManager.Instance.ReturnToDeck(CardDisplay card, bool toTop)`**
    *   Spin. Devolve para o topo (`true`) ou fundo (`false`) do Deck.

### 5.3.5 Lógicas Empacotadas (Subtipos)
A engine foi projetada para que lógicas complexas não precisem ser recriadas do zero:
*   **Fusão (`BeginFusionSummon`):** Abre a `FusionUI` onde o `FusionManager` lida com sacrifícios, materiais curinga e invocações do Extra Deck.
*   **Ritual (`BeginRitualSummon`):** Abre a `RitualUI`, travando o jogo para a seleção matemática de tributos de nível adequado.

### 5.3.6 Interações de Contra-Ataque e Negação (Counter Traps)
Essenciais para cartas de Speed 3 ou de negação direta de ativação.
*   **`GetLinkToNegate(CardDisplay source)`:** Retorna o objeto `ChainManager.ChainLink`. Ele varre a corrente atual e pega exatamente o elo imediatamente abaixo da carta que foi ativada na pilha.
*   **`NegateAndDestroy(source, ChainLink targetLink)`:** Altera a flag de status do `targetLink` para `isNegated = true` e envia o `cardSource` físico do efeito inimigo direto ao cemitério com animação de quebra. Na resolução revesa da corrente LIFO, o efeito será ignorado.

### 5.3.7 Fichas, Armadilhas e Miscelânea
*   **`GameManager.Instance.SpawnToken(forPlayer, atk, def, name, level, race, attribute)`**
    *   Tenta encontrar uma zona livre e cria um Token físico no campo. Recebe automaticamente o ID mágico `"TOKEN"` que faz com que a carta evapore instantaneamente se enviada à Mão, Deck ou GY.
*   **`GameManager.Instance.ConvertTrapToMonster(trapCard, atk, def, level, race, attribute)`**
    *   Transfere uma Armadilha Contínua da Zona de S/T para a Zona de Monstros. Preenche as variáveis `trapMonsterBaseAtk` e muda `isTrapMonster = true` para que funcione no motor de batalha como um monstro real (Ex: *Embodiment of Apophis*).

---

## 5.4 Dicionário Global de Estados e Variáveis de Memória

Consulte este dicionário antes de criar variáveis novas. O jogo já rastreia o estado da partida exaustivamente.

### 5.4.1 Em `CardDisplay` (Estado Local da Instância)
Acesse via `cardDisplay.nomeDaVariavel`. Pertence individualmente a cada carta instanciada.
*   **Batalha:**
    *   `hasAttackedThisTurn`: Marca se concluiu 1 ataque.
    *   `attacksDeclaredThisTurn` / `maxAttacksPerTurn`: O controle de ataques múltiplos. Padrão é 1. Modificado por *Asura Priest*.
    *   `battledThisTurn`: Entrou em cálculo de dano (útil para *Mirage Knight* e *Ryu Kokki* que ativam coisas no fim do dano).
    *   `destroyedMonsterThisTurn`: Venceu um combate letal (*Insect Queen*).
*   **Restrições e Debuffs:**
    *   `cannotAttackDirectly` / `cannotAttackThisTurn`: Bloqueios estritos.
    *   `cannotInflictBattleDamage`: ATK passa e destrói monstros, mas não tira LP (*Union Attack*).
    *   `canBeTributedByOpponent`: Aplicado por *Soul Exchange*.
*   **Fase, Turno e Origem:**
    *   `hasChangedPositionThisTurn`, `summonedThisTurn`, `wasSpecialSummoned`.
    *   `hasUsedEffectThisTurn`: Para habilidades "Once per Turn" manuais.
    *   `isTributeSummoned`, `tributeCount`, `tributedMonsters` (Lista dos sacrifícios exatos).
    *   `originalOwnerIsPlayer`: Para devoluções de controle.
*   **Atributos Falsos (Fake Stats):**
    *   `isTrapMonster`, `trapMonsterBaseAtk / Def / Race / Attribute`.
    *   `temporaryRace`, `temporaryAttribute`: Sobrescrevem o lido do JSON (usado por *DNA Surgery*).
*   **Timers, Contadores e Agendamentos:**
    *   `spellCounters`: Quantidade de marcadores mágicos no objeto.
    *   `turnCounter` / `maxTurnCounter`: O relógio regressivo/progressivo da carta que integra com a UI do `TurnClockUI`.
    *   `scheduledForDestruction` / `scheduledForBanishment`: Agendamentos para a End Phase.
    *   `destructionTurnCountdown`: Turnos até a carta explodir (*Zone Eater*).
    *   `returnControlAtEndPhase`: Devolve para o dono original (*Shien's Spy*).

### 5.4.2 Em `CardEffectManager` (Memória Global e Condições de Campo)
Acesse via `CardEffectManager.Instance.nomeDaVariavel`. Ditam leis e interceptações temporárias.
*   **Modificadores Globais de Regra:**
    *   `reverseStats`: Inverte adições/subtrações matemáticas (*Reverse Trap*).
    *   `banishInsteadOfGraveyard`: Tudo que iria pro GY é banido (*Macro Cosmos*).
    *   `negateContinuousSpells`: (*Mystic Probe*).
    *   `redirectSpellTarget`: Redireciona alvo de magia (*Mystical Refpanel*).
    *   `armoredGlassActive`: Nega todos os cards de Equipamento do campo (*Armored Glass*).
    *   `cannotSummonMonstersThisTurn` / `trapsBlockedThisTurn`: Travas globais de ativação.
*   **Interceptação de Dano Adiado (Pre-Damage Hook):**
    *   `trapOfBoardEraserActive`: Anula próximo Burn e força descarte.
    *   `spellOfPainActive`: Reflete próximo Burn sofrido.
    *   `pikerusCircleActivePlayer / Opponent`: Dano Burn vira 0 neste turno.
*   **Rastreamentos Especiais e Consumíveis:**
    *   `level8OrHigherDestroyedThisTurn`: Gatilho para *A Deal with Dark Ruler*.
    *   `playerDrawsThisTurn` / `opponentDrawsThisTurn`: Quantas compras extras ocorreram fora da Draw Phase (*Greed*).
    *   `secondCoinTossUsedPlayer/Opponent` e `diceReRollUsedPlayer/Opponent`: Consumíveis de minigame de sorte por turno.
*   **Declarações Visuais:**
    *   `dnaSurgeryDeclaredType`, `dnaTransplantDeclaredType`, `arrayOfRevealingLightType`.
*   **Dicionários e Listas Pendentes:**
    *   `blockedZonesByCard` (`Dictionary<CardDisplay, List<Transform>>`): Zonas físicas trancadas e inutilizáveis (*Ojama King*).
    *   `reviveNextStandby` (`List<CardData>`): Vítimas com retorno programado (*Vampire Lord*).
    *   `imT_BanishedCards`: Retorno do *Interdimensional Matter Transporter*.
    *   `pharaohsTreasureCards`: Instâncias do baú mágico no deck.

### 5.4.3 Em `GameManager` e `SpellTrapManager` (Estado da Partida)
*   **No `GameManager`:**
    *   `playerLP`, `opponentLP`: Pontos de vida.
    *   `lpPaidThisTurnPlayer`, `lpPaidLastTurnPlayer`: Usado para cálculos de *Life Absorbing Machine*.
    *   `playerMainDeck`, `playerHand`, `forbiddenSpells` / `prohibitedCards` (*Prohibition*).
    *   `isSelectingFromHand`: Bloqueio de UI durante a "Seleção Direta" de cartas clicando nos modelos 3D na mão.
*   **No `SpellTrapManager`:**
    *   `canActivateTrapsFromHand`: Flag de quebra de regra de Set (*Makyura*).
    *   `extraDrawsPerTurn`: Adiciona repetições no loop automático da próxima Draw Phase.
    *   `skipDrawPhase`: Quebra o ciclo normal da máquina de estados.
    *   `isSelectingTarget`: Indica se uma corrotina de mira no tabuleiro está congelando a tela.

---

## 5.5 Sistemas Complexos e Efeitos Contínuos

A estrutura foi modelada para lidar com mecânicas famosas do jogo sem sujar a base de código do motor com exceções.

### 5.5.1 Efeitos Contínuos e Restrições Globais (Locks)
Gerencia efeitos passivos que impõem regras globais. São verificados dinamicamente antes de permitir ações da UI e da Engine.
*   **Negação de Efeitos (`ExecuteCardEffect`):** O `CardEffectManager` checa a presença de *Skill Drain* (cancela execução de monstros em campo) e *The End of Anubis* (cancela efeitos no GY) logo na entrada do método mestre.
*   **Restrições Físicas e de Batalha:**
    *   *Spatial Collapse:* Impede a UI e a IA de baixar cartas se `GetFieldCardCount >= 5`.
    *   *Rivalry of Warlords:* Impede a seleção de invocações se a raça diferir do que já existe no tabuleiro.
    *   *Gravity Bind / Level Limit:* `BattleManager` invoca `IsAttackPreventedByContinuousEffect(attacker)` no passo 1 do combate.

### 5.5.2 Efeitos Retardados (Delayed Effects)
Gerencia efeitos que não acontecem imediatamente. Tudo é processado globalmente via varredura no `OnPhaseStart`.
*   **End Phase Destruction:** O Evento de End Phase varre todos os monstros para procurar quem tem `scheduledForDestruction == true` e os destrói.
*   **Contador Descrescente:** O Evento varre `destructionTurnCountdown`. Se for > 0 no turno do dono do contador (`destructionCountdownOwnerIsPlayer`), ele decresce. Se bater 0, explode.

### 5.5.3 Buffs Dinâmicos (Dynamic Stat Buffs)
Cartas cujos stats mudam a cada jogada do duelo (ex: *Buster Blader*, *Chaos Necromancer*).
*   **Lógica de Renovação:** Em vez de usar `Update()`, eles recalculam a cada `OnPhaseStart(End)`. O método (ex: `UpdateBusterBladerBuff`) **deve obrigatoriamente chamar** `card.RemoveModifiersFromSource(card)` antes de adicionar o novo `StatModifier` baseado no cemitério para não acumular os números matematicamente.

### 5.5.4 Bloqueio Físico de Zonas (Zone Blocking)
*   **Tranca:** A lógica de *Ojama King* escolhe zonas de tabuleiro, envia um `duelFieldUI.BlockZone(zone)` (que spawna um cadeado visual na cena), e as arquiva no dicionário `blockedZonesByCard`. A partir daí, métodos vitais como `GetFreeMonsterZone` as ignoram como se não existissem.
*   **Liberação Automática:** No evento `OnCardLeavesField`, a engine verifica a existência da carta morta no dicionário. Se achar, ela chama `UnblockZone`, explodindo os cadeados e limpando a memória, tudo de forma modular.

### 5.5.5 Interceptação de Dano de Efeito (Pre-Damage Hook)
Permite que Counter Traps ou efeitos rápidos interceptem a matemática de Burn.
*   Cartas como *Spell of Pain* ativam flags no momento da ativação (`spellOfPainActive = true`).
*   O helper central `Effect_DirectDamage()` olha para essas flags **antes** de tirar a vida do jogador. Se ativadas, inverte o alvo ou zera o dano, e então a flag é consumida (`= false`). São limpas na End Phase caso o jogador a ativou sem alvo.

### 5.5.6 Transferência Dinâmica de Equipamentos e Vínculos
Permite mover um `Equip Spell` já ativo para outro monstro (*Tailor of the Fickle*).
*   A engine acessa a lista de `activeModifiers` do monstro antigo, retira o `StatModifier` onde a "source" é o equipamento específico, e joga o mesmo multiplicador no novo monstro, alterando por fim o objeto destino na classe de "algema" `CardLink.target`. A mágica é transferida sem ir para o cemitério.

### 5.5.7 Sistema de Vínculo Toon (Toon Link)
*   Integrado no `OnCardLeavesField`. Se *Toon World* é destruído ou banido, o script varre todas as Zonas de Monstros simultaneamente. Qualquer `CardDisplay` cuja raça (ou nome) seja "Toon" sofre uma ordem de `SendToGraveyard`, emulando a fragilidade deste arquétipo clássico.

---

## 5.6 Interações de UI, Modais e Minigames

A Engine do jogo roda na Thread principal da Unity. Como as decisões exigem input humano, o jogo utiliza estritamente o padrão assíncrono.

### 5.6.1 O Padrão Assíncrono e a Mágica da Engine LUA (`YieldReq`)
Se você estivesse programando direto em C#, pausaríamos uma rotina usando `while(isWaiting) yield return null`. No sistema híbrido, o **MoonSharp (LUA)** emite uma requisição física chamada `YieldReq` para avisar ao C# que o arquivo LUA está indo dormir.

Quando uma carta Lua invoca `Duel.SelectTarget()`, a ponte age:
```csharp
// 1. C# Avisa a Mão Mestra (Coroutine State) que vai suspender
CardEffectManager.Instance.isWaitingForLuaYield = true;

// 2. Abre os modais da Unity (Painéis e mira do rato)
SpellTrapManager.Instance.StartTargetSelection(unityFilter, (selectedCard) => {
    // 3. Callback (Humano clicou no monstro após 5 segundos!)
    // C# empacota a escolha num formato que o Lua entenda e solta a trava:
    CardEffectManager.Instance.yieldReturnValue = UserData.Create(selectedGroup);
    CardEffectManager.Instance.isWaitingForLuaYield = false;
});

// 4. Comando letal que faz a Máquina Virtual decolar o YieldReq.
return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectTarget") });
```

### 5.6.2 Tipos de Modais Oficiais da Engine
Você não precisa criar UIs novas, tudo está envelopado:

1.  **Confirmação Simples (Sim/Não):** 
    `UIManager.Instance.ShowConfirmation(mensagem, Action onSim, Action onNao)`
2.  **Miras Físicas no Tabuleiro (Targeting):** 
    `SpellTrapManager.Instance.StartTargetSelection(Predicate<CardDisplay> filtro, Action<CardDisplay> callback)`
    *Ex:* `StartTargetSelection((t) => t.isOnField && !t.isPlayerCard, (alvo) => Destroy(alvo.gameObject));`
3.  **Seleção em Listas Múltiplas (Busca, GY, Mão):** 
    `GameManager.Instance.OpenCardMultiSelection(List<CardData> lista, string titulo, int min, int max, Action<List<CardData>> callback)`
    *Inteligência:* Se a lista for puramente da Mão e `min == max`, a engine ativa a mira direta clicando nas cartas físicas, pulando o Popup.
4.  **Digitação de Nome de Carta (Busca Global Otimizada):** 
    `GlobalCardSearchUI.Instance.Show("Declare", (declaredCard) => { ... })`
    *Nota:* Faz uma busca (com limite de 50 no Dropdown) por toda a DB de 2147 cartas para efeitos como *Mind Crush*.
5.  **Textos e Escolhas Arbitrárias:** 
    `MultipleChoiceUI.Instance.Show(List<string> options, "Escolha 1", min, max, (selected) => { ... })`
    *Nota:* Usado para declarar Raças, Atributos ou Efeitos ramificados.
6.  **Teclado Numérico Interativo (Numpad):** 
    `NumericSelectionUI.Instance.Show("Título", "Pague LP:", valorMin, valorMax, passoMultiplier, null, (valor) => { ... })`
    *Nota:* Para declarações de Nível ou Custos precisos de Vida.
7.  **Reordenação de Topo de Deck (Drag & Drop):** 
    `ReorderCardsUI.Instance.Show(topCardsList, "Reordene", (orderedList) => { ... })`

### 5.6.3 O Bypass de IA e Simulação (O Cérebro Lua)
A Engine Lua foi desenhada para interagir com interfaces humanas. Para que o jogo não trave quando a Inteligência Artificial ativa uma carta, um **Bypass Lógico** atua nas funções de mira (`SelectTarget`, `SelectMatchingCard`).
*   **Como funciona:** Se `player == 1` (A IA), a Ponte LUA cancela a Corrotina de Interface e redireciona a lista de cartas candidatas diretamente para o método `OpponentAI.Instance.SelectLuaTargets(LuaGroup, min, max)`. A IA ordena a lista com base em ameaças e devolve a resposta instantaneamente, mantendo a performance fluída e veloz do duelo.
*   **Simulador de Caos:** Se `GameManager.Instance.isSimulating == true` em ações residuais não cobertas pela IA, a Engine simplesmente seleciona o primeiro item válido da lista ou gera números aleatórios, garantindo que o ciclo noturno de testes a 50x de velocidade jamais fique congelado.

### 5.6.4 Minigames de Sorte e Controle Visual
*   **Sorte:** *Nunca* use o `Random` puramente oculto caso a carta exija interação. Chame:
    *   `GameManager.Instance.TossCoin(int count, Action<int> callback)`
    *   `CardEffectManager.Instance.RollDice(int amount, Action<List<int>> callback)`
    *   Estes métodos abstraem cliques físicos nos dados rolando 3D na tela e processam a *Chance de Re-Roll* automaticamente se `Second Coin Toss` estiver ativo.
*   **Revelação Silenciosa:** Se precisar revelar uma carta de face para baixo e evitar que a Engine dispare Correntes ou efeitos de Flip na Unity, use: `card.RevealCard(false, false);`.