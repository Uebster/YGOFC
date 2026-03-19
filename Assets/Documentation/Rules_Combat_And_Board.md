# Mecânicas de Campo e Conflito (Rules, Combat & Board)

Este documento unifica as regras do motor de batalha, o sistema de correntes (LIFO), a alocação física de zonas, os estados visuais e lógicos das cartas na Unity e os procedimentos de Invocação Especial.

---

## 4.1 A Matemática Oculta do Combate (`BattleManager.cs`)

### Visão Geral
O `BattleManager` é a engrenagem que conduz a Battle Phase. Ele não apenas subtrai LPs; ele administra um fluxo estrito de sub-fases (Steps) para permitir que efeitos contínuos, armadilhas e cálculos matemáticos entrem em cena na ordem correta, imitando perfeitamente a "Damage Step" do TCG.

### 4.1.1 O Fluxo de Ataque (The Battle Flow)
Quando o jogador clica em um monstro para atacar e seleciona o alvo, a engine não calcula o dano imediatamente. Ela segue este roteiro:

#### Passo 1: Battle Step (Declaração)
*   **Trava de Legalidade:** A engine checa se o monstro tem a restrição `cannotAttackThisTurn` ou `cannotAttackDirectly`.
*   **Trava Contínua:** Chama `IsAttackPreventedByContinuousEffect()` para checar se cartas como *Gravity Bind* ou *Messenger of Peace* bloqueiam o ataque.
*   **O Gatilho `OnAttackDeclared`:** A engine avisa o `CardEffectManager` sobre o ataque e adiciona um `ChainLink` do tipo `TriggerType.Attack`. Isso abre a janela para o oponente ativar *Mirror Force* ou *Magic Cylinder*.
*   *Redirecionamento:* Cartas como *Patrician of Darkness* ou *Shift* podem alterar a variável `currentTarget` neste exato momento.

#### Passo 2: Damage Step - Início
*   Se o alvo estiver virado para baixo (Set), ele é revelado (Flip).
*   **Importante:** Efeitos FLIP (ex: *Man-Eater Bug*) **NÃO** são ativados agora. Eles são engavetados para o final da Damage Step.

#### Passo 3: Damage Step - Cálculo (`OnDamageCalculation`)
*   Esta é a única janela onde os status (ATK/DEF) podem flutuar antes da pancada.
*   **Injeção Dinâmica:** O `CardEffectManager` analisa o embate. É aqui que *Injection Fairy Lily* pergunta se você quer pagar LP, ou onde o campo *Skyscraper* aumenta em 1000 o ATK dos E-HEROs temporariamente.
*   A matemática pura ocorre: **ATK vs ATK** ou **ATK vs DEF**.

#### Passo 4: Damage Step - Resolução de Dano
*   O dano resultante é passado para uma função de filtro chamada `DealBattleDamage()`.
*   **Interceptação Absoluta:** O jogo processa quem apanha de verdade (Detalhado na seção 1.2 abaixo).

#### Passo 5: Damage Step - Fim (`OnBattleEnd`)
*   Monstros com HP <= 0 são enviados para o cemitério (`SendToGraveyard`).
*   **Resolução Engavetada:** Se o monstro destruído possuía um efeito FLIP, o efeito é disparado **agora**.
*   **Efeitos de Busca:** Gatilhos como os de *Mystic Tomato*, *Sangan* ou as remoções de *D.D. Assailant* ocorrem neste momento, fechando o ataque.

### 4.1.2 O Filtro de Dano (`DealBattleDamage`)
Nunca subtraímos `GameManager.playerLP` direto na hora do ataque. O dano sempre passa por um "Filtro de Dano" para suportar mecânicas clássicas do Yu-Gi-Oh!

A função `DealBattleDamage(bool targetIsPlayer, int amount, CardDisplay damageDealer)` faz as seguintes perguntas antes de descontar a vida:

1.  **Dano Nulo Global:** A flag `wabokuActive` ou `noBattleDamageThisTurn` (de *Winged Kuriboh*) está ativa? Se sim, o dano vira 0.
2.  **Dano Nulo Local:** A carta que está tomando o dano tem a proteção de *Amazoness Fighter* ou *Monk Fighter*? O controlador leva 0 de dano.
3.  **Incapacidade do Atacante:** A carta que bateu possui a flag `cannotInflictBattleDamage` (ex: causada por *Union Attack*)? Se sim, o monstro oponente até morre, mas o excedente de ATK não tira LP.
4.  **Reflexão de Dano (Mirroring):** O monstro que está tomando a pancada é uma *Amazoness Swords Woman*? Se sim, a variável `targetIsPlayer` é **invertida**, forçando o atacante a sofrer o dano no lugar do defensor.

Somente após passar por todas essas validações, a função invoca `GameManager.DamagePlayer(amount)` ou `GameManager.DamageOpponent(amount)`.

### 4.1.3 Dano Perfurante (Piercing Damage)
Quando um monstro com alto ATK ataca uma Defesa pequena, normalmente não há dano de batalha. 

O `BattleManager` contorna isso lendo a propriedade `hasPiercing` da carta atacante.
*   **De onde vem a flag:** Ela é ativada no `OnSummon` (ex: *Spear Dragon* e *Mad Sword Beast* nascem com `hasPiercing = true`), ou injetada por `StatModifiers` do tipo equipamento (Ex: *Fairy Meteor Crush*, *Big Bang Shot*).
*   **A Matemática:** Se `hasPiercing == true` e a posição do alvo for `Defense`, a engine faz `amount = Atacante.currentAtk - Defensor.currentDef`. Se for maior que 0, envia para `DealBattleDamage()`.

### 4.1.4 Ataques Diretos
A engine usa `CanAttackDirectly()` para permitir cliques no avatar do oponente quando ele possui monstros no campo.
*   **Permissão:** O `BattleManager` varre os modificadores do atacante e verifica se ele possui o *Shooting Star Bow - Ceal*, ou se é um *Toon Monster* contra um oponente sem Toons.
*   **Bloqueio Específico:** Se a carta atacante for a *Zombyra the Dark*, ela possui a restrição `cannotAttackDirectly = true` em sua Máquina de Estados, sendo impedida de declarar este tipo de ataque.
*   **Modificadores de Impacto:** Monstros como *Piranha Army* ou *Mefist the Infernal General* interceptam a confirmação do ataque direto para dobrar a variável de dano final ou forçar um descarte do oponente.

---

## 4.2 Arquitetura do Sistema de Correntes (`ChainManager.cs`)

### Visão Geral
O `ChainManager` é o árbitro de tempo e prioridade do jogo. Diferente de um RPG onde ataques acontecem instantaneamente, em Yu-Gi-Oh!, quase toda ação cria uma "janela de resposta". O `ChainManager` constrói uma **Pilha LIFO** (Last-In, First-Out: O último a entrar é o primeiro a sair) para resolver essas respostas na ordem matemática correta.

### 4.2.1 A Anatomia de um Elo da Corrente (`ChainLink`)
Toda vez que uma ação é iniciada, um objeto `ChainLink` é adicionado à lista `currentChain`. Ele guarda na memória o estado exato da ação:

*   `cardSource` (CardDisplay): A carta que iniciou a ação.
*   `isPlayerEffect` (bool): De quem é a carta.
*   `trigger` (ChainManager.TriggerType): O que causou isso? (`CardActivation`, `Summon`, `Attack`).
*   `spellSpeed` (int): A velocidade da carta (1, 2 ou 3).
*   `isNegated` (bool): Flag de status. Se `true`, o efeito será ignorado na resolução.
*   `target` (CardDisplay): O alvo inicial (pode ser modificado por cartas como *Shift* antes de resolver).
*   `linkNumber` (int): A posição na corrente (1, 2, 3...).

### 4.2.2 A Lei das Velocidades (Spell Speeds)
Para que o `SpellTrapManager.GetValidResponses` decida quais cartas o jogador pode ativar em resposta, ele utiliza o método `ChainManager.GetSpellSpeed(cardData)`.

#### Spell Speed 1 (Lento)
*   **Cartas:** Magias Normais, Equipamentos, Magias de Campo, Rituais e Efeitos de Ignição/Flip de monstros.
*   **Regra:** **Não podem** ser usadas para responder a nada. Elas apenas iniciam uma corrente (`Chain Link 1`).

#### Spell Speed 2 (Rápido)
*   **Cartas:** Magias Rápidas (Quick-Play), Armadilhas Normais e Armadilhas Contínuas.
*   **Regra:** Podem responder a ações normais do jogo (Ataques, Invocações) e a cartas de Speed 1 ou Speed 2.

#### Spell Speed 3 (Supremo)
*   **Cartas:** Armadilhas de Resposta (Counter Traps - Ícone de Seta).
*   **Regra:** Podem responder a absolutamente qualquer coisa. A restrição mortal: **Apenas uma carta Speed 3 pode responder a outra carta Speed 3**. Efeitos de monstros rápidos e mágicas rápidas são bloqueados.

### 4.2.3 O Fluxo de Construção (Building the Chain)
O ciclo de uma corrente ocorre da seguinte maneira:

1.  **Gatilho Inicial:** Um jogador ataca ou ativa uma carta mágica. O `GameManager` ou `BattleManager` chama `ChainManager.AddToChain()`. O `Chain Link 1` é criado.
2.  **Passagem de Prioridade:** A engine interrompe a ação. O `ChainManager` chama o `SpellTrapManager` para escanear o campo do **Oponente** procurando cartas *Setadas* com velocidade adequada.
3.  **Prompt de UI:** Se encontrar algo (ex: *Magic Jammer*), exibe na tela: "Deseja encadear Magic Jammer?".
4.  **Encadeamento:** Se o oponente aceitar, `AddToChain()` é chamado novamente. O *Magic Jammer* vira o `Chain Link 2` (Speed 3).
5.  **Rebote (Bounce):** A prioridade volta para o jogador inicial. Ele pode responder ao *Magic Jammer* com outra carta, criando o `Chain Link 3` (ex: *Seven Tools of the Bandit* - Speed 3).
6.  **Fechamento:** Quando ambos os jogadores "passam" (não querem ou não podem mais responder), a corrente é declarada fechada e entra em **Resolução**.

### 4.2.4 O Fluxo de Resolução (Resolving the Chain)
A engine itera pela lista `currentChain` **de trás para frente** (começando pelo maior `linkNumber` até o 1).

#### O Loop Reverso:
Para cada `ChainLink`:
1.  A engine checa a variável `isNegated`. Se estiver `true`, a engine pula este elo e dá um `Debug.Log("Efeito negado.")`.
2.  Se `isNegated` for `false`, a engine invoca a habilidade da carta:
    *   No caso de Magias/Armadilhas: Chama `CardEffectManager.Instance.ExecuteCardEffect(link.cardSource)`.
    *   No caso de Ataques: Invoca o callback `triggerAttackEffect()` engavetado.
3.  **Limpeza Visual:** Cartas Normais, Quick-Play ou Counter Traps que acabaram de resolver seu efeito são destruídas visualmente e enviadas ao GY. (Cartas de Equipamento ou Contínuas permanecem no campo).

### 4.2.5 Como as Negações Funcionam no Código
Cartas que "Negam e Destroem" (Ex: *Magic Jammer*) nunca impedem a carta inicial de ir para a pilha. Elas entram na pilha *acima* do alvo, e quando resolvem primeiro, usam dois *Helpers* vitais:

*   `GetLinkToNegate(source)`: 
    *   Varre a `currentChain` e encontra a carta imediatamente abaixo da *sua* carta na pilha (Se você é o Link 3, ele pega o Link 2).
*   `NegateAndDestroy(source, targetLink)`: 
    *   Acessa o elo capturado e altera `targetLink.isNegated = true`.
    *   Chama `Destroy()` no GameObject físico do alvo imediatamente.
    *   Resultado: Quando o Loop Reverso chegar na vez de ativar o Link 2, a engine verá a flag de negação e não fará nada.

#### Exemplo de Negação Clássica
1. Player 1: Ativa *Raigeki* (Chain Link 1 / Speed 1).
2. Player 2: Encandeia *Magic Jammer* (Chain Link 2 / Speed 3).
*A corrente fecha e resolve de trás para frente:*
3. *Resolve Link 2:* *Magic Jammer* usa `NegateAndDestroy` no Link 1. O Raigeki físico é destruído.
4. *Resolve Link 1:* Engine tenta resolver o *Raigeki*, vê `isNegated = true`, e pula o efeito. Os monstros do Player 2 sobrevivem.

---

## 4.3 Arquitetura Lógica do Tabuleiro e Vínculos (`DuelFieldUI.cs` & `CardLink.cs`)

### Visão Geral
Enquanto o documento `DuelPanelStructure.md` mapeia a árvore visual de GameObjects na Unity, este documento detalha a **lógica de código** por trás do tabuleiro. Como a engine aloca espaços físicos, como zonas são trancadas (Zone Blocking) e como o sistema amarra cartas dependentes umas às outras (Sistema de Equipamentos e CardLinks).

### 4.3.1 Busca e Alocação de Zonas (`GetFreeZone`)
A alocação física de uma carta na tela é gerenciada pelos métodos `GetFreeMonsterZone` e `GetFreeSpellZone` do `GameManager`, que consultam os arrays de zonas no `DuelFieldUI`.

*   **A Ordem de Preenchimento:** A busca itera pelas 5 zonas. Ela retorna o primeiro `Transform` cujo `childCount == 0` (vazio) e que não esteja sinalizado como bloqueado.
*   **Simulação de Perspectiva Inimiga (`fillOpponentZonesFromRight`):** Para o jogador, a zona 1 é a extrema esquerda. Para o oponente virtual sentado à sua frente, a zona 1 dele seria a extrema direita da sua tela. Quando esta flag está ativa, o loop de busca para cartas da IA ocorre de trás para frente (`Length - 1 down to 0`). Isso faz com que as cartas do inimigo preencham o campo "da esquerda dele para a direita dele", opondo-se ao preenchimento do jogador de forma natural.
*   **Otimização de Tributos (`placeTributeSummonInTributeZone`):** Ao realizar um Tribute Summon manual, o `SummonManager` rastreia de qual zona o monstro sacrificado saiu e injeta o `Transform` daquela exata zona no método de invocação final. Isso garante que o novo Boss Monster ocupe o buraco deixado pelo sacrifício, em vez de pular para o lado direito do tabuleiro, mantendo a organização visual escolhida pelo jogador.

### 4.3.2 Sistema de Bloqueio Físico de Zonas (Zone Locks)
Cartas como *Ojama King* ou *Ground Collapse* inutilizam fisicamente as zonas do tabuleiro.

#### A Lógica de Tranca (`blockedZonesByCard`)
*   O `CardEffectManager` mantém um dicionário global `Dictionary<CardDisplay, List<Transform>> blockedZonesByCard`.
*   Quando a restrição é ativada, a engine escolhe as zonas vazias, chama `duelFieldUI.BlockZone(zone)` e arquiva essas zonas no dicionário, usando a carta-fonte (*Ojama King*) como chave.
*   O `BlockZone` instancia um cadeado/marcador visual em cima da área e alerta a interface. A partir desse momento, a função `GetFreeMonsterZone` passa a ignorar solenemente essa zona, impedindo qualquer invocação ali.

#### Limpeza Automática
*   É imperativo que a engine saiba desfazer o bloqueio sem precisar de código *hardcoded* nas cartas de destruição. Para isso, o evento global `OnCardLeavesField(card)` intercepta a saída de qualquer carta. 
*   Ele verifica a premissa de forma limpa: `if (blockedZonesByCard.ContainsKey(card))`.
*   Se verdadeiro, a engine chama `UnblockZone` para esmagar os cadeados visuais, e remove a entrada daquele monstro da memória. A pista do duelo está liberada instantaneamente.

### 4.3.3 O Sistema de Vínculos (`CardLink`)
No Yu-Gi-Oh!, cartas mágicas de equipamento ou armadilhas contínuas (como *Call of the Haunted* ou *Snatch Steal*) precisam ter seus destinos atrelados a alvos móveis. A engine resolve isso de forma independente através do script `CardLink.cs`.

#### O que é o `CardLink`?
Trata-se de um GameObject invisível que atua como uma "algema" de memória entre as duas partes:
*   `Source`: A carta mágica/armadilha geradora (Ex: *Axe of Despair*).
*   `Target`: O monstro que sofre os efeitos da Source.
*   `LinkType`: O tipo do laço, sendo predominantemente `LinkType.Equipment`.

#### Resolução de Dependência (Destruição em Cadeia)
A existência do `CardLink` automatiza a complexa regra oficial de destruição de equipamentos do TCG:
1.  O evento `OnCardLeavesField` varre todos os `CardLink`s ativos na cena quando qualquer carta é destruída.
2.  Se o monstro que acaba de sair do campo (*Target*) era alvo de um link de Equipamento, a engine localiza a `Source` e a destrói automaticamente junto com o monstro.
3.  **Matemática Segura:** De forma inversa, se o equipamento (`Source`) for destruído isoladamente por um *Mystical Space Typhoon*, a engine usa o mesmo laço para descobrir quem era o portador, e usa `target.RemoveModifiersFromSource(source)` para extrair cirurgicamente apenas os Buffs de ATK/DEF atrelados àquela mágica destruída, recalculando a força do monstro de volta ao normal em tempo real.

#### Manipulações Dinâmicas Avançadas
*   **Equipamento do Inimigo (*Snatch Steal*):** O CardLink sustenta o laço firmemente mesmo que a mágica de equipamento fique do seu lado do campo, e o monstro passe para o lado do seu oponente, alterando sua variável `isPlayerCard`.
*   **Transferência Mágica (*Tailor of the Fickle*):** A engine permite mover cartas de equipamento sem precisar destruí-las. Ela acessa o `CardLink`, remove os modificadores matemáticos do monstro antigo, aplica os mesmos multiplicadores no novo monstro escolhido pelo jogador, e apenas sobrescreve a variável `link.target` da "algema", transferindo a posse fluidamente.

---

## 4.4 Anatomia e Máquina de Estados da Carta (`CardDisplay.cs`)

### Visão Geral
O script `CardDisplay.cs` é o componente mais instanciado do jogo. Ele é anexado ao Prefab de cada carta (na mão, no campo, no visualizador) e atua não apenas como a representação visual, mas como a **Máquina de Estados Local** da carta. O `GameManager` e o `BattleManager` consultam o `CardDisplay` o tempo todo para saber se uma carta pode atacar, se já ativou efeito, etc.

### 4.4.1 Ciclo de Vida Visual (Visual Lifecycle)

#### Carregamento de Texturas (Lazy Loading)
O jogo não carrega todas as 2147 cartas na RAM. O `CardDisplay` faz o carregamento assíncrono:
*   **`SetCard`:** Recebe um `CardData` e a textura do verso.
*   **Corrotina `LoadCardFrontTexture`:** Usa `UnityWebRequestTexture` para ler a imagem direto da pasta `StreamingAssets` usando o caminho `file://`.
*   **Prevenção de Vazamento (Memory Leak):** As variáveis `frontTexture` e `currentRequest` são rastreadas. Nos métodos `OnDisable` e `OnDestroy`, se a requisição ainda estiver rodando, ela é morta (`Dispose()`), e as texturas são destruídas para liberar memória VRAM.

#### Posições e Rotações
A variável visual de defesa/ataque não é uma animação complexa, mas uma matemática de eixos baseada no dono da carta (`isPlayerCard`):
*   **Ataque (Player):** 0º Z
*   **Defesa (Player):** 90º Z
*   **Ataque (Oponente):** 180º Z (De ponta-cabeça para o jogador)
*   **Defesa (Oponente):** -90º Z
*   **Face-Down (`isFlipped = true`):** A variável booleana dita se a `RawImage` mostra a `frontTexture` ou a `backTexture`.

#### Efeitos de Interface (Hover / Outline)
*   **Hovering Lift:** Se a carta está na mão (`isInteractable = true`), ao passar o mouse, ela usa `canvas.overrideSorting = true` para saltar para frente e sobe a posição Y sem empurrar as outras cartas do layout.
*   **Outline Dinâmico:** Usa o componente nativo `Outline` da Unity ou uma imagem separada (`outlineImage`). Fica **Verde/Azul** para o jogador, **Amarelo/Vermelho** para o oponente, ou **Vermelho Piscante** se selecionado para atacar.

### 4.4.2 Motor de Status Dinâmico (Stat Modifiers)
Cartas frequentemente ganham buffs e debuffs de Equipamentos ou Magias de Campo. O `CardDisplay` protege os valores originais e recalcula o presente instantaneamente.

#### Como o Recálculo Funciona (`RecalculateStats`)
O cálculo segue uma ordem de precedência estrita para imitar a ruling do TCG:
1.  **Base:** Puxa `originalAtk` e `originalDef` (ou os valores da Trap Monster, se for o caso).
2.  **Operação `Set` (Definir):** Aplica modificadores que congelam o ATK em um número fixo (Ex: *Megamorph* altera o ATK base original).
3.  **Operação `Add` (Soma/Subtração):** Aplica os bônus literais (Ex: +500 de Magia de Campo). Se a flag global `CardEffectManager.reverseStats` (*Reverse Trap*) estiver ativa, inverte o sinal (Buff vira Debuff). A carta também verifica se a negação de Equipamentos (*Armored Glass*) está ativa antes de computar bônus do tipo `Equipment`.
4.  **Operação `Multiply` (Multiplicação):** Dobra ou corta o ATK pela metade (Ex: *Limiter Removal*).
5.  **Gravação Final:** Atualiza `currentAtk` e `currentDef`. Se o valor for negativo, é travado em `0` via `Mathf.Max(0, finalAtk)`.

#### Limpeza Automática
No final do turno, o evento de Fase limpa os modificadores cuja flag `removeAtEndPhase` seja verdadeira, e invoca `RecalculateStats()` para todos voltarem ao normal.

### 4.4.3 As Flags de Memória (Turn States & Memory)
O `CardDisplay` guarda dezenas de booleanas que controlam o fluxo de regras daquele monstro específico:

#### Restrições de Ação
*   `hasAttackedThisTurn`: Marca se o monstro já concluiu um ataque.
*   `hasChangedPositionThisTurn`: Impede que o jogador mude a posição manual mais de 1x.
*   `summonedThisTurn`: Impede a mudança de posição manual no turno em que o monstro desceu ao campo.
*   `battledThisTurn`: Marcado ao entrar em combate. Usado por cartas como *Mirage Knight* (que se banem ao final da batalha).

#### Variáveis Controladas por Outras Cartas (Debuffs / Curses)
*   `cannotAttackThisTurn` / `cannotAttackDirectly`: Aplicados por cartas que travam o combate.
*   `cannotInflictBattleDamage`: Aplicado por *Union Attack*.
*   `scheduledForDestruction` / `destructionTurnCountdown`: Contadores agendados pelo `CardEffectManager` (Ex: *Wild Nature's Release* marca a carta para morrer na End Phase).

#### Atributos Transitórios e Falsos
*   `isTrapMonster`: Flag vital. Se verdadeira, o monstro ignora os status do `CardData` base (que seria o de uma carta de armadilha, que não tem ATK/DEF) e usa as variáveis `trapMonsterBaseAtk` e `trapMonsterBaseDef`.
*   `temporaryRace` / `temporaryAttribute`: Substituem a raça/atributo original. Usado em conjunto com o `CardEffectManager` para aplicar *DNA Surgery* e *DNA Transplant* localmente em uma carta alvo.

### 4.4.4 Delegação de Cliques (Click Event Router)
O método `OnPointerClick` do `CardDisplay` é uma "estação de trem" inteligente. Ele nunca realiza uma ação sozinho; ele lê o contexto (Qual a fase atual? O que o jogador está fazendo agora?) e repassa o clique para o Manager responsável:

1.  **Modo Desenvolvedor (Cheat):** Se `Shift + Botão Direito`, abre o painel do `FullTestManager`.
2.  **Pilhas (Deck/Cemitério):** Se `isInPile`, abre o Visualizador de Cemitério ou saca uma carta do Deck.
3.  **Seleção de Tributo:** Se `SummonManager.isSelectingTributes` for `true`, o clique envia a carta para ser sacrificada.
4.  **Seleção de Alvo (Magia/Armadilha):** Se `SpellTrapManager.isSelectingTarget` for `true`, o clique envia a carta como alvo do efeito.
5.  **Fase de Batalha (Battle Phase):**
    *   Clique Esquerdo num monstro seu: `BattleManager.PrepareAttack()`.
    *   Clique Esquerdo num monstro oponente: `BattleManager.SelectTarget()`.
6.  **Ações Rápidas (Mouse Tooltip):** Se na mão e sem seleções ativas, o clique esquerdo (Invocar/Ativar) ou direito (Setar) repassa a chamada para o `GameManager.TrySummonMonster` ou `GameManager.PlaySpellTrap`.

---

## 4.5 Sistema de Special Summon (`SpecialSummonSystem`)

### Visão Geral
O sistema de Invocação Especial permite colocar monstros no campo sem usar a Invocação Normal do turno. Diferente da Invocação Normal (que é restrita a Ataque Face-Up ou Defesa Face-Down "Set"), a Invocação Especial oferece flexibilidade de posição.

### 4.5.1 Fluxo de Código

#### 1. Gatilho
Uma carta ou efeito inicia o processo (ex: *Monster Reborn*, *Polymerization*, Efeito de Monstro).
O código chama `GameManager.Instance.PerformSpecialSummon(cardObject, cardData)`.

#### 2. Seleção de Posição (`PositionSelectionUI`)
*   O jogo pausa e abre um modal (`Panel_PositionSelection`).
*   O jogador vê a carta e dois botões:
    *   **Ataque:** Ícone da carta na vertical.
    *   **Defesa:** Ícone da carta na horizontal.
*   Diferente do "Set", a Defesa aqui é **Face-Up** (virada para cima), permitindo que efeitos contínuos ou atributos sejam vistos imediatamente, a menos que o efeito especifique "Face-Down" (ex: *The Shallow Grave*).

#### 3. Execução (`GameManager`)
*   Após a escolha, o callback aciona `FinalizeSummon`.
*   O monstro é movido para uma zona livre.
*   A rotação é aplicada (0º para Ataque, 90º para Defesa).
*   O `SummonManager` **não** incrementa o contador de `hasPerformedNormalSummon`, permitindo que o jogador ainda faça sua invocação normal no mesmo turno.

### 4.5.2 Tipos de Special Summon Suportados
*   **Fusão:** Via *Polymerization* ou *Fusion Gate*.
*   **Ritual:** Via cartas de Magia de Ritual.
*   **Ressurreição:** Via *Monster Reborn*, *Call of the Haunted*.
*   **Invocação inerente:** Monstros que se invocam da mão (ex: *Cyber Dragon*).