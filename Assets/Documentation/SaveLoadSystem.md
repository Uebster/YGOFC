# Sistema de Save/Load

## Visão Geral
O sistema de salvamento (`SaveLoadSystem.cs`) é responsável por persistir o progresso do jogador, sua coleção de cartas e seus decks entre as sessões de jogo. Ele utiliza serialização JSON para armazenar dados complexos e `PlayerPrefs` para configurações simples.

## Estrutura de Dados (`PlayerData`)
O arquivo de save principal contém um objeto `PlayerData` com os seguintes campos:
*   **Profile:** Nome do jogador, Avatar ID.
*   **Progress:** Nível máximo desbloqueado na campanha (1-100).
*   **Collection (Trunk):** Lista de IDs de todas as cartas que o jogador possui.
*   **Decks:**
    *   `MainDeck`: Lista de IDs das 40-60 cartas do baralho principal.
    *   `ExtraDeck`: Lista de IDs das cartas de Fusão.
    *   `SideDeck`: (Opcional) Cartas de reserva.
*   **Economy:** Starchips (moeda) acumulados.
*   **Stats:** Vitórias, Derrotas, Rank S obtidos.

## Localização dos Arquivos
*   **Caminho:** `Application.persistentDataPath + "/Saves/"`
*   **Formato:** `.json` (Texto legível para debug, mas pode ser criptografado no futuro).
*   **PlayerPrefs:** Armazena o `LastLoadedSaveID` para carregar automaticamente o último perfil jogado.

## Fluxo de Salvamento
1.  **Auto-Save:** Ocorre após cada duelo (vitória ou derrota), após editar o deck e ao comprar cartas/password.
2.  **Manual Save:** Disponível no menu "Home" (Acampamento).

## Integração com `InitialDeckBuilder`
Ao iniciar um "New Game", se não houver arquivo de save, o `GameManager` solicita ao `InitialDeckBuilder` que gere um deck inicial procedural e imediatamente chama `SaveLoadSystem.SaveGame()` para criar o arquivo físico.