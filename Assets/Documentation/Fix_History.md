# Histórico de Correções Complexas (Fix History)

Este documento serve como um registro de bugs traiçoeiros, suas causas raízes (geralmente relacionadas a peculiaridades da engine da Unity ou do ciclo de vida dos scripts) e as soluções definitivas. É um guia de consulta rápida para evitar a reintrodução de erros clássicos.

---

## 1. O "Primeiro Clique Fantasma" no Menu de Ação

*   **Sintoma:** Ao iniciar um duelo e clicar em uma carta na mão pela *primeira vez*, o log acusava o clique, mas o `DuelActionMenu` (Summon/Set) não aparecia. Clicar pela segunda vez fazia o menu aparecer e funcionar normalmente pelo resto da partida.
*   **A Causa Raiz (Ciclo de Vida da Unity):** O painel do menu começava desativado (`SetActive(false)`) no `Awake()`. Como ele nunca havia ficado ativo, a Unity colocou o método `Start()` do script em "espera". Ao receber o primeiro clique, o script ativava o painel. Nesse exato frame, a Unity percebia a ativação e rodava o `Start()`. O problema é que dentro do `Start()` havia uma chamada para `CloseMenu()`, fazendo o painel abrir e fechar num piscar de olhos.
*   **A Solução:** O método `Start()` foi completamente removido do script `DuelActionMenu.cs`. Como a função dele era apenas garantir que a UI começasse fechada, o `Awake()` já supria essa necessidade de forma muito mais segura.

---

## 2. A "Gaiola do Tempo" e o Vai-e-Volta da Standby Phase

O controle de tempo da *Standby Phase* apresentou dois sintomas distintos que mascaravam um ao outro.

### Sintoma A: Alteração de tempo no código não surtia efeito
*   **O Problema:** Alterar a variável `public float standbyPhaseDuration = 0.4f` para valores absurdos como `5.0f` direto no arquivo `.cs` não alterava o tempo em jogo.
*   **A Causa Raiz (Serialização do Inspector):** Na Unity, variáveis `public` são serializadas na Scene ou no Prefab. O valor que estiver digitado na caixinha do Inspector tem precedência absoluta e *sobrescreve* o que está escrito no código durante o Runtime.
*   **A Solução:** Foi adicionado um `[Tooltip]` no código para alertar os desenvolvedores de que alterações devem ser feitas no Inspector do componente `PhaseManager`.

### Sintoma B: O Pulo Prematuro de Fase
*   **O Problema:** Quando o tempo foi reduzido para ser ágil (ex: `0.3s`), o jogo pulava para a *Main Phase 1* muito rápido, "atropelando" efeitos de *Standby Phase* disparados pela engine LUA. O LUA então tentava exibir um modal de interrupção enquanto a UI já marcava Main Phase, causando dessincronização visual e travamentos de fluxo.
*   **A Causa Raiz:** A corrotina `HandleStandbyPhase` utilizava apenas um `yield return new WaitForSeconds()`, aguardando o tempo cegamente e forçando a mudança de fase, sem checar se as engrenagens lógicas por trás da tela haviam terminado de trabalhar.
*   **A Solução:** A corrotina foi atualizada para atuar como uma "catraca inteligente".
    ```csharp
    // Aguarda o tempo base
    yield return new WaitForSeconds(standbyPhaseDuration);
    
    // CRÍTICO: Congela o avanço se a Engine LUA estiver resolvendo efeitos de Standby ou aguardando resposta humana.
    if (CardEffectManager.Instance != null) {
        yield return new WaitWhile(() => CardEffectManager.Instance.isChainResolving || CardEffectManager.Instance.isWaitingForLuaYield);
    }
    
    TryChangePhase(GamePhase.Main1);
    ```

---

## 3. FieldStats Dinâmico e Posicionamento (ATK/DEF no Campo)
*   **Sintoma:** O display de ATK/DEF não aparecia para todas as invocações, ficava atrás da carta e revelava os status de monstros virados para baixo do oponente.
*   **A Causa Raiz:** O prefab de texto estava sendo instanciado como filho do `CardDisplay`, sofrendo interferência do `Canvas` da carta (renderizando atrás). Além disso, a lógica de instanciação só estava presente na Invocação Especial.
*   **A Solução:** O `FieldStatUI` foi alterado para ser instanciado como *irmão* da carta na hierarquia (na própria zona). Ele usa `LateUpdate()` para seguir as coordenadas da carta alvo e aplicar o `Y Offset` (acima ou abaixo), compensando a escala. O código foi injetado também no `FinalizeSummon` para cobrir Invocações Normais. Adicionamos uma trava no `UpdateStats()`: se a carta estiver virada para baixo (`isFlipped`) e for do oponente, o texto recebe `""` (vazio), impedindo o jogador de espionar os dados antes do ataque.

---

## 4. Cinemáticas de Invocação (Fusão e Ritual)
*   **O Desafio:** As invocações de Fusão e Ritual aconteciam de forma instantânea e sem impacto visual, destoando da importância dessas mecânicas.
*   **A Solução:** Foram criadas rotinas assíncronas no `DuelFXManager` (`PlayFusionCinematic` e `PlayRitualCinematic`). Elas instanciam um `DarkOverlay` no Canvas raiz para focar a atenção, usam matemática de seno/cosseno para fazer as cartas materiais orbitarem o centro (Fusão) ou aplicam símbolos de fundo giratórios (Ritual). A engine Lua é pausada enquanto a animação roda e retoma através do callback `onCinematicComplete` antes de colocar o monstro definitivo no tabuleiro e disparar o evento de sucesso.

---

## 5. Cartas Nascendo Fora do Campo e Escala Incorreta
*   **Sintoma:** Após certas invocações, cinemáticas ou trocas de controle (*Snatch Steal*), a carta aterrissava fora do centro da zona de monstro ou ficava gigantesca.
*   **A Causa Raiz:** Animações baseadas em corrotinas alteravam o `localScale` e a posição global da carta para criar o efeito de voo. Ao trocar o `parent` da carta para a nova zona no final da animação, os valores alterados eram mantidos pela Unity.
*   **A Solução:** Adição de "Hard Resets" no `GameManager`. Sempre que uma carta é anexada a uma zona (ex: `cardGO.transform.SetParent(targetZone)` no `FinalizeSummon` ou no `ControlSwapRoutine`), o código força `localPosition = Vector3.zero` e `localScale = GameManager.Instance.fieldCardScale`, garantindo o encaixe e tamanho perfeitos no tabuleiro.

---

## 6. Spoiler do Nome em Ataques a Monstros Setados
*   **Sintoma:** Ao clicar para atacar um monstro inimigo virado para baixo, a janela de confirmação perguntava "Deseja atacar [Nome do Monstro]?", revelando a identidade da carta antes do Damage Step.
*   **A Causa Raiz:** O modal de UI lia diretamente o `currentCardData.name` do alvo selecionado para montar a string de confirmação, ignorando o estado visual (`isFlipped`) da carta.
*   **A Solução:** No `CardDisplay.cs`, a chamada do modal foi interceptada com uma verificação ternária simples: `string targetName = isFlipped ? "monstro virado para baixo" : currentCardData.name;`.

---

## 7. A Máquina de Estados da Espada de Mira (Targeting Sword)
*   **O Desafio:** A "espadinha" que indica o alvo do ataque era estática, instável e não tinha animação de voo, o que deixava o combate confuso.
*   **A Solução:** O `TargetingSwordUI.cs` foi reescrito como uma Máquina de Estados (`SwordState`).
    1.  **Hovering:** Aparece apontando para o mouse só de passar o cursor sobre um monstro apto a atacar.
    2.  **FollowingMouse:** Prende-se ao atacante selecionado e a ponta acompanha o cursor livremente pelo campo.
    3.  **LockedOnTarget:** Trava no alvo durante o popup de confirmação de ataque, apontando diretamente para ele. Destrava se a ação for cancelada.
    4.  **Attacking:** Inicia uma corrotina que interpola a posição da espada do atacante até o alvo. Adicionamos a corrotina `TrailEffect` que clona o sprite da espada com Alpha reduzido e aplica um *Fade Out*, criando um "Rastro de Sombra" durante o voo até o impacto com instâncias que se autodestroem para evitar memory leaks.

---

## 10. Correções Críticas de Batalha e IA (VFX, Cemitério, Field Spells)
*   **Sintoma:** O console acusava `Attack performed without an attacker set!`, os efeitos visuais geravam quadrados opacos, os monstros derrotados não iam para o cemitério e a IA descartava as Magias de Campo imediatamente após a ativação.
*   **A Causa Raiz:** O atacante não estava sendo passado na chamada assíncrona do ataque no LUA. O envio de monstros para o cemitério dependia da animação visual (que por causa da escala do Canvas estava abortando). O LUA verificava as strings "Field" usando Maiúsculas/Minúsculas fixas (Case Sensitive).
*   **A Solução:** No `DuelFXManager`, passamos o atacante. O `MoveCard` (Envio ao GY + Destroy) foi retornado para o `LuaAPI.cs` logo após os danos, delegando à Unity apenas as criações de fantasmas e poeira no *World Space*. Na limpeza do LUA (`CleanupSpellTrapAfterResolution`) as propriedades foram formatadas com `.ToLower()` para evitar o descarte incorreto.

---

## 11. Limite de Invocações, Batalhas e Looping de Chains
*   **Sintoma:** O jogo entrava em loop infinito de Chain Links (Link 1, 2, 3...) quando a IA usava magias da mão. Era possível invocar monstros na MP1 e na MP2 livremente e alterar posições de batalha à vontade (como um ioiô).
*   **A Causa Raiz:** 
    1. A IA chamava `ExecuteCardEffect` diretamente para magias da mão, o que iniciava a Chain, mas não movia a carta pro campo. Como ela ainda estava na mão, a IA via a mesma jogada vantajosa repetidamente. 
    2. A trava de 1 Invocação Normal não estava implementada fora dos scripts Lua, permitindo que a UI "furasse a fila" via ação rápida.
    3. O `ChangePosition` via clique direito não validava os estados de jogo.
*   **A Solução:**
    1. Em `OpponentAI`, a IA agora usa `PlaySpellTrap` para cartas da mão. Ela também respeita um `WaitWhile` aguardando a Chain resolver antes de planejar a próxima ação.
    2. Adicionado o bloqueio hardcoded `normalSummonsThisTurnPlayer > 0` no `TrySummonMonster` do `GameManager`.
    3. Adicionadas flags `summonedTurnCount` e `hasChangedPositionThisTurn` no `CardDisplay` para bloquear trocas no turno de invocação, ou após atacar, ou mais de 1x por turno.

---

## 12. Ataque Inválido a Partir do Modo de Defesa
*   **Sintoma:** O jogador conseguia declarar ataques com monstros que estavam em posição de defesa, ignorando as regras do jogo.
*   **A Causa Raiz:** O evento `OnPointerClick` da carta, que define o `currentAttacker`, não verificava o estado da propriedade `position`. Ele apenas validava se era um monstro, se estava no campo e se ainda não havia atacado.
*   **A Solução:** Adicionada a validação `if (position != BattlePosition.Attack)` no início do bloco de clique esquerdo durante a Battle Phase, acionando o bloqueio e exibindo o popup "Monstros em Defesa não podem atacar".

---

## 13. Efeitos de Impacto de Batalha (Hit VFX) Invisíveis
*   **Sintoma:** A animação de "corte" ou "impacto" (`attackVFX`) não aparecia no momento em que a espada do ataque atingia o alvo, embora o som do impacto tocasse.
*   **A Causa Raiz (Race Condition):** A corrotina de animação da espada (`AnimateAttackRoutine`) instancionava o prefab da partícula e, no mesmo frame, liberava a engine Lua para continuar o cálculo de dano. A lógica do Lua era tão rápida que, em muitos casos, o monstro alvo era destruído e o estado do jogo mudava antes que o `ParticleSystem` da Unity tivesse a chance de renderizar seu primeiro frame, fazendo com que o efeito "nunca aparecesse".
*   **A Solução:** Foi adicionado um `yield return null;` no final da `AnimateAttackRoutine`, logo após a instanciação do VFX e antes de liberar a engine Lua. Essa pausa de um único frame é imperceptível para o jogador, mas dá tempo suficiente para a Unity processar e iniciar a renderização da partícula, corrigindo a condição de corrida e garantindo que o impacto seja sempre visível.

---

## 14. Renderização de Partículas (VFX) e o "Quadrado Verde"
*   **Sintoma:** Muitos efeitos de batalha (Hit, Deflect, Banish) não apareciam na tela ou eram substituídos por um retângulo verde/rosa. Efeitos que apareciam pareciam "inclinados".
*   **A Causa Raiz:** A inclinação era uma configuração de Rotação (`Rotation over Lifetime`) dentro do próprio prefab da partícula. A invisibilidade e os quadrados verdes tinham duas causas: 1) O `DuelFXManager` aplicava um `Destroy(instance, 3.0f)` fixo em todos os efeitos, cortando-os no meio ou apagando-os antes de começarem; 2) O quadrado verde indica um `Material` ausente no componente `Particle System Renderer` do prefab.
*   **A Solução:** O `DuelFXManager.cs` foi atualizado para gerenciar a destruição de forma inteligente, lendo as propriedades `duration` e `startLifetime` do próprio `ParticleSystem` (`Destroy(instance, ps.main.duration + ps.main.startLifetime.constantMax + 0.5f)`). O script avulso `DestroyOnParticleEnd` foi descartado para manter a arquitetura centralizada e limpa. A correção dos quadrados verdes deve ser feita reatribuindo o material nas propriedades dos prefabs no Inspector.

---

[BUG FIX] Shuffle & Draw VFX Giant Scale (Loss of Reference)
- CAUSA: O `DuelFXManager` perdeu as referências de centro de tabuleiro (`boardCenter`). Isso forçava as animações de VFX a caírem no Canvas Raiz. Ao aplicar `GameManager.fieldCardScale` (0.8) no Root Canvas, a Unity calculava o tamanho relativo ao monitor inteiro, não à zona de UI, deixando os fantasmas gigantes.
- BLINDAGEM APLICADA: As animações (ex: `ShuffleRoutine`) não usam mais a escala hardcoded do GameManager. Em vez disso, o script instancia o fantasma dentro da zona física real do Deck, extrai a `localScale` dinâmica calculada pela Unity, e utiliza essa escala (baseFakeScale) como multiplicador absoluto. Além disso, foi adicionado um fallback (`uiParent.position`) para garantir que as cartas achem o centro da tela se as referências do tabuleiro forem deletadas acidentalmente.

## [Data Atual] - Implementação de Auras Globais e Nível Dinâmico (A Legendary Ocean)

**Nova Feature: Sistema de Auras Globais**
* **`GlobalAuraManager` criado:** Implementado para gerenciar efeitos contínuos de campo que afetam todas as cartas simultaneamente (Ex: Magias de Campo, *Gravity Bind*).
* **Filtros OCGCore no LUA:** Funções nativas como `aux.TargetBoolFunction` e `aux.FilterBoolFunction` foram injetadas no `LuaEngineCore` para permitir que o C# leia com precisão os filtros de quem deve ser afetado pelas Auras (Ex: Apenas monstros do atributo WATER).
* **Fix de Assinatura YGOPro:** O `GlobalAuraManager` foi ajustado para passar `(sourceEffect, card)` durante a validação da Aura, corrigindo falhas de interpretação do LUA.

**Correções Críticas de Engine (Bugs de Nível e Bitwise)**
* **Conversão do `CardLocation` para Flags:** O Enum `CardLocation` foi transformado em `[System.Flags]`. Antes, `CardLocation.Hand` valia `0`, o que quebrava a matemática Bitwise `(location & affectedLocations)` e impedia que Auras afetassem cartas na Mão.
* **Sincronia do Card Viewer:** Corrigido bug onde a carta grande na lateral (UI) exibia os stats como se a carta já estivesse no campo. Agora o visualizador consulta a localização real da carta (`Hand` ou `Field`) antes de pedir os bônus ao Aura Manager.
* **Blindagem de Corrotinas LUA (Crash Yield):** Adicionado `(yieldReturnValue ?? DynValue.Nil)` na rotina `RunGenericLuaCoroutine` do `CardEffectManager`. Isso impede que a Engine inteira "crashe" com `NullReferenceException` se o jogador cancelar uma ação de UI (ex: fechar a caixa de Tributo) e devolver vazio para o motor.

**Sincronização de UI e Invocação (Action Menu)**
* **Nível Dinâmico no Menu:** O `DuelActionMenu` foi reescrito. Ele não lê mais o "nível duro" do JSON. Agora ele pergunta ao `GlobalAuraManager` o nível real. Se uma carta nível 5 virar nível 4 na mão, o menu percebe e **exibe os botões de Summon/Set** sem pedir tributo.
* **Injeção de Nível LUA:** O `GameManager` foi instruído a enviar esse "Nível Dinâmico" mastigado direto como quarto argumento para a instrução `Core.NormalSummon` no OCGCore. Isso impede a máquina LUA de recalcular com os dados base e disparar exigências falsas de tributo.

### Cancelamento e Navegação do Mouse Ajustados
- **Bug Original:** Clicar com o botão direito para cancelar o *Targeting* da espada de ataque abria acidentalmente o Menu de Fases no campo, interrompendo a fluidez.
- **Correção Aplicada:** Introduzida a flag de persistência `justCanceledSomething` no `GameManager`. Ela sobrevive até a Unity concluir o disparo do `Mouse Up`, avisando a UI para "engolir" o evento e impedindo a abertura do `PhaseSelectionMenuUI`.
- **Quality of Life:** O atalho `ESC` foi integrado globalmente. Agora ele espelha o comportamento do botão direito, cancelando miras, fechando modais de seleção, menus e correntes pendentes com segurança.
- **Bloqueio de Turno 1 Refinado:** Ataques no primeiro turno foram bloqueados visual e logicamente no final do Frame de renderização (`WaitForEndOfFrame`), garantindo que o `CardDisplay` não fique travado com a seleção de Atacante (Highlight Vermelho) após a negação do Input.
