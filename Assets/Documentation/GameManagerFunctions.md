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

## Ferramentas de Debug e Teste

### No UIManager (Inspector)
*   `testDuelDirectly`: Se marcado, o jogo pula todas as telas de menu (Abertura, Start, Seleção de Save) e vai direto para o Duelo ao dar Play no Unity. Essencial para iteração rápida.

### No GameManager (Inspector)
*   `devMode`: Habilita trapaças gerais e logs detalhados.
*   `disableDeckShuffle`: Impede o embaralhamento inicial. As cartas virão na ordem que estão no JSON/Lista. Útil para testar combos específicos ("Stacking the Deck").
*   `forcePlayerGoingFirst`: Força o jogador a sempre ter o primeiro turno, ignorando a aleatoriedade.
*   `infiniteLP`: O jogador não toma dano. Útil para testar interações de batalha sem risco de Game Over.
*   `infiniteNormalSummons`: Remove o limite de 1 Invocação Normal/Set por turno.
*   `disableTributeRequirements`: Permite invocar monstros de Nível 5 ou superior sem oferecer tributos.
*   `disableFusionCost`: Invocação-Fusão não consome os materiais nem a carta mágica (permite reutilizar *Polymerization*).
*   `disableRitualCost`: Invocação-Ritual não consome os tributos nem a carta mágica.
*   `alwaysCoinHead`: Força o resultado do lançamento de moeda a ser sempre "Cara" (Heads). Útil para testar cartas de aposta.

## Notas de Implementação
*   **Extra Deck:** Agora é renderizado virado para baixo (Face-Down), mas o dono pode visualizar o conteúdo passando o mouse (Card Viewer).
*   **Nomes na UI:** Ao usar `testPlayerID` ou `testOpponentID`, os nomes na interface de duelo são atualizados automaticamente para corresponder aos personagens carregados.
*   **Fallback de Deck:** Se o deck do oponente falhar ao carregar (IDs inválidos), um deck aleatório de emergência é gerado para evitar travamento do jogo.
*   **Rotação de Decks:** Oponentes agora escolhem aleatoriamente entre seus Decks A, B e C (se disponíveis). Se um deck falhar, o sistema tenta o Deck A como fallback.

## Mecânicas Especiais
*   `BeginFusionSummon(source)`: Inicia o fluxo de UI para Invocação-Fusão.
*   `PerformFusionSummon(...)`: Executa a fusão consumindo materiais.
*   `BeginRitualSummon(source)`: Inicia o fluxo de UI para Invocação-Ritual.
*   `PerformRitualSummon(...)`: Executa o ritual consumindo tributos.
*   `TossCoin(count, callback)`: Executa animação de moeda e retorna resultado.
*   `SpawnToken(...)`: Cria um Token Monster no campo.