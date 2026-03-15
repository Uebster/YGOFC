# Ferramentas e Pipeline de Dados (Tools & Pipeline)

Este documento descreve o ecossistema de scripts Python localizados em `Assets/Tools/`. Estes scripts são responsáveis por gerar, gerenciar, enriquecer e testar os dados estáticos do jogo (Cartas, Personagens, Arenas) antes que eles entrem no Unity.

## 1. Sistema de Pools de Cartas (Raridade e Drop)

O sistema de Pools classifica todas as cartas em 25 níveis de poder/raridade (de **1.1** a **5.5**) para balancear a progressão da campanha, os drops de vitória e a construção de decks procedurais.

### Fluxo de Trabalho (Workflow)
O processo para definir a raridade das cartas é semiautomático para lidar com o volume de 2000+ cartas:

#### Passo 1: Gerar Template (`generate_pool_template.py`)
*   **Função:** Lê o `cards.json` atual e cria uma planilha CSV para edição.
*   **Lógica de "Chute":** O script analisa os atributos da carta (ATK, DEF, Nível, Palavras-chave no texto como "Draw 2") e estima um nível de pool.
    *   *Exemplo:* Um monstro Nível 4 com 1900 ATK recebe sugestão **3.1**.
    *   *Exemplo:* "Pot of Greed" recebe sugestão **5.5**.
*   **Saída:** `Assets/Tools/card_pool_template.csv`.

#### Passo 2: Edição Humana (Excel/Google Sheets)
*   O desenvolvedor abre o arquivo CSV gerado.
*   A coluna `Suggested_Pool` contém a estimativa da IA.
*   O desenvolvedor preenche a coluna `Final_Pool` **apenas** nas cartas onde discorda da sugestão.

#### Passo 3: Aplicação (`apply_pools_to_json.py`)
*   **Função:** Lê o CSV editado e atualiza o `cards.json`.
*   **Lógica:** Se `Final_Pool` estiver preenchido, usa esse valor. Caso contrário, usa o `Suggested_Pool`.
*   **Resultado:** O `cards.json` ganha uma nova propriedade `"pool": "X.Y"` em cada objeto de carta.

---

## 2. Geração de Banco de Dados (`generate_assets.py`)

Este é o script "mestre" que constrói o banco de dados principal do jogo, o `cards.json`. Ele foi projetado para ser flexível, suportando múltiplos formatos de listas de cartas.

*   **Entrada:**
    *   Um arquivo de texto (`.txt`) com a lista de cartas. O script é inteligente e detecta o formato do arquivo:
        *   **Formato `download_cards.py` (Recomendado):** Um arquivo TSV (separado por tabulação) com 10 colunas, incluindo `ATRIBUTO` e `RAÇA/PROPRIEDADE`. O script `generate_assets.py` foi otimizado para ler este formato e extrair todos os dados detalhados.
        *   **Formato Power of Chaos:** Arquivos de texto com o formato `-- NOME -- [ID/TOTAL]`.
        *   **Formato Forbidden Memories:** Listas simples separadas por tabulação.
    *   Imagens na pasta `YuGiOh_OCG_Classic_2147`.
*   **Processamento:**
    *   **Detecção de Formato:** O script lê o cabeçalho do arquivo `.txt` para determinar qual parser usar (`parse_tsv_cards`, `parse_poc_cards`, etc.).
    *   Unifica múltiplas fontes de texto em um único dicionário.
    *   Corrige nomes (Typos conhecidos) e padroniza descrições.
    *   **Extração de Dados (Novo):**
        *   Se o formato do `download_cards.py` for detectado, ele extrai corretamente o `attribute` para monstros e o `property` para magias/armadilhas.
        *   Ele preserva o tipo detalhado do monstro (ex: "Monster (Fusion)").
    *   Mapeia IDs de imagem sequenciais baseados na ordem alfabética.
*   **Saída:** `Assets/StreamingAssets/cards.json`.

## 3. Geração de Personagens (`generate_characters.py`)

Gera o arquivo `characters.json` que define os oponentes da campanha.

*   **Roster:** Define uma lista fixa de 100 oponentes (IDs 001 a 100), organizados por Atos e Dificuldade.
*   **Decks Temáticos:** O script procura cartas no banco de dados baseadas no nome do personagem.
    *   *Ex:* Se o nome for "Weevil", o script busca "Moth", "Insect", "Cocoon".
*   **Preenchimento:** Se o deck temático não atingir 40 cartas, o script preenche o restante com cartas aleatórias válidas do banco de dados.
*   **Saída:** `Assets/StreamingAssets/characters.json` (ou pasta configurada).

### Geração de Decks (`generate_character_decks.py`)
*   **Função:** Preenche os decks (A, B, C) dos personagens automaticamente.
*   **Lógica:**
    *   Analisa o Ato do personagem (ex: ID 015 = Ato 2).
    *   Seleciona cartas baseadas no Pool permitido para aquele Ato.
    *   Inclui cartas "Assinatura" (ex: Blue-Eyes para Kaiba) obrigatoriamente.
    *   Verifica dependências (ex: se adicionar Polimerização, tenta adicionar Fusões).
*   **Formatação:** Gera o JSON com quebras de linha a cada 10 IDs para facilitar leitura humana.

### Geração de Recompensas (`generate_character_rewards.py`)
*   **Função:** Define a lista de drops (`rewards`) para cada personagem.
*   **Estrutura de Drop:**
    *   **S+ (Única):** Seleciona a carta mais forte ou assinatura do personagem.
    *   **Pool Geral:** Coleta todas as cartas usadas nos 3 decks + cartas de preenchimento (filler) para atingir ~120 cartas.
    *   **Classificação:** Ordena o pool por poder e divide em 4 tiers:
        *   **Rank S/A:** Top 15% mais fortes.
        *   **Rank B:** Intermediárias (25%).
        *   **Rank C:** Fracas (30%).
        *   **Rank D:** Lixo/Fodder (30%).

## 4. Aquisição de Dados (`download_cards.py`)

Uma ferramenta web (servidor Flask local) para baixar dados brutos e imagens da API *YGOPRODeck*, gerando um arquivo `lista_cartas.txt` completo e padronizado.

*   **Uso:** Execute o script e acesse `http://localhost:5000` no navegador.
*   **Funcionalidades:**
    *   Filtrar por data (ex: 1999-2005) e região (OCG/TCG).
    *   **Gerar Lista TXT Completa:** Cria um arquivo `lista_cartas.txt` com 10 colunas, incluindo:
        *   `TIPO`: Preserva o tipo completo do monstro (ex: "Fusion Monster").
        *   `ATRIBUTO`: Preenche com o atributo do monstro (ex: "DARK").
        *   `RAÇA/PROPRIEDADE`: Preenche com a raça do monstro (ex: "Warrior") ou a propriedade da magia/armadilha (ex: "Continuous").
    *   Baixar imagens das cartas automaticamente.

## 5. Ferramentas de Teste e Visualização

Scripts auxiliares para validar a integridade dos dados sem precisar abrir o Unity (o que economiza tempo de carga).

### Visualizador de Cartas (`test_card_viewer.py`)
*   **Tecnologia:** Pygame.
*   **Função:** Abre uma janela gráfica que carrega o `cards.json` e as imagens.
*   **Uso:** Permite navegar pelas cartas (Setas Esq/Dir), virar a carta (Clique) e verificar se os textos e atributos estão corretos visualmente.

### Simulador de Deck (`test_deck_system.py`)
*   **Tecnologia:** Python (Console).
*   **Função:** Importa o núcleo de duelo (`duel_core`) e simula a inicialização de um jogador.
*   **Teste:** Carrega um personagem do JSON, monta seu deck, embaralha e saca uma mão inicial de 5 cartas, imprimindo o resultado no console. Útil para verificar se os decks gerados são válidos (têm 40 cartas, IDs existem, etc).

## 6. Outros Geradores

### Gerador de Arenas (`generate_fields.py`)
*   **Entrada:** `Scripts/Fields/Arenas.txt`.
*   **Função:** Converte uma lista simples de texto em um JSON estruturado (`fields.json`) contendo IDs e nomes de arenas, usado pelo sistema de seleção de cenários.

---

## 7. Ferramentas de Editor (Unity Editor Tools)

Esta seção descreve ferramentas customizadas que rodam dentro do Editor do Unity para auxiliar no desenvolvimento e debugging.

### `HierarchyDumper.cs`

*   **Localização do Script:** `Assets/Scripts/Editor/HierarchyDumper.cs`
*   **Função:** Uma ferramenta de debugging de UI extremamente útil para copiar a estrutura de um `GameObject` e seus componentes para a área de transferência. O texto formatado é ideal para colar em chats ou documentos para análise.
*   **Uso:**
    1.  No Editor do Unity, encontre o `GameObject` que você deseja inspecionar na janela **Hierarchy**.
    2.  Clique com o botão direito no `GameObject`.
    3.  No menu de contexto, selecione a opção **"Dump Hierarchy (Copiar Texto)"**.
    4.  A estrutura completa do objeto, incluindo todos os filhos e seus componentes principais, será copiada para sua área de transferência.
    5.  Um log de confirmação aparecerá no Console do Unity.
*   **Exemplo de Saída:**
    ```
    ESTRUTURA DE: Panel_CardChest
    - Panel_CardChest [Image]
      - Scroll View [Image, ScrollRect, TrunkScrollManager]
        - Viewport [Image, Mask]
          - Image [Image, DeckDropZone]
          - Content []
        - Scrollbar Vertical [Image, Scrollbar]
          - Sliding Area []
            - Handle [Image]
    ```
*   **Observação:** O script ignora componentes comuns como `Transform` e `CanvasRenderer` para manter a saída limpa e focada nos scripts e componentes de UI mais importantes.

## 8. In-Game Debug Console (`InGameDebugConsole.cs`)

Para facilitar o rastreamento de bugs e o envio de logs de erro durante as builds do jogo sem precisar depender apenas do Editor da Unity, foi implementado um console de debug integrado ao jogo.

### Como Usar
*   **Atalho:** Pressione a combinação **Ctrl + Shift + D** no teclado em qualquer momento do jogo para abrir ou fechar a janela do console.
*   **Características:**
    *   Armazena as últimas 1000 mensagens disparadas via `Debug.Log`, `Debug.LogWarning` e `Debug.LogError`.
    *   **Auto-Scroll:** A lista desce automaticamente com uso de `ContentSizeFitter` e corrotinas para esperar o fim do frame da Unity.
    *   **Botão "Copy":** Copia todo o texto do console formatado diretamente para a Área de Transferência (Clipboard) do Windows, ideal para relatar erros. O *StackTrace* é incluído automaticamente em casos de Erros e Exceções para facilitar a busca pela linha do código problemática.
    *   **Botão "Clear":** Limpa o histórico de logs visuais para facilitar a leitura de novos eventos e testes.
    *   É um objeto `DontDestroyOnLoad`, sobrevivendo a todas as transições de tela e coletando logs de qualquer cena.