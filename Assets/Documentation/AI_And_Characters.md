# 6. Inteligência Artificial e Personagens (AI & Characters)

## Visão Geral
Este documento detalha o funcionamento cerebral dos oponentes virtuais do jogo. Ele unifica a arquitetura de tomada de decisão (Inteligência Artificial) com o sistema de dados estáticos dos personagens, definindo como a máquina avalia o campo e escolhe o baralho adequado (Decks A, B, C) para enfrentar o jogador.

---

## 6.1 Design da Inteligência Artificial (`OpponentAI.cs`)
A Inteligência Artificial do *Yu-Gi-Oh! Forbidden Chaos* não toma decisões puramente baseadas em aleatoriedade. Ela utiliza um sistema de **Scoring (Pontos de Peso)** e **Heurísticas de Estado de Campo** para simular o pensamento de um jogador de TCG.

### 6.1.1 O Sistema de Pontuação (Scoring Engine)
O script `OpponentAI.cs` utiliza as seguintes métricas como base para suas decisões. A aplicação dessas métricas está em desenvolvimento, com o foco atual em decisões críticas de combate e uso de recursos.
*   **Leitura de Custos LUA (`chk=0`):** Antes de avaliar os pontos de uma jogada, a IA consulta `CardEffectManager.CanActivateEffect()`. Se a carta exigir um custo que a IA não pode pagar, a jogada é descartada, evitando erros.
*   **Board Value (Valor de Campo):** Calcula quem está ganhando a mesa somando o ATK/DEF dos monstros de ambos os lados. Um valor positivo significa que a IA está na frente.
*   **Fear Score (Pontuação de Medo):** Quantidade de S/T setadas pelo jogador. Uma pontuação alta pode levar a IA a tomar decisões mais cautelosas.
*   **Panic Threshold (Limiar de Pânico):** Se os LP da IA caem abaixo deste valor (padrão: 1000), ela adota uma postura mais defensiva.

### 6.1.2 Perfis de Personalidade (AI Archetypes)
Para evitar que todos os duelistas joguem da mesma forma, a IA pode ser configurada com um `AIPersonality`. Embora o sistema de pontuação seja o mesmo, a personalidade pode influenciar limiares de decisão.
*   **Agressivo / Swarm:** Tende a ignorar o *Fear Score* e atacar com mais frequência.
*   **Conservador / Control:** Respeita mais o *Fear Score*, preferindo construir uma defesa sólida.
*   **Combo / Estrategista:** Foco em guardar peças-chave.
*   **Defensivo / Stall & Burn:** Foco em estender o duelo e causar dano por efeito.

*(A implementação detalhada de cada personalidade é um trabalho contínuo e a base atual foca principalmente nas distinções entre Agressivo e Conservador).*

### 6.1.3 As Regras de Ouro (Golden Rules of the Engine)
Estas regras estão codificadas diretamente nas rotinas de tomada de decisão, como `FindBestTarget` e `EvaluateSummonActions`.

#### Bypass Lógico do LUA (Targeting Assíncrono)
Como a engine é baseada em scripts que esperam input do jogador, a IA precisa de um desvio.
*   **A Interceptação (`SelectLuaTargets`):** Se o jogador ativo for a IA (`tp == 1`), a Ponte LUA da API cancela a janela de UI e desvia a lista de alvos possíveis para o método `OpponentAI.Instance.SelectLuaTargets()`.
*   **A Heurística de Alvos:** Este método pontua os alvos (destruir o monstro mais forte do oponente é prioridade máxima) e retorna a melhor opção para o script Lua, que continua sua execução sem interrupções.

#### Combate e Riscos (Fog of War)
*   **Scouting (O Escoteiro):** A IA evita atacar monstros virados para baixo com seu monstro mais forte. O método `FindBestTarget` dá um bônus de pontuação para monstros mais fracos atacarem alvos desconhecidos, "testando o terreno" para armadilhas como *Man-Eater Bug*.
*   **Isca de Armadilha (Baiting):** Se o `fearScore` é alto, a IA pode optar por fazer uma invocação ou ataque de menor valor na Main Phase 1 para forçar a ativação de armadilhas, guardando suas jogadas mais importantes para a Main Phase 2.
*   **Mitigação de Dano:** A IA prefere virar monstros para posição de defesa se o ATK do jogador for muito superior.

#### Gestão de Recursos (Card Advantage)
*   **Overextension:** O método `EvaluateSetActions` limita o número de Magias/Armadilhas que a IA baixa por turno (`maxSafeSets`), evitando perdas catastróficas para *Heavy Storm*.
*   **Board Wipes:** A IA avalia o `boardValue` antes de usar cartas como *Dark Hole*, preferindo usá-las quando está em desvantagem.
*   **Paciência e Antecipação (Trap Hole):** No método `ChooseBestResponse`, a IA avalia o ATK do monstro invocado. Ela não gastará um `Trap Hole` em um monstro com menos de 1500 de ATK, a menos que esteja em uma situação de desespero (LP baixo) ou para prevenir uma Invocação de Tributo.

#### Abdicação e Sacrifício Oculto
*   **Sobrevivência > Posse:** A IA ativará uma armadilha em seu próprio monstro se ele foi roubado pelo jogador e o ataque seria letal.
*   **Não Devolver o que foi Roubado:** Se a IA usa *Change of Heart*, ela tentará sacrificar o monstro roubado na Main Phase 2.
*   **Suicide Crash (Busca):** A rotina `FindBestTarget` permite que a IA ataque um monstro mais forte com um "Floater" (*Sangan*, *Mystic Tomato*) de forma proposital para ativar seu efeito de busca.

#### Paciência Visual (`pendingVisualTasks`)
A IA do jogo age em frações de milissegundos, o que poderia engolir as animações visuais.
*   A flag global `GameManager.Instance.pendingVisualTasks` é incrementada toda vez que o `DuelFXManager` entra em um voo complexo ou Cinemática (Ex: Puxar do Deck, Fusões, Retornos). A rotina central da IA e o encerramento das fases utilizam `yield return new WaitWhile(...)` forçando o "Cérebro" a cruzar os braços até que cada animação termine organicamente na tela.


### 6.1.4 Lógicas Específicas de Cartas e Condições de Vitória (Win-Cons)
A IA evoluiu de decisões guiadas por IDs para uma interpretação de **categorias de efeito** definidas no Lua.
*   **Autonomia de Efeitos:** `EvaluateSpellActions` e `EvaluateFieldMonsterActions` leem as `categories` de um `LuaEffect` (ex: `CATEGORY_DESTROY`, `CATEGORY_DRAW`). Isso permite que a IA pontue e utilize cartas novas sem precisar de código C# específico para elas.
*   **Condições de Vitória Especiais:** Se o deck da IA foca em *Destiny Board* ou *Exodia*, a sua personalidade tende a se tornar mais defensiva (Stall), priorizando a sobrevivência para acumular as peças da vitória.
*   **Deck Out:** A IA verifica o número de cartas restantes em seu deck (`GameManager.Instance.GetOpponentMainDeck().Count`) e evita usar efeitos de compra se estiver com 3 ou menos cartas.

### 6.1.5 Estrutura do Loop de Decisão (Decision Tree)
O `AITurnRoutine` executa as fases na seguinte ordem:
1.  `EvaluateBoardState()`: Lê o estado do campo.
2.  `ExecuteMainPhaseLogic()`: Roda um loop que avalia e executa a melhor ação possível (`EvaluateAllPossibleActions`) até que nenhuma ação vantajosa seja encontrada.
3.  `ExecuteBattlePhaseLogic()`: Se for possível batalhar, entra na Battle Phase e avalia os melhores confrontos.
4.  `ExecuteMainPhaseLogic()`: Roda novamente a lógica da Main Phase para ações pós-batalha (como setar cartas).
5.  `EndTurn()`: Passa o turno.

---

## 6.2 Sistema de Personagens e Decks (`CharacterDatabase.cs`)
Os oponentes (Duelistas) enfrentados pelo jogador, tanto na Campanha quanto na Arena, são definidos centralizadamente em um banco de dados lido a partir de um JSON. 

### 6.2.1 Visão Geral e IDs
O `CharacterDatabase` vincula a identidade visual de um personagem ao seu grau de desafio.
A convenção de IDs segue estritamente o formato `XXX_nome_variacao` para facilitar a ordenação, busca e integração com a campanha.
*   **001-010:** Ato 1 (Ex: `001_novice`, `010_duke`)
*   **011-020:** Ato 2 (Ex: `020_pegasus`)
*   **091-100:** Ato 10 (Ex: `100_marik`)

### 6.2.2 Estrutura de Dados (`CharacterData`)
Cada oponente possui o seguinte esqueleto de dados:
*   **ID:** Identificador único lido do JSON.
*   **Name:** Nome de exibição oficial na UI.
*   **Avatar:** Imagem do rosto (Sprite), exibida durante o duelo e nos diálogos.
*   **Difficulty:** Valor numérico (1-10) que alimenta a interface gráfica e o sistema de drops.
*   **Rewards / DropPool:** Dicionário com as recompensas de cartas categorizadas por Tier (S+, S, A, B, C, D) que este oponente pode fornecer ao ser vencido.

### 6.2.3 Sistema de 3 Decks (Variantes A, B, C)
Para aumentar drasticamente a rejogabilidade e prevenir que o jogador decore perfeitamente a sequência de cartas de um bot, cada personagem possui até 3 listas de deck armazenadas no seu registro. O `GameManager` escolhe aleatoriamente ou sob demanda qual usar.

*   **Deck A (Padrão/Vanilla):**
    *   O deck usado na primeira vez que o oponente é enfrentado na história da Campanha. Balanceado estritamente para a curva de dificuldade e o Pool permitido para o Ato correspondente.
*   **Deck B (Avançado/Hard):**
    *   Selecionado dinamicamente em "New Game+", nos confrontos de "Free Duel (Arena)" avançados ou como um Easter Egg de dificuldade.
    *   Contém menos monstros normais fracos e mais foca em sinergias densas e remoções pesadas.
*   **Deck C (Expert/God Mode):**
    *   A variação mais mortal do duelista.
    *   Dependendo do modo ou de trapaças habilitadas pelo DevMode, pode conter combinações de cartas que ignoram a Banlist, múltiplas cópias de Limitadas (Ex: 3 *Pot of Greed*), ou estratégias quase invencíveis como Exodia no Turno 1. Usado para o *Ultimate Challenge* ou chefes ocultos.