# 5. A Bíblia de Programação de Cartas (Card Programming API)

## Visão Geral
Este documento é o guia absoluto e exaustivo para programar os efeitos das 2147 cartas do jogo. Ele unifica a arquitetura do sistema, os Gatilhos (Hooks), Funções Auxiliares (Helpers), o Dicionário de Variáveis de Estado (Flags), as lógicas de interrupções de UI e subsistemas complexos. Consulte este documento sempre que for implementar a lógica de uma carta no `CardEffectManager_Impl.cs`.

---

## 5.1 Arquitetura do Sistema de Efeitos (`CardEffectManager`)

O `CardEffectManager` é o sistema central responsável por executar a lógica de todas as cartas do jogo. Devido à grande quantidade de cartas, o sistema foi refatorado para usar **Partial Classes**, dividindo o código em múltiplos arquivos.

### 5.1.1 Estrutura de Arquivos
*   **`CardEffectManager.cs` (Core):** Contém a estrutura base da classe (`MonoBehaviour`), o Singleton `Instance`, o dicionário de efeitos (`effectDatabase`) e métodos de infraestrutura (como `DestroyAllMonsters`, `CollectCards`).
*   **`CardEffectManager_Impl.cs` (Implementação Base e Utilitários):** Contém métodos genéricos de efeitos usados por muitas cartas (os *Helpers*), lógica de manutenção e os hooks de eventos principais (`OnPhaseStart`, `OnCardSentToGraveyard`, etc).
*   **`CardEffectManager_Registry.cs`:** Ponto de entrada para o registro de todos os efeitos. O método `InitializeEffects()` chama as inicializações de cada parte.
*   **Arquivos de Registro Parciais (`Registry_PartX.cs`):** Contêm apenas as chamadas `AddEffect("ID", Lógica)` para registrar as cartas no dicionário. Divididos por faixas de ID (ex: 0001-0500).
*   **Arquivos de Implementação Parciais (`Impl_PartX.cs`):** Contêm a implementação detalhada de efeitos específicos para cada carta, seguindo a convenção `Effect_ID_Nome` (ex: `Effect_0031_AirknightParshath(CardDisplay source)`).

### 5.1.2 Fluxo de Execução
1.  O jogador ativa uma carta (clique em "Activate" ou Flip).
2.  O `GameManager` (ou `ChainManager` via corrente) chama `CardEffectManager.Instance.ExecuteCardEffect(card)`.
3.  O Manager busca o ID da carta no `effectDatabase`.
4.  Se encontrado, o `Action<CardDisplay>` correspondente é invocado.
5.  A lógica (genérica ou específica) é executada, interagindo com `GameManager`, `DuelFieldUI`, etc.

---

## 5.2 Referência de Gatilhos e Hooks da Engine

Os Hooks são métodos chamados automaticamente pela Engine nos momentos exatos das regras do jogo. Para adicionar um efeito contínuo ou reativo, injete sua lógica no Hook apropriado em `CardEffectManager_Impl.cs`.

### 5.2.1 Hooks de Fases e Turno
*   **`OnPhaseStart(GamePhase phase)`**
    *   *Momento:* Logo após o `PhaseManager` alterar a fase atual, antes que o jogador aja.
    *   *Uso Frequente:* Custos de manutenção na Standby Phase (*Imperial Order*), contagem regressiva de turnos (*Swords of Revealing Light*), destruição agendada na End Phase. Utilize sempre em conjunto com o helper `CheckActiveCards`.
*   **`OnPreDrawPhaseImpl(bool isPlayerTurn, Action onContinue)`**
    *   *Momento:* Ocorre *antes* da compra normal da Draw Phase.
    *   *Uso Frequente:* Cartas que perguntam ao jogador se ele quer pular a compra em troca de um efeito (ex: *Freed the Matchless General*).
    *   *Atenção Crítica:* Requer uso do padrão assíncrono (Corrotina) com `isWaiting` para não travar a engine. Lembre-se de invocar `onContinue?.Invoke()` se o saque não for cancelado.

### 5.2.2 Hooks de Batalha (Battle Step & Damage Step)
*   **`OnAttackDeclared(CardDisplay attacker, CardDisplay target, Action onContinue)`**
    *   *Momento:* Battle Step. Assim que atacante e alvo são definidos (o ataque ainda pode ser negado). `target` pode ser nulo (Ataque Direto).
    *   *Uso Frequente:* Custos obrigatórios de ataque (*Panther Warrior*), rolagem de moedas/dados pré-dano (*Fairy Box*, *Sasuke Samurai #4*).
    *   *Atenção:* Se o efeito cancelar o ataque ou destruir o alvo aqui, **não** chame `onContinue?.Invoke()`.
*   **`OnDamageCalculation(CardDisplay attacker, CardDisplay target, Action onContinue)`**
    *   *Momento:* Damage Step. A última janela onde atributos podem flutuar antes da subtração matemática de LPs.
    *   *Uso Frequente:* Injeção temporária de ATK/DEF pagando LP (*Injection Fairy Lily*), bônus de campo específicos de combate (*Skyscraper*), ou habilidades de destruição pré-dano (*Drillroid*).
*   **`OnBattleEnd(CardDisplay attacker, CardDisplay target)`**
    *   *Momento:* End of Damage Step. Os monstros derrotados já foram despachados para o cemitério e a matemática já foi aplicada.
    *   *Uso Frequente:* Monstros que banem quem os destruiu (*D.D. Warrior Lady*), gatilhos de busca (*Mystic Tomato*), ou habilidades de abate pós-combate (*Ryu Kokki*).
*   **`IsAttackRestricted(CardDisplay attacker)`**
    *   *Verificação contínua:* Usado no BattleManager para checar se um monstro pode atacar.

### 5.2.3 Hooks de Dano e Pontos de Vida (LP)
*   **`OnDamageDealtImpl(CardDisplay attacker, CardDisplay target, int amount)`**
    *   *Momento:* Após o cálculo de batalha, se um ataque resultou em perda de LP para o oponente.
    *   *Uso Frequente:* Disparo de efeitos de descarte ao causar dano letal ou direto (*Don Zaloog*, *White Magical Hat*, *Robbin' Goblin*).
*   **`OnDamageTaken(bool isPlayer, int amount)`**
    *   *Momento:* Assim que um jogador sofre redução em seus LPs (por batalha ou Burn).
    *   *Uso Frequente:* Reflexão de dano extra (*Dark Room of Nightmare*), ativação de armadilhas como *Numinous Healer* ou *Attack and Receive*.
*   **`OnLifePointsGained(bool isPlayer, int amount)`**
    *   *Momento:* Sempre que um jogador cura LPs via efeito.
    *   *Uso Frequente:* Combos de dano baseados em cura (*Fire Princess*, *Bad Reaction to Simochi*).

### 5.2.4 Hooks de Movimentação e Destruição de Cartas
*   **`OnCardSentToGraveyard(CardData card, bool isOwnerPlayer, CardLocation fromLocation, SendReason reason)`**
    *   *Momento:* Assim que uma carta aterrissa no GY.
    *   *Parâmetros de Contexto:* `fromLocation` (Hand, Deck, Field) e `reason` (Battle, Effect, Cost, Tribute).
    *   *Uso Frequente:* Efeitos de Busca (*Sangan*, *Witch of the Black Forest*), ressurreições engatilhadas.
    *   *Missing Timing (Atenção):* Efeitos opcionais ("Quando... você pode...") devem verificar se a `reason` **não é** `Cost` ou `Tribute` para não perderem a janela de ativação (ex: *Peten the Dark Clown*). Além disso, cartas enviadas como custo não ativam efeitos de "quando destruídas".
*   **`OnCardLeavesField(CardDisplay card)`**
    *   *Momento:* Milissegundos antes da carta física ser destruída, banida ou retornada à mão.
    *   *Uso Frequente:* Remoção de links de Equipamento em cascata, liberação de zonas bloqueadas (*Ojama King*), ou penalidades pela destruição de magias (*Call of the Haunted*).
*   **`OnCardDiscardedImpl(CardDisplay card, bool causedByOpponent)`**
    *   *Momento:* Quando uma carta é fisicamente descartada da mão para o GY.
    *   *Uso Frequente:* A flag `causedByOpponent` é vital para ativar ressurreições do Arquétipo *Dark World* e de cartas como *Regenerating Mummy*.
*   **`OnCardDrawnImpl(CardData card, bool isPlayer)`**
    *   *Momento:* Assim que a carta vai do Deck para a Mão (na Draw Phase ou por efeito).
    *   *Uso Frequente:* Invocação imediata forçada (*Parasite Paracide*) ou escaneamento de compra (*Crush Card Virus*).

### 5.2.5 Hooks de Estado de Campo e Magias
*   **`OnSummonImpl` / `OnSpecialSummonImpl` / `OnSetImpl`**
    *   *Momento:* Assim que uma carta aterrissa no campo com sucesso (virada para cima, invocada de forma especial ou setada).
    *   *Uso Frequente:* Floodgates instantâneos (*King Tiger Wanghu*, *Bottomless Trap Hole*), acúmulo de Spell Counters na invocação (*Breaker*), gatilhos de *Card of Safe Return*.
*   **`OnBattlePositionChangedImpl(CardDisplay card)`**
    *   *Momento:* Pós-mudança de posição (Atk <-> Def).
    *   *Uso Frequente:* Habilidades de reposicionamento (*Dream Clown*, *Crass Clown*, *Tragedy*).
*   **`OnTributeImpl` / `OnControlSwitchedImpl`**
    *   *Momento:* Ao ser sacrificado ou ter seu dono alterado (*Change of Heart*). Ativa dano de *Ameba* ou cura de *Zolga*.
*   **`OnSpellActivated` / `OnCounterTrapResolvedImpl`**
    *   *Momento:* Quando uma mágica é confirmada ou quando uma armadilha Speed 3 resolve (negando algo).
    *   *Uso Frequente:* Adição de Spell Counters globais (*Royal Magical Library*), invocações massivas por anulação de jogada (*Van'Dalgyon*).
*   **`OnCardAddedToHandImpl(CardDisplay card)`**
    *   *Momento:* Quando a carta entra na mão por efeito de busca (não Draw normal).
    *   *Uso Frequente:* *Watapon*.

---

## 5.3 API de Helpers e Caixa de Ferramentas (Card Helpers)

**NUNCA** manipule LPs, altere status na mão grande, destrua GameObjects ou busque no deck diretamente sem usar os Helpers do `CardEffectManager_Impl`. Eles encapsulam animações, imunidades, redirecionamentos e recálculos matemáticos.

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
    *   Remoção massiva e simultânea enviando ao GY e tocando os VFXs (*Mirror Force*, *Raigeki*).
*   **`GameManager.Instance.BanishCard(CardDisplay card)`**
    *   Remove de jogo instantaneamente. Destrói o visual, limpa links e modificadores gerados, e a move para a lista `Removed`.
*   **`GameManager.Instance.ReturnToHand(CardDisplay card)`**
    *   Bounce. Devolve do campo para a mão. Toca animação/lógica de saída.
*   **`GameManager.Instance.ReturnToDeck(CardDisplay card, bool toTop)`**
    *   Spin. Devolve para o topo (`true`) ou fundo (`false`) do Deck.

### 5.3.5 Lógicas Empacotadas (Subtipos)
A engine foi projetada para que lógicas complexas não precisem ser recriadas do zero:
*   **Equipamento (`Effect_Equip`):** Chama seleção de alvo. Se obedecer aos filtros (Raça/Atributo), amarra as cartas na memória usando `CardLink.LinkType.Equipment` e aplica os bônus matemáticos automaticamente.
*   **Campo (`Effect_Field`):** Varre todo o tabuleiro aplicando `StatModifiers` do tipo `Field` aos monstros que batam com a regra.
*   **Controle (`Effect_ChangeControl`):** Permite escolher um monstro do oponente, remove da zona inimiga e o coloca na do jogador (Lida com *Change of Heart* e a flag de devolução `returnControlAtEndPhase`).
*   **Burn por Tributo (`Effect_TributeToBurn`):** Abre modal de tributos recursivo e causa Burn no oponente (*Cannon Soldier*).
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

### 5.6.1 O Padrão Assíncrono (While-Yield e Delegação para IA)
Se você precisar pausar a lógica da carta para perguntar algo ao jogador:
```csharp
bool isWaiting = true;
UIManager.Instance.ShowConfirmation("Deseja ativar o efeito opcional?", 
    () => { /* Ação Sim */ isWaiting = false; }, 
    () => { /* Ação Não */ isWaiting = false; }
);
// Congela a execução de fundo (Corrotina) até o humano clicar
while(isWaiting) yield return null;
```

### 4.2 Tipos de Modais Oficiais da Engine
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

### 4.3 O Bypass de Simulação (Crucial para o DevMode)
**Atenção Absoluta:** Sempre que houver uma condicional que demande a IA escolher algo para ela mesma e o código não estiver programado nativamente em `OpponentAI.cs`, ou se `GameManager.Instance.isSimulating == true` (Chaos Simulator em modo de teste estresse rodando a 50x de velocidade), **NENHUM** destes modais será aberto. 
O sistema automaticamente escolherá um aleatório (ou o 1º índice válido) e prosseguirá o turno sem travar a interface esperando um clique de humano.

### 4.4 Minigames de Sorte e Controle Visual
*   **Sorte:** *Nunca* use o `Random` puramente oculto caso a carta exija interação. Chame:
    *   `GameManager.Instance.TossCoin(int count, Action<int> callback)`
    *   `CardEffectManager.Instance.RollDice(int amount, Action<List<int>> callback)`
    *   Estes métodos abstraem cliques físicos nos dados rolando 3D na tela e processam a *Chance de Re-Roll* automaticamente se `Second Coin Toss` estiver ativo.
*   **Revelação Silenciosa:** Se precisar revelar uma carta de face para baixo e evitar que a Engine dispare Correntes ou efeitos de Flip na Unity, use: `card.RevealCard(false, false);`.