# Sistema de Eventos e Gatilhos (Event & Trigger System)

## Visão Geral
Este documento descreve a arquitetura de eventos globais, gatilhos e sistemas de jogo complexos. O sistema permite que cartas "escutem" ações do jogo (como invocações, ataques, fases) e reajam de acordo, além de gerenciar mecânicas como Fusão, Ritual e Correntes.

A maior parte da lógica reside no `CardEffectManager.cs` (e suas partes parciais), que atua como um hub central de eventos.

## 1. Hooks de Fase (Phase Triggers)

Estes eventos são disparados pelo `PhaseManager` e processados no `CardEffectManager.OnPhaseStart(GamePhase phase)`.

| Fase | Método | Descrição | Exemplos de Uso |
| :--- | :--- | :--- | :--- |
| **Draw Phase** | `OnPhaseStart(GamePhase.Draw)` | Disparado no início da fase de compra. | *Cyber Archfiend* (compra extra se mão vazia). |
| **Standby Phase** | `OnPhaseStart(GamePhase.Standby)` | Disparado na fase de espera. Usado para custos de manutenção e efeitos recorrentes. | *Imperial Order* (custo), *Solar Flare Dragon* (dano), *Dark Snake Syndrome* (dano progressivo). |
| **End Phase** | `OnPhaseStart(GamePhase.End)` | Disparado no final do turno. Usado para limpar efeitos temporários ou resolver efeitos retardados. | *Light of Intervention* (reset), *Dark Magician of Chaos* (recuperar Spell). |

### Implementação
Para adicionar um efeito de fase, use o método auxiliar `CheckActiveCards(id, action)` dentro do bloco da fase correspondente no `CardEffectManager_Impl.cs`.

```csharp
// Exemplo: Dano na Standby Phase
if (phase == GamePhase.Standby) {
    CheckActiveCards("0235", (card) => { // Bowganian
        if (card.isPlayerCard) Effect_DirectDamage(card, 600);
    });
}
```

## 2. Hooks de Batalha (Battle Triggers)

Estes eventos são disparados pelo `BattleManager` durante as diferentes etapas da batalha.

| Evento | Método no Manager | Descrição | Exemplos de Uso |
| :--- | :--- | :--- | :--- |
| **Declaração de Ataque** | `OnAttackDeclared` | Disparado quando um ataque é declarado, antes de qualquer cálculo. | *Mirror Force*, *Magic Cylinder*, *Dark Spirit of the Silent*. |
| **Cálculo de Dano** | `OnDamageCalculation` | Disparado antes de aplicar o dano. Permite modificar ATK/DEF temporariamente. | *Injection Fairy Lily*, *Skyscraper*, *Buster Rancher*. |
| **Fim da Batalha** | `OnBattleEnd` | Disparado após o cálculo de dano e destruição (se houver). | *D.D. Warrior Lady* (banir), *Mystic Tomato* (busca), *B.E.S.* (remove contador). |
| **Dano Causado** | `OnDamageDealtImpl` | Disparado quando um jogador toma dano de batalha. | *Don Zaloog* (descarte), *Dark Scorpions*. |
| **Restrição de Ataque** | `IsAttackRestricted` | Verificação contínua se um monstro pode atacar. | *Gravity Bind*, *Swords of Revealing Light*. |

### Implementação
Os métodos como `OnBattleEnd` recebem o atacante e o alvo. Use verificações de ID ou Tipo para aplicar a lógica.

```csharp
public void OnBattleEnd(CardDisplay attacker, CardDisplay target) {
    // Exemplo: D.D. Assailant
    if (attacker != null && attacker.CurrentCardData.id == "0378") {
        // Lógica de banimento...
    }
}
```

## 3. Hooks de Invocação e Campo (Field Triggers)

Estes eventos monitoram mudanças no estado do tabuleiro.

| Evento | Método | Descrição | Exemplos de Uso |
| :--- | :--- | :--- | :--- |
| **Invocação (Qualquer)** | `OnSummonImpl` | Disparado após qualquer invocação (Normal, Flip, Special) bem-sucedida. | *Torrential Tribute*, *Trap Hole*, *Breaker the Magical Warrior* (contador). |
| **Setar Carta** | `OnSetImpl` | Disparado quando uma carta é baixada face-down. | *D.D. Trap Hole*. |
| **Invocação Especial** | `OnSpecialSummon` | Disparado especificamente após Special Summon. | *Card of Safe Return*. |
| **Mudança de Posição** | `OnBattlePositionChangedImpl` | Disparado quando um monstro muda de Atk <-> Def. | *Tragedy*, *Blade Rabbit*. |
| **Sair do Campo** | `OnCardLeavesField` | Disparado quando uma carta sai do campo (destruída, banida, retornada). | *Sangan*, *Witch of the Black Forest*, *Butterfly Dagger - Elma*. |
| **Ir para o Cemitério** | `OnCardSentToGraveyard` | Disparado quando qualquer carta vai para o GY. | *Coffin Seller*, *Despair from the Dark*. |
| **Descarte** | `OnCardDiscarded` | Disparado quando uma carta é descartada da mão. | *Dark World* monsters, *Blessings of the Nile*. |

## 4. Hooks de Dano e Vida (LP Triggers)

| Evento | Método | Descrição | Exemplos de Uso |
| :--- | :--- | :--- | :--- |
| **Dano Tomado** | `OnDamageTaken` | Disparado quando um jogador perde LP (batalha ou efeito). | *Numinous Healer*, *Attack and Receive*, *Dark Room of Nightmare*. |
| **Ganho de Vida** | `OnLifePointsGained` | Disparado quando um jogador ganha LP. | *Fire Princess*, *Bad Reaction to Simochi* (inverte). |

## 5. Sistema de Fusão (Fusion System)

Este sistema gerencia a Invocação-Fusão.

| Componente | Responsabilidade |
| :--- | :--- |
| **`CardEffectManager`** | A carta de fusão (ex: *Polymerization*) chama `GameManager.BeginFusionSummon(sourceCard)`. |
| **`GameManager`** | `BeginFusionSummon` abre a UI de fusão. `PerformFusionSummon` executa a lógica final. |
| **`FusionUI`** | Apresenta ao jogador as opções de Monstros de Fusão do Extra Deck e os materiais disponíveis na mão e no campo. |
| **`FusionManager`** | `ValidateFusion` verifica se os materiais selecionados são válidos para o monstro de fusão escolhido. |

### Fluxo de Fusão
1.  O jogador ativa uma carta como *Polymerization*.
2.  `GameManager.BeginFusionSummon` é chamado.
3.  `UIManager` exibe a `FusionUI`.
4.  O jogador seleciona o Monstro de Fusão e os materiais. A UI chama `FusionManager.ValidateFusion` a cada seleção para habilitar/desabilitar o botão de confirmação.
5.  Ao confirmar, `GameManager.PerformFusionSummon` é chamado.
6.  Os materiais e a carta de magia são enviados para o cemitério.
7.  O Monstro de Fusão é invocado do Extra Deck para o campo.

## 6. Sistema de Ritual (Ritual System)

Este sistema gerencia a Invocação-Ritual.

| Componente | Responsabilidade |
| :--- | :--- |
| **`CardEffectManager`** | A Magia de Ritual (ex: *Black Luster Ritual*) chama `GameManager.BeginRitualSummon(sourceCard)`. |
| **`GameManager`** | `BeginRitualSummon` abre a UI de ritual. `PerformRitualSummon` executa a invocação. |
| **`RitualUI`** | Apresenta ao jogador os Monstros de Ritual na mão e os monstros disponíveis para tributo na mão e no campo. |
| **`RitualManager`** | `ValidateRitual` verifica se a Magia de Ritual corresponde ao monstro e se a soma dos níveis dos tributos é suficiente. |

### Fluxo de Ritual
1.  O jogador ativa uma Magia de Ritual.
2.  `GameManager.BeginRitualSummon` é chamado.
3.  `UIManager` exibe a `RitualUI`.
4.  O jogador seleciona o Monstro de Ritual da mão e os tributos. A UI chama `RitualManager.ValidateRitual` para validar a seleção.
5.  Ao confirmar, `GameManager.PerformRitualSummon` é chamado.
6.  A Magia de Ritual e os tributos são enviados para o cemitério.
7.  O Monstro de Ritual é invocado da mão para o campo.

## 7. Sistema de Contadores de Magia (Spell Counter System)

Gerencia a adição, remoção e contagem de Spell Counters em cartas.

| Componente | Responsabilidade |
| :--- | :--- |
| **`SpellCounterManager`** | Mantém o estado dos contadores para cada `CardDisplay`. Gerencia a visualização (prefabs de contadores). |
| **`CardDisplay`** | Possui métodos de atalho `AddSpellCounter` e `RemoveSpellCounter` que delegam ao Manager. |

### Uso
*   **Adicionar:** `SpellCounterManager.Instance.AddCounter(card, amount)`
*   **Remover:** `SpellCounterManager.Instance.RemoveCounter(card, amount)`
*   **Verificar:** `SpellCounterManager.Instance.GetCount(card)`
*   **Remover do Campo:** `SpellCounterManager.Instance.RemoveCountersFromField(amount, isPlayer)` (Útil para custos como *Mega Ton Magical Cannon*).

## 8. Sistema de Corrente (Chain System)

O `ChainManager` gerencia a pilha de ativação de efeitos, permitindo respostas e garantindo a ordem de resolução correta (LIFO - Last-In, First-Out).

| Componente | Responsabilidade |
| :--- | :--- |
| **`ChainManager`** | Mantém a lista de `ChainLink`s. Controla a adição de elos e a resolução da corrente. |
| **`ChainLink` (Classe)** | Representa um único elo na corrente. Contém a carta fonte, o jogador, e um estado `isNegated`. |
| **`SpellTrapManager`** | `CheckChainResponse` verifica se o oponente tem uma carta válida para responder (encadear). |
| **`GameManager`** | Inicia a corrente ao ativar uma carta, chamando `ChainManager.AddToChain`. |

### Fluxo da Corrente
1.  Jogador A ativa uma carta (ex: *Raigeki*). `GameManager` chama `ChainManager.AddToChain`. Isso cria o **Chain Link 1**.
2.  O `ChainManager` pausa e chama `SpellTrapManager.CheckChainResponse` para o Jogador B.
3.  O `SpellTrapManager` encontra uma *Counter Trap* setada (ex: *Magic Jammer*) e pergunta ao Jogador B se deseja ativar.
4.  Jogador B confirma. `GameManager` ativa *Magic Jammer*, que chama `ChainManager.AddToChain`. Isso cria o **Chain Link 2**.
5.  O `ChainManager` pausa novamente e verifica se o Jogador A tem uma resposta para o Link 2.
6.  Nenhuma resposta é encontrada. A corrente começa a resolver.
7.  **Resolução (LIFO):**
    *   **Resolve Link 2:** O efeito do *Magic Jammer* é executado. Ele nega o Link 1 (`ChainManager.NegateLink(1)`).
    *   **Resolve Link 1:** O `ChainManager` verifica o estado `isNegated` do Link 1. Como está negado, o efeito do *Raigeki* não acontece.
8.  A corrente termina. As cartas usadas (que não são contínuas) são enviadas ao cemitério.

## 9. Outros Sistemas de Suporte

| Evento | Método | Descrição | Exemplos de Uso |
| :--- | :--- | :--- | :--- |
| **Dano Tomado** | `OnDamageTaken` | Disparado quando um jogador perde LP (batalha ou efeito). | *Numinous Healer*, *Attack and Receive*, *Dark Room of Nightmare*. |
| **Ganho de Vida** | `OnLifePointsGained` | Disparado quando um jogador ganha LP. | *Fire Princess*, *Bad Reaction to Simochi* (inverte). |

### Sistema de Modificadores de Stats (Stat Modifiers)

O sistema de `StatModifier` permite alterações temporárias ou permanentes em ATK/DEF sem alterar os valores base originais.

*   **Tipos:** `Temporary` (até fim do turno), `Permanent`, `Continuous` (enquanto condição for válida), `Equip`, `Field`.
*   **Operações:** `Add` (soma), `Multiply` (multiplica), `Set` (define valor fixo).
*   **Uso:** `card.AddStatModifier(new StatModifier(...))`

### Sistema de Seleção (Targeting)

O `SpellTrapManager` e `GameManager` fornecem métodos para seleção interativa de alvos.

*   `StartTargetSelection(filter, callback)`: Permite ao jogador clicar em uma carta no campo que atenda ao filtro.
*   `OpenCardSelection(list, title, callback)`: Abre um modal com uma lista de cartas (do Deck, GY, Mão) para escolha.
*   `OpenCardMultiSelection(...)`: Permite escolher múltiplas cartas de uma lista.

### Sistema de Moedas e Dados

*   `GameManager.Instance.TossCoin(count, callback)`: Rola moedas e retorna o número de caras.
*   `Random.Range(1, 7)`: Usado para rolar dados (D6).

## Boas Práticas para Adicionar Novas Cartas

1.  **Verifique se já existe um Hook:** Antes de criar um novo sistema, veja se o efeito se encaixa em um dos eventos acima.
2.  **Use IDs:** Sempre verifique `card.CurrentCardData.id` para lógica específica de uma carta.
3.  **Use Tags/Race/Attribute:** Para lógica genérica (ex: "Destruir todos os Dragões"), verifique as propriedades da carta.
4.  **Evite `Update()`:** Não use `Update()` em `CardDisplay` para lógica de jogo. Use os eventos do `CardEffectManager`.