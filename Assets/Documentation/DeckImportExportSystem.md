# Sistema de Importação e Exportação de Decks (Interno)

## Visão Geral
Este sistema permite ao jogador salvar ("exportar") e carregar ("importar") receitas de decks. Diferente de um sistema baseado em arquivos `.ydk` externos, este mecanismo salva as listas de decks diretamente no arquivo de save do jogador (`.save`), garantindo que os decks estejam vinculados ao perfil e progresso do jogador.

## Estrutura da UI
O sistema utiliza um painel principal (`Panel_ImportExport`) que contém dois sub-painéis, um para importação e outro para exportação. A visibilidade deles é controlada pelo `DeckImportExportManager`.

### Panel_Import
Usado para selecionar um deck salvo e carregá-lo no Deck Builder.
*   **Scroll View**: Lista os decks salvos no perfil atual.
*   **Btn_ImportDeck**: Botão de ação que carrega o deck selecionado.
*   **Btn_BackToDeckBuilder**: Fecha o painel e retorna ao Deck Builder.

### Panel_Export
Usado para nomear o deck atual e salvá-lo no perfil.
*   **Input_DeckName**: Campo de texto para o jogador digitar o nome do deck.
*   **Scroll View**: Lista os decks já salvos. Clicar em um preenche o campo de nome, permitindo sobrescrever.
*   **Btn_ExportDeck**: Botão de ação que salva o deck atual com o nome fornecido.
*   **Btn_BackToDeckBuilder**: Fecha o painel.

## Script Principal: `DeckImportExportManager.cs`
Este script gerencia a lógica para os painéis de importação e exportação.

*   **Modos de Operação (`MenuType` enum):**
    *   `Import`: Configura o painel para o modo de importação.
    *   `Export`: Configura o painel para o modo de exportação, exibindo o campo de input de nome.
*   **`Setup(MenuType menuType)`**: Método chamado pelo `DeckBuilderManager` para inicializar o painel no modo correto.
*   **`RefreshList()`**: Carrega a lista de `DeckRecipe` do `SaveLoadSystem` e popula a `ScrollView` com prefabs que representam cada deck salvo.
*   **Ações:**
    *   **Exportar:** Pega o nome do `TMP_InputField`, chama `DeckBuilderManager.Instance.ExportCurrentDeck(deckName)` para salvar a receita no `SaveLoadSystem`, e atualiza a lista.
    *   **Importar:** Pega o nome do deck selecionado, chama `DeckBuilderManager.Instance.ImportDeck(deckName)` para carregar as cartas no Deck Builder, e fecha o painel.

## Armazenamento de Dados (`SaveLoadSystem.cs`)
A persistência dos decks é gerenciada pelo sistema de save principal.

*   **`DeckRecipe` (classe):** Uma nova estrutura de dados que contém:
    *   `deckName` (string)
    *   `mainDeckCardIDs` (List<string>)
    *   `sideDeckCardIDs` (List<string>)
    *   `extraDeckCardIDs` (List<string>)
*   **`GameSaveData` (classe):** A classe principal de save agora contém uma `List<DeckRecipe> savedDecks`.
*   **Métodos no `SaveLoadSystem`:**
    *   `GetSavedDecks()`: Retorna a lista de receitas de deck do save atual.
    *   `SaveDeckRecipe(...)`: Cria ou sobrescreve uma `DeckRecipe` no save atual.
    *   `LoadDeckFromRecipe(...)`: Retorna as listas de IDs de um deck salvo pelo nome.
    *   `DeleteDeckRecipe(...)`: Remove uma receita de deck do save.

## Integração com `DeckBuilderManager.cs`
*   Os botões `Btn_Import` e `Btn_Export` no `Panel_DeckBuilder` agora ativam o `Panel_ImportExport`.
*   O `DeckBuilderManager` chama o método `Setup()` do `DeckImportExportManager` para configurar o modo correto (Importar ou Exportar).
*   Foram adicionados os métodos públicos `ExportCurrentDeck(string deckName)` e `ImportDeck(string deckName)` para serem chamados pelo `DeckImportExportManager`, servindo como uma ponte entre a UI de import/export e a lógica de construção de decks.

## Prefab do Slot de Deck
*   **ImportExportItem** `[Image, Button, SaveSlotUI]`
    *   **Name** `[TextMeshProUGUI]`
    *   **Date** `[TextMeshProUGUI]`