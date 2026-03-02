# Sistema de Eventos e Gatilhos (Event & Trigger System)

## Visão Geral
Este documento descreve a arquitetura de eventos globais e gatilhos utilizada no projeto para implementar efeitos de cartas complexos. O sistema permite que cartas "escutem" ações do jogo (como invocações, ataques, dano, fases) e reajam de acordo.

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

## 5. Sistema de Modificadores de Stats (Stat Modifiers)

O sistema de `StatModifier` permite alterações temporárias ou permanentes em ATK/DEF sem alterar os valores base originais.

*   **Tipos:** `Temporary` (até fim do turno), `Permanent`, `Continuous` (enquanto condição for válida), `Equip`, `Field`.
*   **Operações:** `Add` (soma), `Multiply` (multiplica), `Set` (define valor fixo).
*   **Uso:** `card.AddStatModifier(new StatModifier(...))`

## 6. Sistema de Seleção (Targeting)

O `SpellTrapManager` e `GameManager` fornecem métodos para seleção interativa de alvos.

*   `StartTargetSelection(filter, callback)`: Permite ao jogador clicar em uma carta no campo que atenda ao filtro.
*   `OpenCardSelection(list, title, callback)`: Abre um modal com uma lista de cartas (do Deck, GY, Mão) para escolha.
*   `OpenCardMultiSelection(...)`: Permite escolher múltiplas cartas de uma lista.

## 7. Sistema de Moedas e Dados

*   `GameManager.Instance.TossCoin(count, callback)`: Rola moedas e retorna o número de caras.
*   `Random.Range(1, 7)`: Usado para rolar dados (D6).

## Boas Práticas para Adicionar Novas Cartas

1.  **Verifique se já existe um Hook:** Antes de criar um novo sistema, veja se o efeito se encaixa em um dos eventos acima.
2.  **Use IDs:** Sempre verifique `card.CurrentCardData.id` para lógica específica de uma carta.
3.  **Use Tags/Race/Attribute:** Para lógica genérica (ex: "Destruir todos os Dragões"), verifique as propriedades da carta.
4.  **Evite `Update()`:** Não use `Update()` em `CardDisplay` para lógica de jogo. Use os eventos do `CardEffectManager`.