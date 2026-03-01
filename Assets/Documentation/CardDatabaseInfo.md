# Banco de Dados de Cartas

## Estrutura Técnica
Todas as cartas do jogo estão armazenadas em um arquivo JSON (`StreamingAssets/cards.json`). O jogo carrega este arquivo na inicialização através do `CardDatabase.cs`.

## Formato do JSON (`CardData`)
Cada carta é um objeto com os seguintes campos:

| Campo | Tipo | Descrição |
| :--- | :--- | :--- |
| `id` | string | Identificador único de 4 dígitos (ex: "0001"). |
| `name` | string | Nome da carta (ex: "Blue-Eyes White Dragon"). |
| `description` | string | Texto de efeito ou flavor text. |
| `type` | string | Categoria principal (Monster, Spell, Trap). Pode incluir subtipos (ex: "Monster (Effect)"). |
| `race` | string | Tipo do monstro (Dragon, Warrior, etc) ou ícone da magia (Equip, Field). |
| `attribute` | string | Atributo (LIGHT, DARK, FIRE, etc). |
| `level` | int | Nível do monstro (1-12). |
| `atk` | int | Pontos de Ataque. |
| `def` | int | Pontos de Defesa. |
| `password` | string | Código oficial de 8 dígitos da carta (para sistema de Password). |
| `image_filename` | string | Caminho relativo dentro de StreamingAssets para a imagem. |

## Convenção de Imagens
*   As imagens ficam na pasta `StreamingAssets/YuGiOh_OCG_Classic_2147/`.
*   O carregamento é feito sob demanda (Lazy Loading) via `UnityWebRequest` para economizar memória RAM.
*   O `CardDisplay` é responsável por solicitar a textura ao `GameManager` ou carregá-la diretamente.

## IDs e Sets
O jogo utiliza a base "Classic 2147", cobrindo as cartas mais icônicas da era DM e GX inicial.
*   **Total de Cartas:** ~2147 cartas.
*   **Numeração:** 0001 a 2147.

## Tratamento de Efeitos
Como o JSON contém apenas dados passivos, os efeitos reais (código C#) são gerenciados por scripts como `SpellTrapManager` e `MonsterEffectManager` (futuro), que verificam o ID ou Nome da carta para executar a lógica correta (ex: Se ID = "0336" (Dark Hole) -> Destruir todos os monstros).