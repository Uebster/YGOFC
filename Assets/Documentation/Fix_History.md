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