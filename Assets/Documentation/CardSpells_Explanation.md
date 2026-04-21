# Manual Geral de Magias e Armadilhas

Antes de entrarmos nos detalhes específicos de cada carta, é vital entender as **Regras de Ouro** (Velocidade e Momento de Ativação) que regem as Mágicas e Armadilhas no YGO. O simulador obedece a essas regras rigorosamente:

### 1. Magias Normais, de Equipamento e de Campo (Spell Speed 1)
*   **Velocidade:** São as cartas mais lentas do jogo.
*   **Regra de Uso:** Só podem ser ativadas durante a sua **Main Phase 1** ou **Main Phase 2**.
*   **Restrição:** Não podem ser ativadas em resposta a outras cartas (não podem iniciar ou entrar em uma Corrente/Chain a não ser como o Link 1).

### 2. Magias Rápidas / Quick-Play Spells (Spell Speed 2)
*(Ex: Mystical Space Typhoon, A Deal with Dark Ruler)*
*   **No seu turno (da Mão):** Podem ser ativadas diretamente da sua mão em **qualquer fase** do seu turno (Draw, Standby, Battle, End Phase) e podem ser usadas em resposta a outras cartas.
*   **No turno do oponente (do Campo):** Para usar uma Magia Rápida no turno do oponente, você **deve** Setá-la (baixá-la virada para baixo) no campo durante o seu turno.
*   **⚠️ A Regra de Ouro do Set:** **Você NÃO PODE ativar uma Magia Rápida no mesmo turno em que ela foi Setada no campo.** Se você baixá-la, ela ficará "congelada" até o final do seu turno.

### 3. Cartas de Armadilha / Trap Cards (Spell Speed 2 e 3)
*(Ex: Trap Hole, Mirror Force, Magic Jammer)*
*   **Regra de Uso:** Ao contrário das Magias, Armadilhas **nunca** podem ser ativadas diretamente da mão. Elas devem obrigatoriamente ser Setadas no campo.
*   **⚠️ A Regra de Ouro do Set:** Assim como as Magias Rápidas, **Armadilhas NÃO PODEM ser ativadas no turno em que são baixadas**. Elas precisam passar por pelo menos 1 End Phase antes de ficarem "armadas" e prontas para uso.

## ID: (ID do cards.json) - Nome da carta (Password: Password do cards.json)
---

## ID: DM0004 - 7 (Password: 23771716)

A carta **"7"** é uma Carta Mágica Contínua clássica e possui um efeito dividido em duas partes muito interessantes.

> *"When there are 3 face-up "7" cards on your side of the field, draw 3 cards from your Deck. Then destroy all "7" cards. When this card is sent directly from the field to your Graveyard, increase your Life Points by 700 points."*

### Como ela funciona na prática:

1. **O Efeito de Jackpot (Comprar Cartas):** 
   Como o nome sugere, ela funciona como uma máquina caça-níqueis. Para ativar o efeito principal, você precisa conseguir baixar **três cópias da carta "7"** para que fiquem viradas para cima no seu campo ao mesmo tempo. Assim que a terceira cópia tocar o campo, você atinge o "Jackpot": você compra **3 cartas** do seu deck e, em seguida, as três cópias de "7" são destruídas automaticamente.

2. **O Efeito de Consolação (Cura):** 
   Toda vez que uma carta "7" for enviada *diretamente do campo para o Cemitério*, você ganha **700 Pontos de Vida (LP)**.

### 💡 O grande truque dessa carta:
Esses dois efeitos combaram perfeitamente! Quando você atinge o Jackpot e as três cartas "7" são destruídas pelo próprio efeito, elas estão indo do campo para o cemitério. Isso significa que, além de comprar as 3 cartas, o segundo efeito vai engatilhar três vezes, curando um total de **2100 LP**!

## ID: DM0006 - 7 Completed (Password: 86198326)

A carta **"7 Completed"** é uma Carta Mágica de Equipamento (Equip Spell) muito versátil, focada especificamente em fortalecer monstros mecânicos.

> *"Activate this card by choosing ATK or DEF; equip only to a Machine monster. It gains 700 ATK or DEF, depending on the choice."*

### Como ela funciona na prática:

1. **Alvo Restrito:** Você só pode ativar e equipar esta carta em um monstro do Tipo Máquina (Machine-Type) que esteja virado para cima no campo.
2. **O Poder da Escolha:** No momento em que você ativa a carta, você toma uma decisão tática permanente: escolher entre **Ataque (ATK)** ou **Defesa (DEF)**.
3. **O Bônus:** O monstro equipado ganha **700 pontos** no atributo que você escolheu. O outro atributo permanece inalterado durante todo o tempo em que a carta estiver equipada.

### 💡 O grande truque dessa carta:
A sua flexibilidade é o seu ponto forte. Diferente de outros equipamentos que dão um bônus fixo, com a "7 Completed" você pode se adaptar ao estado do jogo. Precisa destruir um monstro forte do oponente? Coloque os 700 no ATK de um *Jinzo* ou *X-Head Cannon*. Está encurralado e precisa ganhar tempo? Equipe em um monstro setado virado para cima e coloque os 700 na DEF, criando uma barreira de metal quase impenetrável!

## ID: DM0027 - After the Struggle (Password: 25345186)

A carta **"After the Struggle"** (Após a Luta) é uma Carta Mágica Normal (Normal Spell) que instaura uma regra de "Morte Súbita" no campo de batalha para o turno em que é ativada.

> *"This card can only be activated during Main Phase 1. All monsters on both sides of the field that have been involved in damage calculation are destroyed during the End Step of the turn."*

### Como ela funciona na prática:

1. **Ativação Restrita:** Você obrigatoriamente só pode ativá-la na sua **Main Phase 1** (antes da Fase de Batalha).
2. **A Marcação LUA (Flag Effect):** Nas sombras da Engine, a carta cria um efeito contínuo invisível que fica monitorando o campo. Toda vez que dois monstros colidem e entram na etapa de cálculo de dano (`EVENT_BATTLED`), o LUA "carimba" as duas cartas com uma flag invisível.
3. **A Resolução (Morte Certa):** Quando a Fase de Batalha termina e o jogo transita para a Main Phase 2 (ou End Phase), a Mágica cobra o preço: ela varre a mesa e destrói absolutamente todos os monstros (seus e do oponente) que receberam o carimbo de batalha.

### 💡 O grande truque dessa carta:
Essa carta é a definição de "Ataque Suicida Tático" e ignora completamente a matemática de ATK/DEF.

* **Matador de Gigantes:** Se o oponente tem um *Blue-Eyes White Dragon* intocável e você tem apenas um monstro incrivelmente fraco. Você ativa a magia e joga o seu monstro fraco (ataque kamikaze) contra o Dragão dele. Você vai tomar dano e seu monstro vai morrer? Sim. Mas, no fim da Fase de Batalha, o Dragão intocável do oponente será destruído automaticamente porque ele participou de um cálculo de dano contra você!
* **Escudo Envenenado:** Se você ativar essa carta e o oponente (no próximo turno) atacar seus monstros, qualquer monstro dele que tocar nos seus será pulverizado no fim da batalha.

## ID: DM0009 - A Deal with Dark Ruler (Password: 06850209)

A carta **"A Deal with Dark Ruler"** é uma Carta Mágica Rápida (Quick-Play Spell) com um requisito de ativação muito específico que traz ao campo uma besta de puro poder destrutivo.

> *"You can only activate this card during a turn in which a Level 8 or higher monster on your side of the field was sent to the Graveyard. Special Summon 1 "Berserk Dragon" from your hand or Deck."*

### Como ela funciona na prática:

1. **O Gatilho (O Sacrifício do Chefe):** Para poder ativar esta magia, um monstro de **Nível 8 ou maior** que você controla precisa ter sido enviado para o Cemitério neste exato turno. Isso é válido quer ele tenha sido destruído em batalha, destruído por um efeito de carta (inclusive suas próprias cartas, como um *Dark Hole*), ou até mesmo enviado ao cemitério como Tributo para um custo ou invocação!
2. **O Chamado da Besta:** Ao cumprir o requisito e ativá-la, você pode Invocar por Invocação-Especial diretamente da sua Mão ou do seu Deck um monstro específico chamado **"Berserk Dragon"** (Dragão Furioso).
3. **Velocidade Rápida (Quick-Play):** Por ser uma Magia Rápida, você pode ativá-la no turno do oponente (se ela estiver setada no campo) logo após seu monstro Nível 8 ser destruído, ou ativá-la na sua própria Fase de Batalha (Battle Phase) diretamente da mão para realizar um ataque surpresa logo após seu monstro chefe cair.

### 💡 O grande truque dessa carta:
Como o requisito é apenas que o monstro seja "enviado ao Cemitério", você pode criar combos devastadores por conta própria. Por exemplo:
*   Você ataca o oponente diretamente com o *Blue-Eyes White Dragon* (Nível 8).
*   Na sua Main Phase 2, você usa a carta *Destruction Ring* no seu próprio *Blue-Eyes*, causando 3000 de dano de efeito ao oponente.
*   Imediatamente depois, como o *Blue-Eyes* foi para o cemitério, você ativa **A Deal with Dark Ruler** da sua mão e invoca o *Berserk Dragon* (que tem 3500 de ATK e ataca todos os monstros do oponente) para garantir que não sobre nada do outro lado da mesa no próximo turno!

## ID: DM0010 - A Feather of the Phoenix (Password: 49140998)

A carta **"A Feather of the Phoenix"** é uma Carta Mágica Normal (Normal Spell) que oferece uma troca de recursos valiosa para recuperar suas melhores cartas.

> *"Discard 1 card. Select 1 card in your Graveyard and return it to the top of your Deck."*

### Como ela funciona na prática:

1. **O Custo (O Descarte):** Para ativar esta carta, você é obrigado a **descartar 1 carta da sua mão** para o Cemitério. Este é o custo de ativação, o que significa que se a magia for negada (por exemplo, por um *Magic Jammer*), a carta descartada *não* retorna para a sua mão.
2. **O Alvo (A Recuperação):** Ao ativar, você deve escolher exatamente **1 carta no seu Cemitério** como alvo. Pode ser qualquer tipo de carta: Monstro, Magia ou Armadilha.
3. **O Retorno:** Na resolução do efeito, a carta escolhida no Cemitério não vai para a sua mão; em vez disso, ela é colocada no **topo do seu Deck**.

### 💡 O grande truque dessa carta:
Apesar de parecer desvantajosa por "gastar" uma carta da mão e o seu próximo saque, a tática aqui é o planejamento perfeito e a construção de setups. 
*   Você pode descartar um monstro que prefere ter no cemitério (como uma *Sinister Serpent* ou um monstro *Light/Dark* para preparar o custo do *Black Luster Soldier*) para recuperar uma magia ou armadilha devastadora que você já usou (como *Raigeki*, *Pot of Greed* ou *Mirror Force*).
*   No turno seguinte (ou no mesmo turno, se você tiver um efeito de compra extra), você sacará a carta perfeita que acabou de recuperar. É o sacrifício de um recurso presente em prol de garantir a sua principal jogada futura!

## ID: DM0013 - A Legendary Ocean (Password: 00295517)

A carta **"A Legendary Ocean"** é uma Carta Mágica de Campo (Field Spell) que redefiniu completamente como os decks do atributo Água (WATER) eram construídos na era clássica.

> *"This card's name is treated as 'Umi'. Reduce the Level of all WATER monsters in both players' hands and on the field by 1. All WATER monsters gain 200 ATK/DEF."*

### Como ela funciona na prática:

1. **O Disfarce (Identidade como Umi):** A regra mais importante desta carta é que o nome dela é **sempre** tratado como "Umi". Isso tem duas consequências vitais: primeiro, ela serve como gatilho para qualquer carta que exija o "Umi" no campo (como *Tornado Wall* ou *The Legendary Fisherman*). Segundo, pelas regras de construção de deck, você só pode ter um máximo de 3 cópias combinadas de *Umi* e *A Legendary Ocean* no seu baralho.
2. **A Redução de Nível:** Enquanto estiver ativada, todos os monstros do atributo ÁGUA na mão e no campo de ambos os jogadores perdem 1 Estrela (Nível). 
3. **O Bônus de Status:** Como uma boa Magia de Campo clássica, ela fornece um impulso de 200 pontos de ATK e DEF contínuos para todos os monstros de ÁGUA em campo.

### 💡 O grande truque dessa carta:
O ganho de 200 de ATK é apenas a cereja do bolo; o verdadeiro poder desta carta está na **Redução de Nível** na mão do jogador!

* **Quebrando a Regra de Tributos:** Monstros de ÁGUA de Nível 5 originalmente exigem que você sacrifique 1 monstro do campo para serem invocados. Com *A Legendary Ocean* ativa, os monstros Nível 5 na sua mão se tornam **Nível 4**! Isso significa que você pode invocar aberrações como *Giga Gagagigo* ou *The Legendary Fisherman* direto da mão para o campo, sem tributo nenhum, e eles ainda entram ganhando o bônus de ataque!
* **Imunidade a Travas (Stall):** Cartas defensivas populares como *Gravity Bind* e *Level Limit - Area B* travam ou punem qualquer monstro de Nível 4 ou superior. Como *A Legendary Ocean* transforma seus monstros ÁGUA Nível 4 em **Nível 3**, seus monstros conseguem deslizar livremente por debaixo dessas defesas para atacar o oponente diretamente, enquanto os monstros dele (de outros atributos) continuam presos!

## ID: DM0015 - A Wingbeat of Giant Dragon (Password: 28596933)

A carta **"A Wingbeat of Giant Dragon"** (O Bater de Asas do Dragão Gigante) é uma Carta Mágica Normal (Normal Spell) que atua como uma remoção em massa devastadora exclusiva para decks focados em Dragões.

> *"Return 1 Level 5 or higher Dragon-Type monster you control to the hand; destroy all Spell and Trap Cards on the field."*

### Como ela funciona na prática:

1. **O Custo (O Retorno):** Para ativar esta magia, você deve obrigatoriamente devolver 1 monstro do Tipo Dragão (Dragon-Type) de **Nível 5 ou maior** que você controla do campo para a sua mão. Este recolhimento é o "Custo de Ativação", o que significa que mesmo se o oponente negar a sua magia, o seu Dragão já terá voltado para a mão.
2. **A Tempestade (A Destruição):** Após o seu Dragão recuar em segurança, o bater das asas dele cria um furacão que **destrói todas as Cartas de Magia e Armadilha** no campo (tanto as suas quanto as do oponente). Basicamente, é uma *Heavy Storm* (Tempestade Pesada) temática.

### 💡 O grande truque dessa carta:
Embora retornar um monstro forte (que provavelmente exigiu sacrifícios) para a mão pareça uma grande desvantagem de tempo (Tempo Loss), a tática aqui transforma esse custo em uma manobra de evasão perfeita!

* **Combo de Ressurreição Segura:** Imagine que você reviveu o seu *Blue-Eyes White Dragon* usando a armadilha **Call of the Haunted** ou a magia **Premature Burial**. O monstro fica preso e dependente dessa carta (se a magia for destruída, o monstro morre junto). Se você ativar *A Wingbeat of Giant Dragon*, o seu *Blue-Eyes* volta para a sua mão são e salvo. O bater de asas vai destruir o *Call of the Haunted* no campo, mas como o dragão já voltou para a mão como custo, ele escapa da destruição e você limpa a própria sujeira!
* **Limpeza Sem Medo (Bait and Clear):** Se você suspeita que o oponente encheu o campo de armadilhas letais (como *Mirror Force* ou *Torrential Tribute*), você invoca especialmente um Dragão "de graça" (usando o combo *Lord of D.* + *The Flute of Summoning Dragon*), imediatamente o devolve para a mão com esta magia e varre todas as armadilhas inimigas de uma vez só. Com o caminho 100% limpo e sem riscos, você pode invocar o Dragão novamente no mesmo turno e atacar direto nos Pontos de Vida do adversário!

## ID: DM0021 - Abyssal Designator (Password: 89801755)

A carta **"Abyssal Designator"** (Designador do Abismo) é uma Carta Mágica Normal (Normal Spell) cirúrgica, focada em arrancar peças fundamentais do deck ou da mão do oponente antes mesmo que elas sejam jogadas.

> *"Pay 1000 Life Points. Declare 1 Monster Type and 1 Attribute; your opponent must send 1 monster with the declared Type and Attribute from their hand or Deck to the Graveyard."*

### Como ela funciona na prática:

1. **O Custo de Sangue:** Para ativá-la, você deve pagar **1000 Pontos de Vida (LP)**. Este é um custo obrigatório.
2. **A Declaração Cruzada:** Ao ativar a carta, a engine abrirá janelas para você escolher exatamente um **Tipo** (ex: Spellcaster, Dragon, Machine) e um **Atributo** (ex: DARK, LIGHT, WATER). 
3. **A Execução Obrigatória:** O oponente é forçado a vasculhar a própria mão e o baralho inteiro. Se ele possuir qualquer monstro que seja *exatamente* dessa combinação, ele **deve** enviá-lo imediatamente para o Cemitério.

### 💡 O grande truque dessa carta:
Esta carta não destrói o que está no campo; o verdadeiro poder dela é a **Prevenção Tática e Sabotagem (Disruption)**, exigindo que você conheça o baralho do oponente!

* **Caça a Cartas-Chave (Snipe):** Se você sabe que o oponente baseia o jogo inteiro em um Boss Monster específico, você pode declarar a combinação exata dele. Por exemplo: se você suspeita que ele vai usar as peças de *Exodia* (que são Spellcaster / DARK), você declara essa combinação para forçá-lo a jogar uma perna do Exodia no cemitério direto do deck, arruinando a condição de vitória dele!
* **Escavando a Mão Oculta:** O oponente é obrigado a mandar a carta da Mão ou do Deck. Muitas vezes, ele perderá uma carta poderosa que estava guardando na mão para o próximo turno, gerando uma perda terrível de *Card Advantage* para ele.
* **Auditoria de Sistema:** No jogo físico, se o oponente afirmar que "não tem nenhum monstro com essa combinação no deck ou na mão", você ganha o direito de auditar/olhar o baralho inteiro dele para confirmar, o que te dá informações inestimáveis. (No nosso simulador, a engine C#/Lua faz essa verificação matemática instantânea, mas a perda do recurso inimigo é garantida se ele existir!).

## ID: DM0023 - Acid Rain (Password: 21323861)

A carta **"Acid Rain"** (Chuva Ácida) é uma Carta Mágica Normal (Normal Spell) extremamente agressiva, projetada para ser um "Board Wipe" (Limpador de Mesa) focado em aniquilar tecnologia.

> *"Destroy all face-up Machine-Type monsters on the field."*

### Como ela funciona na prática:

1. **O Alvo Específico:** Ao ativá-la, uma tempestade corrosiva se forma sobre o tabuleiro. Ela varre **todos os monstros do Tipo Máquina (Machine)** que estejam virados para cima em ambos os lados do campo.
2. **Ignora Posições Ocultas:** Apenas monstros revelados (Face-up) são destruídos. Monstros Máquina que estejam virados para baixo (Set) sobrevivem à corrosão da chuva ácida.

### 💡 O grande truque dessa carta:
Esta é a clássica carta de **Side Deck** (Baralho Auxiliar). Ela brilha contra oponentes específicos e pode virar um duelo sozinha.

* **O Pesadelo de Bandit Keith:** Durante a campanha, oponentes como Bandit Keith e outros capangas da Kaiba Corp usam Baralhos inteiramente compostos por Máquinas de alto ataque (como *Barrel Dragon* e *Slot Machine*). Usar *Acid Rain* destrói o exército inteiro do oponente de uma só vez, sem custar nenhum Ponto de Vida a você.
* **Atenção ao "Fogo Amigo":** Como a chuva atinge o campo inteiro, **seus próprios** monstros do Tipo Máquina também derreterão. É uma carta que exige precisão (ou que o seu deck seja composto por guerreiros, dragões ou magos para se aproveitar da tempestade sem sofrer as consequências).

## ID: DM0853 - Heavy Storm (Password: 19613556)

A carta **"Heavy Storm"** (Tempestade Pesada) é uma Carta Mágica Normal (Normal Spell) e é, historicamente, a "Rainha das Remoções" no formato Goat e em toda a era clássica do Yu-Gi-Oh!

> *"Destroy all Spell and Trap Cards on the field."*

### Como ela funciona na prática:
Ao ativá-la, uma tempestade varre o tabuleiro inteiro, destruindo instantaneamente **todas as Cartas Mágicas e Armadilhas** em ambos os lados do campo. Isso inclui cartas viradas para baixo (Face-down), cartas viradas para cima (Face-up), Magias de Equipamento e Magias de Campo (Field Spells).

### 💡 O grande truque dessa carta:
O *Heavy Storm* define o ritmo do duelo. A simples existência dessa carta no seu deck impõe o que chamamos de "Regra do Overextension" (Avançar demais).

* **A Punição da Ganância (Punishing Overextension):** Se o oponente baixar (Set) 3 ou 4 armadilhas na mesa para se sentir 100% seguro, um único *Heavy Storm* vai destruir todas elas de uma vez. O oponente perderá 4 cartas, e você apenas 1, criando uma vantagem de recursos (Card Advantage) absurda a seu favor. Por causa do *Heavy Storm*, duelistas de elite raramente baixam mais de 2 cartas por turno.
* **Liberando a Zona de Combate (The OTK Enabler):** A tática mais comum é guardar o *Heavy Storm* na mão até você ter monstros suficientes para vencer a partida no mesmo turno. Você ativa a tempestade, garante que não há mais *Mirror Force* ou *Torrential Tribute* para te impedir, e ataca com todos os monstros de uma vez para zerar a vida do oponente (One-Turn Kill - OTK).
* **Destruição Tática Própria:** Você também pode usar *Heavy Storm* para destruir suas próprias cartas que o estão prejudicando! Por exemplo, se você está perdendo vida por causa do seu próprio *Premature Burial* ou se os seus monstros de nível alto estão presos por conta da sua própria magia *Gravity Bind*, você pode usar a tempestade para limpar a mesa e destravar o seu próprio jogo!
