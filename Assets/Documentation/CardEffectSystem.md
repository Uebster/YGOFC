# Sistema de Efeitos de Cartas (CardEffectManager)

## Visão Geral
O `CardEffectManager` é o sistema central responsável por executar a lógica de todas as cartas do jogo. Devido à grande quantidade de cartas (2147+), o sistema foi refatorado para usar **Partial Classes**, dividindo o código em múltiplos arquivos para facilitar a manutenção e navegação.

## Estrutura de Arquivos

O sistema é composto pelos seguintes arquivos:

### 1. `CardEffectManager.cs` (Core)
*   **Função:** Contém a estrutura base da classe (`MonoBehaviour`), o Singleton `Instance`, o dicionário de efeitos (`effectDatabase`) e o método `Awake`.
*   **Métodos Utilitários:** Contém métodos genéricos usados por muitas cartas, como:
    *   `Effect_DirectDamage`: Causa dano direto.
    *   `Effect_GainLP`: Ganha pontos de vida.
    *   `Effect_DestroyType`: Destrói monstros de um tipo específico.
    *   `Effect_SearchDeck`: Busca cartas no deck.
    *   `Effect_Equip`: Lógica de cartas de equipamento.
    *   `Effect_FlipDestroy`: Lógica de monstros FLIP que destroem.

### 2. `CardEffectManager_Registry.cs`
*   **Função:** Ponto de entrada para o registro de todos os efeitos.
*   **Método:** `InitializeEffects()` chama os métodos de inicialização de cada parte (`InitializeEffects_Part1`, `Part2`, etc.).

### 3. Arquivos de Registro Parciais (`Registry_PartX.cs`)
Estes arquivos contêm apenas as chamadas `AddEffect("ID", Lógica)` para registrar as cartas no dicionário. Eles são divididos por faixas de ID para organização:
*   `CardEffectManager_Registry_Part1.cs`: Cartas 0001 - 0500
*   `CardEffectManager_Registry_Part2.cs`: Cartas 0501 - 1000
*   `CardEffectManager_Registry_Part3.cs`: Cartas 1001 - 1500
*   `CardEffectManager_Registry_Part4.cs`: Cartas 1501 - 2000
*   `CardEffectManager_Registry_Part5.cs`: Cartas 2001 - 2147

### 4. `CardEffectManager_Impl.cs`
*   **Função:** Contém a implementação detalhada de efeitos específicos que são complexos demais para serem escritos como expressões lambda (uma linha) no registro.
*   **Exemplos:**
    *   `Effect_PotOfGreed`: Lógica de comprar 2 cartas.
    *   `Effect_DarkHole`: Lógica de destruir todos os monstros.
    *   `Effect_ChangeControl`: Lógica de troca de controle (Brain Control, Snatch Steal).

## Como Adicionar uma Nova Carta

1.  **Identifique o ID:** Encontre o ID da carta no banco de dados JSON (ex: "0001").
2.  **Escolha o Arquivo de Registro:** Abra o `CardEffectManager_Registry_PartX.cs` correspondente ao ID.
3.  **Adicione o Efeito:**
    *   **Simples:** Use uma lambda diretamente.
        ```csharp
        AddEffect("0001", c => Effect_DirectDamage(c, 500));
        ```
    *   **Complexo:** Crie um método no `CardEffectManager_Impl.cs` e registre-o.
        *   No `Impl.cs`:
            ```csharp
            void Effect_MinhaCartaComplexa(CardDisplay source) { ... }
            ```
        *   No `Registry_PartX.cs`:
            ```csharp
            AddEffect("9999", Effect_MinhaCartaComplexa);
            ```

## Fluxo de Execução
1.  O jogador ativa uma carta (clique em "Activate" ou Flip).
2.  O `GameManager` chama `CardEffectManager.Instance.ExecuteCardEffect(card)`.
3.  O Manager busca o ID da carta no `effectDatabase`.
4.  Se encontrado, o `Action<CardDisplay>` correspondente é invocado.
5.  A lógica (seja genérica ou específica) é executada, interagindo com `GameManager`, `DuelFieldUI`, etc.