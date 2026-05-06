# Fluxo Oficial de Duelo e Regras (The "Gabarito")

Este documento detalha o fluxo exato de um duelo de Yu-Gi-Oh!, desde a inicialização até as condições de vitória. Serve como gabarito para a arquitetura lógica da engine (C# + Lua).

## 1. Inicialização do Jogo (Game Start)
1.  **Setup de Jogadores:** Definir Life Points (Padrão: 8000).
2.  **Preparação dos Decks:** Carregar e embaralhar Main Deck e Extra Deck.
3.  **Sorteio de Turnos:** O sistema (ou minigame) define quem será o Jogador 1 e Jogador 2.
4.  **Compra Inicial (Setup):** *Antes do primeiro turno começar*, ambos os jogadores sacam suas 5 cartas iniciais. Isso garante um fluxo cinematográfico onde a mão já está pronta quando a fase começa.
5.  **Anúncio do Turno:** O jogo anuncia de quem é o turno ("YOUR TURN" ou "OPPONENT'S TURN") e aguarda o texto sumir da tela.
6.  **Início do Duelo:** A `DRAW PHASE` é declarada.
7.  **Regra do 1º Turno (Moderno):** O jogador inicial **NÃO** compra a 6ª carta no seu primeiro turno e **NÃO** pode conduzir a Battle Phase.

---

## 2. Loop Principal (Game Loop)
Enquanto nenhum jogador atender a uma condição de derrota:
1.  Executar turno do jogador ativo.
2.  Após cada ação/resolução: Executar **State Check Global** (verificação de LPs, destruições, correntes).
3.  Alternar jogador.

---

## 3. Estrutura do Turno (Turn Phases)
Cada turno é composto pelas seguintes fases em ordem estrita:
1.  `DRAW PHASE`
2.  `STANDBY PHASE`
3.  `MAIN PHASE 1`
4.  `BATTLE PHASE` (Opcional, proibida no turno 1)
5.  `MAIN PHASE 2` (Apenas se entrou na Battle Phase)
6.  `END PHASE`

---

## 4. Detalhamento das Fases

### 4.1 Draw Phase
*   **Evento:** `EVENT_DRAW_PHASE_START`
*   **Ação Obrigatória:** Comprar 1 carta do topo do deck (exceto no Turno 1 do jogador inicial). Se o deck estiver vazio, o jogador perde imediatamente (Deck Out).
*   **Janela de Resposta:** Após a compra, dispara o `EVENT_CARD_DRAWN`. Cartas de Efeito Rápido e Armadilhas podem ser ativadas.

### 4.2 Standby Phase
*   **Evento:** `EVENT_STANDBY_PHASE`
*   **Ações:** Resolução de custos de manutenção (ex: *Imperial Order*), efeitos contínuos e efeitos "Once per turn" específicos desta fase.
*   **Janela de Resposta:** Ambos os jogadores podem ativar cartas de Speed 2 ou maior.

### 4.3 Main Phase 1 e 2
A fase onde a maioria das ações manuais ocorrem.
*   **Ações Disponíveis (Prioridade do Turn Player):**
    *   **Invocação Normal / Baixar (Set):** Máximo 1x por turno. (Nível 1-4: 0 Tributos | Nível 5-6: 1 Tributo | Nível 7+: 2 Tributos).
    *   **Invocação Especial:** Ilimitada. Inclui Fusão, Ritual, Synchro, Xyz, Link, ou inerente da carta.
    *   **Mudar Posição de Batalha:** 1x por monstro. (Proibido se o monstro foi invocado/setado neste turno, ou se atacou neste turno).
    *   **Ativar / Setar Magias e Armadilhas:** (Armadilhas e Magias Rápidas não podem ser ativadas no mesmo turno em que são setadas).
*   **Janelas:** Cada ação dispara um evento (ex: `EVENT_SUMMON_SUCCESS`) que abre uma corrente (Chain).

### 4.4 Battle Phase
A fase de combate é subdividida em 4 passos (Steps):

#### A. Start Step
*   Declaração de entrada na fase. Janela para cartas como *Threatening Roar*.

#### B. Battle Step
*   **Ação:** Selecionar Atacante -> Declarar Ataque.
*   **Evento:** `EVENT_ATTACK_ANNOUNCE`.
*   **Ação:** Selecionar Alvo (Monstro inimigo ou Ataque Direto).
*   **Janela Principal:** Momento exato para ativar *Mirror Force*, *Magic Cylinder*, etc.
*   **Replay Rule:** Se a quantidade de monstros no campo do oponente mudar durante este step, ocorre um "Replay", permitindo ao atacante reescolher o alvo ou cancelar o ataque.

#### C. Damage Step (Crítico e Restrito)
Esta janela é altamente restrita. Apenas efeitos que modificam ATK/DEF (ex: *Rush Recklessly*, *Honest*) ou Counter Traps podem ser ativados no início deste step.
1.  **Flip:** Monstro atacado Face-down é virado para Face-up (`EVENT_FLIP`). Efeitos de Flip aguardam a resolução do dano.
2.  **Cálculo de Dano:** Modificadores de ATK/DEF são aplicados (`EVENT_DAMAGE_CALCULATING`).
3.  **Aplicação do Dano:** LP são subtraídos.
    *   **ATK vs ATK:** Maior vence. Perdedor destruído. Diferença = Dano aos LP do perdedor. (Se iguais = ambos destruídos. *Exceção: Se ambos tiverem 0 de ATK, nenhum é destruído*).
    *   **ATK vs DEF:** ATK > DEF -> Defensor destruído (0 Dano, *exceto se o atacante tiver Dano Perfurante/Piercing*). ATK < DEF -> Atacante toma dano igual à diferença (Nenhum destruído). ATK = DEF -> Nada acontece.
4.  **Resolução de Destruição:** Monstro derrotado é marcado como "Destruído em Batalha" (`STATUS_BATTLE_DESTROYED`).
5.  **Efeitos Pós-Dano:** Efeitos Flip ativam aqui, seguidos por efeitos do tipo "Quando esta carta for destruída por batalha" (`EVENT_BATTLE_DESTROYED`).

#### D. End Step
*   Janela final para ativar cartas antes da Main Phase 2 (ex: *Gladiator Beasts* retornam ao deck). Dispara `EVENT_BATTLED` e `EVENT_BATTLE_END`.

### 4.5 End Phase
*   **Ações:** Resolução de efeitos "até o fim do turno" (cleanup).
*   **Hand Limit Check:** Se o jogador tiver mais de 6 cartas, deve descartar até ter 6.
*   **Reset:** Limpa flags de Normal Summon, posições de batalha e ataques.

---

## 5. O Sistema de Correntes (Chains) e Spell Speed

### 5.1 Spell Speeds (Velocidades)
Uma carta só pode responder a outra se tiver Spell Speed igual ou maior.
*   **Speed 1:** Magias Normais, Equipamentos, Campo, Rituais e Efeitos de Monstro (Ignition/Trigger). *Não podem responder a nada.*
*   **Speed 2:** Magias Rápidas (Quick-Play), Armadilhas Normais/Contínuas e Efeitos Rápidos de Monstros (Quick Effects). *Podem responder a Speed 1 e Speed 2.*
*   **Speed 3:** Armadilhas de Resposta (Counter Traps). *Podem responder a qualquer coisa.*

### 5.2 Construção e Resolução LIFO (Last-In, First-Out)
1.  **Chain Start:** Um evento ocorre ou uma carta é ativada (Elo 1 / Link 1).
2.  **Build:** O oponente tem a chance de responder. Se o fizer, torna-se o Elo 2. A prioridade alterna até ambos passarem.
3.  **Resolve:** A pilha é resolvida de trás para frente. O último efeito ativado acontece primeiro. *Novas cartas não podem ser ativadas enquanto uma corrente está resolvendo.*

---

### 5.3 Efeitos Simultâneos (SEGOC)
Se múltiplos efeitos de gatilho tentarem entrar na corrente exatamente ao mesmo tempo (Ex: Múltiplos monstros enviados ao cemitério por um *Dark Hole*), eles são empilhados na seguinte ordem estrita de prioridade:
1.  Efeitos **Obrigatórios** do Jogador do Turno (Turn Player).
2.  Efeitos **Obrigatórios** do Oponente.
3.  Efeitos **Opcionais** do Jogador do Turno.
4.  Efeitos **Opcionais** do Oponente.

---

## 6. Miss Timing ("Quando... você pode")
Efeitos de gatilho opcionais formatados como "When... you can" ("Quando... você pode") podem "perder o tempo" (Miss Timing). Isso ocorre se o evento que dispara a carta NÃO for a última coisa a acontecer na corrente. (ex: Usar um monstro como tributo para uma Invocação de Tributo; o tributo não foi a última ação, a invocação foi). Efeitos "If... you can" ("Se... você pode") ou mandatórios NUNCA perdem o tempo.

---

## 7. State Check Global, Cemitério e Condições de Vitória

### 7.1 State Check
O motor do jogo (no caso do C# + Lua, via `GlobalCheck` e flags de engine) checa o estado constantemente após cada ação:
*   LP chegaram a 0?
*   Existem monstros com 0 HP/Marcados para destruição que precisam ir ao cemitério?
*   Cartas contínuas precisam recalcular modificadores?
*   Condições de vitória por efeito foram batidas?

### 7.2 Fluxo de Cemitério
*   **Custos (Descartes, LP, Tributos):** Ocorrem no EXATO MOMENTO da Ativação (Elo da Corrente). **Nunca** são reembolsados, mesmo se a carta for negada.
*   **Materiais de Fusão / Ritual:** Vão ao cemitério apenas na Resolução do efeito da Magia.
*   **Cartas Destruídas:** Vão ao cemitério assim que a matemática as condena ou o efeito de destruição resolve.
*   **Magias / Armadilhas Normais:** Vão ao cemitério apenas no exato momento em que a *Corrente* (Chain) inteira em que elas participaram termina de resolver.

### 7.3 Condições de Vitória
Um duelo termina imediatamente (nem a corrente termina de resolver) se:
1.  Os Life Points de um jogador chegarem a 0.
2.  Um jogador tentar comprar uma carta e seu deck estiver vazio (Deck Out).
3.  Uma condição de vitória alternativa for cumprida (ex: Ter as 5 partes do *Exodia* na mão, ou as letras de *Destiny Board* no campo).

---

## 8. Sincronismo Visual e Animações (VFX & Flow)

A engine lógica (LUA) e a engine visual (C#/Unity) rodam em paralelo. Para garantir que os cálculos não aconteçam invisivelmente de forma instantânea, a arquitetura utiliza o padrão de **Congelamento Assíncrono (Yields e Callbacks)**.

### 8.1 Invocação (Summoning Flow)
1.  **Lógica:** O jogador/IA ativa a invocação.
2.  **Visual & Transições:** Se houver uma Cinemática, o `DuelFXManager` respeita o pacote configurado (`SummonVFXPackage`). 
    *   Um `FieldMarker` pode surgir com atraso (`delayBeforeMarker`).
    *   A carta gigante aguarda na tela (`giantCardHoldDuration`).
    *   Um atraso para o impacto (`delayBeforeImpact`) alinha o momento em que a carta bate no chão.
3.  **Impacto:** A miniatura aterrissa, tocando o Pulso de Impacto ou Prefab de Poeira.
4.  **Resolução:** Apenas *após a animação terminar*, o C# devolve o callback, permitindo ao LUA disparar o `EVENT_SUMMON_SUCCESS`.

*(Nota de Velocidade: Todo o timing visual acima é inversamente proporcional à propriedade `animationSpeed` do `DuelFXManager`. Uma velocidade global de 2.0x cortará todos esses tempos pela metade).*

### 8.2 Batalha e Dano (Combat Visual Flow)
A função `Core.Attack` no LUA orquestra os tempos visuais milimetricamente:
1.  **Declaração:** Dispara o evento de ataque. A UI abre a janela de resposta para armadilhas de combate.
2.  **Animação de Ataque:** Se o ataque não for negado, o Lua emite `yield ('PlayAttackAnimation')`. A Unity congela a lógica, move a carta do atacante (Tween de bote) e toca as faíscas/tiros no alvo.
3.  **Revelação Dramática:** Se o alvo estiver virado para baixo (Set), o Lua emite `yield ('RevealTarget')`. O C# vira a carta lentamente e insere um Delay (0.8s) para o jogador ler o que era antes da matemática agir.
4.  **Dano:** O cálculo ocorre. O C# treme a câmera (Screen Shake) e sobe os números de dano coloridos na tela.
5.  **Destruição:** A carta alvo recebe o VFX de estilhaços (Shatter), som de vidro quebrando, e é fisicamente arremessada ao cemitério.
*(Nota: Os rastros da espada voadora [Shadows vs ContinuousLine] e o tipo de ricochete em defesas falhas são customizáveis diretamente na UI).*

### 8.3 Resolução de Correntes (Chain Visuals)
*   **Ativação:** Ao ser ativada, a carta brilha (Verde para Magia, Roxo para Trap) para indicar quem é a fonte do efeito atual.
*   **Pilha:** Cada vez que um Elo (Link) é adicionado na janela de resposta, a engine visual cria um número holográfico (Link 1, Link 2...) flutuando sobre a carta.
*   **Resolução (LIFO):** A engine congela completamente as fases e UIs da partida. A corrente resolve de trás para frente. Assim que o último link resolve, as magias/armadilhas normais usadas recebem o VFX de destruição (Shatter) e vão ao cemitério, destravando o jogo.

### 8.4 Cinemáticas de Invocação Especial (Fusion & Ritual)
1.  **Gatilho:** O jogador ou IA ativa a invocação. As validações LUA ocorrem e os tributos/materiais vão ao cemitério.
2.  **Visual:** O `GameManager` esconde a carta temporariamente e invoca o `DuelFXManager`. A engine congela aguardando callback.
    *   **Fusão:** A tela escurece. As duas ou mais cartas de material aparecem em uma espiral giratória brilhante (estilo *Polymerization*) com duração de **≈ 1.5s**, culminando em um flash de luz.
    *   **Ritual:** Fogo azul contorna a tela, as cartas de sacrifício são consumidas em chamas (**≈ 1.5s**) e a carta de ritual ascende de um círculo mágico.
3.  **Resolução:** A carta gigante some, a miniatura final aterrissa no tabuleiro. O LUA é destravado e dispara o `EVENT_SPSUMMON_SUCCESS`.

### 8.5 Condições Especiais de Vitória (Game Over)
*   **Exodia:**
    *   **Gatilho:** State Check LUA/C# detecta as 5 peças na mão (`CheckExodiaWin`).
    *   **Tempo/Visual:** O jogo congela incondicionalmente (interrompendo qualquer corrente). A UI escurece, as 5 cartas holográficas voam para o centro formando um pentagrama. O raio "Obliterate" varre a tela (Animação de **≈ 3.0s**).
    *   **Resolução:** A `ExodiaWinUI` repassa o callback para `EndDuel(true)`, ignorando o zeramento de LP.
*   **Destiny Board:**
    *   **Gatilho:** Início/Fim do turno, o efeito contínuo adiciona uma letra.
    *   **Visual:** Overlay visual (Canvas) desenha letras espectrais e flamejantes flutuando sobre o tabuleiro (F - I - N - A - L). Há um delay dramático (**≈ 1.0s**) a cada letra invocada.
    *   **Resolução:** Ao formar a 5ª letra, um clarão negro assume a tela e chama o `EndDuel`.

### 8.6 Interface Tátil e Efeitos em Campo
*   **Magias de Campo (Field Spells):**
    *   **Gatilho:** Resolução final da ativação da carta na corrente.
    *   **Visual:** O tabuleiro antigo dissolve (Fade) e a nova imagem de fundo (`BoardBackground`) é aplicada com possível sobreposição (Ex: *Umi* adiciona filtro azul e partículas de bolhas subaquáticas). Nenhuma pausa mecânica é necessária.
*   **Efeito de Seleção (Targeting):**
    *   **Gatilho:** O LUA emite `yield` de espera para o C#.
    *   **Tempo/Visual:** Cartas não-selecionáveis escurecem. As cartas alvo válidas recebem um `Outline` ou uma mira pulsante. A engine **pausa (tempo indefinido)** aguardando o clique humano.
*   **Flip e Efeitos de Monstro:**
    *   **Gatilho:** Ativação manual (Ignition) ou no momento em que a carta é virada (Damage Step/Flip Summon).
    *   **Visual:** Uma onda de partículas rápidas (Flash mágico) irradia da carta e o `DuelFXManager` toca um "Pling!" sonoro (**≈ 0.5s** de delay visual) antes de confirmar o elo na corrente.
*   **Tokens (Fichas):**
    *   **Gatilho:** Resolução de comando LUA que chama `SpawnToken`.
    *   **Visual:** Uma nuvem de fumaça é instanciada na zona vazia. O som de "Poof" toca e a ficha 3D surge instantaneamente (**≈ 0.8s** de poeira assentando). O LUA dispara o gatilho de *Summon Success*.

### 8.7 Minigames e HUD Dinâmico
*   **Moeda (`CoinTossUI`) e Dado (`DiceRollUI`):**
    *   **Gatilho:** O script LUA ativa a função `Duel.TossCoin` ou `Duel.RollDice`.
    *   **Tempo/Visual:** O tabuleiro sofre desfoque (Blur). O minigame 3D surge no centro. Se a configuração exigir clique manual, o jogo aguarda. A animação de física da rolagem dura **≈ 1.5s a 2.0s** até estabilizar no resultado.
    *   **Resolução:** A UI de foco fecha, e a corrotina envia o número/resultado de volta para a máquina LUA, que avalia a consequência matemática (Ex: *Time Wizard* limpando o tabuleiro).
*   **Relógio de Turnos (`TurnClockUI`):**
    *   **Gatilho:** Instanciado por efeitos de contenção duradoura (ex: *Swords of Revealing Light*, *Final Countdown*).
    *   **Tempo/Visual:** O holograma de espadas ou o relógio permanecem fisicamente renderizados no HUD. A cada transição de fase correspondente, ocorre uma animação de "tique-taque" da UI, subtraindo 1 do mostrador (Delay de **≈ 0.8s** para leitura visual).
    *   **Resolução:** Ao atingir 0, há uma animação de vidro quebrando (Shatter), o holograma desaparece e o LUA encerra o efeito de campo restritivo, devolvendo o fluxo normal.

### 8.8 Manipulação de Deck e Mão (Card Flow VFX)
*   **Embaralhamento (Deck Shuffle):**
    *   **Gatilho:** Resolução de efeitos de busca (Ex: *Sangan*, *Reinforcement of the Army*) ou retorno ao deck (*Pot of Avarice*). O LUA chama `Duel.ShuffleDeck`.
    *   **Tempo/Visual:** A engine aciona `DeckManager.ShuffleDeck`. O motor interpreta qual das 3 rotinas o jogador escolheu (3D Clássico, Hindu Customizado ou 2D Lateral Rápido). 
    *   **Timing Crítico:** Para o LUA saber quando liberar a partida, o método `GetShuffleTotalDuration()` calcula matematicamente o tempo de cada loop + a duração do Prefab de Partícula + a pausa pós-embaralhamento (`shufflePostDelay`). Se esse valor dessincronizar, cartas podem ser compradas com o deck ainda embaralhando.
*   **Compra por Efeito (Draw) e Descarte (Discard):**
    *   **Gatilho:** Efeitos que compram ou descartam múltiplas cartas (Ex: *Pot of Greed*, *Graceful Charity*, *Card Destruction*).
    *   **Tempo/Visual:** O C# não teletransporta 5 cartas de uma vez. Uma corrotina aplica um micro-delay de **≈ 0.3s a 0.5s por carta**, criando uma cadência tátil (um por um) saindo do deck para a mão, ou da mão para o cemitério, permitindo que o cérebro do jogador processe a quantidade exata.

### 8.9 Janelas de Sacrifício e Banimento (Remoções Especiais)
*   **Sacrifício / Tributo (Tribute):**
    *   **Gatilho:** O LUA abre a seleção (`SelectReleaseGroup`) para invocar um monstro Nível 5+ ou para pagar o custo de um efeito (Ex: *Cannon Soldier*, *Share the Pain*).
    *   **Tempo/Visual:** O jogo pausa aguardando o clique do jogador (Highlight azul nos alvos válidos). Ao confirmar, o `DuelFXManager` toca o `PlayTributeEffect` (uma aura de alma ou feixe de luz azul ascendente engolindo o monstro). A engine aguarda **≈ 0.8s** para a carta dissolver visualmente antes de prosseguir com o LUA (instanciar o monstro novo ou disparar a magia).
*   **Removido de Jogo (Banish):**
    *   **Gatilho:** O LUA resolve um efeito de remoção (Ex: *Bottomless Trap Hole*, *Soul Release*).
    *   **Tempo/Visual:** Diferente da destruição comum (estilhaços), o `BanishCard` invoca um vórtice negro ou uma distorção espacial (Fenda) sobre a carta. A carta é "sugada" ou encolhida até sumir acompanhada de um som de vácuo. O delay de **≈ 1.0s** é respeitado antes de a carta ser movida para a zona de Banidos (Removed).
*   **Retorno para a Mão ou Deck (Bounce / Spin):**
    *   **Gatilho:** Efeitos como *Penguin Soldier* ou *Raiza the Storm Monarch*.
    *   **Tempo/Visual:** A carta é envolta em um furacão rápido ou brilho branco. Ela não quebra. Ela voa fisicamente em direção à Mão (Fade out rápido) ou em direção ao Topo do Deck, levando **≈ 0.6s** para a transição.
*   **Mudança de Controle (Control Swap):**
    *   **Gatilho:** Efeitos como *Change of Heart* ou *Snatch Steal*.
    *   **Tempo/Visual:** A carta levita, atravessa o centro do tabuleiro até a zona inimiga e rotaciona seu Eixo Z em 180º para refletir o novo dono. A engine obriga um delay de **≈ 0.8s** para leitura visual do "roubo".

### 8.10 Micro-Interações e Interface Contínua (UX Flow)
Para evitar bugs de "ciclo de vida" (como UIs sumindo instantaneamente, cliques fantasmas ou o LUA atropelando animações), a relação entre o mouse e as cartas segue regras estritas de interrupção:
*   **Hovering e Destaques (Mouse Over):**
    *   **Fluxo:** Ocorre em tempo real (EventSystem). A carta sobe no Eixo Y e acende o `Outline`. A transição visual é sutil (**≈ 0.1s**).
    *   **Trava de Segurança:** Durante a resolução de qualquer *Corrente* ou *Cinemática*, a UI sofre um "Lock". A mão do jogador recebe `interactable = false`. O Hover é mecanicamente desligado para impedir que o jogador dispare um clique que a engine LUA não está aguardando, o que causaria dessincronização fatal.
*   **Tooltip Dinâmico do Mouse (`MouseTooltipUI`):**
    *   **Fluxo:** É um elemento visual passivo que segue o cursor em *overlay*. Ele não tem lógica de jogo, apenas lê o `PhaseManager` e as flags da carta para atualizar os textos de "Esquerdo/Direito".
    *   **Tempo:** Ele não pausa a engine. Seu desaparecimento ou instabilidade não afeta o duelo, pois o gatilho real sempre será retido no evento `OnPointerClick` do objeto físico da carta.
*   **Seleção de Ataque e a "Espadinha" (Combat Targeting):**
    *   **Gatilho/UI:** O jogador clica no atacante. O `CardDisplay` chama `SetAttackSelectionVisual(true)` (Borda Vermelha/Escurecer). Para manter a tela limpa, o `GameManager` oculta todos os marcadores estáticos de `CanAttack` desse monstro no exato momento.
    *   **Mira Física (`TargetingSwordUI`):** O script autônomo assume o controle, lendo a customização visual do `DuelFXManager`, ancorando-se no atacante e girando para seguir o mouse. Se um alvo for clicado e exigir confirmação de UI (Modal Sim/Não), a espada trava a mira (`LockOn`) diretamente sobre o monstro inimigo.
    *   **A Ação e Voo:** Com o alvo confirmado, a UI da mira (`TargetingSwordUI.Hide()`) desaparece e a requisição viaja para o LUA (`Core.Attack`). O LUA emite `yield("PlayAttackAnimation")`, obrigando o `DuelFXManager` a instanciar o voo de ataque (A "Espadinha de Voo" ou Projétil). O motor congela por **≈ 0.4s**. Somente após o impacto do voo no inimigo, a janela de resposta (para ativar uma *Mirror Force*) é aberta.
*   **Equipamentos e Vínculos Físicos (`CardLink`):**
    *   **Fluxo de Montagem:** A magia de equipamento resolve. A carta é enviada para a Zona de S/T. A Unity então cria um objeto fantasma chamado `CardLink` (A "algema" que une a magia ao monstro).
    *   **Tempo/Visual:** Um feixe de luz liga as duas cartas (**≈ 0.5s**).
    *   **Sincronismo de Engine:** O C# **NÃO** soma os pontos de ATK/DEF instantaneamente. Ele chama a corrotina `RecalculateStatsNextFrame()`. Ele espera exato 1 Frame da engine gráfica para garantir que o componente `CardLink` não é nulo antes de fazer a matemática. Isso impede que o LUA crashe buscando um vínculo que ainda estava sendo desenhado.
*   **Fundos de Tabuleiro (Backgrounds / Field Spells):**
    *   **Fluxo:** Alterados via *Magias de Campo*. 
    *   **Tempo/Visual:** A transição do fundo 2D (Crossfade) roda em paralelo. Ela dura **≈ 0.8s** e **NÃO PAUSA** o motor LUA.
    *   **Trava de Segurança:** Partículas de campo dissolvem graciosamente, sem causar *NullReferenceException*.

### 8.11 Anúncios de Fase e Fluxo de HUD
Sempre que a máquina de estados (`PhaseManager`) transita para uma nova etapa, a partida é temporariamente interrompida para um informe visual em texto:
*   **A Rotina (`PhaseAnnouncementSettings`):** O `GameManager` instancia um `TextMeshPro` dinâmico. O texto (ex: "BATTLE PHASE") desliza pela tela (`slideDistance`), permanece parado (`displayDuration`) e realiza um fade out (`fadeDuration`).
*   **Sincronismo Estrito:** Para evitar atropelos visuais (como o jogo sacar a carta antes de dar tempo de ler "DRAW PHASE"), as corrotinas principais do C# leem ativamente a variável `displayDuration`. A IA e o motor de compra de cartas **congelam** pelo tempo exato que o texto fica na tela, garantindo que o jogador consiga ler qual fase acabou de começar antes da ação estourar.
*   **A "Espada" Indicadora:** Guiada por `AttackIndicatorMode`. Se configurada para `AlwaysInBattlePhase`, no exato frame em que o texto "BATTLE PHASE" some, o motor acende automaticamente a UI nativa de `CanAttack` e `Block` sobre todas as cartas relevantes, limpando o tabuleiro apenas quando o jogador sair da fase.

### 8.12 Fila Visual e Cadência Estrita (Game Feel & Visual Queue)
Diferente de jogos de ação que processam múltiplos danos simultaneamente numa nuvem de números caótica, a nossa Engine adota a filosofia de **Cadência Estrita** (inspirada em RPGs clássicos) para garantir legibilidade absoluta durante encadeamentos longos (Ex: Múltiplos danos de efeito na mesma corrente, ou curas sequenciais da carta *Jackpot 7*).
A matemática do LUA ocorre instantaneamente nas sombras, mas a Interface (Unity C#) utiliza uma Fila Visual (`visualEventQueue`) e a Trava Mestra `pendingVisualTasks` para ditar o ritmo da tela e "congelar" o LUA (obrigando-o a cruzar os braços enquanto as animações acontecem).

**Tempos Padrões de Interface (Timings & Delays):**
*   **Rolagem do Placar (LP Roll):** A animação do número de Vida na UI leva **exatos 0.5s** para ir do valor antigo ao novo (efeito velocímetro), independentemente de quão grande seja o valor curado ou recebido.
*   **Respiro Dramático (LP Pause):** Após o placar terminar de rolar fisicamente, a Fila Visual aguarda rigidamente **0.2s** antes de liberar a próxima tarefa. Isso evita que popups de dano encavalem visualmente e dá "peso" à absorção daquela consequência pelo cérebro do jogador.
*   **Popups Flutuantes (Scatter):** Os números de dano/cura não nascem no centro exato do alvo para não formarem um bloco ilegível. Eles sofrem um espalhamento (Scatter) aleatório de **X: -30 a 30px** e **Y: -20 a 20px** e flutuam para cima antes do Fade Out.
*   **Destruição em Série:** Quando um efeito pede que várias cartas explodam em sequência gerando status, o motor aplica a regra do compasso: A próxima explosão da lista só ocorre após um repouso de **0.15s** após a Fila Visual da carta anterior (Explosão -> Envio ao GY -> Rolagem de LP de 0.5s + 0.2s de pausa) relatar 100% de conclusão.
*   **Trava de Corrente LUA:** Todos os processos de resolução LIFO (`ExecuteBattlePhaseLogic`, `ProcessTriggersRoutine`, etc) agora escutam o `GameManager.Instance.pendingVisualTasks > 0`. Enquanto a UI de placar estiver dançando, a Corrente do Yu-Gi-Oh! não avança.