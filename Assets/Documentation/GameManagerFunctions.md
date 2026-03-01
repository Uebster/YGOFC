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
*   `SendToGraveyard(card, isPlayer)`: Envia uma carta para o cemitério e atualiza visuais.
*   `RemoveFromPlay(card, isPlayer)`: Bane uma carta (Remove de jogo).

## Visualização e UI
*   `UpdateCardViewer(card, isFaceUp)`: Mostra a carta no painel lateral esquerdo (Card Viewer).
*   `ViewGraveyard(isPlayer)`: Abre o visualizador de Cemitério.
*   `ViewExtraDeck(isPlayer)`: Abre o visualizador de Extra Deck.
*   `ViewRemovedCards(isPlayer)`: Abre o visualizador de Cartas Removidas.
*   `UpdatePileVisuals()`: Atualiza a altura e topo visual dos Decks e Cemitérios 3D.

## Vida e Dano
*   `DamagePlayer(int amount)`: Reduz LP do jogador, atualiza UI e checa derrota.
*   `DamageOpponent(int amount)`: Reduz LP do oponente, atualiza UI e checa vitória.

## Dados e Perfil
*   `GetPlayerMainDeck()`: Retorna a lista atual do Deck.
*   `PlayerHasCard(id)`: Verifica se o jogador possui uma carta no Trunk (Baú).
*   `SetPlayerProfile(name, saveID)`: Atualiza dados do perfil.

## Modos de Jogo (Variáveis Públicas)
*   `devMode`: Habilita trapaças (sacar a qualquer hora, controlar oponente).
*   `effectTestMode`: Habilita menu de teste de VFX.
*   `canPlayerDrawFromDeck`: Permite clique no deck.
*   `showOpponentHand`: Mostra cartas do oponente viradas para cima.