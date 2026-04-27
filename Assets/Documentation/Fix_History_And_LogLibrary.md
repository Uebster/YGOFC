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

## 15. A Síndrome de Estocolmo (Dono vs Controlador)
*   **Sintoma:** Cartas roubadas do oponente (ex: através da magia *Autonomous Action Unit* ou *Change of Heart*) iam para o cemitério, mão ou pilha de banimento de quem as controlava no momento em que eram destruídas, e não para as pilhas do dono original (Owner).
*   **A Causa Raiz:** Os métodos do `GameManager_BoardActions` utilizavam estritamente a variável `card.isPlayerCard` (que define o controlador atual para acender luzes na UI) na hora de despachar as cartas para o `MoveCard` ou `SendToGraveyard`.
*   **A Solução:** A propriedade `ownerPlayer` foi oficializada no `CardDisplay`. Todos os métodos de Invocação Especial e Adição à Mão do `GameManager` foram atualizados para herdar e carimbar o dono original lendo de qual pilha a carta saiu. As funções de varredura e o `MoveCard` passaram a utilizar exclusivamente o `isOwner` para decidir o destino físico da carta, mantendo o `isController` apenas para questões de rendering e controle de turnos.

## 16. O Falso Spin (Retorno ao Baralho com Parâmetro Nil)
*   **Sintoma:** Ao usar magias de Spin (Ex: *Back to Square One*), monstros inimigos eram mandados para o topo do NOSSO deck, e a animação de voo fazia a carta aterrissar no deck com a face virada para cima (Face-Up).
*   **A Causa Raiz:** O script Lua nativo enviava `nil` no parâmetro `player` da função `Duel.SendtoDeck`. O conversor C# traduzia `nil` para `0` (Jogador Humano), basicamente sequestrando os monstros do oponente para o nosso baralho. Além disso, a animação do `DuelFXManager` estava com `endFaceUp = true` fixado no código (hardcoded).
*   **A Solução:** A corrotina `SendtoDeckRoutine` no `LuaDuel_Actions` foi totalmente reescrita. Agora, ela verifica se a variável `player` foi intencionalmente fornecida pelo LUA. Caso seja `nil`, ela extrai o `ownerPlayer` de *cada* carta individualmente dentro do grupo selecionado. Paralelamente, o `DuelFXManager_Flights` passou a aceitar a flag de estado `startFaceUp`, permitindo que o holograma suba do campo com a arte à mostra, mas forçando o pouso com `endFaceUp = false`, virando a carta para o verso com perfeição ao tocar no deck de quem for de direito.

## 17. O Cérebro do "Smart Select" (Avaliador de Subconjuntos Matemáticos)
*   **Sintoma:** O seletor tátil de tributos para monstros de Ritual alternava burramente entre cartas redundantes, gerando "peso morto" na seleção, ou exigindo que o jogador desmarcasse as cartas manualmente. Além disso, a UI impedia selecionar monstros direto na mão forçando um Janelão Modal.
*   **A Causa Raiz:** O algoritmo usava um sistema simples de "Rolling Selection" (se estourar o limite máximo, remova a carta mais antiga da lista). Isso não entendia Matemática Discreta para achar a soma exata dos níveis dos rituais. A janela tátil também só funcionava se `min == max`.
*   **A Solução:** Implementação do algoritmo *Power-Set* (Subconjuntos) no `GameManager_Selections`. Ao clicar em uma nova carta, o C# avalia todas as `1 << (n - 1)` permutações possíveis das cartas que já estavam selecionadas + a carta recém clicada. Ele encontra o subconjunto que fecha a conta matematicamente exata e descarta silenciosamente as cartas excedentes. Além disso, liberou-se o clique na mão com a mecânica de "Right-Click to Finish", permitindo aprovar seleções parciais se o mínimo já foi atingido.

## 18. A Cinemática de Extração (Pop-Out) e o Fallback do Topo da Pilha
*   **Sintoma:** Extrações de cartas do meio do deck ou do cemitério (ex: Sangan, Monster Reborn) aconteciam invisivelmente ou o holograma atirava a carta para fora da tela.
*   **A Causa Raiz:** Faltava uma rotina de apresentação para as pilhas invisíveis (Piles). Quando criamos o `slideOffset` de 150 no `DuelFXManager`, ele foi aplicado no World Space do Canvas sem multiplicar pelo `lossyScale`, arremessando a carta a milhares de pixels de distância.
*   **A Solução (Extração OCG):** Criamos a `ExtractionCinematic`. O C# gera um fantasma, desliza a carta pelo eixo X mitigado pela escala do Canvas para tirá-la de baixo da pilha, dá um "Scale Pulse" para inflar e um Flip 3D para revelar. Tudo de forma modular.
*   **O Fallback de Inteligência (IsTopCard):** Se a carta a ser extraída for a última filha instanciada na UI do Cemitério/Deck (`childCount - 1`), a Engine entende que ela *já está no topo*. Sendo assim, o C# dá bypass (pula) o deslize horizontal e apenas dá um pulo vertical rápido (`topJumpHeight`). Se a configuração `ReturnToTop` estiver ativa, ela faz o recuo caindo exatamente na origem antes de liberar o Voo (`CardFlight`).

## 19. O Atropelo do Cemitério e o Vórtice de Banimento
*   **Sintoma:** Ao banir cartas diretamente do cemitério, o Vórtice de Banimento aparecia deslocado fisicamente. O pior ocorria quando a *Big Bang Shot* era destruída: ela puxava o monstro para banir antes mesmo que a própria magia terminasse de aterrissar no cemitério, causando pulos bizarros e falsas duplicações de VFX.
*   **A Causa Raiz (Race Condition):** Assincronicidade agressiva. A mágica perdia o equipamento e ia para o GY; no exato milissegundo seguinte, acionava o efeito LUA de banir. O componente `HorizontalLayoutGroup` e o script `PileDisplay` da Unity ainda não tinham processado o novo filho, então não tinham o `anchoredPosition` real atualizado.
*   **A Solução:** Injeção de pausas táticas. Adicionado `yield return new WaitForSeconds(0.6f)` na `RemoveRoutine` do LUA. A engine C# agora literalmente "cruza os braços", espera a poeira abaixar, deixa a Unity recalcular a grade do Cemitério, e só então procura a coordenada absoluta exata da carta. Após o Vórtice engolir o alvo, o fantasma é destruído forçosamente `Destroy(ghost)` para prevenir a ilusão da duplicata voadora.

## 20. O Brilho Universal no Cemitério (Ghostbusters / Caça-Fantasmas)
*   **Sintoma:** O *Sangan* e o *Black Pendant* ativavam seus efeitos e causavam dano/buscavam cartas de forma totalmente silenciosa e invisível quando caíam no cemitério, sem o pulso luminoso ciano ou a tag "Link 1".
*   **A Causa Raiz:** A Engine de *Chain Link* tentava criar a luz lendo a propriedade `luaCard.unityCard`. Porém, efeitos de cemitério usam o gatilho `EVENT_TO_GRAVE`. No exato momento em que este gatilho roda, o GameObject da carta que estava no tabuleiro já sofreu `Destroy()` no C#. Como a referência física antiga é nula, a engine visual abortava a animação da corrente.
*   **A Solução (FindCardDisplayInPiles):** Criamos um radar de almas. Ao iniciar qualquer corrente, se a carta alvo não estiver ativa no tabuleiro (`unityCard == null` ou inativo), o `ChainManager` chama `FindCardDisplayInPiles()`. Ele escaneia silenciosamente as UIs de `GraveyardDisplay`, `DeckDisplay` e `RemovedDisplay` de trás pra frente (para achar a recém-chegada). 
*   Ao achar a representação visual da carta morta na pilha, a Engine a arrasta para a frente (`SetAsLastSibling`), invoca a `AnimateCardActivationInPileRoutine` (Pulso Ciano Neon de 1.4x de escala) e coloca o texto "Link 1" sobre ela, unificando definitivamente o feedback visual de QUALQUER carta ativada fora de campo!

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

### O "Engasgo" do Card Viewer (Verso Preso)
- **Bug Original:** Ao passar o mouse rapidamente de uma carta virada para baixo (Face-down) para uma carta idêntica virada para cima (Face-up), o CardViewer continuava mostrando o verso da carta, dando a sensação de "travada".
- **Correção Aplicada:** A lógica de otimização de renderização do `GameManager` verificava apenas se o ID da carta sob o mouse era o mesmo da memória do painel. Criamos a flag `cardViewerShowingBack` para forçar o recarregamento instantâneo da textura frontal se a memória anterior estivesse presa no verso, matando o engasgo.

### Fantasma de Clique no Turno 1 (Hover Preso)
- **Bug Original:** Mesmo com o ataque bloqueado no Turno 1 e a `TargetingSwordUI` impedida de nascer, clicar em um monstro do jogador fazia ele acender a borda vermelha (Attack Selection) e ficar travado aguardando um alvo fantasma.
- **Correção Aplicada:** A trava visual da espada no `TargetingSwordUI` foi movida para uma corrotina `WaitForEndOfFrame()`. Isso permite que a Unity processe a bagunça inteira do clique (EventSystem) e, no milissegundo final antes de desenhar a tela, a engine passa uma "borracha", forçando `SetAttackSelectionVisual(false)` e limpando o alvo do motor Lua.

### Conflito de Eixo X no Hover da Mão (Layout Group)
- **Bug Original:** Ao manter o mouse sobre uma carta na mão enquanto uma nova carta era sacada (ou a mão reordenada), a carta sob o hover dava um "pulo" bizarro ou tremia para os lados, ficando fora de sincronia com as outras.
- **Correção Aplicada:** O `HoverAnimationRoutine` no `CardDisplay` estava memorizando e forçando a posição absoluta nos eixos X e Y (`basePosition`). Como a Mão usa um *Horizontal Layout Group* (que controla o X dinamicamente), ocorria uma "briga" entre a animação e o Layout. A corrotina foi alterada para interpolar estritamente o eixo Y (`anchoredPosition.y`), deixando o eixo X livre para a Unity deslizar a carta suavemente para os lados durante o Hover.



# Lista de Logs (Log Library)

**SaveLoadMenu**
        Debug.Log($"[SaveLoadMenu - {menuType}] Awake: Iniciando auto-configuração.");
