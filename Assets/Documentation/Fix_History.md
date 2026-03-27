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