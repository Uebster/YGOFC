# Sistema de Efeitos de Cartas (CardEffectManager)

## Visão Geral
O `CardEffectManager` é o sistema central responsável por executar a lógica de todas as cartas do jogo. Devido à grande quantidade de cartas (2147+), o sistema foi refatorado para usar **Partial Classes**, dividindo o código em múltiplos arquivos para facilitar a manutenção e navegação.

## Estrutura de Arquivos

O sistema é composto pelos seguintes arquivos:

### 1. `CardEffectManager.cs` (Core)
*   **Função:** Contém a estrutura base da classe (`MonoBehaviour`), o Singleton `Instance`, o dicionário de efeitos (`effectDatabase`) e o método `Awake`.
*   **Métodos de Infraestrutura:** Contém métodos para coletar e destruir cartas em massa (`DestroyAllMonsters`, `CollectCards`).

### 2. `CardEffectManager_Impl.cs` (Implementação Base e Utilitários)
*   **Função:** Contém métodos genéricos de efeitos usados por muitas cartas, lógica de manutenção e hooks de eventos principais (`OnPhaseStart`, `OnCardSentToGraveyard`, etc).
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

## Padronização de Subtipos (Subtypes)

A engine foi projetada para que você não precise recriar lógicas complexas do zero. Ao implementar Magias e Armadilhas, utilize os Helpers globais disponíveis no `CardEffectManager`:

*   **Equip (Equipamento):** Para cartas mágicas de equipamento, use `Effect_Equip(source, atkBonus, defBonus, requiredRace, requiredAttribute)`. Ele gerencia a criação do vínculo (`CardLink`) e aplica os modificadores de status (StatModifiers) automaticamente.
*   **Field (Campo):** Utilize o `Effect_Field(source, atk, def, race, attribute)`. Ele aplica modificadores do tipo `Field` varrendo o tabuleiro inteiro.
*   **Continuous (Contínuo):** Cartas contínuas geralmente não precisam de um "efeito de ativação". A lógica delas deve existir dentro de Hooks globais como `OnPhaseStart` ou `OnSummon`, encapsulada em um bloco `CheckActiveCards("ID_DA_CARTA", (card) => { ... })`.
*   **Counter (Resposta):** Para armadilhas de resposta (Speed 3), a infraestrutura do `ChainManager` resolve tudo. Apenas use a combinação: `GetLinkToNegate(source)` para pegar o alvo, e `NegateAndDestroy(source, link)`.
*   **Quick-Play (Rápida):** Magias rápidas (Speed 2) funcionam normalmente graças ao controle nativo da engine. Basta implementar o que a carta faz (destruir, curar, buffar).
*   **Ritual:** Centralizado no GameManager. A carta mágica apenas precisa invocar `GameManager.Instance.BeginRitualSummon(source)`.

## Sistemas de Sorte e Minigames

Para integrar as cartas com a Interface Visual (UI) do jogo sem acoplar código, utilize os métodos oficiais da engine em vez de gerar números aleatórios isolados (`Random.Range`):

*   **Moedas (Coin Toss):**
    *   Use `GameManager.Instance.TossCoin(int count, Action<int> callback)` para rolar 1 ou mais moedas. O sistema aguardará a animação da UI terminar e retornará a contagem exata de "Caras" (Heads) no callback.
    *   Para o caso padrão de destruir ao acertar a moeda, utilize o atalho `Effect_CoinTossDestroy(source, moedas, acertosNecessarios, tipoAlvo)`.
*   **Dados (Dice Roll):**
    *   Use `CardEffectManager.Instance.RollDice(int amount, Action<List<int>> callback)`. Isso invocará a UI de dados 3D rolando na tela e devolverá os resultados de cada dado no final.
*   **Relógio/Turnos (Clock System):**
    *   Para cartas que ficam no campo por um tempo limite (ex: *Swords of Revealing Light*), use `SetClockCounter(targetCard, turnos)` no momento da ativação. O sistema desenhará um relógio holográfico sobre a carta.
    *   Dentro do evento `OnPhaseStart`, chame `HandleTurnCounter(card)` para que a engine cuide do decréscimo e destrua a carta sozinha quando o tempo acabar.

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
