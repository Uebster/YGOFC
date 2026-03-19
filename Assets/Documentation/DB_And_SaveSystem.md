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
| `type` | string | Categoria principal (Monster, Spell, Trap). **Deve incluir** subtipos exatos (ex: "Monster (Fusion)"). Vital para a engine diferenciar cartas no Extra Deck. |
| `race` | string | Tipo do monstro (Dragon, Warrior) ou Propriedade da magia/armadilha (Equip, Field, Continuous). |
| `attribute` | string | Atributo (LIGHT, DARK, FIRE, etc). |
| `level` | int | Nível do monstro (1-12). |
| `atk` | int | Pontos de Ataque base. |
| `def` | int | Pontos de Defesa base. |
| `password` | string | Código oficial de 8 dígitos da carta. |
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
1.  **`generate_pool_template.py`:** Lê o `cards.json` cru e gera a planilha `card_pool_template.csv`. Usa heurística analisando ATK, DEF, Nível e Palavras-chave (ex: "Draw 2") para dar um "chute" de poder na coluna `Suggested_Pool`.
2.  **Edição Humana:** O desenvolvedor avalia e preenche a coluna `Final_Pool` (apenas onde discordar da IA).
3.  **`apply_pools_to_json.py`:** Lê o CSV e injeta a propriedade final `"pool": "X.Y"` de volta no JSON mestre.

### 2.3.2 Geração Mestra (`generate_assets.py`)
O script-mestre central de construção do `cards.json`. Flexível e multi-formato:
*   **Detecção de Formato:** Lê nativamente TSV (separado por tabulação), listas antigas do *Power of Chaos* (`-- NOME -- [ID]`) e *Forbidden Memories*.
*   **Limpeza de Dados:** Extrai corretamente a diferença entre `attribute` (para monstros) e `property` (para mágicas/armadilhas) que vêm fundidos das APIs originais. Preserva subtipos exatos (ex: "Monster (Fusion)").
*   **Mapeamento de Imagem:** Numera e linka as imagens sequenciais baseadas na ordem alfabética das cartas.

### 2.3.3 Aquisição e Scrapers (`download_cards.py`)
Um servidor local Flask (`http://localhost:5000`) que consome a API da *YGOPRODeck*. 
*   Filtra as cartas por data (escopo de 1999 a 2005) e região (OCG/TCG).
*   Gera o arquivo `lista_cartas.txt` padronizado com 10 colunas e baixa as texturas automaticamente para a pasta `StreamingAssets`.

### 2.3.4 Geradores de Personagens e Bots
*   **`generate_characters.py`:** Monta o esqueleto do `characters.json`, definindo 100 oponentes organizados por Atos e Dificuldade.
*   **`generate_character_decks.py`:** Preenche os Decks (A, B, C) dos personagens usando busca por palavras-chave (ex: "Moth" para Weevil), respeitando o nível de Pool do Ato e incluindo cartas "Assinatura" obrigatórias.
*   **`generate_character_rewards.py`:** Isola o Boss Monster do personagem para ser o drop "S+ (Única)" e distribui as cartas usadas por ele em 4 tiers matemáticos baseados em poder (Rank S/A: Top 15%, B: 25%, C: 30%, D: 30%) preenchendo as listas de recompensas.

### 2.3.5 Validadores Externos
*   **`test_card_viewer.py`:** Visualizador ágil em *Pygame*. Permite navegar (Setas Esq/Dir) e renderizar a arte e texto lidos do JSON localmente, poupando a lentidão de compilar no Editor da Unity.
*   **`test_deck_system.py`:** Importa o núcleo (`duel_core`) para simular a criação, embaralhamento e compra de uma mão de 5 cartas de um bot no console Python.
*   **`generate_fields.py`:** Converte a lista de textos em `fields.json` para o sistema de arenas.

### 2.3.6 Ferramentas de Editor Unity (`HierarchyDumper.cs`)
*   **Caminho:** `Assets/Scripts/Editor/HierarchyDumper.cs`
*   **Função:** Ferramenta de debugging de UI. Ao clicar com o botão direito em um GameObject na janela *Hierarchy* e selecionar **"Dump Hierarchy (Copiar Texto)"**, o script copia a árvore completa do objeto e seus componentes (ignorando lixo como `Transform` e `CanvasRenderer`) diretamente para a área de transferência do Windows. Vital para documentação e para enviar o esqueleto da interface gráfica (ex: Árvore do Deck Builder) para a IA.

### 2.3.7 Debug In-Game (`InGameDebugConsole.cs`)
Painel invisível, configurado como `DontDestroyOnLoad`, que coleta logs durante builds compiladas.
*   **Atalho:** Pressione **Ctrl + Shift + D** no teclado em qualquer momento.
*   **Características:** 
    *   Armazena as últimas 1000 mensagens (`Log`, `Warning`, `Error`).
    *   Auto-Scroll dinâmico.
    *   **Botão "Copy":** Formata e copia o histórico para a área de transferência. Em caso de Exceções/Erros, embute o *StackTrace* completo para facilitar a caça a bugs fora da engine.

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
