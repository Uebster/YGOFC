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
| **Mudança de Posição** | `OnBattlePositionChanged` | Disparado quando um monstro muda de Atk <-> Def. | *Tragedy*, *Blade Rabbit*. |
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
| | *Nota:* `disableFusionCost` no GameManager permite realizar a fusão sem consumir materiais. |

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
| | *Nota:* `disableRitualCost` no GameManager permite realizar o ritual sem consumir tributos. |

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
*   **Acumulação Automática:** O evento `OnSpellActivated` no `CardEffectManager` verifica cartas como *Royal Magical Library* e adiciona contadores automaticamente.
*   **Remover do Campo:** `SpellCounterManager.Instance.RemoveCountersFromField(amount, isPlayer)` (Útil para custos como *Mega Ton Magical Cannon*).

## 8. Sistema de Corrente (Chain System)
## 8. Sistema de Gatilhos de Cemitério e Banimento (Graveyard & Banish Triggers)

### Contexto do Evento
Para resolver problemas de "Missing Timing" e condições de ativação complexas, o evento `OnCardSentToGraveyard` foi aprimorado para incluir contexto sobre como e de onde a carta foi enviada.

*   **`CardLocation fromLocation`**: Indica a origem da carta (`Hand`, `Deck`, `Field`).
*   **`SendReason reason`**: Indica o motivo do envio (`Battle`, `Effect`, `Cost`, `Tribute`).

### Implementação
O `CardEffectManager` agora usa esses parâmetros para validar gatilhos com precisão:

*   **"Quando... você pode..." (Missing Timing):** Efeitos opcionais como o de *Peten the Dark Clown* agora verificam se `reason` **não é** `Cost` ou `Tribute`. Se a carta foi enviada como custo para ativar outro efeito, o gatilho de *Peten* é perdido.
*   **Origem da Carta:** Efeitos como o de *Despair from the Dark* verificam se `fromLocation` é `Hand` ou `Deck` e se `reason` é um efeito do oponente.
*   **Destruição por Batalha:** Efeitos como o de *Mystic Tomato* ou *Sangan* verificam se `reason` é `Battle` e `fromLocation` é `Field`.

Este sistema garante que os efeitos sejam ativados apenas sob as condições corretas, espelhando as regras oficiais do TCG/OCG.

## 9. Sistema de Corrente (Chain System)

O `ChainManager` gerencia a pilha de ativação de efeitos, permitindo respostas e garantindo a ordem de resolução correta (LIFO - Last-In, First-Out).

| Componente | Responsabilidade |
| :--- | :--- |
| **`ChainManager`** | Mantém a lista de `ChainLink`s. Controla a adição de elos, a janela de resposta e a resolução da corrente. |
| **`ChainLink` (Classe)** | Representa um único elo na corrente. Contém a carta fonte, o jogador, o tipo de gatilho (`TriggerType`), a velocidade do efeito (`SpellSpeed`) e um estado `isNegated`. |
| **`SpellTrapManager`** | `GetValidResponses` verifica se um jogador tem cartas válidas para responder ao último elo da corrente. |
| **`GameManager`** | Inicia a corrente ao ativar uma carta, chamando `ChainManager.AddToChain`. |

### Helpers de Negação
*   `GetLinkToNegate(CardDisplay source)`: Identifica o elo anterior na corrente que pode ser negado pela carta atual.
*   `NegateAndDestroy(CardDisplay source, ChainLink targetLink)`: Nega o efeito do elo alvo, destrói a carta de origem e a envia para o cemitério. Usado por *Magic Jammer*, *Seven Tools of the Bandit*, etc.

### Fluxo da Corrente
1.  **Gatilho**: Jogador A declara um ataque. `BattleManager` chama `ChainManager.AddToChain` com `TriggerType.Attack`. Isso cria o **Chain Link 1**.
2.  **Janela de Resposta**: O `ChainManager` pausa o jogo e passa a prioridade para o Jogador B.
3.  **Verificação de Resposta**: O `ChainManager` chama `SpellTrapManager.GetValidResponses` para o Jogador B. O `SpellTrapManager` encontra uma *Sakuretsu Armor* setada, que pode ser ativada em resposta a um ataque.
4.  **Ação do Jogador**: O `UIManager` exibe uma janela para o Jogador B, perguntando se ele deseja ativar *Sakuretsu Armor*.
5.  **Construção da Corrente**: Jogador B confirma. `GameManager` ativa a armadilha, que chama `ChainManager.AddToChain`. Isso cria o **Chain Link 2**.
6.  **Troca de Prioridade**: A prioridade agora volta para o Jogador A. O `ChainManager` verifica se ele tem alguma resposta para o Chain Link 2 (ex: uma *Seven Tools of the Bandit*).
7.  **Passando a Vez**: Jogador A não tem resposta e "passa". Jogador B também não tem mais nada para responder e "passa".
8.  **Resolução (LIFO)**: Como ambos os jogadores passaram em sequência, a corrente começa a resolver de trás para frente:
    *   **Resolve Link 2:** O efeito da *Sakuretsu Armor* é executado. O monstro atacante do Jogador A é destruído.
    *   **Resolve Link 1:** O `ChainManager` verifica o estado do Link 1. Como o ataque foi interrompido (o atacante foi destruído), o efeito do ataque não acontece.
9.  A corrente termina. As cartas usadas (que não são contínuas) são enviadas ao cemitério. O ataque original não é concluído.

## 10. Outros Sistemas de Suporte

| Evento | Método | Descrição | Exemplos de Uso |
| :--- | :--- | :--- | :--- |
| **Dano Tomado** | `OnDamageTaken` | Disparado quando um jogador perde LP (batalha ou efeito). | *Numinous Healer*, *Attack and Receive*, *Dark Room of Nightmare*. |
| **Ganho de Vida** | `OnLifePointsGained` | Disparado quando um jogador ganha LP. | *Fire Princess*, *Bad Reaction to Simochi* (inverte). |

#### Sistema de Limpeza de Equipamentos (Equip Cleanup)
*   **Evento:** `OnCardLeavesField`
*   **Descrição:** Quando um monstro sai do campo (destruído, tributado, etc.), o sistema agora verifica automaticamente se alguma Carta de Equipamento estava ligada a ele. Se sim, a Carta de Equipamento também é destruída e enviada ao cemitério.
*   **Implementação:** A lógica reside no `CardEffectManager_Impl.cs`, que itera sobre os `CardLink`s existentes na cena.

#### Sistema de Modificadores de Stats (Stat Modifiers)

O sistema de `StatModifier` permite alterações temporárias ou permanentes em ATK/DEF sem alterar os valores base originais.

*   **Tipos:** `Temporary` (até fim do turno), `Permanent`, `Continuous` (enquanto condição for válida), `Equip`, `Field`.
*   **Operações:** `Add` (soma), `Multiply` (multiplica), `Set` (define valor fixo).
*   **Uso:** `card.AddStatModifier(new StatModifier(...))`

#### Sistema de Inversão de Stats (Stat Reversal)
*   **Flag Global:** `CardEffectManager.Instance.reverseStats`
*   **Descrição:** Quando esta flag está `true` (ativada por cartas como *Reverse Trap*), o método `CardDisplay.RecalculateStats` inverte a operação de todos os modificadores do tipo `Add`. Buffs (+500) se tornam debuffs (-500) e vice-versa.
*   **Reset:** A flag é automaticamente resetada para `false` no início de cada fase pelo `OnPhaseStart`.

#### Sistema de Seleção (Targeting)

O `SpellTrapManager` e `GameManager` fornecem métodos para seleção interativa de alvos.

*   `StartTargetSelection(filter, callback)`: Permite ao jogador clicar em uma carta no campo que atenda ao filtro.
*   `OpenCardSelection(list, title, callback)`: Abre um modal com uma lista de cartas (do Deck, GY, Mão) para escolha.
*   `OpenCardMultiSelection(...)`: Permite escolher múltiplas cartas de uma lista.

#### Sistema de Moedas e Dados

*   `GameManager.Instance.TossCoin(count, callback)`: Rola moedas e retorna o número de caras.
    *   *Nota:* O resultado pode ser forçado para "Cara" com `alwaysCoinHead` no GameManager (Debug).
*   `Random.Range(1, 7)`: Usado para rolar dados (D6).

## 10. Sistema de Manipulação de Fases e Turno (Phase & Turn Manipulation)

Este sistema permite que cartas alterem o fluxo normal do jogo, pulando fases específicas do turno atual ou do próximo.

| Componente | Responsabilidade |
| :--- | :--- |
| **`PhaseManager`** | Mantém o estado das fases e flags de pulo (`skipDrawPhase`, etc.). |
| **`GameManager`** | Controla a alternância de turnos (`SwitchTurn`) e verifica condições de vitória. |

### Funcionalidades
*   **Pular Fase Atual:** Cartas como *Thunder of Ruler* definem flags como `skipBattlePhase = true` para o turno corrente.
*   **Pular Fase Futura:** Cartas como *Time Seal* usam `RegisterSkipNextPhase` para agendar um pulo de fase no próximo turno do oponente.
*   **Métodos:**
    *   `RegisterSkipNextPhase(bool targetPlayerIsHuman, GamePhase phase)`: Agenda o pulo.
    *   `StartTurn()`: Aplica os pulos agendados e reseta as flags.

## 11. Sistema de Efeitos Contínuos e Restrições (Continuous Effects & Locks)

Gerencia efeitos passivos que impõem regras globais ao jogo, verificados dinamicamente antes de permitir ações.

### Negação de Efeitos
Implementado no `CardEffectManager.ExecuteCardEffect`. Antes de executar qualquer efeito, o sistema verifica:
*   **Skill Drain (1655):** Se ativo, impede a execução de efeitos de monstros face-up no campo.
*   **The End of Anubis (1858):** Se ativo, impede a execução de efeitos de cartas no Cemitério.

### Restrições de Invocação e Jogo
Implementado no `GameManager` (`TrySummonMonster`, `PlaySpellTrap`) e `BattleManager`.
*   **Spatial Collapse (1716):** Verifica `GetFieldCardCount`. Se >= 5, impede novas cartas.
*   **Rivalry of Warlords (1541):** Verifica se o novo monstro compartilha o Tipo dos monstros já controlados.
*   **The Regulation of Tribe (1884):** Verifica no `BattleManager.CanAttack` se o monstro atacante pertence ao Tipo declarado.

## 12. Sistema de Efeitos Retardados (Delayed Effects)

Este sistema gerencia efeitos que não acontecem imediatamente, mas em uma fase futura ou após um número de turnos. A lógica é processada no `CardEffectManager.OnPhaseStart`.

| Componente | Responsabilidade |
| :--- | :--- |
| **`CardDisplay`** | Armazena flags e contadores para efeitos retardados (`scheduledForDestruction`, `destructionTurnCountdown`). |
| **`CardEffectManager`** | No `OnPhaseStart`, verifica todas as cartas no campo para processar os contadores e disparar os efeitos quando as condições são atendidas. |

### Tipos de Efeitos
*   **Destruição na End Phase:** Cartas como *Wild Nature's Release* marcam seu alvo com `scheduledForDestruction = true`. O `CardEffectManager` destrói a carta na End Phase.
*   **Contador de Turnos:** Cartas como *Viser Des* ou *Zone Eater* definem `destructionTurnCountdown` em seus alvos. O `CardEffectManager` decrementa o contador a cada Standby Phase do jogador relevante e destrói a carta quando o contador chega a zero.
*   **Acumulação por Fase:** Cartas como *Wave-Motion Cannon* usam o `turnCounter` genérico do `CardDisplay`, que é incrementado a cada Standby Phase do controlador.

## 13. Lógica de Batalha e Invocação Avançada

#### Ataques Múltiplos e Restrições
*   **Ataque Duplo:** `BattleManager` verifica `maxAttacksPerTurn` no `CardDisplay`. Cartas como *Tyrant Dragon* e *Mermaid Knight* modificam esse valor.
*   **Bloqueio de Dano:** A flag `cannotInflictBattleDamage` no `CardDisplay` (usada por *Union Attack*) impede que o oponente tome dano, mas a destruição de monstros ainda ocorre.
*   **Restrições de Ataque:** `BattleManager.CanAttack` verifica condições complexas como *Wall of Revealing Light* (baseado em `paidLifePoints`) e *Vengeful Bog Spirit*.

#### Tributos Especiais
*   **Tributo Duplo:** O `SummonManager.HasEnoughTributes` agora verifica o monstro alvo. Cartas como *Unshaven Angler* contam como 2 tributos se o monstro invocado for do Atributo correto (WATER).

#### Flags Globais de Turno
As flags que alteram regras fundamentais são distribuídas entre os gerenciadores conforme sua responsabilidade:

**No `BattleManager`:**
*   `forceDirectAttack`: Obriga ataques a serem diretos (*Absolute End*).
*   `globalPiercing`: Todos os monstros causam dano perfurante (*Meteorain*).
*   `wabokuActive`: Previne dano de batalha e destruição (*Waboku*).
*   `noBattleDamageThisTurn`: Previne dano de batalha (*Winged Kuriboh*).
*   `cannotAttackFaceDown`: Impede ataques a monstros virados para baixo (*A Feint Plan*).
*   `battlePositionsLocked`: Impede mudança de posição (*Mesmeric Control*).

**No `CardEffectManager`:**
*   `reverseStats`: Inverte buffs/debuffs (*Reverse Trap*).
*   `banishInsteadOfGraveyard`: Redireciona cartas para a zona de banimento (*Spirit Elimination*, *Macro Cosmos*).
*   `negateContinuousSpells`: Nega efeitos de magias contínuas (*Mystic Probe*).
*   `redirectSpellTarget`: Redireciona o alvo de magias (*Mystical Refpanel*).

## 14. Padrões de Implementação por Subtipos (Subtypes)
O sistema foi desenhado para facilitar a criação de magias e armadilhas complexas reutilizando Helpers globais:

*   **Equip (Equipamento):** Utilize `Effect_Equip(source, atkBonus, defBonus, requiredRace, requiredAttribute)`. Este helper cuida de toda a criação do `CardLink`, valida o alvo e aplica os `StatModifiers` corretamente.
*   **Field (Campo):** Utilize `Effect_Field(source, atkBonus, defBonus, requiredRace, requiredAttribute)`. Ele varre o campo automaticamente aplicando os bônus aos tipos especificados.
*   **Continuous (Contínuo):** Não necessita de um helper de ativação direta. O segredo é adicionar a lógica passiva dentro do evento adequado, utilizando `CheckActiveCards("ID_DA_CARTA", (card) => { ... })` para varrer os campos e aplicar efeitos apenas se a carta contínua estiver ativada.
*   **Counter (Resposta):** Utilize a dupla de métodos nativos do ChainManager: `GetLinkToNegate(source)` para pegar o alvo, seguido de `NegateAndDestroy(source, link)`. A velocidade da carta (Speed 3) é tratada automaticamente pela Engine.
*   **Quick-Play (Rápida):** A velocidade (Speed 2) permite ativação livre. Se a carta possui um efeito direto, use os helpers padrão de destruição ou buff.
*   **Ritual:** Chame diretamente `GameManager.Instance.BeginRitualSummon(source)`.

## 15. Sistemas de Sorte e Tempo (Minigames)
A engine fornece suporte embutido para mecânicas de moedas, dados e contagem de turnos. Use sempre os métodos oficiais em vez de funções de Random isoladas para facilitar a futura integração de interface gráfica (UI).

#### Sistema de Moedas (Coin Toss)
*   **`GameManager.Instance.TossCoin(int count, Action<int> callback, bool loopUntilTails = false)`**
    *   O sistema visual interage com o jogador, abstraindo escolhas e interações físicas. O callback sempre retorna a quantidade absoluta de **Acertos**.
    *   **Variação 1 (Aposta Simples):** `TossCoin(1, result => ...)` -> Ideal para *Time Wizard*. Exibe os botões de escolha e joga 1 moeda. Retorna 1 se o jogador acertou a previsão, 0 se errou.
    *   **Variação 2 (Múltiplas Moedas):** `TossCoin(3, result => ...)` -> Ideal para *Barrel Dragon*. Pula os botões de escolha, instancia 3 moedas na tela de uma vez e as gira, retornando o número total de vitórias (Caras).
    *   **Variação 3 (Loop Contínuo):** `TossCoin(1, result => ..., true)` -> O loop gira a moeda. Se o jogador ganhar, o sistema não fecha, mas oferece a chance de girar de novo para acumular a "Streak" até que ele perca, retornando o acumulado.
    *   **Configurações do GameManager (Inspect):**
        *   `coinTossRequiresChoice`: Se ativado, a "Variação 1 e 3" vai perguntar se o jogador quer Cara ou Coroa antes de começar. Se desativado, o jogo pula a tela de botões e assume que "Cara = Vitória".
        *   `coinTossManualSpin`: Se ativado, as moedas spawnadas ficam congeladas aguardando o jogador **Clicar** sobre elas fisicamente para fazê-las pular e girar no ar. Se desativado (automático), elas começam a pular sozinhas assim que aparecem com um leve delay de cascata.
        *   `alwaysCoinHead`: Se ativado, serve como trapaça (Cheat) para testes, garantindo que o resultado será sempre o de vitória.
*   **Helper:** `Effect_CoinTossDestroy(source, numCoins, requiredHeads, targetType)` automatiza a rolagem e a destruição condicional de alvos no campo.

#### Sistema de Dados (Dice Roll)
*   **`GameManager.Instance.RollDice(int count, bool requireChoice, Action<List<int>> callback)`**
    *   O sistema visual interage com o jogador, abstraindo a física da rolagem. O callback retorna a lista com o resultado exato de cada dado (1 a 6).
    *   **Variação 1 (Rolagem Simples/Múltipla):** `RollDice(2, false, result => ...)` -> Ideal para *Snipe Hunter*. Joga 2 dados na tela e retorna os resultados.
    *   **Variação 2 (Adivinhação Prévia):** `RollDice(1, true, result => ...)` -> Ideal para *Blind Destruction*. Abre um painel de UI para o jogador escolher um palpite (1 a 6) antes de rolar o dado.
    *   **Configurações do GameManager (Inspect):**
        *   `diceRollManualSpin`: Se ativado, os dados esperam que o jogador clique fisicamente neles para rodopiar. Se desativado, rolam automaticamente.
        *   `alwaysDiceSix`: Se ativado, serve como trapaça (Cheat) garantindo que o resultado sempre será 6.
*   **Helper:** `CardEffectManager.Instance.RollDice(amount, callback, requireChoice)` repassa a chamada para as cartas de forma simplificada.

#### Sistema de Relógio (Clock / Turn Counters)
*   **Ativação:** `CardEffectManager.Instance.SetClockCounter(targetCard, turns)`
    *   Força uma carta a receber um "relógio" visual e inicia o contador com o número máximo de turnos (Ex: *Swords of Revealing Light*).
*   **Manutenção:** Em `OnPhaseStart`, utilize a função genérica `HandleTurnCounter(card)` para decrementar o relógio automaticamente. Quando chegar a 0, a carta é destruída pelo sistema.

#### Lógica Visual do Relógio (TurnClockUI)
*   **Fatias de Pizza:** O visual do relógio é dividido dinamicamente com base no total de turnos da carta. Se a carta dura 3 turnos, o relógio é dividido em 3 fatias de 120 graus. O ponteiro (`clockHand`) avança exatamente uma fatia por turno.
*   **Limite de 12 Turnos:** Cada mostrador de relógio suporta no máximo 12 turnos (semelhante a um relógio real). 
*   **Overflow (Transbordamento):** Se um efeito exigir mais de 12 turnos (Ex: *Final Countdown* - 20 turnos), o sistema instanciará **múltiplos relógios** lado a lado no `clocksContainer` (ex: 1 relógio cheio com 12, e 1 relógio com 8 fatias).
*   **Unity Setup:** Utiliza o componente nativo `Image.fillAmount` configurado como `Radial 360` para mascarar as fatias perfeitamente sem precisar de texturas adicionais.

## 16. Sistema de Fichas (Tokens)

Fichas são monstros ilusórios criados por efeitos (ex: *Scapegoat*). Elas possuem uma regra fundamental: **só podem existir no campo**.

*   **Criação:** Use `GameManager.Instance.SpawnToken(forPlayer, atk, def, name, level, race, attribute)`. O sistema automaticamente assinala o ID mágico `"TOKEN"` a esta carta.
*   **Aparência:** O sistema utiliza o `TokenPrefab` (geralmente uma carta com fundo cinza/prata) definido no GameManager.
*   **Evaporação (Destruição Absoluta):** Graças à barreira de ID implementada no GameManager e DeckManager, se um Token for alvo de efeitos que o enviariam para a Mão (ex: *Penguin Soldier*), Cemitério, Deck ou Pilha de Banidos, a engine detecta o ID `"TOKEN"`, destrói o GameObject e **aborta** a adição nas listas de dados, fazendo-o "evaporar" perfeitamente de acordo com as regras do TCG.

## 17. Boas Práticas para Adicionar Novas Cartas

1.  **Verifique se já existe um Hook:** Antes de criar um novo sistema, veja se o efeito se encaixa em um dos eventos acima.
2.  **Use IDs:** Sempre verifique `card.CurrentCardData.id` para lógica específica de uma carta.
3.  **Use Tags/Race/Attribute:** Para lógica genérica (ex: "Destruir todos os Dragões"), verifique as propriedades da carta.
4.  **Evite `Update()`:** Não use `Update()` em `CardDisplay` para lógica de jogo. Use os eventos do `CardEffectManager`.

## 18. Regras de Validação de Ativação (Activation Legality)

Como regra global de design oficial de Yu-Gi-Oh!, **nenhuma carta mágica, armadilha ou efeito de ignição pode ser ativada se suas condições de resolução não puderem ser cumpridas no momento da ativação**.
Isso evita que o jogador ative a carta à toa (ex: ativar um Equip Spell sem monstros válidos no campo, ou ativar *Dark Hole* com o campo vazio).

*   **Implementação Futura:** Será necessário criar um método de pré-validação (`CanActivate(CardDisplay card)`) no `CardEffectManager` que será chamado pela UI (Menu de Ação) antes de habilitar o botão "Activate".
*   **Condição Intermediária:** Enquanto a interface não for automatizada para bloquear o clique, todas as lógicas em `CardEffectManager_Impl` que abrem janelas de seleção **devem** começar checando se há alvos válidos. Se não houver, deve apenas dar um `Debug.Log` ou `ShowMessage` e abortar.