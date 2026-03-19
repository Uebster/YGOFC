# Funções do GameManager

O `GameManager` é um Singleton (`GameManager.Instance`) acessível globalmente.

## Controle de Duelo
*   `StartDuel()`: Inicia um duelo (Free Duel ou Campanha). Limpa o tabuleiro, embaralha decks e compra mãos iniciais.
*   `EndDuel(bool playerWon)`: Finaliza o duelo, calcula pontuação e rank.
*   `CleanupDuelState()`: Reseta todas as listas e destrói objetos visuais.

## Ações de Cartas
*   `DrawCard(bool ignoreLimit)`: Compra uma carta do Deck do jogador.
*   `DrawOpponentCard()`: Compra uma carta para o oponente.
*   `TrySummonMonster(cardGO, data, isSet)`: Tenta invocar um monstro (valida regras via SummonManager).
*   `PerformSpecialSummon(cardGO, data)`: Abre o modal de escolha de posição e realiza Special Summon.
*   `PlaySpellTrap(cardGO, data, isSet)`: Ativa ou Seta uma Magia/Armadilha.
*   `ActivateFieldSpellTrap(cardGO)`: Ativa uma carta que já estava setada no campo.
*   `TributeCard(card)`: Envia uma carta do campo para o cemitério como tributo (com VFX).
*   `SendToGraveyard(card, isPlayer)`: Envia uma carta para o cemitério e atualiza visuais.
*   `RemoveFromPlay(card, isPlayer)`: Bane uma carta (Remove de jogo).
*   `DiscardCard(card)`: Descarta uma carta da mão para o cemitério.
*   `DiscardHand(isPlayer)`: Descarta toda a mão.
*   `ReturnToHand(card)`: Retorna uma carta do campo para a mão.
*   `ReturnToDeck(card, toTop)`: Retorna uma carta para o Deck (topo ou fundo).
*   `ShuffleDeck(isPlayer)`: Embaralha o deck.
*   `MillCards(isPlayer, amount)`: Envia cartas do topo do deck para o cemitério.
*   `CreateCardLink(source, target, type)`: Cria um vínculo lógico (ex: Equipamento) entre duas cartas.

## Visualização e UI
*   `UpdateCardViewer(card, isFaceUp)`: Mostra a carta no painel lateral esquerdo (Card Viewer).
*   `ViewGraveyard(isPlayer)`: Abre o visualizador de Cemitério.
*   `ViewExtraDeck(isPlayer)`: Abre o visualizador de Extra Deck.
*   `ViewRemovedCards(isPlayer)`: Abre o visualizador de Cartas Removidas.
*   `UpdatePileVisuals()`: Atualiza a altura e topo visual dos Decks e Cemitérios 3D.

## Vida e Dano
*   `DamagePlayer(int amount)`: Reduz LP do jogador, atualiza UI e checa derrota.
*   `DamageOpponent(int amount)`: Reduz LP do oponente, atualiza UI e checa vitória.
*   `PayLifePoints(isPlayer, amount)`: Tenta pagar LP. Retorna `false` se não tiver o suficiente.
*   `GainLifePoints(isPlayer, amount)`: Aumenta os LP.

## Dados e Perfil
*   `GetPlayerMainDeck()`: Retorna a lista atual do Deck.
*   `PlayerHasCard(id)`: Verifica se o jogador possui uma carta no Trunk (Baú).
*   `SetPlayerProfile(name, saveID)`: Atualiza dados do perfil.

## Modos de Jogo (Variáveis Públicas)
*   `devMode`: Habilita trapaças (sacar a qualquer hora, controlar oponente).
*   `effectTestMode`: Habilita menu de teste de VFX.
*   `testOpponentID`: ID do personagem oponente para teste rápido (ex: "021_kaiba").
*   `testPlayerID`: ID do personagem para substituir o deck do jogador (ex: "020_pegasus").
*   `testActThemeIndex`: Número do Ato (1-10) para forçar o carregamento de um tema visual específico.
*   `showOpponentHand`: Mostra cartas do oponente viradas para cima (Cheat).
*   `canPlayerDrawFromDeck`: Permite clique no deck.
*   `ToggleFullscreen()`: Alterna entre modo janela e tela cheia (Atalho: F11 em DevMode).
*   `fillOpponentZonesFromRight`: Define se o oponente preenche as zonas da direita para a esquerda (perspectiva do jogador), simulando a esquerda dele.

## Configurações de Velocidade
*   `playerDrawSpeed`: Tempo (em segundos) entre cada carta comprada pelo jogador na animação inicial.
*   `opponentDrawSpeed`: Tempo (em segundos) entre cada carta comprada pelo oponente na animação inicial.

## Variáveis de Estado e Controle (Inspector)

### Controle de Fluxo
*   `isDuelOver`: (Read-only) Indica se o duelo terminou. Bloqueia ações de jogo quando verdadeiro.
*   `isSimulating`: Indica se o "Simulador de Caos" está rodando. Ignora certas travas de segurança e UI.
*   `turnCount`: Contador de turnos do duelo.

### Regras e Permissões
*   `canPlacePlayerCards`: Define se o jogador pode baixar cartas da mão (Invocar/Setar/Ativar). Usado para efeitos de restrição.
*   `canPlaceOpponentCards`: (Dev) Permite ao jogador manipular as cartas da mão do oponente.
*   `revealOpponentDraw`: Se verdadeiro, as cartas compradas pelo oponente são reveladas (efeito de *Pikeru's Second Sight*).

### Visualização
*   `use3DFlipEffect`: Define o estilo de virar as cartas (Rotação 3D vs Troca de Textura 2D).
*   `enableFieldHoverOutline`: Ativa o brilho ao passar o mouse sobre cartas no campo (Monstros/S&T).
*   `enableDeckHoverOutline` / `enableGraveyardHoverOutline`: Controles granulares para o brilho ao passar o mouse sobre as pilhas (Deck, GY, etc).
*   `showOpponentHand`: (Dev) Mostra as cartas da mão do oponente viradas para cima.
*   `enableTurnClockVisuals`: Ativa a exibição de um relógio sobre cartas que possuem contagem de turnos (ex: *Swords of Revealing Light*). Requer `turnClockPrefab`.
*   `enableTributeSummonAnimation`: Ativa um efeito visual específico ao realizar uma Invocação-Tributo.

## Ferramentas de Debug e Teste (No GameManager)
*   `devMode`: Habilita trapaças gerais e logs detalhados.
*   **Hierarquia de Teste Direto:** O sistema agora segue uma prioridade para as flags de teste direto. Se mais de uma estiver marcada, a de maior prioridade será executada.
    1.  `testDuelDirectly`: (Prioridade 1) Se marcado, o jogo pula todos os menus e inicia um duelo livre imediatamente.
    2.  `testDeckBuilderDirectly`: (Prioridade 2) Se `testDuelDirectly` estiver desmarcado e este estiver marcado, o jogo pula para a página de Construção de Deck.
    3.  `testLibraryDirectly`: (Prioridade 3) Se os dois anteriores estiverem desmarcados e este estiver marcado, o jogo pula para a página da Biblioteca de Cartas.
*   **Nota de Implementação:** O pulo para as telas de teste é feito com um pequeno atraso (1 frame) para garantir que o `GameManager` tenha tempo de inicializar os dados do jogador (como a coleção de cartas) antes que a tela de destino tente acessá-los.
*   `unlockAllCards`: (Requer `devMode`) Se marcado, ao iniciar o jogo, adiciona 3 cópias de TODAS as cartas do banco de dados ao Baú (Trunk). **Nota:** As cartas adicionadas por este método são automaticamente marcadas como "Usadas" para evitar que a Biblioteca fique cheia de tags "New".
*   `disableDeckShuffle`: Impede o embaralhamento inicial. As cartas virão na ordem que estão no JSON/Lista. Útil para testar combos específicos ("Stacking the Deck").
*   `forcePlayerGoingFirst`: Força o jogador a sempre ter o primeiro turno, ignorando a aleatoriedade.
*   `infiniteLP`: O jogador não toma dano. Útil para testar interações de batalha sem risco de Game Over.
*   `infiniteNormalSummons`: Remove o limite de 1 Invocação Normal/Set por turno.
*   `disableTributeRequirements`: Permite invocar monstros de Nível 5 ou superior sem oferecer tributos.
*   `disableFusionCost`: Invocação-Fusão não consome os materiais nem a carta mágica (permite reutilizar *Polymerization*).
*   `disableRitualCost`: Invocação-Ritual não consome os tributos nem a carta mágica.
*   `alwaysCoinHead`: Força o resultado do lançamento de moeda a ser sempre "Cara" (Heads). Útil para testar cartas de aposta.
*   `enableHandLimit`: Ativa/Desativa a regra de limite de cartas na mão.
*   `allowForbiddenCards`: Se marcado, permite adicionar cartas Proibidas (Limit 0) aos decks, tratando-as como Limitadas (Limit 1).
*   `disableBanlist`: Se marcado, ignora completamente a lista de restrições. Todas as cartas (incluindo Proibidas e Limitadas) podem ter até 3 cópias no deck.
*   `handLimit`: Define o limite máximo de cartas na mão ao final do turno (Padrão: 6). Se excedido, o jogador deve descartar.
*   `placeTributeSummonInTributeZone`: Se marcado, monstros invocados por tributo ocupam a zona do primeiro monstro sacrificado. Se desmarcado, vão para a primeira zona livre.
*   **Atalho F11:** Se `devMode` estiver ativo, pressionar F11 alterna o modo Fullscreen instantaneamente.

### Opções de Input e UI
*   `enableRightClickPhaseMenu`: Habilita abrir um menu de seleção de fase ao clicar com o botão direito no campo (área vazia).
*   `confirmBattlePositionChange`: Se desmarcado, a mudança de posição de batalha (Ataque/Defesa) ocorre imediatamente ao clicar, sem janela de confirmação.
*   `confirmAttackTarget`: Se desmarcado, o ataque ocorre imediatamente ao clicar no alvo, sem janela de confirmação.
*   `useDirectHandSelection`: Se marcado, permite selecionar cartas da mão (para descarte/efeitos) clicando diretamente nelas, em vez de abrir uma janela de lista.
*   `confirmHandSelection`: Se marcado, pede confirmação após selecionar a carta na mão durante a seleção direta.
*   `useMouseTooltipUI`: Se marcado, ativa o prefab dinâmico de mouse e executa as ações baseado no clique esquerdo/direito da carta, ignorando a interface clássica do `DuelActionMenu`.
*   `quickSummonFromHand`: Se marcado, permite invocar monstros da mão com um único clique (Esquerdo = Ataque, Direito = Defesa/Set), pulando o menu de ação.
*   `quickSpellTrapFromHand`: Se marcado, permite jogar Spells/Traps da mão com um único clique (Esquerdo = Ativar, Direito = Setar), pulando o menu de ação.
*   `quickAttackDirectly`: Se marcado, clicar no seu monstro durante a Battle Phase realiza um ataque direto imediatamente se o oponente não tiver monstros (evita ter que clicar no campo do oponente).

## Notas de Implementação
*   **Extra Deck:** Agora é renderizado virado para baixo (Face-Down), mas o dono pode visualizar o conteúdo passando o mouse (Card Viewer).
*   **Nomes na UI:** Ao usar `testPlayerID` ou `testOpponentID`, os nomes na interface de duelo são atualizados automaticamente para corresponder aos personagens carregados.
*   **Fallback de Deck:** Se o deck do oponente falhar ao carregar (IDs inválidos), um deck aleatório de emergência é gerado para evitar travamento do jogo.
*   **Rotação de Decks:** Oponentes agora escolhem aleatoriamente entre seus Decks A, B e C (se disponíveis). Se um deck falhar, o sistema tenta o Deck A como fallback.
*   **Posicionamento de Tributo:** Ao realizar uma Invocação-Tributo manual, o novo monstro ocupará a zona do primeiro monstro selecionado como tributo.
*   **Limite de Mão:** Implementada a regra de descarte na End Phase se a mão exceder o limite (6). A IA possui lógica para descartar estrategicamente (ex: descartar monstros de nível alto se tiver *Monster Reborn*).

## Mecânicas Especiais
*   `BeginFusionSummon(source)`: Inicia o fluxo de UI para Invocação-Fusão.
*   `PerformFusionSummon(...)`: Executa a fusão consumindo materiais.
*   `BeginRitualSummon(source)`: Inicia o fluxo de UI para Invocação-Ritual.
*   `PerformRitualSummon(...)`: Executa o ritual consumindo tributos.
*   `TossCoin(count, callback)`: Executa animação de moeda e retorna resultado.
*   `SpawnToken(...)`: Cria um Token Monster no campo.