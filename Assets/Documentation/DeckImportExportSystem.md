# Sistema de Importação e Exportação de Decks (Interno)

## Visão Geral
Este sistema permite ao jogador salvar ("exportar") e carregar ("importar") receitas de decks. Diferente de um sistema baseado em arquivos `.ydk` externos, este mecanismo salva as listas de decks diretamente no arquivo de save do jogador (`.save`), garantindo que os decks estejam vinculados ao perfil e progresso do jogador.

## Estrutura da UI
O sistema utiliza um painel principal (`Panel_ImportExport`) que é ativado a partir do Deck Builder. A sua aparência e funcionalidade são alteradas dinamicamente pelo `DeckImportExportManager` dependendo da ação (Importar ou Exportar).

*   **Scroll View**: Em ambos os modos, exibe uma lista dos decks já salvos no perfil atual.
*   **Botão de Ação Principal**: Muda de "Import" para "Export" dependendo do modo.
*   **Campo de Nome (`Input_DeckName`):** Visível apenas no modo `Export`. Permite ao jogador nomear o deck a ser salvo. Clicar em um deck existente na lista preenche este campo, facilitando a sobrescrita.
*   **Botão de Deletar:** Permite apagar um deck selecionado da lista.

#### Estrutura de UI (Hierarquia)
Abaixo está a estrutura recomendada para os painéis de Importação e Exportação.

*   **Panel_Export** `[Image, DeckImportExportManager]`
    *   **Panel_ExportHeader** `[Image]`
        *   **Text (TMP)** `[TextMeshProUGUI]` (Título: "Export Deck")
    *   **Scroll View** `[Image, ScrollRect]`
        *   **Viewport** `[Image, Mask]`
            *   **Content** `[VerticalLayoutGroup]` (Onde os `DeckSlotUI` são instanciados)
    *   **InputDeckName** `[Image, TMP_InputField]`
    *   **Btn_ExportDeck** `[Image, Button]` (Botão de Ação Principal)
    *   **Btn_DeleteDeck** `[Image, Button]` (Botão para Deletar Deck Selecionado)
    *   **Panel_ImportFooter** `[Image]`
        *   **Btn_BackToDeckBuilder** `[Image, Button]`
    *   **ConfirmationExport** `[Image]`
        *   **Text (TMP)** `[TextMeshProUGUI]`
        *   **Btn_Yes** `[Image, Button]`
            *   **Text (TMP)** `[TextMeshProUGUI]`
        *   **Btn_No** `[Image, Button]`
            *   **Text (TMP)** `[TextMeshProUGUI]`

*   **Panel_Import** `[Image, DeckImportExportManager]`
    *   *(Estrutura similar ao Export, mas sem os botões de Input e Delete)*

## Arquitetura e Scripts

### 1. `DeckImportExportManager.cs` (Gerenciador da UI)
Este script é o cérebro do painel de import/export.
*   **Modos de Operação (`MenuType` enum):**
    *   `Import`: Configura o painel para carregar um deck. O botão de ação principal importará o deck selecionado.
    *   `Export`: Configura o painel para salvar o deck atual. O campo de nome fica visível.
*   **`Setup(MenuType menuType)`**: Método chamado pelo `DeckBuilderManager` para inicializar o painel no modo correto.
*   **`RefreshList()`**: Carrega a lista de `DeckRecipe` do `SaveLoadSystem` e popula a `ScrollView` com prefabs `DeckSlotUI`, que exibem o nome e a contagem de cartas de cada deck salvo.

### 2. `SaveLoadSystem.cs` (Persistência de Dados)
O sistema de save principal é responsável por armazenar os decks.
*   **`DeckRecipe` (classe):** Uma nova estrutura de dados que contém:
    *   `deckName` (string)
    *   `mainDeckCardIDs` (List<string>)
    *   `sideDeckCardIDs` (List<string>)
    *   `extraDeckCardIDs` (List<string>)
*   **`GameSaveData` (classe):** A classe principal de save agora contém uma `List<DeckRecipe> savedDecks`.
*   **Métodos de Acesso:**
    *   `GetSavedDecks()`: Retorna a lista de receitas de deck do save atual.
    *   `SaveDeckRecipe(...)`: Cria ou sobrescreve uma `DeckRecipe` no save atual.
    *   `LoadDeckFromRecipe(...)`: Retorna as listas de IDs de um deck salvo pelo nome.
    *   `DeleteDeckRecipe(...)`: Remove uma receita de deck do save.
    *   **Importante:** Após qualquer modificação nas receitas (`SaveDeckRecipe` ou `DeleteDeckRecipe`), é crucial chamar `SaveLoadSystem.Instance.SaveGame()` para persistir essas alterações no arquivo `.save`.

### 3. `DeckBuilderManager.cs` (Ponte de Lógica)
Atua como uma ponte entre a UI de import/export e a lógica de construção de decks.
*   **`ExportCurrentDeck(string deckName)`**: Chamado pelo `DeckImportExportManager`, este método coleta as listas de cartas atuais (Main, Side, Extra) e as passa para o `SaveLoadSystem` para serem salvas.
*   **`ImportDeck(string deckName)`**: Chamado pelo `DeckImportExportManager`, este método recebe as listas de IDs de cartas do `SaveLoadSystem` e as carrega no Deck Builder, limpando o deck anterior.

### 4. `DeckSlotUI.cs` (Prefab do Slot)
Este script está no prefab que representa um item na lista de decks salvos.
*   **Responsabilidade:** Exibir o nome do deck e a contagem de cartas (M/S/E).
*   **Interação:** Ao ser clicado, chama um callback no `DeckImportExportManager` para selecionar aquele deck.
*   **Modo Export:** No modo de exportação, um slot especial `[ Create New Deck ]` é adicionado para permitir salvar um deck com um nome totalmente novo.