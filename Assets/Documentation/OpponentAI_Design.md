# Design Architecture: Opponent AI (Inteligência Artificial)

## Visão Geral
A Inteligência Artificial do *Yu-Gi-Oh! Forbidden Chaos* não toma decisões puramente baseadas em RNG (Aletoriedade). Ela utiliza um sistema de **Scoring (Pontos de Peso)** e **Heurísticas de Estado de Campo**, simulando o pensamento de um jogador veterano de TCG.

## 1. O Sistema de Pontuação (Scoring Engine)
O script `OpponentAI.cs` utilizará as seguintes métricas antes de executar uma jogada:
*   **Board Value (Valor de Campo):** Calcula quem está ganhando. Soma do ATK/DEF dos monstros + peso das cartas S/T ativas.
*   **Fear Score (Pontuação de Medo):** Quantidade de cartas setadas (viradas para baixo) pelo jogador. Dita a agressividade da IA.
*   **Target Score (Alvo Prioritário):** Define qual monstro/carta do jogador deve ser destruída primeiro (Floodgates como *Jinzo* têm pontuação altíssima).
*   **Panic Threshold (Limiar de Pânico):** Se os LP da IA caem abaixo de 1000, ela para de pagar custos opcionais (Ex: *Imperial Order*) e passa a usar recursos defensivos (*Swords of Revealing Light*) imediatamente.

## 2. Perfis de Personalidade (AI Archetypes)
Para evitar que todos os duelistas joguem da mesma forma, o componente da IA terá um Enum `AIPersonality`. As decisões de peso mudam dependendo do perfil do oponente:

1.  **Agressivo / Swarm (Ex: Joey, Rex Raptor):**
    *   Ignora parte do *Fear Score*. Ataca sempre que possível.
    *   Invoca múltiplos monstros por turno.
    *   Foca em Dano Perfurante e buffs de ATK de campo.
2.  **Conservador / Control (Ex: Odion, Ishizu):**
    *   Respeita totalmente o *Fear Score*. Prefere setar monstros a invocá-los em ataque.
    *   Guarda remoções (*Dark Hole*, *Trap Hole*) apenas para ameaças reais (ATK >= 2000).
    *   Nunca dá "All-in" esgotando a mão.
3.  **Combo / Estrategista (Ex: Seto Kaiba, Yugi):**
    *   Esconde peças na mão até poder invocar o "Boss Monster" no mesmo turno.
    *   Sabe sacrificar lacaios de forma inteligente na Main Phase 2.
4.  **Defensivo / Stall & Burn (Ex: Strings, Bakura):**
    *   Joga para estender o duelo ao máximo (fadiga e *Deck Out*).
    *   Foca em cartas que impedem o ataque (*Gravity Bind*, *Swords of Revealing Light*) e causam dano de efeito contínuo.
    *   Seta monstros com alta DEF para forçar o jogador a tomar dano de reflexão e pune ataques descuidados.
5.  **Healer / LP Gain (Ex: Téa Gardner, Ishizu):**
    *   Prioriza curar e manter seus LPs os mais altos possíveis.
    *   Foca em monstros tipo Fada e magias/armadilhas de ganho de vida contínuo (*Solemn Wishes*, *Cure Mermaid*).
    *   Usa a gigantesca vantagem de LP para pagar custos altos (*Wall of Revealing Light*) sem correr riscos.
6.  **Temático Específico (Foco em Tipos/Atributos):**
    *   **Água (WATER):** Prioriza manter o campo *Umi* ativo a todo custo. Esconde monstros e ataca direto.
    *   **Terra (EARTH):** Abusa do campo *Gaia Power*. Foca em invocar múltiplos monstros EARTH para um ataque massivo (já que a DEF cai, a estratégia é 100% agressiva).
    *   **Luz (LIGHT):** Sinergia brutal com *Luminous Spark*. Foca em monstros Fada e Guerreiros LIGHT para criar barreiras de alto ATK.
    *   **Fogo (FIRE):** Sinergia com *Molten Destruction* e dano direto (Burn). Se beneficia de ataques suicidas que causam dano duplo.
    *   **Trevas (DARK):** Utiliza *Mystic Plasma Zone* ou *Yami* para transformar monstros fracos de controle em batedores perigosos.
    *   **Vento / Alados (WIND):** Utilizam o campo *Rising Air Current* e *Mountain*. Focam em controle de campo devolvendo cartas para a mão (Bounce) e ataques diretos rápidos (ex: Harpias).
    *   **Zumbi:** Suicida monstros propositalmente (Crash) para invocar versões mais fortes do GY.
    *   **Inseto:** Foca em travar o campo adversário e causar dano contínuo (Burn).
    *   **Dragão:** Joga em função de invocar "Boss Monsters" de altíssimo ATK (Tributo/Especial). Destrói o campo antes de atacar com tudo.
    *   **Máquina:** Alto risco e alta recompensa. Busca aumento brutal de status e não hesita em destruir as próprias peças por poder (*Limiter Removal*).
    *   **Mago / Demônio:** Gerencia acúmulo de magias e contadores. Abusa do campo *Yami* para um buff rápido sem as grandes perdas de DEF das outras magias de atributo.
    *   **Guerreiro:** Sinergia de campo de batalha (*The A. Forces*). Cria fileiras de ataque e foca em equipamentos para esmagar monstros de frente.
    *   **Dinossauro:** Pura agressividade desmedida. Foca em atacar sempre a defesa com monstros de alto ATK base e dano perfurante (*Piercing*).
    *   **Rocha:** Utiliza extrema defesa e efeitos de Flip. Beneficia-se do campo *Wasteland*. A tática é causar frustração forçando o oponente a atacar paredes intransponíveis (Stall).

## 3. As Regras de Ouro (Golden Rules of the Engine)
Estas regras estão codificadas diretamente nas rotinas de tomada de decisão:

### Combate e Riscos (Fog of War)
*   **Scouting (O Escoteiro):** Nunca atacar um monstro Face-down com o Boss Monster da IA. Sempre atacar primeiro com o monstro mais fraco capaz de causar dano, para testar se é um *Man-Eater Bug* ou *Cyber Jar*.
*   **Isca de Armadilha (Baiting):** Se o oponente tem cartas setadas, invocar um monstro mediano e atacar para forçar a ativação de *Mirror Force*. Só então, na Main Phase 2, realizar invocações importantes.
*   **Mitigação de Dano:** Virar lacaios para Defesa se o ATK do jogador for muito superior, aceitando a destruição para proteger os LPs.
*   **Ordem de Batalha de Fases:** Sempre que for realizar uma Tribute Summon, bater com o tributo na Battle Phase primeiro (se ele tiver chance de sobreviver) e sacrificá-lo na Main Phase 2.

### Gestão de Recursos (Card Advantage)
*   **Overextension:** Nunca setar mais de 2 Magias/Armadilhas no campo de uma vez, evitando perdas totais para *Heavy Storm*.
*   **Tall vs Wide:** Se o oponente tem um monstro gigante, concentrar os Equip Spells no monstro mais forte da IA (Deus Intocável). Se o campo está vazio, distribuir equipamentos nos monstros médios para dividir o risco de um *Trap Hole*.
*   **Board Wipes:** Usar *Dark Hole* ou *Raigeki* apenas se a IA tiver uma desvantagem nítida de monstros (Trade Negativo). Nunca usar 1-por-1.
*   **Reciclagem:** Usar *Giant Trunade* ou efeitos de Bounce para salvar as próprias cartas contínuas prestes a expirar (Ex: *Swords of Revealing Light*).
*   **Paciência e Antecipação (Trap Hole):** Não gastar remoções premium (*Bottomless Trap Hole*, *Trap Hole*) em monstros fracos (Iscas). Só engatilhar se o monstro invocado tiver ATK >= 1500. 
    *   *Exceção 1 (Sobrevivência):* Ativa mesmo em monstros fracos caso os LPs da IA estejam críticos (<= Panic Threshold).
    *   *Exceção 2 (Negação de Tributo):* Ativa no monstro fraco caso o jogador já tenha outro monstro no campo, impedindo-o de acumular sacrifícios para invocar um Boss Monster.
    *   *Exceção 3 (Proteção de Tributo):* Ativa no monstro fraco caso a IA possua um monstro forte na mão (Nível 5+) e o monstro recém-invocado pelo jogador ameace destruir o único monstro "lacaio" da IA no campo, garantindo o sacrifício no próximo turno.

### Abdicação e Sacrifício Oculto
*   **Sobrevivência > Posse:** Se o jogador usar *Change of Heart* em um Boss da IA e atacar a própria IA de forma letal, a IA ativará *Mirror Force* destruindo o próprio monstro para não perder o duelo.
*   **Não Devolver o que foi Roubado:** Se a IA usar *Change of Heart* ou *Snatch Steal*, ela usará o monstro do jogador para atacar e, na MP2, o sacrificará como Tributo, garantindo que nunca volte ao dono.
*   **Suicide Crash (Busca):** Atacar um monstro mais forte do jogador com o *Sangan* da IA intencionalmente, tomando dano apenas para ativar o efeito de busca na Damage Step.
*   **Imperial Order / LP Cost:** Na Standby Phase, a IA recusa o pagamento de manutenção de 700 LP da sua própria *Imperial Order* caso queira utilizar Magias de sua própria mão naquele mesmo turno, ou se seu HP for menor que o Limiar de Pânico.

## 4. Lógicas Específicas de Cartas e Condições de Vitória (Win-Cons)
A IA possui rotinas de interceptação para cartas famosas:
*   **Relinquished:** Absorver sempre o monstro virado para cima com maior ATK. Ignorar monstros virados para baixo (que dariam 0 de bônus).
*   **Mystical Space Typhoon (MST):** Setar e usar preferencialmente na End Phase do jogador para destruir a carta que ele acabou de baixar, a menos que a IA tenha dano letal no próprio turno e precise "limpar a pista".
*   **Destiny Board & Exodia:** Se o deck da IA foca nessas condições, o *Panic Threshold* muda. A IA jogará monstros em defesa face-down a todo custo e guardará todas as mágicas defensivas para ganhar tempo (Stall).
*   **Deck Out (Morte por Falta de Cartas):** Se o Deck da IA tiver menos de 3 cartas, efeitos de compra (como *Pot of Greed* ou *Graceful Charity*) recebem pontuação negativa absoluta e não são ativados.

## 5. Estrutura do Loop de Decisão (Decision Tree)
O ciclo de turno da IA funcionará na seguinte ordem:
1.  `EvaluateBoardState()`: Lê LPs, contagem de cartas, buffs.
2.  `ExecuteMainPhase1()`: 
    *   Usa mágicas de busca/limpeza.
    *   Realiza Flip Summons seguros.
    *   Usa Normal Summon (como isca ou suporte).
3.  `ExecuteBattlePhase()`: 
    *   Calcula quem ataca quem (Scouting -> Clears -> Direct Hits).
4.  `ExecuteMainPhase2()`: 
    *   Realiza Tribute Summons se os lacaios atacaram e sobreviveram.
    *   Seta mágicas/armadilhas e monstros em defesa face-down.
5.  `EndTurn()`