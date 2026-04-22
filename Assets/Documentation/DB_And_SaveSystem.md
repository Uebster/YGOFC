# 2. Banco de Dados e Persistência (DB & Save System)

Este documento centraliza toda a arquitetura de armazenamento de dados do jogo, desde a extração via Python, a estrutura JSON das cartas, o balanceamento de Tiers, até como o progresso do jogador é salvo de forma definitiva no disco na Unity.

---

## 2.1 Banco de Dados de Cartas (`cards.json`)

### 2.1.1 Estrutura Técnica e JSON (`CardData`)
Todas as cartas do jogo estão armazenadas em um arquivo JSON (`StreamingAssets/cards.json`). O jogo carrega este arquivo na inicialização através do `CardDatabase.cs`.

Cada carta é um objeto com os seguintes campos:

| Campo | Tipo | Descrição |
| :--- | :--- | :--- |
| `id` | string | Identificador único de 4 dígitos (ex: "0001"). |
| `name` | string | Nome da carta (ex: "Blue-Eyes White Dragon"). |
| `description` | string | Texto de efeito ou flavor text. |
| `type` | string | Categoria principal e subtipo simplificado para a UI (ex: "Monster (Fusion)", "Spell"). |
| `typeline` | string | A string crua e oficial da API preservada (ex: "Flip Effect Monster", "Synchro Tuner Monster"). Vital para Filtros OCGCore avançados. |
| `race` | string | Tipo do monstro (Dragon, Warrior) ou Propriedade da magia/armadilha (Equip, Field, Continuous). |
| `attribute` | string | Atributo (LIGHT, DARK, FIRE, etc). |
| `level` | int | Nível do monstro (1-12). |
| `atk` | int | Pontos de Ataque base. |
| `def` | int | Pontos de Defesa base. |
| `password` | string | Código oficial de 8 dígitos da carta. |
| `archetype` | string | Nome do arquétipo associado (ex: "Amazoness", "Blue-Eyes"). Retorna `""` se não houver. |
| `first_set` | string | Primeira coleção oficial onde a carta foi impressa. |
| `scale` | int | Escala de Pêndulo (Preparação futura para Master Rule 3+). |
| `linkval` / `linkmarkers` | int / List | Valor Link e vetor de setas (Preparação futura para Master Rule 4+). |
| `image_filename` | string | Caminho relativo dentro de StreamingAssets para a imagem. |
| `pool` | string | (Injetado via Python) Define a raridade/tier da carta (ex: "3.4"). |

### 2.1.2 Convenção de Imagens e Lazy Loading
*   As imagens ficam na pasta `StreamingAssets/YuGiOh_OCG_Classic_2147/`.
*   O carregamento é feito sob demanda (Lazy Loading) via `UnityWebRequest` para economizar memória RAM VRAM.
*   O `CardDisplay` é responsável por solicitar a textura. Assim que a carta for destruída (ou o painel for fechado), o processo `Dispose()` é chamado para evitar Memory Leaks.

### 2.1.3 Escopo de IDs (Classic 2147)
O jogo utiliza a base estrita "Classic 2147", cobrindo o ápice do formato nostálgico Goat/2005.
*   **Capacidade Máxima:** Exatos 2147 IDs.
*   **Numeração Oficial:** De `0001` a `2147`.

---

## 2.2 Ranking de Cartas e Pools (Tiers)

Para balancear a progressão (RPG) do jogo, as cartas possuem um "peso" ou "tier" implícito classificado de **1.1 a 5.5**. Isso é vital para:
1.  **Gerar o Deck Inicial** (Experiência Procedural/Roguelike balanceada).
2.  **Definir a Dificuldade dos Oponentes** (Ex: Ato 1 usa Pools menores).
3.  **Controlar o Drop Rate** (Cartas de Tier Elite caem com muito menos frequência).

### 2.2.1 Classificação de Monstros (Tier 0 a 4)
*   **Tier 0 "Fodder" (Lixo/Início):** ATK 0 - 950. A "Bucha de canhão". Usado no deck inicial para defesas rápidas. Ex: *Kuriboh, Petit Moth*.
*   **Tier 1 "Weak" (Básico):** ATK 951 - 1300. A espinha dorsal das batalhas do Ato 1. Ex: *Silver Fang, Mammoth Graveyard*.
*   **Tier 2 "Medium" (Competitivo Inicial):** ATK 1301 - 1600. Os "Ases" dos duelistas normais do início do jogo. Ex: *Giant Soldier of Stone, Rogue Doll*.
*   **Tier 3 "Strong" (Beatsticks):** ATK 1601 - 1900. Monstros imensos sem sacrifício, prêmios dos sub-chefes. Ex: *La Jinn, 7 Colored Fish, Vorse Raider*.
*   **Tier 4 "Elite" (Lendários):** ATK 1900+ (Nível 4) ou 2400+ (Nível 5/6). Fim de jogo e Boss Monsters. Ex: *Gemini Elf, Summoned Skull, Jinzo*.

### 2.2.2 Classificação de Magias e Armadilhas (Pool A, B, C)
*   **Pool A (Fracos):** Modificadores brandos (< 500), curas baixas, dano de raspão. Ex: *Sparks, Red Medicine, Legendary Sword*.
*   **Pool B (Utilitários):** Remoções com travas condicionais fortes. Ex: *Trap Hole, Fissure, De-Spell*.
*   **Pool C "Staples" (Poder Absoluto):** Efeitos em área incondicionais ou que quebram a economia do jogo. **Estritamente proibidas em Decks Iniciais**. Ex: *Raigeki, Mirror Force, Pot of Greed, Monster Reborn*.

---

## 2.3 Ferramentas e Pipeline de Dados (Python & Unity)

A fundação dos dados não é inserida à mão na Unity. O projeto conta com um ecossistema autossuficiente de scripts Python (`Assets/Tools/`) para raspar, gerar, limpar e validar o banco de dados antes dele virar código.

### 2.3.1 Sistema de Pools (`generate_pool_template.py`, `apply_pools_to_json.py`)
O sistema classifica todas as cartas em 25 níveis de poder/raridade (de **1.1** a **5.5**).
1.  **`generate_pool_template.py`:** Lê o `cards.json` cru e gera a planilha `card_pool_template.csv`. 
    *   **Dicionários Estritos:** Utiliza `PREDEFINED_POOLS` (ex: Exodia, Relinquished, Staples antigas) para forçar tiers fixos de forma inquestionável, e analisa a `BANLIST` para impor tetos de poder (Forbidden = 5.5, Limited = 4.5, Semi = 3.5).
    *   **Heurística Pente Fino:** Se não estiver fixada, analisa ATK, DEF, Taxa de Tributo (Nível vs Status) e Palavras-chave (ex: "destroy all", "draw 2") para dar um "chute" lógico na coluna `Suggested_Pool`.
2.  **Edição Humana:** O desenvolvedor avalia o CSV no Excel/Sheets e preenche a coluna `Final_Pool` (apenas caso discorde da IA).
3.  **`apply_pools_to_json.py`:** Lê o CSV. Prioriza `Final_Pool` (se preenchido) sobre `Suggested_Pool`. Valida a sintaxe "X.Y" em fallback (padrão 1.1) e injeta a propriedade `"pool"` de volta no JSON mestre (`cards.json`).

### 2.3.2 Geração Mestra (`generate_assets.py`)
O script-mestre central de construção do `cards.json`. Flexível e multi-formato:
*   **Detecção de Formato:** Lê nativamente TSV (separado por tabulação), listas antigas do *Power of Chaos* (`-- NOME -- [ID]`) e *Forbidden Memories*.
*   **Limpeza de Dados:** Extrai corretamente a diferença entre `attribute` (para monstros) e `property` (para mágicas/armadilhas) que vêm fundidos das APIs originais. Preserva subtipos exatos (ex: "Monster (Fusion)").
*   **Mapeamento de Imagem:** Numera e linka as imagens sequenciais baseadas na ordem alfabética das cartas.

### 2.3.3 Aquisição e Scrapers (`download_cards_ultimate.py`)
A aquisição de dados brutos foi centralizada em uma única e poderosa ferramenta, `download_cards_ultimate.py`. Este script não é uma simples ferramenta de linha de comando, mas sim uma aplicação web local completa, construída com Flask.

*   **Interface Web (UI):** Ao ser executado, o script inicia um servidor em `http://localhost:5000` que renderiza uma interface gráfica no navegador. A partir dela, o desenvolvedor pode configurar e disparar diferentes tarefas de extração.
*   **Preparação Multi-Eras:** Inclui presets automáticos de datas para fatiar o banco de dados oficial em eras exatas (Clássica/DM, GX, 5D's, ZEXAL, ARC-V, VRAINS, MR5). O banco agora suporta Syncros, Xyz e Links intactos, caso desejado.
*   **Prefixo Inteligente de Imagens:** Através de um botão nativo do Explorer, o usuário seleciona a pasta alvo (ex: `DMCardImages`). A ferramenta extrai o prefixo automaticamente e injeta direto no JSON, blindando a engine C# contra problemas de quebra de diretório (salvando como `DMCardImages/0001 - Carta.jpg`).
*   **Multi-Funcionalidade ("Ultimate Extractor"):** A ferramenta pode executar uma variedade de tarefas de forma independente:
    *   **Gerar TXT/CSV:** Cria listas de cartas formatadas com colunas selecionáveis (incluindo as novas opções como Typeline Cru e Link Markers).
    *   **Gerar JSON Master:** Produz o `cards_ultimate.json`, uma versão mais completa e estruturada que a usada pelo jogo, servindo como uma base de dados crua.
    *   **Baixar Imagens (HD):** Utiliza Multi-Threading para baixar rapidamente todas as imagens de alta resolução da API, incluindo artes alternativas.
    *   **Baixar Scripts LUA:** Busca os scripts de efeito de múltiplos repositórios do YGOPro (ProjectIgnis, etc.) para garantir a maior cobertura possível.
    *   **Auditoria:** Varre as pastas de destino e gera um relatório `audit_report.txt` com todas as imagens e scripts LUA que estão faltando.
    *   **Repetir Erros:** Analisa um `error_log.txt` para tentar baixar novamente apenas os arquivos que falharam em uma execução anterior.
    *   **Backup (ZIP):** Compacta toda a base de dados gerada em um único arquivo `.zip`.
*   **Geração de ID Customizado:** Uma das características mais importantes deste script é a geração de um ID customizado e imutável para cada carta (ex: `DM0001`). Ele faz isso categorizando todas as cartas da história do Yu-Gi-Oh! por era (DM, GX, 5D's, etc.) e depois ordenando-as alfabeticamente dentro de cada era. Isso garante que, mesmo que a API mude os IDs oficiais, o sistema interno do jogo permaneça estável.
*   **Robustez:** A ferramenta inclui um sistema de `retry` automático para downloads que falham e um botão de "Cancelar" na UI para interromper processos longos.

*(Nota: O antigo `download_cards.py` foi mantido como um fallback mais simples, mas o fluxo de trabalho principal agora depende da versão "ultimate").*

### 2.3.4 Geradores de Personagens e Bots
*   **`generate_characters.py`:** Monta o esqueleto inicial do `characters.json`. Define estritamente os 100 oponentes listados no Roteiro (Atos 1 ao 10), atrelando seus IDs oficiais (`XXX_nome_variacao`), dificuldade, `story_role` (Treinamento, Guardião, Chefe, etc.) e o ambiente virtual (`field`).
*   **`generate_character_decks.py`:** O cérebro procedural de baralhos virtuais. 
    *   **`ACT_POOL_RANGES`:** Restringe matematicamente as cartas baseadas no Ato (Ex: Ato 1 apenas usa cartas de Pool 1.1 a 1.5).
    *   **`THEMES` & `CORES`:** Perfilamento inteligente. Associa strings no ID do duelista a pacotes de arquétipos estruturados (Ex: "Weevil" = "Insect", "Pegasus" = "Toon", "Marik" = "Burn").
    *   **`EXACT_DEPENDENCIES`:** Regra dura da engine. Se uma carta for invocada aleatoriamente (Ex: *Relinquished*), a dependência (*Black Illusion Ritual*) é inserida automaticamente para evitar soft-locks no baralho do inimigo.
    *   **Variantes A, B, e C:** Gera 3 decks únicos. As modificações de dificuldade B e C aumentam o teto do `Pool Range` (+0.2 / +0.5) e injetam `STAPLES` mais punitivas no preenchimento do Deck.
*   **`generate_character_rewards.py`:** 
    *   **Extração da Única (S+):** Separa a carta de maior raridade (normalmente Boss Monsters S+ inseridos via `FORCED_CARDS`) baseada no pool, limitando-a ao Drop S+ de apenas 15%.
    *   **Fatiamento de Tiers:** Junta as 120 cartas de todas as variantes de Decks (A, B e C), limpa as duplicatas, ordena por poder e divide percentualmente: S (Top 15%), B (Mid 25%), C (Low 30%) e D (Fodder 30%).
    *   **Preenchimento Inteligente:** Caso a soma dos decks do personagem não alcance 120 cartas, o algoritmo buscará na `cards_by_pool` preenchimentos genéricos ("fillers") nivelados ao Ato atual do personagem.

### 2.3.5 Validadores Externos
*   **`test_card_viewer.py`:** Visualizador ágil em *Pygame*. Permite navegar (Setas Esq/Dir) e renderizar a arte e texto lidos do JSON localmente, poupando a lentidão de compilar no Editor da Unity.
*   **`test_deck_system.py`:** Importa o núcleo (`duel_core`) para simular a criação, embaralhamento e compra de uma mão de 5 cartas de um bot no console Python.
*   **`generate_fields.py`:** Converte a lista de textos em `fields.json` para o sistema de arenas.

### 2.3.6 Ferramentas de Editor Unity (`HierarchyDumper.cs`, `InspectorDumper.cs`, `VFXOptimizer.cs`, `VFXBuilder.cs`)
Estas ferramentas facilitam a extração e criação ágil de dados da Unity.

*   **`HierarchyDumper.cs`:**
    *   **Caminho:** `Assets/Scripts/Editor/HierarchyDumper.cs`
    *   **Função:** Ferramenta de debugging estrutural. Ao clicar com o botão direito em um GameObject na janela *Hierarchy* e selecionar **"Dump Hierarchy (Copiar Texto)"**, o script copia a árvore completa do objeto e seus componentes (ignorando lixo como `Transform` e `CanvasRenderer`) diretamente para a área de transferência. Vital para enviar o esqueleto da interface gráfica (ex: Árvore do Deck Builder).
*   **`InspectorDumper.cs`:**
    *   **Caminho:** `Assets/Scripts/Editor/InspectorDumper.cs`
    *   **Função:** Ferramenta de extração de propriedades. Lê todos os valores, checkboxes, cores e referências visíveis no *Inspector*. Funciona tanto para GameObjects na cena quanto para Assets na pasta Project (Prefabs, ScriptableObjects, Materiais). Acessível via clique direito **"Dump Inspector (Copiar Texto)"**. Excelente para debugar configurações matemáticas ou copiar parâmetros complexos de *Particle Systems*.
*   **`VFXOptimizer.cs`:**
    *   **Caminho:** `Assets/Scripts/Editor/VFXOptimizer.cs`
    *   **Função:** Canivete suíço de Editor para partículas. Possui ferramentas acessíveis via clique direito para formatar qualquer partícula da internet para a engine do jogo. **"Otimizar para Impacto"** troca emissões contínuas por "Bursts" curtos. **"Escalar para UI"** multiplica os vetores matemáticos da partícula por 50x, resolvendo o bug visual de sistemas de partículas 3D se tornarem invisíveis (tamanho de 2 a 3 pixels) ao serem instanciados em *Canvas* 2D.
*   **`VFXBuilder.cs`:**
    *   **Caminho:** `Assets/Scripts/Editor/VFXBuilder.cs`
    *   **Função:** "Engine de VFX" portátil ativada no menu de topo `Tools -> VFX Builder`. Gera *Particle Systems* complexos prontos para Canvas UI com apenas um clique. Possui 6 *presets* base (Fogo, Fumaça, Luz, Água, etc.) e permite afinação fina de Duração, Âncora de Espalhamento (Shape), Velocidade, Fades, Rastros e customização automática de Materiais arrastando arquivos `.png` crus pro painel.

### 2.3.7 Debug In-Game (`InGameDebugConsole.cs`)
Painel invisível, configurado como `DontDestroyOnLoad`, que coleta logs durante builds compiladas.
*   **Atalho:** Pressione **Ctrl + Shift + D** no teclado em qualquer momento.
*   **Características:** 
    *   Armazena as últimas 1000 mensagens (`Log`, `Warning`, `Error`).
    *   Auto-Scroll dinâmico.
    *   **Botão "Copy":** Formata e copia o histórico para a área de transferência. Em caso de Exceções/Erros, embute o *StackTrace* completo para facilitar a caça a bugs fora da engine.

### 2.3.8 Analisador Estático de Scripts Lua (`cardslua_analyzer.py`)
Ferramenta suprema de validação de estabilidade do ecossistema OCGCore (Lua) perante o motor C# da Unity.
*   **Função:** Executa uma varredura estática profunda em todos os milhares de scripts `.lua` na pasta `LuaScripts`, identificando precocemente chamadas de métodos inexistentes ou constantes globais não mapeadas.
*   **Como funciona:**
    *   Cruza os dados com `cards.json` para saber exatamente quais cartas deveriam ter scripts (monstros de efeito, magias, armadilhas) e alerta sobre faltas ou sobras (`ExtraneousScript`).
    *   Vasculha o `LuaAPI.cs` extraindo dinamicamente, via Expressões Regulares (Regex), todos os métodos públicos (`public bool IsType()`, etc) suportados pela engine C#.
    *   Lê o `constant.lua` e o `CardEffectManager.cs` para mapear todas as variáveis globais de status e fases injetadas (`LOCATION_DECK`, `EFFECT_TYPE_SINGLE`).
    *   Limpa falsos positivos de forma inteligente: ignora comentários textuais, funções exclusivas da própria carta (ex: `target`, `condition`) e operadores nativos do Lua (`elseif`, `for`, `math`).
*   **Exportação de Relatórios:** Sistema interativo via terminal. Pergunta ao desenvolvedor onde deseja salvar e permite despejar relatórios consolidados em formatos `JSON`, `CSV` e `TXT` formatado.
*   **Fallback Inteligente (GUI):** Se o script for executado de um ambiente desconfigurado (diretório inválido), ele não "crasha". Em vez disso, utiliza a biblioteca `Tkinter` nativa para abrir janelas do *Windows Explorer*, solicitando que o usuário aponte os caminhos dos arquivos necessários (C#, JSON e Lua).

---

## 2.4 Sistema de Save e Carregamento (Persistência)

O armazenamento real do progresso é blindado pelo Singleton `SaveLoadSystem.cs` (`DontDestroyOnLoad`). O formato físico é um arquivo `.save` em texto limpo com dados em JSON, salvo obrigatoriamente no diretório `Application.persistentDataPath` do sistema operacional.

### 2.4.1 Estrutura de Memória (`GameSaveData`)
A classe mestra que serializa a vida do duelista. Carrega blocos cruciais:
*   `saveID`: ID único embutindo Nome + Timestamp.
*   `playerName`: Nome/Apelido do jogador.
*   `campaignProgress`: Nível numérico indicando em qual Ato/Oponente do mapa o jogador está travado.
*   `trunkCards`: A colossal lista (List de Strings) dos IDs de **todas** as cartas que o jogador possui no Baú.
*   `mainDeck`, `sideDeck`, `extraDeck`: As listas de IDs das cartas ativamente equipadas no baralho principal.
*   `playerExtraDeckIDs`: Lista de IDs dedicada para o Extra Deck do jogador, separada do `extraDeck` genérico.
*   `libraryData`: Metadados da enciclopédia. Contém:
    *   Vitórias acumuladas contra cada oponente (vital para liberar a visualização do deck do bot na biblioteca).
    *   Lista de cartas já usadas/vistas (vital para a remoção da etiqueta "NEW").
*   `trophyData`: Banco numérico com totais de Dano, Fichas Invocadas, etc. para validar platina e Troféus.
*   `savedDecks`: Coleção de `DeckRecipe` (Os baralhos engavetados).

### 2.4.2 Interface de Save/Load (`SaveLoadMenu.cs`)
Opera nas pontas `Save`, `Load` ou `Delete`.
*   **Manejo de Slots (UI):** Para evitar a lentidão ou o *flickering* visual de destruir e recriar dezenas de painéis dentro do `ScrollRect`, o sistema instancia os prefabs `SaveSlotUI` e itera mudando apenas as cores do componente de *Highlight* ao clicar.
*   **O Novo Jogo:** Ao clicar em `[ Create New Save ]`, o sistema injeta um `null` no seletor de ID. O `SaveLoadSystem` apaga toda a memória volátil da sessão anterior, gera um novo ID único, e aciona o gatilho "Roguelike", invocando o script `InitialDeckBuilder` para sortear o primeiro deck proceduralmente e salvar no disco.

### 2.4.3 Receitas de Deck Locais (`DeckRecipe` & `DeckImportExportManager`)
Emuladores antigos salvavam os baralhos como arquivos `.ydk` soltos na pasta do PC. Neste projeto, as receitas pertencem à alma do arquivo `.save` do jogador.
*   **`DeckRecipe`:** Objeto C# que empacota um `deckName` (string) junto de 3 listas autônomas de IDs (Main, Side, Extra).
*   **Lógica de UI:** O `DeckImportExportManager.cs` intercepta os botões "Import" e "Export" da tela do Deck Builder. Ele lê os IDs da receita e cruza com a variável `playerTrunk` (O Baú). Se a receita pedir um *Pot of Greed*, mas o jogador não tiver a carta no Baú, a carta não será importada.
*   **A Regra de Ouro da Persistência:** Os métodos `SaveDeckRecipe` e `DeleteDeckRecipe` operam apenas na memória RAM da sessão. É **estritamente obrigatório** que a UI chame `SaveLoadSystem.Instance.SaveGame()` logo em seguida para costurar as novas receitas no arquivo físico do HD.
