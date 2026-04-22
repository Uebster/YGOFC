# Referência de Regras Oficiais (OCG/TCG - Era 2005)

Este documento serve como o "Gabarito de Ouro" das regras oficiais do Yu-Gi-Oh! Original Card Game (OCG). Toda a arquitetura do motor C# e dos scripts LUA deste projeto deve obedecer a estas regras estritamente, simulando o ambiente competitivo da era Clássica/Goat Format.

## 📌 Instrução de Manutenção (Evolução das Eras)
Como o simulador será expandido para suportar novas mecânicas no futuro, **SEMPRE cite as mudanças de regras conforme a Era ou Master Rule**. Se uma regra mudou com o tempo, explique a transição (ex: "Na era X era assim, mas a partir da era Y passou a ser assado").

### Linha do Tempo das Master Rules (Eras)
*   **Era 1 e 2 (DM & GX) - Master Rule 1:** O formato base. Introduziu o limite de 6 cartas na mão e o baralho de Fusão (hoje Extra Deck). *(Foco Atual do Projeto - Goat Format 2005)*.
*   **Era 3 (5D's) - Master Rule 2:** Introdução dos monstros **Synchro**. O "Fusion Deck" foi renomeado para "Extra Deck". O limite do Extra Deck foi fixado em 15 cartas.
*   **Era 4 (ZEXAL) - Master Rule 3:** Introdução dos monstros **Xyz**. **Mudança Crítica:** O jogador que inicia o duelo perde o direito de sacar uma carta no Turno 1.
*   **Era 5 (ARC-V) - New Master Rule:** Introdução dos monstros **Pendulum**. **Mudança Crítica no Tabuleiro:** Duas novas **Zonas de Pêndulo** são adicionadas nas extremidades das Zonas de Magia/Armadilha. Outra mudança vital é que cada jogador passa a poder controlar sua própria Magia de Campo simultaneamente (antes, ativar uma destruía a do oponente).
*   **Era 6 (VRAINS) - Master Rule 4:** Introdução dos monstros **Link**. **Mudança Crítica no Tabuleiro:** Duas novas **Extra Monster Zones (EMZ)** são adicionadas no centro do campo, entre os dois jogadores. Monstros invocados do Extra Deck (Fusão, Synchro, Xyz, Link) só podiam ser colocados na EMZ ou em Zonas de Monstro Principais para as quais um Monstro Link apontasse.
*   **Era 7+ (SEVENS em diante) - Master Rule 2020 / MR5:** Revisão da MR4. Monstros de Fusão, Synchro e Xyz voltaram a poder ser invocados nas zonas principais (Main Monster Zones) livremente, sem precisar de Links. A restrição da MR4 agora se aplica apenas a monstros Link e Pêndulo invocados do Extra Deck.

## 1. Estrutura do Turno (Turn Structure)

Todo turno é composto pelas seguintes fases, em ordem obrigatória:

1. **Draw Phase (Fase de Compra):**
   * O Jogador do Turno (Turn Player) compra 1 carta.
   * *Diferença entre Eras (Turno 1):* Na Era Clássica (MR1 e MR2 / Goat Format), o jogador inicial **comprava** uma 6ª carta no Turno 1. A partir da Era ARC-V (Master Rule 3 - 2014 em diante), a regra mudou para equilibrar a vantagem de quem joga primeiro: o jogador que começa **NÃO** compra carta no Turno 1.
   * Após a compra, abre-se uma janela de *Fast Effect* (Efeitos Rápidos podem ser ativados).
2. **Standby Phase (Fase de Espera):**
   * Resolução de custos de manutenção e efeitos que especificam "durante a Standby Phase".
3. **Main Phase 1 (Fase Principal 1):**
   * Janela aberta para o jogador realizar Invocação Normal/Set, invocações especiais, ativar magias/armadilhas e mudar posições de batalha.
4. **Battle Phase (Fase de Batalha):**
   * *Regra:* Proibida no Turno 1 do duelo.
   * Se o jogador entrar na Battle Phase, ele é **obrigado** a passar pela Main Phase 2 antes de encerrar o turno.
5. **Main Phase 2 (Fase Principal 2):**
   * Mesmas ações da MP1. É a última chance de baixar armadilhas ou defender a mesa.
6. **End Phase (Fase Final):**
   * Resolução de efeitos que especificam "até o fim do turno".
   * Verificação de Limite de Mão (Descarte se tiver > 6 cartas).

---

## 2. A Fase de Batalha e o Damage Step

A Battle Phase não é um bloco único. Ela possui 4 passos estruturais cruciais para o disparo de eventos na Engine:

### 2.1 Start Step
O momento de transição. Cartas como *Threatening Roar* costumam ser ativadas aqui antes que qualquer ataque seja declarado.

### 2.2 Battle Step
* O Turn Player declara um atacante e um alvo válido. (Evento: `EVENT_ATTACK_ANNOUNCE`).
* **Janela de Resposta Crítica:** É o exato momento para ativar *Mirror Force*, *Magic Cylinder*, etc.
* **Regra de Replay (Re-alvo):** Se a quantidade de monstros no lado do defensor mudar após a declaração do ataque, ocorre um "Replay". O atacante pode re-escolher o alvo ou abortar o ataque.

### 2.3 Damage Step (Passo de Dano)
*Esta é a janela mais restrita do jogo.* Apenas efeitos de modificação de ATK/DEF (ex: *Rush Recklessly*), Efeitos Counter (Speed 3) ou gatilhos específicos podem ser ativados aqui. Ocorre em 5 sub-etapas:
1. **Início do Damage Step:** Cartas como *Ally of Justice Catastor* ativam.
2. **Antes do Cálculo de Dano:** Monstros Face-down são virados (Flip). Efeitos Flip engatilham, mas **só resolvem depois** do cálculo.
3. **Cálculo de Dano (`EVENT_DAMAGE_CALCULATING`):** A matemática final é aplicada. Apenas cartas que especificam "Durante o cálculo de dano" podem ser usadas (ex: *Kuriboh*).
4. **Após o Cálculo de Dano:** Efeitos de dano ocorrem. Monstros com HP 0 são marcados como "Destruídos", mas continuam fisicamente no campo. Efeitos de Flip (que viraram no passo 2) resolvem agora.
5. **Fim do Damage Step (`EVENT_BATTLED`):** Os monstros destruídos são enviados ao Cemitério (`EVENT_BATTLE_DESTROYED`). Cartas como *Mystic Tomato* ou *After the Struggle* ativam aqui.

### 2.4 End Step
Finalização da batalha. Última janela antes de entrar na Main Phase 2.

---

## 3. Spell Speeds (Velocidade das Magias) e Correntes (Chains)

O Yu-Gi-Oh! baseia a resolução de conflitos em pilhas LIFO (Último a entrar, Primeiro a sair).

* **Spell Speed 1 (Lento):** Magias Normais, de Equipamento, de Campo, Rituais e Efeitos de Ignição/Gatilho de Monstros.
  * *Regra:* Não podem "responder" a outras cartas. Só iniciam correntes (Link 1).
* **Spell Speed 2 (Rápido):** Magias Rápidas (Quick-Play), Armadilhas Normais/Contínuas e Efeitos Rápidos de Monstros.
  * *Regra:* Podem responder a Speed 1 e Speed 2.
  * *Restrição Magias Rápidas:* Podem ser ativadas da mão no turno do dono. Para usar no turno inimigo, devem ser baixadas (Setadas), mas **não podem** ser ativadas no mesmo turno em que foram baixadas.
  * *Restrição Armadilhas:* Devem ser sempre Setadas. **Não podem** ser ativadas no turno em que foram baixadas.
* **Spell Speed 3 (Contra-Ataque):** Armadilhas de Resposta (Counter Traps, ex: *Magic Jammer*).
  * *Regra:* Nada pode responder a uma Spell Speed 3, exceto outra carta de Spell Speed 3.

---

## 4. SEGOC (Simultaneous Effects Go On Chain)

O que acontece quando várias cartas de efeito obrigatório tentam ativar exatamente no mesmo milissegundo (ex: um *Dark Hole* destrói dois *Sangan* simultaneamente)?

Elas não abrem correntes separadas. Elas se empilham em uma única corrente (Chain) na seguinte ordem de prioridade:
1. Efeitos **Obrigatórios** do Jogador do Turno (Turn Player). *(Serão o Chain Link 1)*
2. Efeitos **Obrigatórios** do Oponente.
3. Efeitos **Opcionais** do Jogador do Turno.
4. Efeitos **Opcionais** do Oponente. *(Serão o Chain Link mais alto)*

Como a corrente resolve de trás para frente, o Oponente costuma ver seu efeito resolver primeiro!

---

## 5. Timing de Efeitos e "Miss the Timing"

* **Efeitos "If... you can" (Se... você pode) ou Obrigatórios:** Sempre resolvem, não importa o que aconteça. O jogo "lembra" que a condição ocorreu e inicia uma nova corrente assim que a atual terminar.
* **Efeitos "When... you can" (Quando... você pode):** O efeito corre risco de "Perder o Tempo" (Miss Timing). A condição de gatilho **deve** ter sido a última coisa a acontecer na partida. 
  * *Exemplo clássico:* Você sacrifica *Pinch Hopper* para Invocação de Tributo. A ação de ir ao cemitério não foi a última coisa que aconteceu (a invocação do novo monstro foi). O efeito do *Pinch Hopper* perde o timing e não ativa.

---

## 6. Manipulação de Deck (Buscas e Embaralhamento)

* **A Regra do Auto-Shuffle:** Sempre que um jogador vasculhar, procurar ou extrair uma carta do seu próprio Deck através de um efeito (Ex: *Reinforcement of the Army*, *Sangan*, *Abyssal Designator*), o Deck **deve obrigatoriamente ser embaralhado** imediatamente após a resolução completa do efeito. Esta é uma regra de integridade absoluta que se aplica a **todas as Eras** do jogo.

---

## 7. Match Winners (Cartas Prêmio de Campeonato)
No universo de Yu-Gi-Oh!, existem cartas únicas e lendárias entregues fisicamente como prêmios aos vencedores e finalistas dos Campeonatos Mundiais de cada época.
*   **Era DM (Clássica):** *Meteo the Matchless*, *Ulevo*, *Victory Dragon*, entre outras.
*   **Era GX:** *King of Destruction - Xexex*, *Queen of Fate - Eternia*, *Testament of the Arcane Lords*, *Chimaera, the Master of Beasts* e *Emperor of Lightning*.
*   **Era 5D's:** *Tyr, the Vanquishing Warlord*, *Lorelei, the Symphonic Arsenal*, *Aggiba, the Malevolent Sh'nn*, *Skuna, the Leonine Rakan*, *Stardust Divinity* e *Grizzly, the Red Star Beast*.
*   **Era ZEXAL:** *King Landia the Goldfang*, *Queen Nereia the Silvercrown*, *Legendary Dragon of White*, *Legendary Magician of Dark* e *Grandopolis, The Eternal Golden City*.
*   **Era ARC-V:** *The Leonardo's Silver Skyship*, *The Twin Kings, Founders of the Empire*, *Kuzmuko the Dracopense*, *Sakyo, Swordmaster of the Far East*, *Shelga, the Tri-Warlord* e *Juno, the Celestial Goddess*.
*   **Era VRAINS:** *Iron Knight of Revolution*, *Amatsu-Okami of the Divine Peaks* e *The Kaiser, the Heavenly Sky*.
*   **Efeito Singular (A Morte Absoluta):** Quase todas compartilham o mesmo princípio destrutivo - "Se esta carta ataca diretamente os Pontos de Vida do oponente e os reduz a 0, o controlador desta carta vence o Match (a Melhor de 3)".
*   **Restrição Oficial:** Tradicionalmente, todas essas cartas são **Ilegais (Banned/Forbidden)** para torneios oficiais regulares. Muitas delas são impressas com a frase real: *"This card cannot be used in a Duel"*. O motivo de seu banimento é o risco antidesportivo de forçarem o oponente a desistir (Surrender) de forma abrupta *antes* do golpe de misericórdia conectar, apenas para evitar perder a "Melhor de 3" instantaneamente.
*   **Era Moderna (Pós-VRAINS):** A Konami mudou sua filosofia de premiação. As cartas prêmio mais recentes são versões ultra-raras (ex: aço inoxidável) de cartas já existentes e jogáveis, em vez de monstros únicos com efeitos de "vencer o Match".

*   **No Simulador:** Essas cartas raras existem em nosso banco de dados de 2147 cartas e seus scripts originais (.lua) tentam acionar o fim definitivo da partida. No entanto, pelo simulador não focar estruturalmente em "Matches" (MD3) na campanha tradicional, e com a *Banlist OCG* mantendo-as seladas (0 cópias), a sua aplicação no ambiente atual serve como uma curiosidade técnica. Elas são excelentes de usar (como Tier 5.5s absolutos) nos cenários End-Game caso o Modo de Desenvolvedor `DevMode` e `DisableBanlist` sejam destravados.

### 7.1 Cartas Promocionais de Evento
Ocasionalmente, a Konami lança cartas promocionais injogáveis como piadas ou memorabílias. O exemplo mais notável da Era GX é a Magia Contínua **Get Your Game On!**, entregue aos participantes do World Championship 2007. Seu efeito dita que "Você só pode jogar esta carta se esteve presente no Campeonato de 2007", concedendo +1000 ATK aos Elemental HEROs e Neo-Spacians. Em simuladores digitais (OCGCore/EDOPro), essas cartas frequentemente carecem de scripts lógicos LUA, atuando apenas como colecionáveis na enciclopédia.

---

## Fontes Oficiais de Consulta Adicionais
* **Fast Effect Timing Chart:** Yugipedia: Fast Effect Timing
* **Detalhes do Damage Step:** Yugipedia: Damage Step