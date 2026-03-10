# Sistema de Save/Load

## Visão Geral
O sistema de Save/Load é composto por dois scripts principais: `SaveLoadSystem.cs` e `SaveLoadMenu.cs`. O primeiro lida com a lógica de salvar e carregar os dados em arquivos, enquanto o segundo gerencia a interface do usuário (UI) para apresentar e interagir com esses arquivos.

---

## `SaveLoadSystem.cs` - O Cérebro

Este é um Singleton persistente (`DontDestroyOnLoad`) que atua como o back-end do sistema.

### Responsabilidades
*   **Serialização:** Converte os dados de jogo (progresso, decks, coleção) em formato JSON para serem salvos em disco.
*   **Leitura de Arquivos:** Lê os arquivos `.save` do disco e os converte de volta para objetos C# (`GameSaveData`).
*   **Gerenciamento de Dados:** Contém as estruturas de dados principais que são salvas, como `GameSaveData`, `LibrarySaveData` e `TrophySaveData`.
*   **Operações de CRUD:** Fornece métodos públicos para `SaveGame`, `LoadGame`, `GetAllSaves` e `DeleteSave`.

### Estrutura de Dados (`GameSaveData`)
A classe principal que representa um arquivo de save. Contém:
*   `saveID`: Um identificador único, gerado a partir do nome do jogador e da data/hora de criação.
*   `playerName`: Nome do duelista.
*   `lastPlayedTime`: Data e hora do último salvamento.
*   `campaignProgress`: O nível máximo desbloqueado na campanha.
*   `trunkCards`: Lista de IDs de todas as cartas que o jogador possui no baú.
*   `mainDeck`, `sideDeck`, `extraDeck`: Listas de IDs das cartas nos respectivos decks.
*   `libraryData`: Dados da biblioteca (cartas vistas, vitórias contra oponentes).
*   `trophyData`: Conquistas e estatísticas do jogador.

## Localização dos Arquivos
*   **Caminho:** `Application.persistentDataPath` (uma pasta segura e específica do sistema operacional).
*   **Formato:** `.save` (que internamente é um arquivo de texto JSON).

---

## `SaveLoadMenu.cs` - A Interface

Este script é anexado aos painéis de Save, Load e Delete na UI. Ele é responsável por toda a interação do jogador com os arquivos de save.

### Funcionalidades

#### 1. Modos de Operação (`MenuType`)
O script pode operar em três modos, definidos por um `enum` no Inspector:
*   **Save:** Exibe os saves existentes para sobrescrever e uma opção para criar um novo save.
*   **Load:** Exibe os saves existentes para carregar.
*   **Delete:** Exibe os saves existentes para apagar.

#### 2. Geração Dinâmica da Lista
*   Ao ser ativado (`OnEnable`), o script chama `RefreshList()`.
*   `RefreshList()` limpa a lista visual, chama `SaveLoadSystem.Instance.GetAllSaves()` para obter os dados mais recentes e instancia um prefab (`SaveSlotUI`) para cada save encontrado.

#### 3. Slot de "New Save"
*   No modo **Save**, um slot especial `[ Create New Save ]` é adicionado no topo da lista.
*   Clicar neste slot ativa o modo `isCreatingNewSave`, que muda o comportamento do botão de ação principal para "Save New".
*   A confirmação da criação de um novo save chama `SaveLoadSystem.Instance.SaveGame(null)`, passando `null` para que o sistema gere um novo ID único.

#### 4. Seleção e Destaque
*   O script `SaveSlotUI.cs` (no prefab do slot) gerencia a exibição dos dados (Nome, Data, Info).
*   Quando um slot é clicado, ele chama o callback `OnSlotClicked` no `SaveLoadMenu`.
*   `OnSlotClicked` armazena o `selectedSave` e percorre todos os slots visuais para atualizar a cor de destaque, garantindo que apenas o selecionado fique amarelo (ou a cor customizada).
*   A lógica foi projetada para **não recriar a lista inteira** ao selecionar, evitando "flicker" e perda da posição do scroll.

#### 5. Ações Principais (Save, Load, Delete)
*   O botão de ação principal (`mainActionButton`) tem sua função definida pelo `menuType`.
*   **Save (Overwrite):** Pede confirmação e chama `SaveGame(selectedSave.saveID)`.
*   **Save (New):** Pede confirmação e chama `SaveGame(null)`.
*   **Load:** Chama `LoadGame(selectedSave.saveID)` e transiciona para o menu principal do jogo.
*   **Delete:** Pede confirmação e chama `DeleteSave(selectedSave.saveID)`, limpando a seleção e atualizando a lista.

#### 6. Auto-Configuração (`Awake`)
*   Para aumentar a robustez e facilitar o setup no editor, o método `Awake` procura automaticamente por referências essenciais da UI (como `listContent`, `mainActionButton`, `confirmationPopup`) usando `transform.Find()`. Isso reduz a chance de erros por referências não atribuídas no Inspector.

#### 7. Customização de Cores
*   Foram adicionadas duas variáveis públicas, `selectedColor` e `defaultColor`, no `SaveLoadMenu.cs`.
*   O `SaveSlotUI.cs` agora busca a referência do seu `SaveLoadMenu` pai para usar essas cores, permitindo que você as ajuste facilmente no Inspector do painel principal (Panel_Save, Panel_Load, etc.).