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

Este é o script "mestre" que constrói o banco de dados principal do jogo.

*   **Entrada:**
    *   Arquivos de texto (`.txt`) na pasta `Scripts/Cards/` (suporta formatos Power of Chaos, Forbidden Memories e TSV).
    *   Imagens na pasta `YuGiOh_OCG_Classic_2147`.
*   **Processamento:**
    *   Unifica múltiplas fontes de texto em um único dicionário.
    *   Corrige nomes (Typos conhecidos) e padroniza descrições.
    *   Mapeia IDs de imagem sequenciais baseados na ordem alfabética.
*   **Saída:** `Assets/StreamingAssets/cards.json`.

## 3. Geração de Personagens (`generate_characters.py`)

Gera o arquivo `characters.json` que define os oponentes da campanha.

*   **Roster:** Define uma lista fixa de 100 oponentes (IDs 001 a 100), organizados por Atos e Dificuldade.
*   **Decks Temáticos:** O script procura cartas no banco de dados baseadas no nome do personagem.
    *   *Ex:* Se o nome for "Weevil", o script busca "Moth", "Insect", "Cocoon".
*   **Preenchimento:** Se o deck temático não atingir 40 cartas, o script preenche o restante com cartas aleatórias válidas do banco de dados.
*   **Saída:** `Assets/StreamingAssets/characters.json` (ou pasta configurada).

## 4. Aquisição de Dados (`download_cards.py`)

Uma ferramenta web (servidor Flask local) para baixar dados brutos e imagens da API *YGOPRODeck*.

*   **Uso:** Execute o script e acesse `http://localhost:5000` no navegador.
*   **Funcionalidades:**
    *   Filtrar por data (ex: 1999-2005) e região (OCG/TCG).
    *   Gerar lista `.txt` compatível com o `generate_assets.py`.
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
