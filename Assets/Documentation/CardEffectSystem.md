# Sistema de Efeitos de Cartas (CardEffectManager)

## Visão Geral
O `CardEffectManager` é o sistema central responsável por executar a lógica de todas as cartas do jogo. Devido à grande quantidade de cartas (2147+), o sistema foi refatorado para usar **Partial Classes**, dividindo o código em múltiplos arquivos para facilitar a manutenção e navegação.

## Estrutura de Arquivos

O sistema é composto pelos seguintes arquivos:

### 1. `CardEffectManager.cs` (Core)
*   **Função:** Contém a estrutura base da classe (`MonoBehaviour`), o Singleton `Instance`, o dicionário de efeitos (`effectDatabase`) e o método `Awake`.
*   **Métodos de Infraestrutura:** Contém métodos para coletar e destruir cartas em massa (`DestroyAllMonsters`, `CollectCards`).

### 2. `CardEffectManager_Common.cs` (Reutilizáveis)
*   **Função:** Contém métodos genéricos de efeitos usados por muitas cartas.
*   **Exemplos:**
    *   `Effect_DirectDamage`: Causa dano direto.
    *   `Effect_GainLP`: Ganha pontos de vida.
    *   `Effect_DestroyType`: Destrói monstros de um tipo específico.
    *   `Effect_SearchDeck`: Busca cartas no deck.
    *   `Effect_Equip`: Lógica de cartas de equipamento.
    *   `Effect_FlipDestroy`: Lógica de monstros FLIP que destroem.

### 3. `CardEffectManager_Registry.cs`
*   **Função:** Ponto de entrada para o registro de todos os efeitos.
*   **Método:** `InitializeEffects()` chama os métodos de inicialização de cada parte (`InitializeEffects_Part1`, `Part2`, etc.).

### 4. Arquivos de Registro Parciais (`Registry_PartX.cs`)
Estes arquivos contêm apenas as chamadas `AddEffect("ID", Lógica)` para registrar as cartas no dicionário. Eles são divididos por faixas de ID para organização:
*   `CardEffectManager_Registry_Part1.cs`: Cartas 0001 - 0500
*   `CardEffectManager_Registry_Part2.cs`: Cartas 0501 - 1000
*   `CardEffectManager_Registry_Part3.cs`: Cartas 1001 - 1500
*   `CardEffectManager_Registry_Part4.cs`: Cartas 1501 - 2000
*   `CardEffectManager_Registry_Part5.cs`: Cartas 2001 - 2147

### 5. Arquivos de Implementação Parciais (`Impl_PartX.cs`)
*   **Função:** Contém a implementação detalhada de efeitos específicos para cada carta, seguindo a convenção de nomenclatura `Effect_ID_Nome`.
*   **Estrutura:** Cada arquivo corresponde a uma parte do registro (Part1 a Part5).
*   **Exemplo:**
    ```csharp
    void Effect_0031_AirknightParshath(CardDisplay source) { ... }
    ```

## Como Adicionar uma Nova Carta

1.  **Identifique o ID:** Encontre o ID da carta no banco de dados JSON (ex: "0001").
2.  **Escolha a Parte Correta:** Abra o `CardEffectManager_Impl_PartX.cs` e `Registry_PartX.cs` correspondentes ao ID.
3.  **Implemente o Efeito:**
    *   No arquivo `Impl_PartX.cs`, crie o método:
        ```csharp
        void Effect_0001_NomeDaCarta(CardDisplay source) {
            // Lógica aqui (use métodos do Common se possível)
            Effect_DirectDamage(source, 500);
        }
        ```
4.  **Registre o Efeito:**
    *   No arquivo `Registry_PartX.cs`, adicione:
        ```csharp
        AddEffect("0001", Effect_0001_NomeDaCarta);
        ```

## Fluxo de Execução
1.  O jogador ativa uma carta (clique em "Activate" ou Flip).
2.  O `GameManager` chama `CardEffectManager.Instance.ExecuteCardEffect(card)`.
3.  O Manager busca o ID da carta no `effectDatabase`.
4.  Se encontrado, o `Action<CardDisplay>` correspondente é invocado.
5.  A lógica (seja genérica ou específica) é executada, interagindo com `GameManager`, `DuelFieldUI`, etc.
