# Mecânicas de Campo e Conflito (Rules, Combat & Board)

Este documento unifica as regras do motor de batalha, o sistema de correntes (LIFO), a alocação física de zonas, os estados visuais e lógicos das cartas na Unity e os procedimentos de Invocação Especial.

---

## 4.1 A Matemática Oculta do Combate (Fluxo de Batalha C# -> Lua)
A lógica de combate não é centralizada em um `BattleManager` dedicado. Em vez disso, ela é um fluxo de eventos que começa na UI (`CardDisplay`), é orquestrado pelo `GameManager` e executado em grande parte pelo motor Lua dentro do `CardEffectManager`.

*   **4.1.1 O Fluxo de Ataque (Início na UI)**
    1.  **Seleção do Atacante:** Durante a Battle Phase, o jogador clica em um de seus monstros em posição de ataque. `CardDisplay.OnPointerClick` detecta isso e define `CardEffectManager.Instance.luaDuel.currentAttacker`. Um brilho vermelho indica o estado de "ataque selecionado".
    2.  **Seleção do Alvo:** O jogador clica em um monstro inimigo (ou no campo vazio).
        *   **Alvo é Monstro:** `CardDisplay.OnPointerClick` do alvo é acionado. Ele chama `ExecuteAttackToTarget(this)`.
        *   **Ataque Direto:** `DuelFieldUI.OnPointerClick` é acionado. Ele chama `ExecuteDirectAttack()`.
    3.  **Execução em Lua:** Ambos os métodos (`ExecuteAttackToTarget`, `ExecuteDirectAttack`) marcam o monstro como tendo atacado (`hasAttackedThisTurn = true`) e iniciam a corrotina `RunGenericLuaCoroutine` no `CardEffectManager`, chamando a função global `Core.Attack(attacker, target)` no ambiente Lua.
    4.  **Resolução da Corrotina Lua:** O script `Core.Attack` emula os passos da Battle Phase, usando `coroutine.yield()` para devolver o controle à Unity em momentos cruciais:
        *   `yield('WaitChain')`: Abre a janela de resposta para o oponente ativar armadilhas (ex: `Mirror Force`).
        *   `yield('PlayAttackAnimation')`: Sinaliza ao `DuelFXManager` para tocar a animação de ataque.
        *   `yield('RevealTarget')`: Vira o monstro alvo se estiver em modo de defesa virado para baixo.
        *   `Duel.CalculateDamage(attacker, target)`: Função final que calcula o dano, destrói as cartas e atualiza os LPs.

*   **4.1.2 Dano Perfurante (`hasPiercing`)**
    *   A flag `hasPiercing` é uma propriedade booleana no `CardDisplay`. Se `true`, a função `Duel.CalculateDamage` em Lua ignora a defesa e subtrai o ATK do oponente diretamente dos LPs se o alvo estiver em modo de defesa.

---

## 4.2 Arquitetura do Sistema de Correntes (`CardEffectManager.cs`)
O `ChainManager` foi absorvido pelo `CardEffectManager` para unificar a lógica de efeitos. O sistema de Corrente (Chain) é a espinha dorsal da interatividade do jogo.

*   **4.2.1 A Anatomia de um Elo da Corrente (`ChainLink`)**
    *   Quando um efeito é ativado, um objeto `ChainLink` é criado. Ele contém:
        *   `chainIndex`: A posição na corrente (1, 2, 3...).
        *   `effect`: A referência para o `LuaEffect` sendo ativado.
        *   `card`: A `LuaCard` que originou o efeito.
        *   `player`: O jogador que ativou.
        *   `isNegated` / `isActivationNegated`: Flags que marcam se o efeito foi negado.
*   **4.2.2 A Lei das Velocidades (Spell Speeds)**
    *   Embora não seja uma variável explícita, a lógica de resposta segue as regras de Spell Speed. Efeitos de `Speed 1` (Ignition, a maioria dos monstros) não podem ser acorrentados a nada. Efeitos de `Speed 2` (Quick Effects, Traps, Quick-Play Spells) podem responder a outros Speed 1 ou 2. Efeitos de `Speed 3` (Counter Traps) podem responder a tudo.
*   **4.2.3 O Fluxo de Construção e Resolução (LIFO)**
    1.  **Início:** Uma ação (ex: `ActivateCard`) chama `BuildAndResolveChainRoutine`.
    2.  **Custo e Alvo:** O `costFunc` e `targetFunc` do efeito são executados com `chk=1` para pagar custos e selecionar alvos.
    3.  **Adição à Pilha:** Um `ChainLink` é criado e adicionado à lista `currentChain`.
    4.  **Janela de Resposta:** A rotina `ResponseWindowRoutine` é chamada, pausando a execução. Ela verifica se o oponente tem alguma carta de `Speed 2` ou maior (`GetValidResponses`) e abre uma UI para que ele possa "acorrentar" um efeito.
    5.  **Recursão:** Se o oponente acorrenta um efeito, `BuildAndResolveChainRoutine` é chamado novamente para o Link 2, que por sua vez abre outra janela de resposta.
    6.  **Resolução:** Quando ambos os jogadores passam a vez, o `BuildAndResolveChainRoutine` do Link 1 (o que iniciou tudo) começa a resolver a pilha em ordem inversa (LIFO - Last-In, First-Out), executando o `operationFunc` de cada `ChainLink` do último para o primeiro.
    7.  **Limpeza:** `CleanupSpellTrapAfterResolution` envia Mágicas/Armadilhas normais para o cemitério após a resolução.
*   **4.2.3.1 A Lógica de Confirmação (Warning de Corrente)**
    *   Para garantir um fluxo fluído (UX), a janela global de confirmação *"Deseja responder?"* não interrompe o jogo ativamente durante a Main Phase do jogador se ele apenas ativou sua própria carta. A Engine usa uma regra de Auto-Pass.
    *   **Aviso Apenas Quando Necessário:** O Warning só aparece quando o jogo precisa parar para permitir interrupções táticas (ex: Responder a um ataque inimigo, responder a uma mágica do oponente, ou durante uma janela "Fast Effect" de mudança de fase).
    *   **Atalhos:** O aviso pode ser aceito instantaneamente com a tecla **ENTER**, habilitando o jogador a passar o mouse e ver os balões de ativação das cartas candidatas. Pressionar **ESC** recusa a resposta e o duelo continua.
*   **4.2.4 Como as Negações Funcionam no Código**
    *   Cartas de negação (ex: *Magic Jammer*) simplesmente definem a flag `isActivationNegated = true` no `ChainLink` alvo. Durante a fase de resolução, qualquer link com essa flag ativada é simplesmente ignorado.

---

## 4.3 Arquitetura Lógica do Tabuleiro e Vínculos (`DuelFieldUI.cs` & `CardLink.cs`)
Esta seção detalha a **lógica de código** por trás do tabuleiro, incluindo alocação de espaços, bloqueio de zonas e o sistema de equipamentos.

### 4.3.1 Busca e Alocação de Zonas (`GetFree...Zone`)
A alocação física de uma carta na tela é gerenciada pelos métodos `GetFreeMonsterZone` e `GetFreeSpellZone` do `GameManager`, que consultam os arrays de zonas no `DuelFieldUI`.

*   **A Ordem de Preenchimento:** A busca itera pelas 5 zonas e retorna o primeiro `Transform` cujo `childCount == 0` (vazio) e que não esteja sinalizado como bloqueado.
*   **Simulação de Perspectiva Inimiga (`fillOpponentZonesFromRight`):** Para o jogador, a zona 1 é a extrema esquerda. Para o oponente, a zona 1 dele seria a extrema direita. Se a flag estiver ativa, a busca para a IA ocorre de trás para frente (`Length - 1 down to 0`), tornando o preenchimento mais natural.
*   **Otimização de Tributos (`placeTributeSummonInTributeZone`):** Ao realizar um Tribute Summon, o `GameManager` pode receber uma zona específica para garantir que o novo monstro ocupe o espaço do monstro sacrificado.

### 4.3.2 Sistema de Bloqueio Físico de Zonas (Zone Locks)
Cartas como *Ojama King* inutilizam fisicamente as zonas do tabuleiro.

*   **A Lógica de Tranca:** O `CardEffectManager` mantém um dicionário `blockedZonesByCard`. Quando o efeito de bloqueio é ativado, ele chama `duelFieldUI.BlockZone(zone)`, que instancia um cadeado visual, e armazena as zonas bloqueadas no dicionário. A partir daí, `GetFreeMonsterZone` ignora essas zonas.
*   **Limpeza Automática:** O evento `OnCardLeavesField(card)` intercepta a saída de qualquer carta. Se a carta que saiu era a fonte de um bloqueio, a engine chama `UnblockZone` para remover os cadeados e limpar o registro do dicionário.

### 4.3.3 O Sistema de Vínculos (`CardLink`)
Cartas de equipamento ou armadilhas contínuas com alvos (como *Call of the Haunted*) precisam ter seus destinos atrelados. A engine resolve isso com o script `CardLink.cs`.

*   **O que é o `CardLink`?** Um GameObject invisível que atua como uma "algema" de memória entre `Source` (a Magia/Armadilha) e `Target` (o monstro).
*   **Resolução de Dependência:** O evento `OnCardLeavesField` varre os `CardLink`s ativos. Se o `Target` de um equipamento sai de campo, a `Source` (o equipamento) é destruída. Se a `Source` é destruída, os modificadores de status são removidos do `Target`.
---

## 4.4 Anatomia e Máquina de Estados da Carta (`CardDisplay.cs`)
O script `CardDisplay.cs` é o componente mais instanciado do jogo, atuando como a representação visual e a **Máquina de Estados Local** da carta.

### 4.4.1 Ciclo de Vida Visual (Visual Lifecycle)
*   **Lazy Loading:** Usa `UnityWebRequestTexture` para carregar a imagem da carta de forma assíncrona a partir da pasta `StreamingAssets`.
*   **Posições e Rotações:** Uma matemática de eixos Z define a posição de Ataque (0º/180º) e Defesa (90º/-90º) com base no dono da carta. A flag `isFlipped` controla se a textura da frente ou do verso é exibida.
*   **Efeitos de Interface (Hover):** Usa `canvas.overrideSorting = true` para levantar a carta na mão sem quebrar o layout. O `Outline` nativo do Unity provê o brilho colorido.

### 4.4.2 Motor de Status Dinâmico
*   O `CardDisplay` armazena `originalAtk`/`originalDef` e `currentAtk`/`currentDef`. Efeitos de cartas não alteram `currentAtk` diretamente. Em vez disso, eles adicionam ou removem `StatModifier`s a uma lista na carta.
*   A função `RecalculateStats()` é chamada sempre que um modificador é adicionado/removido, e ela refaz a matemática do zero, garantindo a ordem correta de operações (SET -> ADD -> MULTIPLY).

### 4.4.3 Flags de Memória e Estado
O `CardDisplay` guarda dezenas de booleanas que controlam o fluxo de regras:
*   `hasAttackedThisTurn`, `hasChangedPositionThisTurn`, `summonedThisTurn`.
*   `cannotAttackThisTurn`, `isTrapMonster`, etc.

### 4.4.4 Delegação de Cliques (Click Event Router)
O método `OnPointerClick` é uma "estação de trem". Ele lê o contexto (Qual a fase? O jogador está selecionando um alvo?) e repassa o clique para o Manager correto:
1.  **Pilhas (Deck/Cemitério):** Chama `GameManager.ViewGraveyard()` ou `GameManager.DrawCard()`.
2.  **Seleção de Alvo/Descarte:** Se `GameManager.isSelectingFromHand` for `true`, repassa o clique para `GameManager.HandleHandCardClick()`.
3.  **Fase de Batalha:** Define `CardEffectManager.luaDuel.currentAttacker` ou chama a rotina de ataque.
4.  **Fase Principal (Menu de Ação vs One-Click):** 
    *   **Menu Clássico:** Se `GameManager.activateEffectsWithOneClick` for falso (ou para ações de invocação da mão), chama `DuelActionMenu.Instance.ShowMenu(this)` para abrir o clássico Submenu.
    *   **One-Click Activate:** Se a opção for verdadeira, clicar com o Botão Esquerdo em uma carta virada para cima ou setada no campo que possua um efeito ativável irá dispará-la instantaneamente. O visual disso é auxiliado pelo *Hover Preditivo* (veja a documentação de UI).
5.  **Correntes Múltiplas (Response Chain):** Durante janelas de resposta rápidas, o motor liga os brilhos diretamente nas cartas válidas (`isSelectingResponse`). O Botão Esquerdo engatilha o elo da Corrente (Link) instantaneamente, e o Botão Direito no espaço vazio atua como um atalho invisível de cancelamento para "Passar a Vez" e iniciar a Resolução (LIFO).
---

## 4.5 Sistema de Special Summon
A Invocação Especial permite colocar monstros no campo sem usar a Invocação Normal do turno.

### 4.5.1 Fluxo de Código
1.  **Gatilho:** Uma carta ou efeito chama `GameManager.Instance.PerformSpecialSummon(cardObject, cardData)`.
2.  **Seleção de Posição (`PositionSelectionUI`):** O jogo pausa e abre um modal para o jogador escolher entre Ataque ou Defesa (Face-Up, a menos que o efeito especifique o contrário).
3.  **Execução (`GameManager`):** Após a escolha, `FinalizeSummon` é chamado para mover a carta para o campo na posição correta e disparar os efeitos visuais e gatilhos de `OnSpecialSummon`.
4.  O contador `normalSummonsThisTurnPlayer` não é incrementado.

### 4.5.2 Tipos de Special Summon Suportados
*   **Fusão:** Via `BeginFusionSummon`.
*   **Ritual:** Via `BeginRitualSummon`.
*   **Ressurreição:** Via `SpecialSummonFromData` (usado por efeitos que revivem do cemitério).
*   **Invocação inerente:** Monstros que se invocam da mão (lógica dentro do script Lua da própria carta).

## 4.6 Interfaces de Seleção (Direct Selection vs UI Panels)
A Engine suporta múltiplos fluxos visuais para a seleção de alvos e invocações complexas.

*   **Seleção Direta (Smart Select & Power-Set):** Se a opção `GameManager.useDirectHandSelection` estiver ativada, a engine evita modais pesados. As cartas piscam na mão e campo (`SelectionState.Available`). 
    *   **Ritual e Fusão Exata:** O C# utiliza um algoritmo de subconjuntos (*Power-Set*). Ao clicar numa nova carta, o motor varre as combinações matemáticas possíveis. Se a soma bater exatamente com o Nível do ritual, ele remove silenciosamente o "peso morto" (cartas antigas desnecessárias) da sua mão, otimizando o sacrifício de forma invisível.
    *   **Conclusão Antecipada (Right-Click):** Para efeitos que pedem "de 1 a 3 monstros", ao atingir o mínimo (1), o jogador pode apertar o Botão Direito ou ESC para fechar a seleção parcial com sucesso, em vez de cancelar a jogada.
    *   **Fusão:** Usa a Caixa de Seleção (`ShowCardSelection`) para escolher o Monstro de Fusão, pois ele reside no *Extra Deck* (que é uma pilha invisível, não clicável individualmente). Porém, os *Materiais* são selecionados via Seleção Direta no tabuleiro/mão.
*   **Painéis Customizados (Custom UI):** As opções `useCustomFusionUI` e `useCustomRitualUI` desviam o fluxo para painéis específicos que mostram todas as etapas de uma vez (Ex: Monstro e Tributos na mesma janela).
*   **Single Target Auto-Select:** Se houver apenas 1 alvo válido, a engine pula a seleção para poupar cliques. Isso pode ser desativado ativando `alwaysConfirmSingleTarget` no GameManager, forçando a seleção visual e confirmação mesmo para opções únicas.
*   **Modo de Visualização Cinematográfica (View-Only):** Utilizado por cartas de espionagem (Ex: *Ancient Telescope*). Quando a Engine LUA solicita confirmar cartas que não estão no tabuleiro (ex: `Duel.ConfirmDecktop` ou `Duel.ConfirmCards` na Mão do Oponente), a interface `CardSelectionUI` transmuta-se para o modo de exibição (Mínimo = 0, Máximo = 0):
    *   **Sem Ícones de Alvo:** As mecânicas de clique, bordas de seleção (Highlighter) e numeração de "alvo" (Badges) são estritamente desativadas para não sugerir que o jogador pode interagir ou escolher aquelas cartas.
    *   **Fundo e Imersão:** A moldura branca tradicional do painel fica invisível e um fundo escuro translúcido (`Alpha 0.85`) domina a tela para dar foco total às cartas reveladas.
    *   **Animação Direcional (Voo Fantasma):** Cartas são ejetadas do Deck/Mão em um arco parabólico suave, com rastros de sombra (`Shadows`), virando em 3D no ar antes de pousar na grade central. A engine identifica a origem (Jogador vs IA) e inverte a ordem matemática da animação (0 a N vs N a 0) para que as cartas nunca cruzem o caminho umas das outras no ar.
    *   **Scale Swap (Inversão de Escala):** Para evitar o "pop" visual gerado por layouts de painel (GridLayoutGroup), a Engine C# compreende a direção do voo. No voo de ida (Intro), a carta usa sua escala normal até o tamanho final da UI. No voo de volta (Outro), as matrizes de escala inicial e final são invertidas matematicamente, encolhendo suavemente do tamanho da UI até as dimensões do Deck (-20% para encaixe no buraco da pilha).
*   **Modo de Confronto Cinemático (Versus / Comparison UI):** Utilizado por cartas de aposta e duelo de status (Ex: *Ante*, *Dice Jar*). Em vez de poluir e sobrecarregar o painel de Seleção em Grade (CardSelectionUI), a engine transfere o foco para um palco simétrico e isolado:
    *   **O Buffer de Espionagem:** Em vez de editar os scripts LUA originais que revelam as mãos em chamadas separadas, o `LuaDuel.cs` usa um buffer temporal (`pendingComparisonCards`). Quando ele identifica 1 carta de cada jogador no gatilho do Efeito, a Engine silencia o UI nativo do LUA, absorve os dados das duas e unifica as chamadas em um Evento Cinemático global.
    *   **O Minigame de Confronto:** As cartas ejetadas voam para seus lados na tela e giram para revelar suas artes (`CardComparisonUI`). Seus status nativos (Nível, ATK, etc.) são lidos diretamente do LUA e carimbados na tela em uma animação pulsante.
    *   **A Punição Final:** A UI matemática julga o menor valor. O perdedor da aposta sofre a explosão direto no Canvas 2D. Todos os rastros (Win/Lose) somem junto com a destruição e a Engine libera o fluxo LUA (Yield), mandando a carta fisicamente para o cemitério.

*(Nota: Os ícones visuais do estado de seleção são orquestrados pelo `DuelFXManager.SetSelectionIcon`, mapeando categorias como Tributo, Fusão ou Ritual para seus respectivos pacotes de partículas).*