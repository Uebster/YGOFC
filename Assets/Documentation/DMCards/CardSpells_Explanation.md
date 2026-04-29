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

## ID: DM0046 - Amazoness Spellcaster (Password: 81325903)

A carta **"Amazoness Spellcaster"** (Feiticeira Amazona) é uma Carta Mágica Normal (Normal Spell) com um efeito de inversão de poder projetado para destruir Boss Monsters (Monstros Chefões).

> *"Target 1 "Amazoness" monster you control and 1 face-up monster your opponent controls; switch their original ATK until the end of this turn."*

### Como ela funciona na prática:

1. **Duplo Alvo (Targeting):** Você precisa escolher dois alvos simultâneos para a carta ativar: 1 monstro do arquétipo "Amazoness" que você controla, e 1 monstro virado para cima no campo do oponente.
2. **A Troca de Poder (Stat Swap):** Ao resolver, a magia pega o ATK Original de ambos os monstros e os inverte até o final do turno. 

### 💡 O grande truque dessa carta:
Esta é a carta definitiva de "Davi contra Golias" do arquétipo Amazoness.

* **Matando Deuses:** Se o oponente invocar um *Blue-Eyes White Dragon* (3000 ATK) e você tiver apenas uma *Amazoness Paladin* (1700 ATK) no campo. Você ativa a mágica, alveja os dois. Imediatamente, a sua Paladina sobe para 3000 de ATK e o Blue-Eyes cai para 1700. Você então ataca o Dragão com a sua guerreira, destruindo-o e causando 1300 de dano ao oponente no processo!
* **A Identidade do Arquétipo (SetCode):** Nas sombras da Engine, o LUA pergunta ao C# se a carta é `SET_AMAZONESS` (Código `0x04`). Como o nosso banco de dados não tem uma coluna de "Arquétipo", o C# usa uma heurística genial: ele verifica se o ID é `0x04` e, em caso positivo, varre o **nome** da sua carta para ver se a palavra "Amazoness" está escrita lá!

## ID: DM0053 - Amplifier (Password: 00303660)

A carta **"Amplifier"** (Amplificador) é uma Carta Mágica de Equipamento (Equip Spell) extremamente específica, criada sob medida para interagir com um dos monstros mais temidos do formato clássico: o *Jinzo*.

> *"Equip only to "Jinzo". While this card is equipped, the equipped monster's effect does not negate the effects of its controller's Trap Cards. When this card is removed from the field, destroy the equipped monster. This card's activation and effect cannot be negated."*

### Como ela funciona na prática:

1. **Alvo Exclusivo:** Você só pode ativar e equipar esta carta em um monstro virado para cima chamado **"Jinzo"**.
2. **O Hack do Sistema (Filtro de Negação):** O *Jinzo* possui um efeito contínuo brutal que desativa absolutamente todas as armadilhas no campo para ambos os jogadores. O *Amplifier* reescreve essa regra de forma assimétrica: ele injeta uma exceção no campo que faz com que o *Jinzo* pare de negar as **suas** armadilhas, enquanto as armadilhas do oponente continuam totalmente bloqueadas!
3. **A Imunidade (Velocidade e Proteção):** A ativação e o efeito desta carta **não podem ser negados**. Isso significa que o oponente não pode usar uma *Counter Trap* (como *Magic Jammer* ou *Solemn Judgment*) para impedir que você a ative e a equipe no seu *Jinzo*.
4. **O Calcanhar de Aquiles (O Preço de Morte):** Esse poder tem um risco altíssimo. Se o *Amplifier* for removido do campo de qualquer forma (destruído, devolvido para a mão ou banido), o *Jinzo* que estava equipado com ele é **destruído imediatamente**.

### 💡 O grande truque dessa carta:
Esta é a carta definitiva para estabelecer um "Monopólio de Jogo" (Lockdown Absoluto), mas que exige cuidado com o posicionamento.

* **O Monopólio das Armadilhas:** Com o *Amplifier* ativo, você joga um jogo completamente desleal. Você pode usar cartas como *Mirror Force* para limpar o campo do oponente ou *Call of the Haunted* para reviver seus monstros livremente, enquanto o oponente fica apenas assistindo, impossibilitado de ativar qualquer armadilha para se defender.
* **A Isca Perfeita (Baiting):** O oponente fará de tudo para destruir o *Amplifier* (usando *Mystical Space Typhoon* ou *Heavy Storm*), pois isso é um "dois em um" (destrói a mágica de equipamento e destrói o *Jinzo* de quebra). Sabendo disso, você pode usar o *Amplifier* como isca para forçar o oponente a gastar essas valiosas magias de remoção rápida logo no início, deixando o caminho livre para as suas outras mágicas ou simplesmente punindo-o ativando as armadilhas que o Jinzo acabou de te devolver o direito de usar!
* **Atenção à Engine LUA (Para o Simulador):** Para o nosso motor em Unity, essa carta é um desafio de programação fantástico. Ela não adiciona ATK/DEF, mas injeta uma condição no evento contínuo do Jinzo e cria um "Vínculo de Morte" (`CardLink`). A engine precisa garantir que se o equipamento for enviado ao cemitério (`EVENT_LEAVE_FIELD`), o C# empurre o *Jinzo* para o cemitério junto na mesma resolução!

## ID: DM0065 - Ancient Telescope (Password: 17092736)

A carta **"Ancient Telescope"** (Luneta Antiga) é uma Carta Mágica Normal (Normal Spell) clássica de pura espionagem e coleta de informações.

> *"See the top 5 cards of your opponent's Deck. Return the cards to the Deck in the same order."*

### Como ela funciona na prática:

1. **O Foco (A Espionagem):** Ao ativar esta carta, você ganha o direito inquestionável de olhar as 5 cartas do topo do baralho do seu oponente.
2. **A Manutenção (Sem Alteração):** Após visualizar e memorizar quais são essas cartas, elas devem ser devolvidas exatamente na mesma ordem em que estavam no topo do Deck. Você não pode reordená-las ou forçar um embaralhamento.
3. **A Experiência no Simulador:** Na nossa Engine, o comando `Duel.ConfirmDecktop` e `Duel.GetDecktopGroup` disparam a abertura de um painel de UI seguro e assíncrono. O jogo congela perfeitamente, permitindo que você leia as cartas com tranquilidade. Assim que você fecha o modal de espionagem, a corrente é liberada e o jogo segue.

### 💡 O grande truque dessa carta:
À primeira vista, gastar uma carta da sua mão apenas para "olhar" o baralho inimigo parece uma desvantagem numérica terrível (você perdeu 1 recurso e não alterou o estado do campo). No entanto, Yu-Gi-Oh! é um jogo de "conhecimento oculto" e *Card Advantage* psicológico. Saber o futuro pode ser devastador!

* **Previsão Absoluta (Read):** Saber as próximas 5 compras do oponente significa que você sabe exatamente quais ameaças ele terá em mãos nos próximos turnos. Se você espionar e vir que um *Heavy Storm* ou *Raigeki* está chegando, você saberá que **não deve** baixar todas as suas armadilhas ou invocar seus melhores monstros simultaneamente, frustrando completamente a futura limpeza de mesa dele!
* **Sinergia de Sabotagem (Mind Crush):** O conhecimento perfeito é a maior arma para as cartas de descarte. Sabendo exatamente o que o oponente vai sacar, você pode preparar uma armadilha como *Mind Crush* ou a magia *Abyssal Designator*. Assim que o oponente iniciar o turno dele e comprar a carta que você já sabe qual é, você ativa sua armadilha, declara o nome com 100% de precisão e rasga o trunfo dele antes que ele tenha a chance de sorrir!
* **Controle de Fluxo (Destruição Preditiva):** Se você olhar o topo e constatar que as próximas 5 cartas do oponente são "tijolos" ou cartas inúteis para a situação atual, você simplesmente o deixa sacar e morrer lentamente. Porém, se a luneta revelar que as peças do *Exodia* ou as ferramentas de um combo mortal estão empilhadas no topo, você pode usar imediatamente uma magia sua que force o oponente a embaralhar o próprio deck ou enviar cartas do topo pro cemitério (Efeitos de Mill como *Needle Worm*), arruinando totalmente a sorte iminente que o aguardava.

## ID: DM0070 - Ante (Password: 11324436)

A carta **"Ante"** (Aposta) é uma Carta Mágica Normal (Normal Spell) que cria um minigame de "Risco e Recompensa" intenso usando os recursos diretos das mãos dos jogadores.

> *"Each player selects 1 card from their hand, then both players reveal them. The player who revealed the card with the lower Level takes 1000 damage, and sends that card to the Graveyard. The player who revealed the card with the higher Level adds their card to their hand. If both players reveal a card with the same Level, they are both sent to the Graveyard. (Spell and Trap Cards are treated as Level 0.)"*

### Como ela funciona na prática:

1. **A Escolha:** Após ativada, ambos os jogadores devem escolher, em segredo, exatamente 1 carta de suas próprias mãos.
2. **A Revelação:** As cartas escolhidas são reveladas simultaneamente para ambos. (No nosso simulador, esse é um momento perfeito para a nossa interface "View-Only" brilhar exibindo o confronto!).
3. **A Batalha de Níveis:** O Nível das cartas reveladas é comparado. Monstros usam a quantidade de estrelas nativa deles. Cartas de Mágica e Armadilha possuem "Nível 0".
4. **A Consequência e Punição:**
   * **O Perdedor (Menor Nível):** Toma 1000 pontos de dano e a carta que apostou é mandada imediatamente para o Cemitério.
   * **O Vencedor (Maior Nível):** Pega a carta que apostou de volta para a própria mão, são e salvo.
   * **Empate:** Ninguém leva dano, mas a magia envia as DUAS cartas apostadas para o Cemitério!

### 💡 O grande truque dessa carta:
Embora o nome sugira uma "Aposta" cega, você nunca deve jogar essa carta dependendo apenas da sorte. Você deve manipulá-la para encurralar o oponente!

* **A Aposta do Trapaceiro (Nível 8+):** Se você usa um deck focado em Boss Monsters pesados (como *Blue-Eyes White Dragon*, *Dark Magician* ou monstros de Ritual), você pode apostar um deles com segurança. A chance matemática do oponente ter um Nível 8 na mão dele e decidir apostá-lo às cegas é minúscula. Você quase sempre vencerá a aposta, o que significa que o oponente **vai perder uma carta da mão dele** E **levar 1000 de dano**, enquanto você recupera seu dragão intacto!
* **Reciclagem Tática de Cemitério:** E se você *quiser* perder a aposta ou empatar de propósito? A carta que você apostou vai ser enviada ao Cemitério por um efeito de carta. Isso é ideal para "jogar fora" monstros que ativam efeitos lá do fundo (como *Sinister Serpent* que volta todo turno, ou *Night Assailant*).
* **O Carrasco do Fim de Jogo:** Quando as mãos dos jogadores estão com poucas cartas, o oponente frequentemente só terá cartas mágicas ou armadilhas (Nível 0) guardadas na mão para se defender. Se você ativar a *Ante* e revelar qualquer monstro (mesmo de Nível 1), você já garante a vitória automática, arrancando o último recurso de defesa dele e fechando o duelo com aqueles cruéis 1000 pontos de dano!

## ID: DM0089 - Archfiend's Oath (Password: 32015116)

A carta **"Archfiend's Oath"** (Juramento do Arquidemônio) é uma Carta Mágica Contínua (Continuous Spell) que transforma o seu baralho num jogo de adivinhação incrivelmente recompensador.

> *"Once per turn: You can pay 500 Life Points, then declare 1 card name; excavate the top card of your Deck, and if it is the declared card, add it to your hand. Otherwise, send it to the Graveyard."*

### Como ela funciona na prática:

1. **O Contrato (Custo):** Uma vez por turno, enquanto esta carta estiver virada para cima no seu campo, você pode ativá-la pagando **500 LP**.
2. **A Previsão:** Ao ativar, a interface de busca global se abre e você deve digitar/declarar o nome de **exatamente 1 carta** do jogo.
3. **A Revelação:** A engine do jogo escava (revela) a carta do topo do seu baralho.
4. **O Julgamento:**
   * **Acertou:** Se a carta revelada for exatamente a que você declarou, ela vai direto para a sua mão (um saque extra perfeito!). Pelas regras do OCGCore, o baralho **não é embaralhado** (Disable Shuffle).
   * **Errou:** Se você errar a adivinhação, a carta revelada é enviada imediatamente para o Cemitério.

### 💡 O grande truque dessa carta:
Embora pareça uma loteria, no Yu-Gi-Oh! de alto nível nós não dependemos da sorte. O verdadeiro poder desta carta vem das **Sinergias e Manipulações de Topo de Deck**!

* **O Combo Perfeito (O Olho Que Tudo Vê):** Se você leu sobre a carta **"Ancient Telescope"** (Luneta Antiga), você já conhece o truque. Você usa magias que permitem espiar ou organizar o topo do seu próprio deck. Ao saber com precisão qual é a próxima carta, você ativa o *Archfiend's Oath*, declara o nome que você acabou de ver e garante um saque extra imbatível por meros 500 LP! E como ela é contínua, você pode fazer isso todo turno se tiver controle do seu topo.
* **O Moinho Tático (Self-Mill Controlado):** Em decks que dependem fortemente de monstros no cemitério (como decks de *Chaos*, *Zumbis* ou peças cruciais para reviver depois), você pode usar o *Archfiend's Oath* e declarar um nome absurdo de propósito. O resultado? Você erra a adivinhação garantidamente e "manda" a carta do topo do deck pro cemitério. É uma forma fantástica e barata de acelerar (Milar) o próprio deck ativamente a cada turno, alimentando sua pilha de cemitério para jogadas futuras!

## ID: DM0107 - Array of Revealing Light (Password: 69806154)

A carta **"Array of Revealing Light"** (Matriz da Luz Reveladora) é uma Magia de Campo (Field Spell) que atua como uma ferramenta severa de controle de ritmo (*Stall*), punindo diretamente invocações.

> *"Declare 1 Type of monster. Any monster of the declared Type cannot declare an attack during the turn it is Normal Summoned, Special Summoned, or Flip Summoned."*

### Como ela funciona na prática:

1. **A Declaração:** Ao ativar esta Magia de Campo, o painel de Seleção de Raças da nossa Engine (`AnnounceSelectionUI`) se abre, pedindo para você escolher um Tipo específico (ex: *Dragon*, *Machine*, *Warrior*).
2. **O Status no Tabuleiro:** Por ser uma Magia de Campo, ela permanece ativa na mesa indefinidamente até ser destruída ou substituída por outra Magia de Campo.
3. **A Restrição (Enjoo de Invocação):** Enquanto a carta estiver ativa, qualquer monstro que for Invocado (Normal, Special ou Flip) que possua o Tipo que você escolheu **ficará proibido de atacar naquele mesmo turno**.
   * *Atenção:* O efeito dura **apenas no turno em que o monstro nasceu**. Se ele sobreviver até o próximo turno, ele estará livre para atacar normalmente!

### 💡 O grande truque dessa carta:
Diferente da *Swords of Revealing Light* (que trava tudo por 3 turnos e some), a *Array* é um escudo permanente contra "Ataques Surpresa", exigindo que você conheça bem o baralho do oponente!

* **O Matador de Combos/OTK:** Muitos decks de campeonato (como dragões ou zumbis) dependem de invocar o chefe deles e atacar no mesmo turno para zerar seus Pontos de Vida (OTK - One Turn Kill). Ao declarar a raça principal do oponente, você garante que qualquer "monstrão" que ele chame da mão ou do cemitério ficará "congelado" assistindo, dando a você um turno inteiro para destruí-lo com uma mágica antes que ele possa desferir o golpe!
* **A Integração na Engine C#:** No nosso motor de jogo, essa magia inaugura a varredura da flag `STATUS_SUMMON_TURN`. A Engine C# agora marca o turno exato de nascimento de cada carta no tabuleiro, permitindo que a IA do oponente e a interface do jogador (Espada de Ataque) saibam exatamente se estão paralisados ou prontos para a guerra!

## ID: DM0118 - Autonomous Action Unit (Password: 71453557)

A carta **"Autonomous Action Unit"** (Unidade de Ação Autônoma) é uma Carta Mágica de Equipamento (Equip Spell) extremamente agressiva, que te permite roubar recursos diretamente do cemitério inimigo.

> *"Pay 1500 Life Points, then target 1 monster in your opponent's Graveyard; Special Summon that target to your side of the field in face-up Attack Position, and equip it with this card. When this card leaves the field, destroy the equipped monster."*

### Como ela funciona na prática:

1. **O Custo de Sangue:** Para ativar esta magia, você precisa pagar um custo salgado de **1500 LP**. Se a ativação for negada (ex: *Magic Jammer*), seus 1500 LP já foram gastos e não voltam.
2. **A Invasão (Target):** Você alveja exatamente 1 monstro no Cemitério do seu oponente. Esse monstro precisa ser passível de invocação especial (ex: um *Black Luster Soldier* que não foi invocado corretamente antes não pode ser pego).
3. **O Sequestro:** O monstro é Invocado Especialmente para o **seu lado do campo**, obrigatoriamente em **Posição de Ataque**, e a *Autonomous Action Unit* se equipa a ele.
4. **O Vínculo:** Assim como *Premature Burial* ou *Call of the Haunted*, a vida do monstro fica atrelada à magia. Se a *Autonomous Action Unit* for destruída ou removida do campo, o monstro é destruído junto. *(E se o monstro morrer primeiro, a magia perde o alvo e também é destruída pelas regras básicas do jogo).*

### 💡 O grande truque dessa carta:
No formato Goat, o cemitério do oponente costuma ser uma mina de ouro cheia de monstros de efeito absurdos. Essa carta não só te dá um monstro "de graça" (sem gastar sua Normal Summon), mas também tira uma peça de reviver do oponente!

* **Roubo de Habilidades (Efeitos Flip e Passivos):** A melhor utilidade dessa carta é roubar um *Jinzo* do oponente para você bloquear as armadilhas dele, ou roubar um *Sangan* para que, quando ele morrer no seu lado do campo, **você** ative o efeito de buscar uma carta do *seu* deck para a *sua* mão!
* **O Sacrifício Imediato (Tribute Fodder):** O monstro sequestrado está livre para ser usado como Tributo para a invocação de um monstro de nível alto seu (ex: *Blue-Eyes White Dragon*). Ao tributá-lo, a *Autonomous Action Unit* vai para o cemitério sem problemas, e o monstro volta para o cemitério do oponente após já ter servido ao seu propósito.
* **⚠️ Regra de Ouro (Dono vs Controlador):** Lembre-se, mesmo que você controle o monstro do oponente, o **Dono (Owner)** sempre será ele. Se o monstro for destruído ou devolvido para a mão (ex: *Penguin Soldier*), ele voltará para o Cemitério ou Mão originais do seu oponente!

## ID: DM0121 - Axe of Despair (Password: 40619825)

A carta **"Axe of Despair"** (Machado do Desespero) é uma Carta Mágica de Equipamento (Equip Spell) que oferece um dos maiores bônus de ataque irrestritos do jogo clássico, além de uma mecânica de reciclagem.

> *"(This card is always treated as an "Archfiend" card.) The equipped monster gains 1000 ATK. When this card is sent from the field to the Graveyard: You can Tribute 1 monster; place this card on the top of your Deck."*

### Como ela funciona na prática:

1. **O Bônus de Força:** O monstro equipado ganha instantaneamente **1000 pontos de ATK**. Ela pode ser equipada a qualquer monstro no campo, seu ou do oponente.
2. **A Reciclagem Opcional:** Se esta carta for enviada do campo para o Cemitério (seja porque o monstro foi destruído, ou a própria magia foi alvejada por um *Mystical Space Typhoon*), um efeito opcional é ativado. Você pode **Tributar 1 monstro** do seu lado do campo. Se o fizer, o *Axe of Despair* não fica no cemitério; ele é colocado no **topo do seu baralho**.
3. **Membro da Família:** Por uma regra oculta escrita em seu texto, ela é eternamente tratada como uma carta do arquétipo **"Archfiend"** (Arquidemônio), o que a permite ser buscada e interagida por suportes dessa família (como o bônus de imunidade em uma zona *Pandemonium*).

### 💡 O grande truque dessa carta:
O aumento massivo de 1000 ATK transforma instantaneamente monstros medianos de Nível 4 (como *Goblin Attack Force* ou *Gemini Elf*) em ameaças com força de Boss Monsters (2900 ATK), capazes de destruir até um *Blue-Eyes White Dragon* em combate!

* **O Custo do Retorno:** Cuidado ao usar o efeito de retornar a carta para o topo do Deck! Embora recicle o Machado, isso significa que a sua **próxima compra na Draw Phase será o próprio Machado**. Isso pode "travar" a sua mão se você estiver precisando de monstros urgentes para se defender.
* **Sinergia com Fichas (Tokens):** O tributo exigido para o Machado voltar ao topo não especifica o tipo de monstro. Você pode sacrificar uma mera Ficha de Ovelha (*Scapegoat*) para garantir que o Machado volte para você no próximo turno, reciclando recursos sem valor em poder real!

## ID: DM0125 - Back to Square One (Password: 47453433)

A carta **"Back to Square One"** (De Volta à Estaca Zero) é uma Carta Mágica Normal (Normal Spell) com um dos tipos de remoção mais cruéis e estratégicos de todo o jogo.

> *"Discard 1 card, then target 1 monster on the field; place that target on the top of the Deck."*

### Como ela funciona na prática:

1. **O Custo (O Descarte):** Para ativá-la, você é obrigado a **descartar 1 carta da sua mão**. Assim como a maioria das cartas de custo, se a magia for negada (por *Magic Jammer*), a carta descartada se perde no processo.
2. **O Alvo (Spin):** Você seleciona **1 monstro no campo** (seu ou do oponente, virado para cima ou para baixo) e o devolve para o **topo do Deck** do dono original dele.

### 💡 O grande truque dessa carta:
No Yu-Gi-Oh!, a mecânica de "Spin" (Devolver para o Deck) é infinitamente superior à mecânica "Destruir" (Mandar para o Cemitério) por vários motivos cruciais:

* **Bypass Absoluto de Proteção:** Cartas indestrutíveis em batalha (*Spirit Reaper*, *Marshmallon*) ou protegidas por efeitos mágicos contra destruição, são alvos fáceis para essa magia, pois ela não "destrói", ela apenas "move".
* **O "Tijolo" no Saque (Draw Denial):** Ao colocar o monstro do oponente no topo do baralho dele, você garante matematicamente que a próxima carta que ele comprar será exatamente a mesma. Em vez de sacar uma magia ou armadilha que poderia salvá-lo, o oponente acabou de "perder" a rodada de saque, puxando uma carta que ele *já tinha* na mesa. É praticamente um roubo de turno!
* **Negando Gatilhos de Cemitério:** Monstros infames como *Sangan*, *Mystic Tomato* ou *Vampire Lord* ativam seus efeitos poderosos unicamente quando são **enviados ao Cemitério**. Ao devolvê-los para o Deck, você impede que qualquer um desses efeitos mortais dispare, limpando a ameaça de campo com 100% de segurança!
* **Custo Útil:** O custo de descartar uma carta da mão pode ser transformado numa vantagem! Você pode descartar propositalmente uma *Sinister Serpent* (que volta de graça no seu turno) ou monstros *LIGHT / DARK* para carregar o seu Cemitério rápido e invocar um *Black Luster Soldier - Envoy of the Beginning*.

---

## ID: DM0129 - Bait Doll (Password: 07165085)

A carta **"Bait Doll"** (Isca de Boneca) é uma Carta Mágica Normal (Normal Spell) única, usada como uma ferramenta de desarmamento de armadilhas e espionagem, com a vantagem de ser reciclada.

> *"Target 1 Set card in the Spell & Trap Card Zone; reveal that target, force its activation if it is a Trap Card, then negate its effect if the activation timing is incorrect, and if you do, destroy it. (If it is not a Trap Card, return it face-down.) When this card resolves, shuffle it into the Deck instead of sending it to the Graveyard."*

### Como ela funciona na prática:

1. **O Alvo (A Isca):** Você deve alvejar 1 carta que esteja virada para baixo (Set) na Zona de Mágicas e Armadilhas. A carta é então revelada para ambos os jogadores.
2. **A Forçada de Ativação:** Se a carta revelada for uma Armadilha (Trap Card), o efeito da *Bait Doll* a obriga a tentar ser ativada naquele exato momento.
3. **O Fator "Timing" (Momento Correto):** Como a *Bait Doll* é ativada na sua Main Phase, a grande maioria das armadilhas mortais do jogo (como *Mirror Force*, *Magic Cylinder* ou *Torrential Tribute*) não pode ser legalmente ativada nessa fase. Como o "Timing" está incorreto, a armadilha inimiga simplesmente falha, é negada e destruída instantaneamente!
4. **O Caso Mágico:** Se a carta revelada for uma Mágica em vez de uma Armadilha (ou se o "Timing" por acaso estiver correto), ela é apenas virada para baixo novamente na mesma posição.
5. **A Reciclagem Infinita:** Diferente de 99% das mágicas normais do jogo, a *Bait Doll* **nunca vai para o Cemitério** após o uso. Ela é embaralhada de volta no seu Deck principal.

### 💡 O grande truque dessa carta:
Esta é a melhor carta do jogo para "testar as águas" antes de realizar uma grande invocação ou um ataque decisivo.

* **O Pior Pesadelo das Battle Traps:** Armadilhas que dependem de ataques (*Mirror Force*, *Sakuretsu Armor*) são varridas do mapa de forma humilhante. Você ativa a *Bait Doll* na Main Phase 1, revela a *Mirror Force* do oponente, e como não há nenhum ataque acontecendo, ela é destruída sem cerimônias.
* **Espionagem Gratuita:** Mesmo que você acerte uma Carta Mágica (como um *Mystical Space Typhoon* setado), a carta volta a ficar virada para baixo, mas agora você **sabe** exatamente o que o oponente tem ali! Isso permite que você planeje os próximos turnos sem medo de ser surpreendido.
* **Deck Thinning vs Reciclagem:** Embora ela volte pro deck e evite que você fique sem cartas (Deck Out), cuidado para que o seu deck não fique sobrecarregado de cópias da *Bait Doll* quando você na verdade precisava sacar um monstro para se defender no late game.

---

## ID: DM0132 - Banner of Courage (Password: 10012614)

A carta **"Banner of Courage"** (Estandarte da Coragem) é uma Carta Mágica Contínua (Continuous Spell) focada em agressividade massiva, fornecendo um bônus tático para enxames de monstros.

> *"All monsters you control gain 200 ATK during your Battle Phase only."*

### Como ela funciona na prática:

1. **Aura Restrita:** Ela é uma carta contínua que afeta **todos** os monstros do seu lado do campo simultaneamente, sem distinção de raça ou atributo. O bônus é de modestos +200 Pontos de Ataque.
2. **A Janela de Oportunidade:** O grande detalhe desta mágica é que o bônus de força só é válido **durante a sua Fase de Batalha (Battle Phase)**. Assim que a Battle Phase termina, seus monstros voltam ao seu ATK original. E no turno do oponente, eles não recebem absolutamente nenhum bônus desta carta.

### 💡 O grande truque dessa carta:
O *Banner of Courage* não serve para transformar um monstro em um tanque de guerra, mas sim para "quebrar limites matemáticos" em combates e aumentar o dano letal (OTK).

* **Vantagem Cumulativa (Swarm):** Se você tiver 5 monstros no campo (mesmo os mais fracos, como Tokens ou tokens do *Scapegoat* transformados em agressivos), o estandarte aumenta o dano total que você pode causar no turno em **1000 Pontos** (+200 para cada um dos 5 monstros atacando). É uma carta feita para decks de enxame (Swarm)!
* **A Quebra de Limiar de Nível 4:** No formato clássico, o ATK "padrão de ouro" para monstros Nível 4 costuma bater em 1800 a 1900. Um monstro seu de 1800 ATK pode, durante a sua Battle Phase, chegar a 2000 ATK, conseguindo destruir ameaças inimigas de Nível 4 (como *Gemini Elf* ou *Vorse Raider*).
* **O Engodo Defensivo:** Como o bônus some no turno do oponente, seus monstros parecerão fracos na tela dele. Um oponente desavisado pode tentar atacar seu monstro na vez dele achando que está em vantagem, esquecendo que no *seu* turno o monstro dele será atropelado com os +200 de bônus!

---

## ID: DM0142 - Battery Charger (Password: 61181383)

A carta **"Battery Charger"** (Carregador de Bateria) é uma Carta Mágica Normal (Normal Spell) que atua como um *Monster Reborn* exclusivo e barateado para o arquétipo "Batteryman" (Homem-Pilha).

> *"Pay 500 Life Points. Special Summon 1 "Batteryman" monster from your Graveyard."*

### Como ela funciona na prática:

1. **A Conta de Luz (O Custo):** Para ativá-la, você precisa pagar **500 Pontos de Vida (LP)**.
2. **A Bateria (O Alvo):** Você escolhe exatamente 1 monstro no seu Cemitério que tenha "Batteryman" no nome.
3. **A Recarga (O Retorno):** O monstro escolhido é invocado por Invocação-Especial (Special Summon) para o seu lado do campo. Não há restrição de posição (Ataque ou Defesa).

### 💡 O grande truque dessa carta:
O poder desta carta está em seu custo baixíssimo quando comparado a outras opções de reviver monstros. No formato Clássico, o *Monster Reborn* é frequentemente banido ou limitado a 1 cópia, e o *Premature Burial* custa 800 LP e deixa o monstro vulnerável à destruição da própria magia. 

* **O Curto-Circuito (Swarm de Batteryman AA):** O monstro *Batteryman AA* ganha 1000 ATK para cada *Batteryman AA* em campo. Com o *Battery Charger*, você pode reviver um do cemitério de forma extremamente barata para somar forças com os que você já tem na mão, criando rapidamente monstros com 2000 ou 3000 de ATK sem gastar a sua Invocação Normal do turno!
* **Efeitos de Entrada:** Como a carta apenas diz "Special Summon", ela também serve para ativar efeitos de monstros que engatilham ao entrar no campo, mantendo a presença de tabuleiro (Board Presence) do seu exército elétrico.

---

## ID: DM0152 - Beast Fangs (Password: 46009906)

A carta **"Beast Fangs"** (Presas de Besta) é uma Carta Mágica de Equipamento (Equip Spell) primordial, do início do jogo, projetada para impulsionar feras selvagens.

> *"A Beast-Type monster equipped with this card increases its ATK and DEF by 300 points."*

### Como ela funciona na prática:

1. **O Alvo Restrito:** Você **só pode** ativar e equipar esta carta a um monstro virado para cima no campo que seja do Tipo **Besta (Beast-Type)**. Se tentar equipar em um Guerreiro ou Mago, o jogo (e a Engine) não permitirá.
2. **O Bônus Simples:** Enquanto estiver equipada, o monstro recebe um bônus constante de **+300 ATK e +300 DEF**.
3. **Regra de Vínculo:** Se o monstro perder o Tipo Besta por algum efeito (como *DNA Surgery* mudando todos para Máquina), o equipamento deixa de ter um alvo válido e é destruído instantaneamente pelas regras do jogo.

### 💡 O grande truque dessa carta:
Em um cenário competitivo avançado, +300 ATK pode parecer pouco, mas no "Beatdown" (trocação de força bruta) de formatos base (ou rascunhos de Decks Iniciais), isso vira o jogo.

* **Quebrando Empates de Nível 4:** Muitos dos melhores monstros atacantes de Nível 4 sem efeito têm 1900 de ATK. Um monstro Tipo Besta comum, se equipado com *Beast Fangs*, ultrapassa essa barreira mágica dos 1900.
* **O Sinergia com Berserk Gorilla:** O *Berserk Gorilla* é um dos monstros Besta de nível 4 mais agressivos do formato (2000 ATK). Equipando *Beast Fangs* nele, ele salta para **2300 ATK**, sendo capaz de atropelar até os monstros de Defesa mais rígidos ou bater de frente com a maioria dos monstros Nível 5 e 6 que exigem sacrifício!
* **Proteção Dupla:** Por também fornecer +300 em DEF, ajuda na sobrevivência de monstros Besta defensivos ou com efeitos de recrutamento, como o *Giant Rat* (Rato Gigante).

---

## ID: DM0156 - Beastly Mirror Ritual (Password: 81933259)

A carta **"Beastly Mirror Ritual"** (Ritual do Espelho Bestial) é uma Carta Mágica de Ritual (Ritual Spell) usada para invocar o clássico "Fiend's Mirror".

> *"This card is used to Ritual Summon "Fiend's Mirror". You must also Tribute monsters from your hand or field whose total Levels equal 6 or more."*

### Como ela funciona na prática:

1. **A Condição de Invocação:** Para ativar esta magia, você precisa ter o monstro de Ritual *"Fiend's Mirror"* na sua mão.
2. **O Tributo:** Você deve sacrificar (Tributar) monstros da sua mão ou do seu lado do campo. A soma dos Níveis desses monstros deve ser **exatamente 6 ou mais**.
3. **A Chegada do Demônio:** Após o sacrifício, o *Fiend's Mirror* é Invocado por Invocação-Ritual para o campo.

### 💡 O grande truque dessa carta:
Sendo uma magia de ritual clássica, ela não possui efeitos secundários no cemitério como as modernas, mas cumpre seu papel perfeitamente em decks nostálgicos.
* **Tributos Econômicos:** Como a exigência é nível 6 ou *mais*, você pode usar um único monstro de nível 6 (como um *Summoned Skull* na mão que estava "preso" sem tributos) para realizar a invocação inteira, otimizando seus recursos.

---

## ID: DM0169 - Big Bang Shot (Password: 61127349)

A carta **"Big Bang Shot"** (Tiro do Big Bang) é uma Carta Mágica de Equipamento (Equip Spell) ofensiva que esconde uma das mecânicas de remoção mais criativas e letais do formato clássico.

> *"The equipped monster gains 400 ATK. If the equipped monster attacks a Defense Position monster, inflict piercing battle damage to your opponent. When this card leaves the field, banish the equipped monster."*

### Como ela funciona na prática:

1. **O Bônus Duplo:** O monstro equipado recebe +400 de ATK e ganha a habilidade de causar **Dano Perfurante** (Piercing Damage) contra monstros em posição de defesa.
2. **A Maldição do Exílio:** O verdadeiro custo desta carta está no seu gatilho de saída. Se o *Big Bang Shot* sair do campo (seja destruído, devolvido para a mão ou banido), o monstro que estava equipado com ele é **Banido (Removido de Jogo)** imediatamente.

### 💡 O grande truque dessa carta:
O texto não diz que você precisa equipá-la em um monstro *seu*. E é aí que a mágica acontece no cenário competitivo!

* **O Combo de Remoção Absoluta:** Você equipa o *Big Bang Shot* em um "Boss Monster" indestrutível do **oponente**. No mesmo turno, você joga a magia *Giant Trunade* ou *Mystical Space Typhoon* mirando no SEU próprio *Big Bang Shot*. A magia sai do campo, o gatilho ativa, e o monstro do oponente é banido do jogo sem sequer ter sido alvo de um efeito de destruição! É a remoção perfeita que burla proteções como *My Body as a Shield*.
* **Cuidado com o Feitiço contra o Feiticeiro:** Lembre-se de que o dano perfurante é sempre causado *ao oponente* da carta que ataca. Se você equipar o *Big Bang Shot* no monstro do oponente, e ele atacar um monstro SEU em modo de defesa, VOCÊ tomará o dano perfurante!

---

## ID: DM0175 - Big Wave Small Wave (Password: 51562916)

A carta **"Big Wave Small Wave"** (Grande Onda, Pequena Onda) é uma Carta Mágica Normal (Normal Spell) criada para acelerar drasticamente baralhos temáticos de Água.

> *"Destroy all face-up WATER monsters you control, then you can Special Summon WATER monsters from your hand, up to the number of monsters destroyed by this effect."*

### Como ela funciona na prática:

1. **A Vazante (Destruição em Massa):** Ao ativá-la, a magia destrói **todos** os seus monstros virados para cima que possuam o Atributo ÁGUA (WATER). Monstros virados para baixo (Set) sobrevivem ilesos.
2. **A Enchente (Invocação Especial):** Imediatamente após a destruição, você pode invocar da sua mão a mesma quantidade de monstros de ÁGUA que foram destruídos.

### 💡 O grande truque dessa carta:
Esta carta quebra a regra básica de economia de sacrifícios do jogo, permitindo invocar deuses do mar com custo zero.

* **Trocando Lixo por Ouro:** Você pode ativar cartas que geram "Fichas" (Tokens) de Água fracas (como os da *Oyster Meister* ou *Lekunga*), ou usar monstros de nível baixo. Com *Big Wave Small Wave*, essas criaturas fracas são destruídas e trocadas instantaneamente por ameaças massivas da sua mão, como o *Levia-Dragon - Daedalus* ou o *Suijin*, sem gastar invocações normais ou tributos!
* **Limpando Zonas:** Como o efeito destrói suas próprias cartas, você pode abrir espaço no seu campo quando ele estiver travado com monstros inúteis de ÁGUA, ou até mesmo "desviar" de cartas de equipamento nocivas (ex: *Mask of the Accursed*) que o oponente colocou nas suas criaturas.

---

## ID: DM0183 - Black Illusion Ritual (Password: 41426869)

A carta **"Black Illusion Ritual"** (Ritual de Ilusão Negra) é a Carta Mágica de Ritual clássica de Maximillion Pegasus, usada para trazer o seu monstro mais aterrorizante à vida.

> *"This card is used to Ritual Summon "Relinquished". You must also Tribute a monster from your hand or field whose Level is 1 or more."*

### Como ela funciona na prática:

1. **A Condição de Invocação:** Você precisa ter o monstro de Ritual *"Relinquished"* na sua mão.
2. **O Tributo:** Você deve sacrificar (Tributar) monstros da sua mão ou do seu lado do campo. A soma dos Níveis deve ser **1 ou mais**.
3. **A Chegada do Pesadelo:** O *Relinquished* é Invocado por Invocação-Ritual para o campo.

### 💡 O grande truque dessa carta:
O maior trunfo deste ritual não é a magia em si, mas a acessibilidade do seu alvo.
* **Custo Quase Nulo:** Como o *Relinquished* é um monstro de Nível 1, você pode usar literalmente *qualquer* monstro do jogo como tributo para esta magia (inclusive Tokens como as Ovelhas de *Scapegoat*, se a regra específica permitir, ou os monstros mais fracos da sua mão).
* **Regra de Excesso:** Lembre-se, mesmo que a magia diga "1 ou mais", a regra oficial de Tributos Redundantes proíbe você de usar dois monstros se apenas um já cobria o Nível 1. Se você selecionar um monstro de Nível 4, o ritual será pago, e o jogo não te deixará jogar mais nada no cemitério!

---

## ID: DM0184 - Black Luster Ritual (Password: 55761792)

A carta **"Black Luster Ritual"** (Ritual do Lustro Negro) é a Carta Mágica de Ritual icônica do Yugi, usada para evocar o guerreiro supremo.

> *"This card is used to Ritual Summon "Black Luster Soldier". You must also Tribute monsters from your hand or field whose total Levels equal 8 or more."*

### Como ela funciona na prática:

1. **A Condição de Invocação:** Você precisa ter o monstro de Ritual *"Black Luster Soldier"* na sua mão.
2. **O Tributo:** Você deve sacrificar monstros da sua mão ou campo cuja soma de Níveis seja **8 ou mais**.
3. **A Chegada do Soldado:** O *Black Luster Soldier* é Invocado Especialmente para o campo.

### 💡 O grande truque dessa carta:
Nos formatos clássicos, invocar um monstro de 3000 ATK através de Ritual exige recursos altos, mas o impacto no campo é devastador.
* **Tributos Inteligentes:** Evite usar muitos monstros de baixo nível para somar 8. A melhor tática é sacrificar um único monstro de Nível 8 (como um *Blue-Eyes White Dragon* "morto" na mão) ou combinar dois monstros de Nível 4 que ativam efeitos no cemitério (ex: *Sangan*).

---

## ID: DM0187 - Black Magic Ritual (Password: 76792184)

A carta **"Black Magic Ritual"** (Ritual de Magia Negra) é a Carta Mágica de Ritual que eleva o Mago Negro à sua forma mais poderosa.

> *"This card is used to Ritual Summon "Magician of Black Chaos". You must also Tribute monsters from your hand or field whose total Levels equal 8 or more."*

### Como ela funciona na prática:

1. **A Condição de Invocação:** Você precisa ter o monstro de Ritual *"Magician of Black Chaos"* na sua mão.
2. **O Tributo:** Você deve sacrificar monstros da sua mão ou campo cuja soma de Níveis seja **8 ou mais**.
3. **A Chegada do Mestre:** O *Magician of Black Chaos* (2800 ATK) é Invocado para a batalha.

### 💡 O grande truque dessa carta:
* **Sinergia Temática:** Embora o *Magician of Black Chaos* seja um monstro de Ritual, ele se beneficia de suportes genéricos para Magos (Spellcasters).
* **Dica de Invocação:** Novamente, o segredo de Rituais de Nível 8 é a otimização da mão. O próprio *Dark Magician* (Nível 7) precisa de apenas mais um monstro de Nível 1 (como um *Kuriboh* na sua mão) para completar o ritual, tornando a invocação temática e eficiente!

---

## ID: DM0188 - Black Pendant (Password: 65169794)

A carta **"Black Pendant"** (Pingente Negro) é uma Carta Mágica de Equipamento (Equip Spell) que fornece um bônus sólido de ataque e um "bote" venenoso quando é destruída.

> *"The equipped monster gains 500 ATK. If this card is sent from the field to the Graveyard: Inflict 500 damage to your opponent."*

### Como ela funciona na prática:

1. **O Bônus:** O monstro equipado ganha +500 de ATK.
2. **O Dano de Queima (Burn):** Se a carta for enviada do campo para o Cemitério (seja porque o monstro foi destruído ou porque a própria magia foi alvo de um *Mystical Space Typhoon*), ela causa 500 pontos de dano direto aos Pontos de Vida do oponente.

### 💡 O grande truque dessa carta:
Ela pune o oponente por tentar limpar o seu campo.
* **O Finalizador (Finisher):** Se o oponente estiver com 500 ou menos Pontos de Vida, você pode equipar o *Black Pendant* em qualquer monstro (até mesmo no do oponente) e depois ativar o seu próprio *Mystical Space Typhoon* ou *Heavy Storm* para destruir o pingente. O dano será causado instantaneamente, garantindo a sua vitória sem precisar atacar!

---

## ID: DM0202 - Blessings of the Nile (Password: 30655537)

A carta **"Blessings of the Nile"** (Bênçãos do Nilo) é uma Carta Armadilha Contínua (Continuous Trap) projetada para transformar o sofrimento do oponente em vitalidade para você.

> *"Each time a card(s) is discarded from your opponent's hand to the Graveyard by a card effect, gain 1000 Life Points."*

### Como ela funciona na prática:

1. **O Gatilho:** Ela só ativa quando cartas são descartadas da mão do oponente para o Cemitério **por um efeito de carta**.
2. **A Recompensa:** Você ganha 1000 LP cada vez que esse evento ocorre. (Nota: Se várias cartas forem descartadas ao mesmo tempo pelo mesmo efeito, como em *Card Destruction*, você ganha 1000 LP uma única vez por aquela resolução).
3. **O Que Não Funciona:** Ela **não** ativa por descartes de Custo (ex: oponente descartar para ativar *Tribe-Infecting Virus*), nem por descartes do Limite de Mão na End Phase.

### 💡 O grande truque dessa carta:
No formato clássico (Goat Format), a manipulação de mão é a estratégia dominante.
* **Sinergia de Descarte:** Combina perfeitamente com cartas de controle de mão que você mesmo ativa! Se você usar *Delinquent Duo* (custa 1000 LP), o oponente descarta duas cartas, e a *Blessings of the Nile* te cura 1000 LP, anulando totalmente o seu custo de ativação! O mesmo vale para *Confiscation* e *Morphing Jar*.

---

## ID: DM0205 - Block Attack (Password: 25880422)

A carta **"Block Attack"** (Bloquear Ataque) é uma Carta Mágica Normal (Normal Spell) simples, mas com uma utilidade tática excelente para contornar monstros imensos.

> *"Target 1 face-up Attack Position monster your opponent controls; change it to face-up Defense Position."*

### Como ela funciona na prática:

1. **O Alvo:** Você escolhe 1 monstro do oponente que esteja virado para cima em Posição de Ataque.
2. **O Efeito:** O monstro é forçado a mudar para a Posição de Defesa, permanecendo virado para cima.

### 💡 O grande truque dessa carta:
No Yu-Gi-Oh!, a grande maioria dos "Boss Monsters" e agressores (Beatsticks) focam todo o seu poder no Ataque, possuindo uma Defesa miserável.
* **Derrubando Muralhas:** Um *Summoned Skull* tem terríveis 2500 de ATK, mas apenas 1200 de DEF. Um *Jinzo* tem 2400 de ATK e apenas 1500 de DEF. Se você não tem um monstro forte o suficiente para bater de frente com eles, o *Block Attack* os coloca de joelhos, permitindo que monstros mais fracos da sua mão (como *Mystic Tomato* ou *Archfiend Soldier*) passem por cima deles facilmente em batalha!

---

## ID: DM0209 - Blue Medicine (Password: 20871001)

A carta **"Blue Medicine"** (Remédio Azul) é uma Carta Mágica Normal (Normal Spell) clássica, focada em recuperação de Pontos de Vida de forma simples e direta.

> *"Increase your Life Points by 400 points."*

### Como ela funciona na prática:

1. **Ativação:** Por ser uma Magia Normal, você só pode ativá-la durante a sua Main Phase 1 ou Main Phase 2.
2. **O Efeito:** Ao ser ativada, você ganha imediatamente **400 Pontos de Vida (LP)**.

### 💡 O grande truque dessa carta:
Em um formato onde o dano pode escalar rapidamente, 400 LP pode não parecer muito, mas em duelos longos e estratégicos, cada ponto conta.
*   **Sobrevivência Mínima:** Pode ser a diferença entre sobreviver a um ataque direto com 100 LP restantes ou perder o duelo no próximo turno.
*   **Custo de Ativação:** Em decks que usam cartas com custos de LP (como *Delinquent Duo* ou *Premature Burial*), o *Blue Medicine* ajuda a mitigar esses custos, mantendo você em uma posição ligeiramente mais segura.

---

## ID: DM0224 - Book of Life (Password: 00596051)

A carta **"Book of Life"** (Livro da Vida) é uma Carta Mágica Normal (Normal Spell) com um efeito duplo poderoso, exclusivo para decks de Zumbis, que atua de forma cirúrgica tanto na ofensiva quanto na defensiva.

> *"Target 1 Zombie-Type monster in your Graveyard and 1 monster in your opponent's Graveyard; Special Summon the first target, then banish the second target."*

### Como ela funciona na prática:

1. **Alvo Duplo (Requisito Estrito):** Para ativá-la, duas condições devem ser cumpridas simultaneamente: você deve ter 1 monstro do Tipo **Zumbi (Zombie-Type)** no *seu* Cemitério E o oponente deve ter pelo menos 1 monstro (qualquer tipo) no Cemitério *dele*. Se um dos cemitérios estiver vazio para esses alvos, a carta não pode ser ativada.
2. **A Ressurreição:** Na resolução, o seu monstro Zumbi escolhido é Invocado por Invocação-Especial para o seu lado do campo.
3. **O Exílio:** Em seguida, o monstro alvo no cemitério do oponente é **Banido (Removido de Jogo)**.

### 💡 O grande truque dessa carta:
Esta carta é uma das mágicas de suporte mais mortais do jogo, pois ela gera uma oscilação de recursos (Swing) colossal em uma única jogada.

*   **Ressurreição de Custo Zero:** Diferente do *Premature Burial*, ela não custa LP e não prende o monstro revivido a uma carta frágil no campo. Você pode trazer de volta um Boss Monster como *Vampire Lord* ou *Ryu Kokki* totalmente de graça.
*   **Controle de Cemitério:** Em formatos clássicos, o cemitério é um segundo baralho. Ao banir a carta do oponente, você desmantela combos inimigos. Você pode banir um monstro *LIGHT* ou *DARK* que ele estava preparando para invocar o *Black Luster Soldier*, ou sumir com a *Sinister Serpent* dele para sempre!

---

## ID: DM0225 - Book of Moon (Password: 14087893)

A carta **"Book of Moon"** (Livro da Lua) é uma das Cartas Mágicas Rápidas (Quick-Play Spell) mais icônicas, flexíveis e táticas de toda a história de Yu-Gi-Oh!.

> *"Target 1 face-up monster on the field; change that target to face-down Defense Position."*

### Como ela funciona na prática:

1. **Alvo Universal:** Você escolhe 1 monstro que esteja virado para cima em qualquer lugar do campo (seu ou do oponente).
2. **A Ocultação:** O monstro alvo é virado instantaneamente para a **Posição de Defesa Virado para Baixo (Face-down Defense Position)**.
3. **A Velocidade:** Por ser uma Quick-Play, você pode usá-la da sua mão durante o seu turno, ou deixá-la Setada para surpreender o oponente durante o turno dele (nas fases de Compra, Principal ou de Batalha).

### 💡 O grande truque dessa carta:
Sua simplicidade a torna um "Canivete Suíço" para quase qualquer situação de jogo imaginável.

*   **Interrupção de Ataque:** O oponente declara um ataque fatal. Você ativa o *Book of Moon* no atacante. Como monstros virados para baixo não podem atacar, o ataque é sumariamente cancelado.
*   **Silenciando Ameaças:** Monstros com efeitos contínuos perigosos (como *Jinzo*, que bloqueia armadilhas) perdem seus efeitos passivos quando estão virados para baixo. Você vira o *Jinzo* dele para baixo, e suas Armadilhas voltam a funcionar no mesmo instante!
*   **Reset de Efeitos e Status:** Se o oponente usar *Snatch Steal* para roubar seu monstro, ou *Shrink* para reduzir o ATK dele, você pode usar o *Book of Moon* no seu próprio monstro. Quando ele é virado para baixo, todas as magias de equipamento acopladas a ele são destruídas, e os modificadores de status são "resetados".
*   **Reutilização de Efeitos FLIP:** Se você ativou o Efeito FLIP de uma *Magician of Faith* e ela sobreviveu à batalha, você pode usar o *Book of Moon* nela durante a Main Phase 2, deixando-a pronta para ser Flipada de novo (virada para cima) no próximo turno e resgatar mais uma Mágica!

---

## ID: DM0226 - Book of Secret Arts (Password: 91595718)

A carta **"Book of Secret Arts"** (Livro das Artes Secretas) é uma Carta Mágica de Equipamento (Equip Spell) fundamental, criada para dar um pequeno, mas útil, impulso aos monstros do tipo Mago.

> *"A Spellcaster-Type monster equipped with this card increases its ATK and DEF by 300 points."*

### Como ela funciona na prática:

1.  **Alvo:** Você só pode equipar esta carta em um monstro do Tipo **Mago (Spellcaster)** que esteja virado para cima no campo.
2.  **O Efeito:** O monstro equipado ganha um bônus permanente de **+300 pontos tanto no ATK quanto na DEF**.

### 💡 O grande truque dessa carta:
Embora o bônus pareça modesto, ele é estratégico para superar monstros de nível similar.

*   **Quebrando Limites:** Um monstro como *Skilled Dark Magician* (1900 ATK) passa a ter 2200 ATK, superando a maioria dos monstros de Nível 4 e até alguns de Nível 5.
*   **Sobrevivência:** O bônus de 300 na DEF pode ser a diferença que impede seu mago de ser destruído em batalha quando está em modo de defesa.

---

## ID: DM0227 - Book of Taiyou (Password: 38699854)

A carta **"Book of Taiyou"** (Livro do Sol) é uma Carta Mágica Normal (Normal Spell) e a contraparte direta do famoso *Book of Moon*. Em vez de ocultar, ela revela.

> *"Flip 1 face-down monster on the field into face-up Attack Position."*

### Como ela funciona na prática:

1.  **Alvo:** Você escolhe 1 monstro que esteja virado para baixo no campo (seu ou do oponente).
2.  **O Efeito:** O monstro alvo é imediatamente virado para a **Posição de Ataque Virado para Cima (Face-up Attack Position)**.

### 💡 O grande truque dessa carta:
Sua principal função é acelerar suas próprias jogadas ou expor as fraquezas do oponente.

*   **Ativação de Efeitos FLIP:** É a maneira mais rápida de ativar o efeito de um monstro FLIP seu (como *Magician of Faith* ou *Man-Eater Bug*) durante a sua Main Phase, sem precisar esperar que o oponente o ataque.
*   **Expondo Armadilhas:** Se o oponente baixou um monstro suspeito, você pode usar o *Book of Taiyou* para forçá-lo a virar para cima. Se for um monstro fraco, você pode destruí-lo facilmente em batalha. Se for um monstro com um efeito FLIP perigoso, você o ativa no seu próprio turno, sob seu controle, em vez de ser pego de surpresa no turno dele.

---

## ID: DM0234 - Brain Control (Password: 87910978)

A carta **"Brain Control"** (Controle Cerebral) é uma das Cartas Mágicas Normais (Normal Spell) mais poderosas e icônicas do formato clássico, permitindo que você "roube" temporariamente um monstro do oponente.

> *"Pay 800 LP, then target 1 face-up monster your opponent controls that can be Normal Summoned/Set; take control of that target until the End Phase."*

### Como ela funciona na prática:

1.  **Custo e Alvo:** Você paga **800 Pontos de Vida** e escolhe 1 monstro virado para cima que o oponente controla.
2.  **A Restrição:** O monstro alvo precisa ser um que possa ser invocado normalmente (Normal Summoned/Set). Isso significa que você **não pode** roubar a maioria dos monstros de Fusão, Ritual ou monstros com efeitos que dizem "Cannot be Normal Summoned/Set".
3.  **O Roubo:** Você ganha o controle do monstro até o final do seu turno.

### 💡 O grande truque dessa carta:
A versatilidade do *Brain Control* é o que a torna lendária.

*   **Remoção e Ataque:** A utilidade mais óbvia é roubar o monstro mais forte do oponente e usá-lo para atacar diretamente ou para destruir outro monstro dele.
*   **Tributo Perfeito:** A jogada mais devastadora é roubar um monstro do oponente e, na mesma Main Phase, **Tributá-lo** para a sua própria Invocação-Tributo (como para um *Monarch* ou *Jinzo*). O monstro do oponente é removido do campo permanentemente, e você ainda coloca uma ameaça sua na mesa!

---

## ID: DM0238 - Breath of Light (Password: 20101223)

A carta **"Breath of Light"** (Sopro de Luz) é uma Carta Mágica Normal (Normal Spell) de remoção em massa, altamente específica e devastadora contra um determinado Tipo de monstro.

> *"Destroy all face-up Rock-Type monsters on the field."*

### Como ela funciona na prática:

1.  **Ativação:** Ao ser ativada, ela destrói todos os monstros do Tipo **Rocha (Rock)** que estiverem virados para cima no campo, em ambos os lados.
2.  **Efeito Colateral:** Assim como outras cartas de remoção em massa, ela também destruirá os seus próprios monstros do Tipo Rocha, caso você tenha algum.

### 💡 O grande truque dessa carta:
Esta é uma carta de "Side Deck" por excelência. Ela é inútil contra a maioria dos baralhos, mas contra um deck focado em Rocha (como os que usam *Giant Rat* para buscar *Gigantes* ou que travam o jogo com *Guardian Sphinx*), ela pode vencer o duelo sozinha ao limpar completamente o campo do oponente.

---

## ID: DM0244 - Burning Land (Password: 24294108)

A carta **"Burning Land"** (Terra em Chamas) é uma Carta Mágica Contínua (Continuous Spell) que serve tanto para remover Magias de Campo quanto para infligir dano constante a ambos os jogadores.

> *"When this card is activated: If there are any Field Spell Cards on the field, destroy them. During each player's Standby Phase: The turn player takes 500 damage."*

### Como ela funciona na prática:

1.  **Anti-Campo:** No momento em que é ativada, seu primeiro efeito verifica se há alguma Magia de Campo na mesa (sua ou do oponente) e a destrói.
2.  **Dano Contínuo (Burn):** Enquanto permanecer em campo, durante a Standby Phase de **cada jogador**, o jogador do turno atual perde **500 Pontos de Vida**.

### 💡 O grande truque dessa carta:
É uma ótima ferramenta para estratégias de "Burn" (queima de LP) ou para neutralizar decks que dependem de suas Magias de Campo.

*   **Quebra de Estratégia:** Ativá-la destrói imediatamente cartas como *Necrovalley* ou *A Legendary Ocean*, que são o coração de muitos decks.
*   **Relógio da Morte:** O dano de 500 LP por turno para ambos os jogadores coloca um "relógio" no duelo, forçando ambos a agirem mais rápido antes que seus Pontos de Vida se esgotem. Em um deck de Burn, onde você já causa dano com outras cartas, *Burning Land* acelera a sua condição de vitória.

---

## ID: DM0245 - Burning Spear (Password: 18937875)

A carta **"Burning Spear"** (Lança em Chamas) é uma Carta Mágica de Equipamento (Equip Spell) clássica, focada em fornecer poder de fogo bruto com uma pequena desvantagem temática.

> *"A FIRE monster equipped with this card increases its ATK by 400 points and decreases its DEF by 200 points."*

### Como ela funciona na prática:

1.  **Alvo Específico:** Só pode ser equipada em um monstro cujo Atributo seja **FOGO (FIRE)**.
2.  **Troca Equivalente:** O monstro equipado ganha instantaneamente +400 de ATK, mas sacrifica -200 de sua DEF.

### 💡 O grande truque dessa carta:
Nos primórdios do jogo, bônus de 400 pontos costumavam ser a diferença entre um monstro sobreviver ou dominar a mesa.
*   **Ofensiva Focada:** Monstros medianos de FOGO atingem valores altos o suficiente para passar por cima de ameaças comuns sem precisar de sacrifícios. A penalidade na defesa raramente é um problema se o seu baralho dita o ritmo de ataque da partida!

---

## ID: DM0247 - Burst Stream of Destruction (Password: 17655904)

A carta **"Burst Stream of Destruction"** (Raio Explosivo de Destruição) é a Carta Mágica Normal (Normal Spell) que materializa o icônico ataque assinatura do lendário *Blue-Eyes White Dragon*.

> *"If you control "Blue-Eyes White Dragon": Destroy all monsters your opponent controls. "Blue-Eyes White Dragon" you control cannot attack the turn you activate this card."*

### Como ela funciona na prática:

1.  **Condição Mítica:** Para ativá-la, você precisa ter um *Blue-Eyes White Dragon* legítimo virado para cima no seu lado do campo.
2.  **O Raio Absoluto:** Se a condição for cumprida, ela varre o campo adversário como um verdadeiro *Raigeki*, aniquilando absolutamente todos os monstros inimigos.
3.  **A Fadiga (Restrição):** O preço por esse poder maciço é que nenhum *Blue-Eyes White Dragon* sob o seu controle poderá declarar ataques no turno em que esta mágica foi ativada.

### 💡 O grande truque dessa carta:
Embora o seu Dragão exausto não possa atacar, isso está longe de significar que o seu turno acabou!
*   **Ataque Paralelo:** A restrição de ataque se aplica *apenas* à carta com o nome exato "Blue-Eyes White Dragon". Todos os outros monstros guerreiros, magos ou dragões do seu exército têm o caminho completamente livre para atacar os Pontos de Vida desprotegidos do adversário!
*   **Evasão da Restrição:** Após limpar a mesa, você pode usar o Dragão fadigado no campo como material para Invocação, sacrificando-o (Tributo) ou usando uma *Polymerization* para mesclá-lo no temível *Blue-Eyes Ultimate Dragon*. Esse novo monstro-chefe não carrega a restrição mágica e poderá atacar brutalmente na mesma rodada!

---

## ID: DM0249 - Buster Rancher (Password: 84740193)

A carta **"Buster Rancher"** (Tratador de Bestas) é uma Carta Mágica de Equipamento (Equip Spell) desenhada taticamente para transformar criaturas inofensivas em "Matadoras de Gigantes".

> *"Only a monster with an ATK of 1000 points or less can be equipped with this card. During damage calculation, increase the ATK of the monster equipped with this card by 2500 points if the opponent's monster that battles it is in Attack Position and its ATK is 2500 or more, OR if the opponent's monster that battles it is in Defense Position and its DEF is 2500 or more."*

### Como ela funciona na prática:

1.  **O Fraquinho:** A mágica só obedece monstros muito fracos, exigindo que o alvo de equipamento tenha um ATK nativo de **1000 ou menos**.
2.  **A Batalha do Chefe:** O equipamento só desperta se o seu monstro for se chocar contra um "Chefão" oponente que possua **2500 ou mais** no atributo de combate pertinente (se estiver em pé: 2500+ de ATK. Se estiver deitado: 2500+ de DEF).
3.  **O Golpe Matador:** Exclusivamente durante a Etapa de Cálculo de Dano (onde armadilhas raramente funcionam), o seu monstrinho canaliza energia e recebe incríveis **+2500 Pontos de Ataque** para vencer a trocação de espadas!

### 💡 O grande truque dessa carta:
É o clássico conto de "Davi contra Golias" em forma de cartão.
*   **Punição ao Metagame:** Oponentes que dependem unicamente de jogar Deuses e Dragões massivos entram em pânico. Um simples *Kuriboh* (300 ATK) equipado com esta carta chega repentinamente a assustadores 2800 de ATK ao colidir de frente com o *Dark Magician* do inimigo, estraçalhando-o!
*   **Bloqueio Psicológico:** Deixar um monstrinho Setado com esse equipamento força o oponente a gastar uma remoção cara de Mágica (*Mystical Space Typhoon*) em uma criatura que ele considerava inútil.

---

## ID: DM0250 - Butterfly Dagger - Elma (Password: 69243953)

A carta **"Butterfly Dagger - Elma"** (Adaga Borboleta - Elma) é uma Carta Mágica de Equipamento (Equip Spell) lendária, marcada na história como a semente de um dos combos (loops) mais perigosos que obrigaram a Konami a bani-la eternamente do cenário oficial de TCG.

> *"The equipped monster gains 300 ATK. When this card is destroyed and sent to the Graveyard while equipped: You can return this card to the hand."*

### Como ela funciona na prática:

1.  **O Afiar das Lâminas:** Fornece módicos +300 Pontos de ATK ao monstro empunhando a adaga.
2.  **O Retorno Imortal:** Se esta carta estiver legitimamente equipada em um monstro no campo, e sofrer Destruição (seja porque um oponente a explodiu com *Heavy Storm* ou porque o monstro dela morreu e a levou junto), a adaga ativa no Cemitério: ela recusa a morte e **volta imediatamente para a sua mão**.

### 💡 O grande truque dessa carta (O Motivo de Seu Banimento):
O retorno à mão desta carta não possui nenhuma limitação de "Apenas uma vez por turno" (Once Per Turn).

*   **O "Gearfried Loop" Infinito:** O monstro clássico *Gearfried the Iron Knight* tem um Efeito Contínuo: qualquer carta de equipamento atrelada a ele é instantaneamente destruída por ele mesmo! O Combo: Você equipa a Adaga Elma no *Gearfried*. Ele quebra a adaga. A adaga percebe que foi "destruída enquanto equipada", aciona o efeito e volta para a sua mão. Você equipa ela nele de novo. Ele quebra de novo. Você gera um **loop inquebrável e infinito** no seu próprio turno.
*   **A Condição de Vitória (Exodia/Burn):** Se você tiver no campo uma *Royal Magical Library* (que acumula contadores para comprar cartas extras cada vez que uma Mágica é ativada), você pode repetir o loop até comprar o seu Deck inteiro na 1ª Rodada e sacar as 5 peças do *Exodia*. Se você tiver no campo *Morale Boost*, pode ganhar LP infinito; Se tiver uma *Fire Princess*, pode disparar o loop para queimar e reduzir os pontos do oponente de 8000 a 0 usando as faíscas infinitas!