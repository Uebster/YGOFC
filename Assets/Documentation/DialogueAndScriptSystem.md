# Sistema de Diálogos e Roteiro (Script System)

## Visão Geral
Este documento descreve o funcionamento do sistema de narrativa visual (Visual Novel style) que ocorre antes e depois dos duelos na Campanha. O objetivo é criar imersão e contar a história da jornada do duelista.

## Elementos da Cena
A cena de diálogo é composta por:
1.  **Background (Fundo):** Imagem estática que define o local (ex: Academia, Praça, Arena). Pode mudar dinamicamente durante a conversa para indicar movimento ou passagem de tempo.
2.  **Avatar A (Esquerda):** Personagem de suporte/coadjuvante.
    *   *Função:* Mentor (Kairo), narrador, ou alguém incentivando o jogador.
3.  **Avatar B (Direita):** O Oponente do nível atual.
    *   *Função:* Desafiar, provocar ou reagir ao duelo.
4.  **Caixa de Texto:** Exibe o nome do falante e o texto do diálogo.

## Sistema de Expressões (Avatares)
Os personagens possuem sprites fixos com variações faciais para transmitir emoção. O script deve indicar qual emoção usar em cada fala.

### Emoções Suportadas (Tags)
1.  **[Neutro/Confiança]:** Estado padrão, explicação, seriedade.
2.  **[Alegria]:** Vitória, elogio, risada.
3.  **[Raiva]:** Provocação, frustração, dano.
4.  **[Tristeza]:** Derrota, preocupação, decepção.
5.  **[Medo]:** Surpresa, intimidação, choque.
6.  **[Deboche]:** Sarcasmo, arrogância, desprezo.

## Fluxo do Jogo (Game Loop)

1.  **Seleção de Nível:** Jogador escolhe o próximo oponente no Mapa.
2.  **Roteiro Pré-Duelo (Pre-Duel):**
    *   Carrega Background inicial.
    *   Exibe diálogo entre Avatar A e Avatar B.
    *   Define o contexto da batalha.
3.  **Duelo:** A batalha de cartas acontece.
4.  **Roteiro Pós-Duelo (Post-Duel):**
    *   **Se Vitória:** Avatar B reage à derrota, Avatar A parabeniza e avança a história.
    *   **Se Derrota:** Avatar B zomba, Avatar A dá dicas ou encoraja a tentar novamente.
5.  **Tela de Conclusão (Level Complete):**
    *   Exibe Rank e Drops.
    *   **Botão [Continue]:** Avança imediatamente para o próximo roteiro/oponente (Fluxo contínuo).
    *   **Botão [Back/Camp]:** Retorna ao Mapa/Acampamento para salvar o progresso, editar o deck ou farmar cartas no *Free Duel* contra o oponente recém-desbloqueado.

---

# Roteiro da Campanha

## Personagens Recorrentes
*   **Protagonista:** (Você) - O novo duelista.
*   **Kairo:** O Mentor. Um duelista experiente que guia o protagonista no início da jornada.

---

## Ato 1: O Início (Academia de Duelos)
**Contexto:** O protagonista acaba de entrar na academia e Kairo está lhe mostrando o básico.

### 001: Novice Duelist (O Portão da Academia)
*   **Local:** Entrada da Academia (Dia).
*   **Avatar A:** Kairo.
*   **Avatar B:** Novice Duelist.

#### Pré-Duelo
**(Background: Entrada da Academia - Dia)**
*   **Kairo [Confiança]:** Finalmente você chegou. Estava te observando desde que cruzou os portões. Eu me chamo Kairo, e serei seus olhos e ouvidos nesta jornada.
*   **Kairo [Alegria]:** Você carrega um deck... sinto uma energia familiar nele. Aqui na Academia de Duelos, não ensinamos apenas regras; buscamos o domínio sobre o "Forbidden Chaos". Mas, para entrar, você precisa de mais do que potencial. Precisa de vitórias.
*   **Novice Duelist [Deboche]:** Ei, Kairo! Já está tentando recrutar mais um perdedor para as suas teorias de "energia de cartas"?
*   **Novice Duelist [Confiança]:** Escuta aqui, calouro. Eu sou o teste de nivelamento. Se você não conseguir passar nem por mim, é melhor dar meia-volta e ir jogar baralho com o seu avô. O que me diz? Está pronto para ser humilhado no seu primeiro dia?
*   **Kairo [Raiva]:** Ele é sempre assim, ignorante quanto ao verdadeiro peso das cartas.
*   **Kairo [Confiança]:** Mostre a ele, [Nome do Jogador]. Aceite o desafio e use sua estratégia para silenciar essa arrogância. O duelo é o único idioma que esses valentões entendem!

#### Pós-Duelo (Vitória)
*   **Novice Duelist [Medo]:** M-mas o quê?! Eu perdi para um novato? Meu deck de Guerreiros era infalível! Isso... isso deve ter sido sorte!
*   **Kairo [Deboche]:** Sorte é o nome que os derrotados dão à estratégia dos vencedores. Saia da frente, o caminho dele agora é outro.

---

### 002: Student (O Corredor dos Veteranos)
*   **Local:** Corredor Principal (Interior).
*   **Avatar A:** Kairo.
*   **Avatar B:** Student.

#### Pré-Duelo
**(Background: Corredor Principal - Interior)**
*   **Kairo [Confiança]:** Nada mal para o começo. Mas não se empolgue. Aquele garoto no portão era apenas o nível mais baixo. À medida que entramos na escola, os oponentes começam a entender as correntes de efeito e os combos básicos.
*   **Student [Confiança]:** Eu vi o que você fez lá fora. Foi impressionante, mas eu não sou o "Novice". Eu estudo as probabilidades de cada invocação.
*   **Student [Deboche]:** Dizem que você tem algo especial nas mãos... Eu gostaria de analisar isso de perto. Se você me vencer, eu te mostro onde ficam as salas de duelo avançadas. Caso contrário, confiscarei sua carta mais rara como "taxa de estudo".
*   **Kairo [Confiança]:** Um duelista calculista. Eles são perigosos porque raramente cometem erros técnicos.
*   **Kairo [Alegria]:** É a oportunidade perfeita para testar se você consegue manter a pressão. Não deixe que ele dite o ritmo do jogo!
*   **Student [Confiança]:** Prepare-se! Meu deck está embaralhado e minha estratégia está traçada!

#### Pós-Duelo (Vitória)
*   **Student [Tristeza]:** Meus cálculos estavam corretos... mas você introduziu uma variável que eu não previ. Seu estilo de jogo é... caótico, mas preciso.
*   **Kairo [Confiança]:** O "Caos" não é falta de ordem, é uma ordem que os olhos comuns não conseguem enxergar. Você está progredindo rápido. Siga-me, a próxima oponente é uma das melhores duelistas femininas deste bloco.

---

### 003: Female Student (O Jardim do Conhecimento)
*   **Local:** Jardim da Academia (Pôr do Sol).
*   **Avatar A:** Kairo.
*   **Avatar B:** Female Student.

#### Pré-Duelo
**(Background: Jardim da Academia - Pôr do Sol)**
*   **Kairo [Alegria]:** Sente esse ar? Aqui a energia é diferente. Muitos duelistas vêm para cá para meditar sobre suas cartas. Mas não se engane com a tranquilidade...
*   **Female Student [Deboche]:** Kairo, sempre poético. Mas você esqueceu de mencionar que este jardim é o meu território de treino.
*   **Female Student [Confiança]:** Ouvi dizer que um novo duelista venceu os rapazes do portão principal. Deixe-me adivinhar... você é o tipo que confia apenas na força bruta dos seus monstros, não é? Na minha estratégia, a sutileza vence o poder.
*   **Kairo [Confiança]:** Ela utiliza efeitos de cartas mágicas e armadilhas para controlar o campo. É um estilo de jogo que exige que você pense dois passos à frente.
*   **Female Student [Deboche]:** Exatamente. Vamos ver se o seu deck tem a elegância necessária para me enfrentar. Se você perder, terá que reorganizar minhas cartas da biblioteca por uma semana!
*   **Kairo [Alegria]:** Não dê esse prazer a ela. Mostre que o "Forbidden Chaos" também possui sua própria harmonia!

#### Pós-Duelo (Vitória)
*   **Female Student [Tristeza]:** Minhas armadilhas... você as desarmou como se pudesse ler meu pensamento. Que tipo de cartas você carrega nesse deck?
*   **Kairo [Confiança]:** Não são as cartas, é o elo entre o duelista e elas. Você jogou bem, mas ele tem algo que a lógica pura não explica.

---

### 004: Intermediate Duelist (O Salão dos Desafiantes)
*   **Local:** Sala de Duelos Intermediária (Neon Azul).
*   **Avatar A:** Kairo.
*   **Avatar B:** Intermediate Duelist.

#### Pré-Duelo
**(Background: Sala de Duelos Intermediária)**
*   **Kairo [Neutro]:** Aqui a brincadeira acaba. Este setor é reservado para quem já provou que conhece o básico. Os decks aqui são mais rápidos e os combos, mais letais.
*   **Intermediate Duelist [Confiança]:** Então você é o "fenômeno" que todos estão comentando? Francamente, não vejo nada de especial. Você parece apenas mais um novato com sorte.
*   **Intermediate Duelist [Deboche]:** Eu passei meses refinando meu deck de fusões e equipamentos. Eu não jogo por diversão, eu jogo para subir no ranking da Kaiba Corp. Você está pronto para um duelo de verdade ou vai correr para o colo do Kairo?
*   **Kairo [Raiva]:** A arrogância dele é o seu maior ponto fraco, mas as cartas dele não mentem. Ele usa cartas de equipamento para tornar monstros fracos em gigantes.
*   **Kairo [Confiança]:** Mantenha o foco. Se ele fortalecer um monstro, encontre uma forma de tirá-lo de campo imediatamente. O destino deste duelo está nas suas mãos.
*   **Intermediate Duelist [Confiança]:** Chega de conversa! Vou te mostrar a diferença entre um amador e um duelista de elite! DUELO!

#### Pós-Duelo (Vitória)
*   **Intermediate Duelist [Raiva]:** Isso não pode estar certo! Meu deck de elite... esmagado por um calouro?! Eu exijo uma revanche!
*   **Kairo [Confiança]:** A derrota é a melhor lição que você pode receber hoje.
*   **Kairo [Alegria]:** Você está atraindo muita atenção. O próximo nível da Academia já sabe que você está vindo. E acredite... os especialistas não serão tão gentis.

---

### 005: Expert Duelist (O Terraço dos Especialistas)
*   **Local:** Terraço da Academia (Noite).
*   **Avatar A:** Kairo.
*   **Avatar B:** Expert Duelist.

#### Pré-Duelo
**(Background: Terraço da Academia - Noite)**
*   **Kairo [Neutro]:** Olhe ao seu redor. Poucos chegam até este terraço. Aqueles que duelam aqui não buscam apenas notas ou status... eles buscam a perfeição técnica.
*   **Expert Duelist [Confiança]:** Kairo. Vejo que trouxe o novo "prodígio". Sabe, eu já vi muitos como ele passarem por aqui. Todos acham que têm um "elo" com as cartas, até encontrarem um deck que não permite erros.
*   **Expert Duelist [Deboche]:** Meu deck é uma máquina de precisão. Eu não dependo do "coração das cartas", eu dependo da probabilidade exata. Se você não terminar este duelo em cinco turnos, sua derrota será matematicamente inevitável.
*   **Kairo [Confiança]:** Não se deixe intimidar pela frieza dele. Especialistas jogam com o tempo. Eles tentam te cansar até você cometer um erro bobo.
*   **Kairo [Raiva]:** Mostre a ele que o Caos não segue cálculos matemáticos! Quebre a lógica dele com a sua vontade!
*   **Expert Duelist [Confiança]:** A análise começou. Vamos ver se o seu deck sobrevive ao meu sistema de jogo!

#### Pós-Duelo (Vitória)
*   **Expert Duelist [Medo]:** Não... o cálculo falhou? A probabilidade de você sacar aquela carta era de menos de 2%! Como você pôde...?
*   **Kairo [Alegria]:** Como eu disse: o Caos é imprevisível. Você confiou demais nos números e esqueceu de olhar para o oponente.

---

### 006: Mokuba Kaiba (A Visita da Kaiba Corp)
*   **Local:** Heliponto da Academia / Área VIP.
*   **Avatar A:** Kairo.
*   **Avatar B:** Mokuba Kaiba.

#### Pré-Duelo
**(Background: Heliponto da Academia)**
*   **Kairo [Medo]:** (Sussurrando) Tome cuidado. Aquele ali é Mokuba Kaiba. Se ele está aqui, significa que o próprio Seto Kaiba está de olho no que está acontecendo nesta Academia.
*   **Mokuba [Confiança]:** Ora, ora! Então é você o novato que está causando todo esse barulho? Meu irmão me contou que alguém estava vencendo todos os especialistas da escola de uma vez só.
*   **Mokuba [Deboche]:** Eu vim ver de perto se você é realmente tudo isso ou se esses duelistas daqui é que ficaram moles. Eu tenho acesso às cartas mais raras da Kaiba Corp, sabia? Vencer você vai ser mais fácil do que ganhar um videogame novo!
*   **Kairo [Neutro]:** Mokuba pode parecer apenas uma criança, mas ele tem recursos que nenhum outro aluno tem. O deck dele é imprevisível e cheio de truques da corporação.
*   **Mokuba [Confiança]:** Se você me vencer, talvez eu deixe você entrar no banco de dados da Kaiba Corp. Mas se eu vencer... você vai ter que admitir que meu irmão é o maior duelista de todos os tempos! Preparado?
*   **Kairo [Confiança]:** É a sua chance de provar que o "Forbidden Chaos" é superior até mesmo à tecnologia de ponta. Vá em frente!
*   **Mokuba [Alegria]:** É hora do duelo! Não chora depois, hein!

#### Pós-Duelo (Vitória)
*   **Mokuba [Raiva]:** Ei! Isso não vale! Você usou algum truque estranho! Eu vou contar tudo pro Seto... ele não vai gostar nada de saber que um novato me venceu!
*   **Kairo [Confiança]:** (Sorrindo) Diga a ele o que quiser, Mokuba. Os fatos estão no campo. Você acabou de derrotar um Kaiba.
*   **Kairo [Neutro]:** Prepare-se... depois disso, a dificuldade vai escalar para os veteranos lendários. Tristan e Tea não vão deixar isso passar em branco.

---

### 007: Tristan Taylor (O Desafio do Veterano)
*   **Local:** Pátio dos Fundos (Entardecer).
*   **Avatar A:** Kairo.
*   **Avatar B:** Tristan Taylor.

#### Pré-Duelo
**(Background: Pátio dos Fundos)**
*   **Kairo [Neutro]:** A notícia correu rápido. Derrotar um Kaiba colocou um alvo nas suas costas.
*   **Tristan [Raiva]:** Ei! Você aí! Então você é o cara que fez o Mokuba sair correndo chorando? O garoto pode ser mimado, mas ele faz parte da nossa turma.
*   **Tristan [Confiança]:** Eu não sou um "especialista" em cálculos como aqueles caras do terraço, mas eu tenho algo que eles não têm: garra! Meu deck de Guerreiros não recua diante de nenhum "Caos". Se você quer continuar na Academia, vai ter que passar pela minha defesa primeiro!
*   **Kairo [Confiança]:** (Para o Protagonista) Este é o seu momento. Tristan joga com o coração e com monstros de alto ataque. Eu vou ficar por aqui apenas observando... desta vez, a resposta ao desafio deve ser inteiramente sua.
*   **Tristan [Deboche]:** Pode vir com tudo! Vou te mostrar como um verdadeiro veterano duela!

#### Pós-Duelo (Vitória)
*   **Tristan [Tristeza]:** Caramba... você é bom mesmo. Minha defesa não aguentou.
*   **Kairo [Alegria]:** Viu? Força bruta não é tudo. O equilíbrio entre ataque e estratégia é o que define um campeão.

---

### 008: Téa Gardner (A Amizade e o Espírito)
*   **Local:** Praça da Amizade.
*   **Avatar A:** Tristan Taylor (Mudança de Aliado).
*   **Avatar B:** Téa Gardner.

#### Pré-Duelo
**(Background: Praça da Amizade)**
*   **Tristan [Alegria]:** Cara, eu tenho que admitir... você tem estilo! Aquele seu último combo foi bruto. Deixa eu te apresentar a Téa. Ela é o coração do nosso grupo.
*   **Téa [Confiança]:** Então esse é o duelista que o Kairo está treinando? Prazer em te conhecer! O Tristan me contou sobre o duelo de vocês.
*   **Téa [Neutro]:** Mas sabe... duelo não é só sobre vencer ou perder. É sobre o que você sente pelas suas cartas. Eu uso um deck de Fadas e Magos que foca no suporte mútuo. Quero ver se o seu "Caos" tem lugar para a amizade ou se é apenas destruição!
*   **Tristan [Deboche]:** Cuidado, hein! Ela parece gentil, mas o deck dela recupera pontos de vida mais rápido do que você consegue tirar!
*   **Téa [Alegria]:** Pronto para testar seu espírito? Vamos lá!

#### Pós-Duelo (Vitória)
*   **Téa [Alegria]:** Uau! Você joga com tanto coração! Foi um duelo lindo, mesmo eu perdendo.
*   **Tristan [Confiança]:** Ele é bom, né? Acho que ele está pronto para conhecer o Vovô.

---

### 009: Grandpa Muto (A Sabedoria do Mestre)
*   **Local:** Loja de Cartas do Vovô.
*   **Avatar A:** Téa Gardner.
*   **Avatar B:** Grandpa Muto.

#### Pré-Duelo
**(Background: Loja de Cartas)**
*   **Téa [Alegria]:** Você passou no meu teste! Consigo ver que você respeita o seu deck. Por isso, decidi te trazer ao melhor lugar da cidade: a loja do Vovô do Yugi.
*   **Grandpa [Confiança]:** Ho ho ho! Então este é o jovem de quem todos estão falando? Kairo me mandou uma mensagem dizendo que você carrega uma energia muito parecida com as relíquias que eu coleciono.
*   **Grandpa [Neutro]:** O "Forbidden Chaos" é um poder antigo, meu jovem. Antes de você enfrentar o desafio final deste Ato, eu preciso ter certeza de que você é digno de carregar esse fardo. Meu deck é cheio de segredos e cartas raras que guardo há anos. Mostre-me o seu valor!
*   **Téa [Confiança]:** O Vovô é uma lenda! Preste atenção em cada jogada dele, é uma aula de Yu-Gi-Oh!
*   **Grandpa [Alegria]:** Vamos ver se a nova geração está pronta para o que vem por aí!

#### Pós-Duelo (Vitória)
*   **Grandpa [Alegria]:** Ho ho! Excelente! Você tem o toque de um mestre. Suas cartas confiam em você.
*   **Téa [Alegria]:** Sabia que ele conseguiria! Kairo escolheu bem.

---

### 010: Duke Devlin (O Mestre dos Dados - Chefe do Ato 1)
*   **Local:** Arena Principal da Academia (Noite).
*   **Avatar A:** Kairo (Retorno e Despedida).
*   **Avatar B:** Duke Devlin.

#### Pré-Duelo
**(Background: Arena Principal - Noite)**
*   **Kairo [Neutro]:** Chegamos ao fim da sua primeira etapa. Duke Devlin é o atual campeão deste setor. Ele não usa apenas cartas; ele usa a sorte e o risco a seu favor.
*   **Duke Devlin [Deboche]:** Bem-vindo ao show principal! Eu vi seus duelos contra os amadores lá fora, mas aqui na minha arena, as regras são outras.
*   **Duke Devlin [Confiança]:** Meu estilo é visual, é arriscado, é... perfeito! Seus monstros de "Caos" não significam nada se os meus dados não permitirem que eles ataquem. Prepare-se para ser a estrela do meu próximo vídeo de derrota!
*   **Kairo [Confiança]:** [Nome do Jogador], este é o momento em que nossos caminhos se dividem. Eu te ensinei a ouvir as cartas. Agora, você deve aprender a guiar o seu próprio destino.
*   **Kairo [Alegria]:** Vença o Duke, e o portão para o Reino dos Duelistas se abrirá para você. Não olhe para trás. Confie no seu deck. Adeus por enquanto... nos veremos novamente quando o Caos despertar de verdade.
*   *(Kairo sai da tela. O Avatar A fica vazio por um segundo, simbolizando a sua independência.)*
*   **Duke Devlin [Deboche]:** Parece que seu mentor te deixou sozinho. Que pena! Vai ser mais divertido te esmagar assim. VAMOS NESSA!

#### Pós-Duelo (Fim do Ato 1)
*   **Duke Devlin [Tristeza]:** Eu não acredito... Meus dados nunca falham assim! Você... você forçou a sorte ao seu favor? Que tipo de duelista é você?
*   **[Sistema]:** O LOGO "ACT 2: DUELIST KINGDOM" APARECE NO CENTRO.
*   **Kairo (Voz em Off):** "O primeiro selo foi quebrado. A jornada para a Ilha de Pegasus começou. Boa sorte, Duelista do Caos."

---

## Ato 2: Reino dos Duelistas
**Contexto:** O protagonista viaja para a ilha de Pegasus para participar do torneio.

### 011: Rex Raptor (O Desembarque na Ilha)
*   **Local:** Costa da Ilha (Dia).
*   **Avatar A:** Joey Wheeler.
*   **Avatar B:** Rex Raptor.

#### Pré-Duelo
**(Background: Costa da Ilha - Dia)**
*   **Joey [Alegria]:** Ei, amigão! Conseguimos chegar na ilha! Mas olha só a recepção que temos... parece que os dinossauros ainda não foram extintos por aqui.
*   **Rex Raptor [Deboche]:** Ora, ora... se não é o Joey e o seu novo "guarda-costas". Vocês acham que o Reino dos Duelistas é um piquenique?
*   **Rex Raptor [Confiança]:** Este território é meu! Meus dinossauros estão famintos e o seu deck de "Caos" parece um ótimo lanche. Se vocês querem sair dessa praia vivos, vão ter que passar por cima do meu exército pré-histórico!
*   **Joey [Raiva]:** Escuta aqui, cabeça de lagarto! O meu amigo aqui acabou de chegar e já derrotou o Duke Devlin. Você não tem chance!
*   **Joey [Confiança]:** (Para o Protagonista) Vai lá! Mostra pra ele que o tamanho do monstro não importa se a estratégia for maior. Detona esses dinossauros!
*   **Rex Raptor [Confiança]:** Preparem-se para serem pisoteados! É HORA DO DUELO!

#### Pós-Duelo (Vitória)
*   **Rex Raptor [Raiva]:** Meus dinossauros... extintos de novo?! Isso não vai ficar assim!
*   **Joey [Deboche]:** Volta pra era do gelo, Rex! A gente tem um torneio pra vencer.

---

### 012: Weevil Underwood (Armadilha na Floresta)
*   **Local:** Floresta Densa.
*   **Avatar A:** Joey Wheeler.
*   **Avatar B:** Weevil Underwood.

#### Pré-Duelo
**(Background: Floresta Densa)**
*   **Joey [Medo]:** Que lugar bizarro... estou sentindo algo rastejando pela minha bota. Eu odeio insetos!
*   **Weevil [Deboche]:** Hi-hi-hi-hi! Vocês caíram direitinho na minha rede! Bem-vindos ao meu jardim, onde os insetos dominam e os duelistas tolos servem de alimento.
*   **Weevil [Confiança]:** Eu vi o seu duelo na praia. Muito barulhento... muita força bruta. Mas aqui na floresta, meus insetos atacam das sombras. Quando você perceber, seus pontos de vida já estarão em zero! Hi-hi-hi!
*   **Joey [Raiva]:** Esse nanico de óculos é um trapaceiro! Ele usa efeitos de veneno e cartas que travam o campo.
*   **Joey [Confiança]:** Não deixa ele te cercar! Esmaga esses bichos antes que eles se multipliquem!
*   **Weevil [Deboche]:** Vejam o poder da minha Grande Mariposa! O seu caos vai virar casulo! DUELO!

#### Pós-Duelo (Vitória)
*   **Weevil [Tristeza]:** Minha rainha... meus insetos... esmagados! Como você escapou da minha teia?
*   **Joey [Alegria]:** Nada como um bom inseticida logo de manhã! Vamos sair dessa floresta antes que apareça mais algum bicho.

---

### 013: Mako Tsunami (O Terror dos Sete Mares)
*   **Local:** Penhasco à Beira-Mar.
*   **Avatar A:** Mai Valentine (Mudança de Aliado).
*   **Avatar B:** Mako Tsunami.

#### Pré-Duelo
**(Background: Penhasco à Beira-Mar)**
*   **Mai [Confiança]:** Joey e seus amigos... sempre metidos em confusão. Deixem que eu assumo daqui. Esse próximo duelista tem um código de honra que vocês, garotos, mal entendem.
*   **Mako [Confiança]:** Uma duelista que respeita o mar? Raro de se ver. Mas eu luto pelo sustento da minha tripulação e pela memória do meu pai. O oceano é um mestre impiedoso, e meu deck reflete a sua fúria!
*   **Mako [Neutro]:** As águas estão agitadas hoje. Elas pressagiam uma batalha épica. Mostre-me se o seu espírito é tão profundo quanto o abismo marítimo ou se você vai apenas afogar-se na minha maré!
*   **Mai [Deboche]:** Ele é dramático, mas o deck de Água dele é sólido. Ele usa a neblina para esconder seus monstros.
*   **Mai [Confiança]:** Mostre a ele que o seu "Caos" pode vaporizar até o oceano mais profundo. Estou assistindo!
*   **Mako [Alegria]:** Pelo mar e pela glória! DUELO!

#### Pós-Duelo (Vitória)
*   **Mako [Tristeza]:** Você duela com a força de um tsunami... Minhas criaturas marinhas foram superadas. Foi um duelo honrado.
*   **Mai [Alegria]:** Nada mal, novato. Você está começando a ficar interessante. Mas guarde o fôlego... ouvi dizer que o Joey está em apuros em algum lugar da ilha. Vamos, temos que encontrá-lo!

---

### 014: Joey Wheeler (A Prova de Amizade)
*   **Local:** Campo de Treino da Ilha (Dia).
*   **Avatar A:** Mai Valentine.
*   **Avatar B:** Joey Wheeler.

#### Pré-Duelo
**(Background: Campo de Treino)**
*   **Mai [Neutro]:** Olha só quem está ali. O Joey. Parece que ele está treinando sozinho, mas algo está errado... ele está agindo de forma estranha, como se estivesse sob algum tipo de pressão.
*   **Joey [Raiva]:** (Falando para si mesmo) Eu preciso vencer! O Seto Kaiba não vai ficar rindo da minha cara se eu conseguir as estrelas suficientes para o torneio final!
*   **Joey [Confiança]:** Ei! Você! Eu não sei quem te colocou no meu caminho, mas eu preciso dessas estrelas para avançar. Não é nada pessoal, amigão, mas eu vou ter que te derrotar aqui e agora!
*   **Mai [Deboche]:** (Para o Protagonista) Ele está perdendo a cabeça com essa obsessão por estrelas. Mostre a ele que o verdadeiro valor de um duelista não está em números, mas na qualidade do jogo. Eu vou ficar aqui apenas como espectadora desta vez.
*   **Joey [Confiança]:** Meu "Red-Eyes" está faminto por um desafio. Vamos ver se o seu deck de "Caos" aguenta o calor! DUELO!

#### Pós-Duelo (Vitória)
*   **Joey [Alegria]:** (Rindo) Cara, que duelo! Eu quase esqueci como é bom enfrentar alguém que me obriga a dar o meu máximo. Você venceu, e minhas estrelas são suas. Mas ó... no torneio, eu não vou facilitar da próxima vez!
*   **Mai [Confiança]:** Ele é um bobão, mas tem um coração de ouro. Agora que resolvemos isso, vamos seguir. O próximo desafio é comigo, não é? Vamos testar se você é realmente digno da minha atenção.

---

### 015: Mai Valentine (O Desafio da Dama)
*   **Local:** Jardim de Rosas da Ilha (Noite).
*   **Avatar A:** Joey Wheeler (Recuperado).
*   **Avatar B:** Mai Valentine.

#### Pré-Duelo
**(Background: Jardim de Rosas - Noite)**
*   **Joey [Confiança]:** Pronto! Agora é a vez da Mai. Ela é durona, não se deixe levar pelo charme dela, ou você vai ver as suas cartas desaparecerem num piscar de olhos!
*   **Mai [Confiança]:** Joey, você fala demais.
*   **Mai [Deboche]:** Estava ansiosa por este momento. Você derrotou o Joey, o que é um feito e tanto. Mas agora você vai enfrentar a verdadeira estratégia. O meu deck de "Harpies" é como um vento cortante: você nem verá de onde veio o ataque antes de ser derrotado.
*   **Joey [Medo]:** Ela não está brincando! As cartas de suporte dela tornam as "Harpies" uma unidade imparável.
*   **Joey [Confiança]:** (Para o Protagonista) Você consegue! Use a fraqueza dela contra ela mesma!
*   **Mai [Confiança]:** Prepare-se para voar para fora deste torneio! É HORA DO DUELO!

#### Pós-Duelo (Vitória)
*   **Mai [Alegria]:** Impressionante... você realmente sabe como ler as intenções do oponente. Eu subestimei o seu deck de "Caos". Você provou que merece seguir em frente.
*   **Joey [Alegria]:** Isso aí! Ninguém segura o nosso time! Agora estamos a um passo de enfrentar os figurões mais perigosos desta ilha. O Bandit Keith já está nos observando de longe...

---

### 016: Bandit Keith (A Emboscada das Máquinas)
*   **Local:** Entrada das Cavernas (Crepúsculo).
*   **Avatar A:** Joey Wheeler.
*   **Avatar B:** Bandit Keith.

#### Pré-Duelo
**(Background: Entrada das Cavernas)**
*   **Joey [Raiva]:** Eu reconheceria esse cheiro de óleo de motor em qualquer lugar... Keith! Apareça e lute como um duelista de verdade, em vez de se esconder nas sombras!
*   **Bandit Keith [Deboche]:** Ora, se não é o pequeno Joey e o seu novo "cachorrinho" de estimação. Eu não luto por honra, garoto. Eu luto por dinheiro e poder. E as suas estrelas valem uma fortuna no mercado negro desta ilha.
*   **Bandit Keith [Confiança]:** Meu deck de Máquinas é blindado. Enquanto você tenta entender o que está acontecendo, meus canhões já vão estar apontados para os seus pontos de vida. "Forbidden Chaos"? Para mim, isso parece apenas mais sucata para o meu ferro-velho!
*   **Joey [Medo]:** Cuidado! Esse cara não joga limpo. Ele esconde cartas na manga e usa monstros que não podem ser destruídos facilmente por batalha!
*   **Joey [Raiva]:** (Para o Protagonista) Não dê chances para ele armar os combos. Detone essas máquinas antes que elas se tornem uma fortaleza!
*   **Bandit Keith [Confiança]:** Prepare-se para ser esmagado pelas engrenagens do destino! DUELO!

#### Pós-Duelo (Vitória)
*   **Bandit Keith [Raiva]:** Trapaça! Você deve ter trapaceado! Ninguém vence minhas máquinas!
*   **Joey [Deboche]:** Aceita, Keith. Na América ou aqui, você perdeu. E sem trapaças, só talento.

---

### 017: Espa Roba (A Farsa dos Poderes Psíquicos)
*   **Local:** Acampamento Abandonado.
*   **Avatar A:** Mai Valentine.
*   **Avatar B:** Espa Roba.

#### Pré-Duelo
**(Background: Acampamento Abandonado)**
*   **Mai [Deboche]:** (Olhando para os lados) Dizem que há um duelista por aqui que consegue ler a mente dos oponentes. Ele afirma ter poderes extrasensoriais... mas eu só vejo um amador tentando assustar os outros.
*   **Espa Roba [Confiança]:** Eu posso sentir os seus pensamentos, "Dama das Harpias". E posso sentir o medo que emana do seu deck de "Caos", novato. Não adianta esconder suas cartas mágicas... minhas ondas cerebrais já as detectaram!
*   **Espa Roba [Deboche]:** Meus irmãos estão espalhados pela ilha, transmitindo cada movimento seu para a minha mente. Você está jogando com as cartas viradas para cima para mim! Meu Jinzo vai silenciar todas as suas armadilhas antes mesmo de você pensar em ativá-las!
*   **Mai [Raiva]:** É um truque! Ele deve estar usando binóculos ou algum sistema de comunicação. Não se deixe levar pela pressão psicológica dele.
*   **Mai [Confiança]:** (Para o Protagonista) Confie no seu instinto. Se ele diz que sabe o que você tem, mude a sua estratégia no último segundo e quebre a "visão" dele!
*   **Espa Roba [Confiança]:** A transmissão começou! Vamos ver se o seu cérebro aguenta o choque! DUELO!

#### Pós-Duelo (Vitória)
*   **Espa Roba [Medo]:** Minhas... minhas leituras falharam?! A energia desse "Caos" é tão instável que eu não consegui prever nada! Meus irmãos... me desculpem... eu perdi.
*   **Mai [Alegria]:** Viu só? Nada de poderes psíquicos, apenas um bom e velho blefe quebrado pela força bruta.
*   **Mai [Neutro]:** Mas fique alerta. A temperatura da ilha está caindo... e o próximo duelista que nos aguarda não usa truques de rádio. Ele usa algo muito mais sombrio. Bakura está à nossa espera.

---

### 018: Bakura Ryou (O Grito de Socorro)
*   **Local:** Cemitério da Ilha (Nevoeiro).
*   **Avatar A:** Joey Wheeler.
*   **Avatar B:** Bakura Ryou.

#### Pré-Duelo
**(Background: Cemitério da Ilha)**
*   **Joey [Medo]:** Bakura! É você? Cara, o que você está fazendo aqui sozinho no meio desse cemitério? Você parece pálido... mais do que o normal!
*   **Bakura [Tristeza]:** Joey... [Nome do Jogador]... por favor, saiam daqui. Eu sinto que algo está para acontecer. O meu Anel do Milénio... ele está a vibrar de uma forma terrível.
*   **Bakura [Medo]:** Ele quer duelar. Ele quer as vossas almas! Eu não consigo controlar... as minhas mãos estão a mexer-se sozinhas! Por favor, derrotem-me rápido antes que a sombra assuma o controlo total!
*   **Joey [Raiva]:** Aguenta firme, Bakura!
*   **Joey [Confiança]:** (Para o Protagonista) Ele está a ser possuído por aquele objeto estranho! Temos que vencer o duelo para quebrar o transe dele. O deck dele usa espíritos e monstros de ocultismo. Não tenhas pena, luta com tudo para o salvar!
*   **Bakura [Tristeza]:** Sinto muito... aqui vou eu! DUELO!

#### Pós-Duelo (Vitória)
*   **Bakura [Alívio]:** Obrigado... a voz parou. Eu me sinto mais leve. Desculpem por causar problemas.
*   **Joey [Confiança]:** Descansa, amigo. Nós cuidamos do resto. O Anel do Milênio não vai mais te controlar hoje.

---

### 019: Yami Bakura (O Despertar do Espírito do Anel)
*   **Local:** Reino das Sombras (Vórtice Roxo).
*   **Avatar A:** Mai Valentine.
*   **Avatar B:** Yami Bakura.

#### Pré-Duelo
**(Background: Reino das Sombras)**
*   **Mai [Medo]:** O que é isto?! O Bakura mudou completamente... o olhar dele, a voz... não é mais aquele rapaz gentil. A temperatura baixou tanto que consigo ver a minha própria respiração!
*   **Yami Bakura [Deboche]:** (Risada sinistra) O hospedeiro foi dormir. Agora, vocês estão a lidar com o verdadeiro senhor do Anel do Milénio. Bem-vindos ao meu Jogo das Trevas!
*   **Yami Bakura [Confiança]:** [Nome do Jogador], o seu "Forbidden Chaos" é uma energia curiosa... mas não é nada comparado ao vazio das sombras. O meu deck de "Destiny Board" vai escrever o seu fim, letra por letra. Se não me derrotar a tempo, a sua alma ficará presa neste tabuleiro para sempre!
*   **Mai [Raiva]:** Eu não acredito em fantasmas, mas sinto uma pressão enorme no peito!
*   **Mai [Neutro]:** (Para o Protagonista) Ele está a tentar vencer por contagem de turnos ou efeitos de deck. Tens que ser agressivo! Quebra o tabuleiro dele antes que seja tarde demais!
*   **Yami Bakura [Deboche]:** As trevas estão famintas... Vamos ver quem será o primeiro a ser consumido! DUELO!

#### Pós-Duelo (Vitória)
*   **Yami Bakura [Raiva]:** Maldição! O poder do Caos interferiu com as sombras... Mas não penses que isto acabou. Eu voltarei quando menos esperares.
*   *(Yami Bakura desaparece. Bakura Ryou volta, desmaiado no chão.)*
*   **Joey [Alegria]:** Conseguiste! O Bakura está a respirar normal agora. Mas olha lá para cima...
*   *(Destaque de Background: O Castelo de Pegasus surge no horizonte, iluminado por relâmpagos.)*
*   **Joey [Neutro]:** O portão está aberto. Todas as estrelas que acumulaste até agora levaram-nos aqui. O Pegasus está à nossa espera para o banquete final. Estás pronto para o duelo mais difícil da tua vida?

---

### 020: Maximillion Pegasus (O Criador e o Olho que Tudo Vê - Chefe do Ato 2)
*   **Local:** Salão Real do Castelo.
*   **Avatar A:** Joey Wheeler & Mai Valentine.
*   **Avatar B:** Maximillion Pegasus.

#### Pré-Duelo
**(Background: Salão Real do Castelo)**
*   **Joey [Raiva]:** Finalmente te encontramos, Pegasus! Chega de joguinhos e de sequestrar almas! O meu amigo aqui veio pegar o que é dele e acabar com esse seu torneio maluco!
*   **Pegasus [Alegria]:** Welcome! Sejam muito bem-vindos ao meu santuário. Eu estava observando seus duelos através das câmeras... e através de algo muito mais eficiente.
*   *(Pegasus levanta o cabelo, revelando o Olho do Milênio brilhando)*
*   **Pegasus [Confiança]:** [Nome do Jogador]-boy... você trouxe uma energia fascinante para a minha ilha. O "Forbidden Chaos" é como uma mancha de tinta em um quadro perfeito. Mas você deve entender uma coisa: eu criei este jogo. Eu conheço cada carta, cada efeito... e agora, eu conheço cada pensamento seu.
*   **Mai [Medo]:** Cuidado! Dizem que ele consegue ver as cartas na sua mão antes mesmo de você pensar em jogá-las! É como duelar contra um espelho!
*   **Pegasus [Deboche]:** Exatamente, Mlle. Valentine. Por que você não se junta aos seus amigos e assiste ao nascimento de uma nova obra de arte? O meu Mundo Toon vai transformar seus monstros de caos em meras caricaturas!
*   **Joey [Confiança]:** Não escuta ele, parceiro! O coração das cartas é mais forte do que qualquer olho mágico! Acredita no seu deck e acaba com a graça desse palhaço!
*   **Pegasus [Confiança]:** It's showtime! Vamos ver se o seu Caos consegue sobreviver à minha imaginação! DUELO!

#### Pós-Duelo (Fim do Ato 2)
*   **Pegasus [Tristeza]:** Incredible... Eu não consegui ver... a escuridão do seu deck era tão profunda que meu Olho do Milênio só via o vazio. Você... você derrotou o criador no seu próprio domínio.
*   **Joey [Alegria]:** ISSO! Conseguimos! O campeão da ilha é você! Pegasus, agora cumpra sua palavra e liberte todos!
*   **Mai [Confiança]:** Você foi incrível. Mas olhe para o céu... algo está mudando. A vitória aqui é só o começo de algo maior.
*   **[Sistema]:** O LOGO "ACT 3: BATTLE CITY" APARECE.
*   **Kairo (Voz em Off):** "A ilha foi apenas o teste de fogo. Agora, o mundo todo se tornará um campo de batalha. Prepare suas cartas, pois em Battle City, os Deuses estão à espreita."

---

## Ato 3: Batalha na Cidade
**Contexto:** O torneio mudou para as ruas de Domino City. Cuidado com os Rare Hunters.

### 021: Rare Hunter (Ameaça nas Sombras)
*   **Local:** Beco Escuro da Cidade (Noite).
*   **Avatar A:** Yugi Muto.
*   **Avatar B:** Rare Hunter.

#### Pré-Duelo
**(Background: Beco Escuro - Noite)**
*   **Yugi [Confiança]:** Fico feliz que tenha aceitado o convite para o torneio de Battle City. Mas tome cuidado. Há duelistas aqui que não jogam pelas regras da Kaiba Corp. Eles buscam apenas cartas raras... a qualquer custo.
*   **Rare Hunter [Deboche]:** (Rosto coberto) Hehe... falando em cartas raras... eu sinto o cheiro de uma relíquia vindo desse seu deck de "Caos". Meu mestre, Marik, ficaria muito satisfeito em tê-la em nossa coleção.
*   **Rare Hunter [Confiança]:** Você pode ter vencido no Reino dos Duelistas, mas aqui nas ruas, a sobrevivência é para poucos. Meu deck não precisa de monstros gigantes para te esmagar... eu só preciso das cinco peças certas. Se eu completar o Exodia, sua alma e seu deck serão meus!
*   **Yugi [Séria]:** Um Caçador de Raros! Eles usam táticas de compra acelerada para invocar o Exodia. Você precisa ser rápido e agressivo antes que ele junte todas as partes!
*   **Yugi [Confiança]:** (Para o Protagonista) Não deixe o medo do Exodia te travar. O "Forbidden Chaos" é imprevisível demais para ser contido por correntes. Ataque com tudo!
*   **Rare Hunter [Deboche]:** As peças estão se movendo... o fim está próximo para você! DUELO!

#### Pós-Duelo (Vitória)
*   **Rare Hunter [Medo]:** O mestre Marik não vai gostar disso... Exodia falhou... Minhas peças...
*   **Yugi [Séria]:** Diga ao seu mestre que estamos indo atrás dele. Battle City não pertence aos Rare Hunters.

---

### 022: Rare Hunter Elite (O Caçador de Elite)
*   **Local:** Praça Central da Cidade (Dia).
*   **Avatar A:** Yugi Muto.
*   **Avatar B:** Rare Hunter Elite.

#### Pré-Duelo
**(Background: Praça Central - Dia)**
*   **Yugi [Raiva]:** Eles não param! Derrotar um deles só atraiu a atenção de alguém mais perigoso. Veja aquele homem ali... a aura dele é muito mais sombria do que a do anterior.
*   **Rare Hunter Elite [Confiança]:** Aquele fracassado não era digno de representar os Rare Hunters. Eu, por outro lado, entendo a verdadeira natureza do poder. Nós não queremos apenas suas cartas raras... queremos testar o limite desse seu "Caos" proibido.
*   **Rare Hunter Elite [Deboche]:** O meu deck é projetado para anular estratégias baseadas em efeitos especiais. Você se sente seguro com suas cartas mágicas? Veremos quanto tempo elas duram sob o meu controle. Se você perder, entregue seu localizador de Battle City e desapareça desta cidade!
*   **Yugi [Séria]:** Ele parece mais experiente. Ele vai tentar travar suas jogadas e te forçar a cometer erros por frustração.
*   **Yugi [Confiança]:** (Para o Protagonista) Confie no elo que você construiu com o seu deck. O "Caos" não pode ser controlado por quem não o entende. Vamos mostrar a ele o que um verdadeiro duelista pode fazer!
*   **Rare Hunter Elite [Confiança]:** Prepare-se para o seu julgamento. DUELO!

#### Pós-Duelo (Vitória)
*   **Rare Hunter Elite [Medo]:** Como?! Meus bloqueios foram despedaçados! Marik não vai acreditar nisso... você é uma anomalia neste torneio!
*   **Yugi [Alegria]:** Bom trabalho! Você está limpando as ruas desses mercenários. Mas sinto que o Marik está usando alguém como marionete para se aproximar de nós.
*   **Yugi [Séria]:** Um duelista silencioso chamado Strings foi visto perto do porto. Dizem que ele carrega uma carta de poder divino... Tenha muito cuidado.

---

### 023: Strings (O Boneco Silencioso)
*   **Local:** Docas da Cidade (Noite).
*   **Avatar A:** Yugi Muto.
*   **Avatar B:** Strings.

#### Pré-Duelo
**(Background: Docas da Cidade - Noite)**
*   **Yugi [Medo]:** (Sussurrando) Ele não diz uma palavra... Olhe nos olhos dele. Estão vazios. Ele está sendo controlado mentalmente pelo Marik através de uma relíquia milenar!
*   **Strings (Marik) [Tristeza]:** Finalmente nos encontramos, portador do Caos. Strings é apenas minha marionete, mas através dele, eu vou drenar cada gota de energia do seu deck.
*   **Strings (Marik) [Confiança]:** Você acha que suas cartas de Caos são especiais? Meu deck de Slime é imortal. Ele se regenera, ele se multiplica e ele vai sufocar sua estratégia até que você não tenha mais nada para sacar. Prepare-se para ver seu "Forbidden Chaos" ser devorado pelo meu abismo infinito!
*   **Yugi [Raiva]:** Ele usa cartas de regeneração constante! Se você não destruir os monstros dele de uma vez, eles voltarão turno após turno.
*   **Yugi [Confiança]:** (Para o Protagonista) Não deixe o silêncio dele te desconcentrar. Foque no ponto cego da defesa dele. O Caos pode romper qualquer ciclo de regeneração!
*   **Strings [Séria]:** O duelo começa agora. E será o seu último.

#### Pós-Duelo (Vitória)
*   **Strings [Silêncio]:** ... (O boneco cai de joelhos)
*   **Yugi [Tristeza]:** A conexão mental foi cortada. Ele está livre agora. Marik perdeu seu controle sobre ele.

---

### 024: Arkana (O Mestre das Ilusões Sombrias)
*   **Local:** Teatro Abandonado.
*   **Avatar A:** Yugi Muto.
*   **Avatar B:** Arkana.

#### Pré-Duelo
**(Background: Teatro Abandonado)**
*   **Yugi [Raiva]:** Esse lugar... sinto uma presença maligna vinda do palco. Arkana se diz o verdadeiro mestre do Mago Negro, mas ele não tem respeito pelas suas próprias cartas!
*   **Arkana [Deboche]:** (Aparece em uma nuvem de fumaça) Sejam bem-vindos ao meu último show! Yugi Muto... e o seu novo assistente de palco. Dizem que o Caos é a força suprema, mas o que é o Caos diante da verdadeira magia das sombras?
*   **Arkana [Confiança]:** Meu Mago Negro é impiedoso. Diferente do seu, Yugi, o meu não conhece a palavra "lealdade", apenas a "vitória". Eu preparei um campo de duelo onde cada erro seu será punido com lâminas reais! Vamos ver se o seu deck de Caos consegue escapar do meu truque final!
*   **Yugi [Séria]:** Cuidado! Arkana usa versões alteradas de cartas mágicas e armadilhas letais. Ele vai tentar banir seus monstros antes mesmo deles atacarem.
*   **Yugi [Confiança]:** Mostre a ele que um verdadeiro duelista confia no seu deck, enquanto ele só confia em truques baratos. É hora de fechar as cortinas para ele!
*   **Arkana [Alegria]:** Que comece o espetáculo! DUELO!

#### Pós-Duelo (Vitória)
*   **Arkana [Medo]:** Não! Meus magos... minha mágica perfeita... tudo despedaçado! O Caos... ele não segue as regras do meu show! Marik... me ajude!
*   **Yugi [Tristeza]:** Ele era apenas mais uma peça no jogo do Marik. O próximo passo nos levará ao coração da organização dos Rare Hunters.
*   **Yugi [Confiança]:** O caminho está livre para enfrentarmos o braço direito de Marik, Odion, e a misteriosa Ishizu Ishtar. O destino da cidade está sendo decidido agora!

---

### 025: Odion (A Lealdade de Ferro)
*   **Local:** Pátio de um Templo Antigo.
*   **Avatar A:** Yugi Muto.
*   **Avatar B:** Odion.

#### Pré-Duelo
**(Background: Pátio do Templo)**
*   **Yugi [Séria]:** Diferente dos outros Rare Hunters, este homem não exala malícia... ele exala disciplina. Odion é o protetor da família Ishtar. Ele não luta por ganância, mas por um dever que jurou cumprir até a morte.
*   **Odion [Confiança]:** Sua jornada foi impressionante até aqui, jovem duelista. Você derrotou os impuros, mas agora está diante do guardião das sombras. Meu mestre Marik ordenou que eu testasse a resistência do seu espírito.
*   **Odion [Séria]:** O meu deck é uma fortaleza de armadilhas. Cada passo que você der no campo será um convite para a sua própria destruição. O Caos é uma força explosiva, mas contra a minha paciência milenar, as explosões costumam se voltar contra quem as criou. Mostre-me sua convicção!
*   **Yugi [Medo]:** Cuidado! Odion raramente invoca monstros de forma direta. Ele prefere cartas de armadilha que se transformam em monstros ou que causam dano direto.
*   **Yugi [Confiança]:** (Para o Protagonista) Você terá que ser cirúrgico. Se atacar sem pensar, cairá nas garras dele. Use o Caos para desestabilizar a defesa perfeita de Odion!
*   **Odion [Séria]:** Que o destino decida o vencedor. DUELO!

#### Pós-Duelo (Vitória)
*   **Odion [Respeito]:** Você lutou com honra. Talvez haja esperança para o meu mestre através de você.
*   **Yugi [Séria]:** Nós vamos salvá-lo, Odion. Eu prometo. A escuridão não vai consumir a família Ishtar.

---

### 026: Ishizu Ishtar (O Olhar do Futuro)
*   **Local:** Sala das Antiguidades (Museu).
*   **Avatar A:** Yugi Muto.
*   **Avatar B:** Ishizu Ishtar.

#### Pré-Duelo
**(Background: Sala das Antiguidades)**
*   **Yugi [Séria]:** Ishizu... a portadora do Colar do Milênio. Ela possui a habilidade de prever o futuro. Duelo contra ela é como lutar contra um destino que já foi escrito.
*   **Ishizu [Confiança]:** Tudo o que aconteceu até agora foi previsto pelas sombras do Colar. Seu encontro com o Caos, suas vitórias... e até este momento. Eu vim para alertá-lo: o poder que você carrega pode ser a salvação ou a ruína deste mundo.
*   **Ishizu [Séria]:** Meu deck foca no controle do cemitério e na manipulação do tempo. Se você confia apenas no que está na sua mão, já perdeu. Eu vi o fim deste duelo, e ele termina com o seu deck vazio e suas esperanças esmagadas. Você é capaz de mudar o que já foi escrito?
*   **Yugi [Raiva]:** Não aceite o destino dela! O futuro é algo que construímos com cada carta que sacamos!
*   **Yugi [Confiança]:** (Para o Protagonista) O deck dela usa cartas como "Exchange of the Spirit" para virar o jogo contra você. Mantenha o seu cemitério sob controle e não deixe que ela preveja o seu próximo passo. Surpreenda o futuro!
*   **Ishizu [Confiança]:** Mostre-me a luz que o Colar não conseguiu enxergar. DUELO!

#### Pós-Duelo (Vitória)
*   **Ishizu [Alegria]:** Incrível... pela primeira vez, as visões do Colar do Milênio falharam. Você criou um novo caminho. Talvez haja esperança contra a escuridão que Marik está invocando.
*   **Yugi [Alegria]:** Você conseguiu o impossível! Mas a nossa trégua termina aqui. Os próximos duelistas que restam em Battle City são veteranos que não se importam com o destino do mundo, apenas com o poder.
*   **Yugi [Séria]:** Prepare-se. O caminho para enfrentar Seto Kaiba está sendo bloqueado por duelistas vindos de outras regiões... e o próprio Vovô parece estar envolvido em um desafio estranho no setor leste.

---

### 027: Female Duelist GBA (A Desafiante das Sombras GBA)
*   **Local:** Parque de Diversões Abandonado.
*   **Avatar A:** Yugi Muto.
*   **Avatar B:** Female Duelist GBA.

#### Pré-Duelo
**(Background: Parque de Diversões Abandonado)**
*   **Yugi [Séria]:** Algo está errado... essa duelista não parece estar registrada no sistema da Kaiba Corp. Sinto uma energia vinda dela que remete a duelos de uma era diferente... como se ela tivesse viajado através do tempo ou de uma memória.
*   **Female Duelist GBA [Confiança]:** Então você é o famoso mestre do "Forbidden Chaos"? No meu mundo, as regras são mais rígidas e o poder é medido pela raridade da alma, não apenas das cartas. Eu vim de longe para ver se esse Caos é real ou apenas um truque de luzes.
*   **Female Duelist GBA [Deboche]:** Meu deck é composto por cartas que desafiam o meta atual. Eu não sigo as modas de Battle City. Se você não conseguir acompanhar o meu ritmo, sua jornada terminará neste parque esquecido. Vamos ver se o seu Caos brilha no escuro!
*   **Yugi [Confiança]:** (Para o Protagonista) Ela joga com uma precisão cirúrgica. Os duelistas dessa "linhagem" costumam usar cartas de efeito que punem jogadores agressivos demais.
*   **Yugi [Séria]:** Mantenha a guarda alta. O "Forbidden Chaos" precisa de equilíbrio agora mais do que nunca!
*   **Female Duelist GBA [Confiança]:** Que a melhor estratégia vença. DUELO!

#### Pós-Duelo (Vitória)
*   **Female Duelist GBA [Alegria]:** Nada mal para esta dimensão! Vou levar essa derrota como lição para o meu mundo.
*   **Yugi [Alegria]:** Foi um duelo incrível. É sempre bom ver estilos diferentes de duelo.

---

### 028: Grandpa GBA (O Reencontro do Mestre GBA)
*   **Local:** Rua Lateral do Museu (Entardecer).
*   **Avatar A:** Yugi Muto.
*   **Avatar B:** Grandpa GBA.

#### Pré-Duelo
**(Background: Rua Lateral do Museu)**
*   **Yugi [Alegria]:** Vovô?! O que o senhor está fazendo aqui? E por que está vestindo seu antigo traje de mestre de jogos?
*   **Grandpa GBA [Confiança]:** Ho ho ho! Yugi, às vezes um velho precisa mostrar que ainda tem alguns truques na manga! [Nome do Jogador], eu estive observando sua evolução de longe. Mas a versão "moderna" de mim é gentil demais.
*   **Grandpa GBA [Séria]:** Para enfrentar o que vem a seguir — Kaiba e o próprio Yugi — você precisa enfrentar a sabedoria dos clássicos. Meu deck de GBA é focado em combos de cartas que você provavelmente nunca viu em Battle City. Se você não puder vencer o seu próprio mentor em sua forma mais estratégica, você não terá chance no topo da Torre de Duelos!
*   **Yugi [Alegria]:** O Vovô está falando sério! Ele está usando o deck que o tornou famoso no mundo todo antes de se aposentar.
*   **Yugi [Confiança]:** (Para o Protagonista) Não facilite só porque é ele. O Vovô quer ver o brilho total do seu poder. Mostre a ele que o aluno superou o mestre!
*   **Grandpa GBA [Alegria]:** Prepare-se, jovem! Vamos ver se o "Forbidden Chaos" aguenta o peso da experiência! DUELO!

#### Pós-Duelo (Vitória)
*   **Grandpa GBA [Alegria]:** Incrível! Simplesmente magnífico! Seus movimentos são fluidos como a água e destrutivos como o fogo. Você está pronto.
*   **Yugi [Séria]:** Vovô tem razão. O caminho à frente se divide.
*   **Yugi [Confiança]:** As finais de Battle City estão começando. No topo daquele arranha-céu, o Rei dos Jogos e o Imperador da Kaiba Corp estão esperando. Eu serei o seu próximo oponente, [Nome do Jogador]. E saiba que, no campo de duelo, não haverá amizade que me impeça de dar o meu melhor!

---

### 029: Yugi Muto (O Desafio do Rei dos Jogos)
*   **Local:** Topo do Dirigível da Kaiba Corp (Noite).
*   **Avatar A:** NENHUM (Espaço Vazio).
*   **Avatar B:** Yugi Muto.

#### Pré-Duelo
**(Background: Topo do Dirigível - Noite)**
*   **Yugi [Confiança]:** Chegamos ao ponto onde as palavras não são mais necessárias. Você derrotou os Rare Hunters, superou os Ishtar e provou que o seu "Forbidden Chaos" não é uma maldição, mas uma extensão da sua alma.
*   **Yugi [Séria]:** Mas para ser o Rei dos Jogos, você precisa enfrentar o meu Mago Negro. Eu não vou lutar apenas com cartas; vou lutar com o elo que tenho com cada monstro no meu deck. Se o seu Caos for apenas destruição, ele será consumido pela minha luz. Você está pronto para o duelo que definirá sua lenda?
*   **[Sistema]:** CONEXÃO COM MENTOR INDISPONÍVEL. O DUELISTA ESTÁ POR CONTA PRÓPRIA.
*   **Yugi [Confiança]:** Não procure por ajuda nos lados. Olhe apenas para o seu deck. É hora do duelo!

#### Pós-Duelo (Vitória)
*   **Yugi [Alegria]:** Você me superou! O título de Rei dos Jogos está em boas mãos... por enquanto!
*   **[Sistema]:** CONEXÃO COM MENTOR RESTABELECIDA. KAIRO RETORNA.

---

### 030: Seto Kaiba (O Imperador da Kaiba Corp - Chefe do Ato 3)
*   **Local:** Torre de Duelos - Estádio Supremo.
*   **Avatar A:** Kairo (Interferência/Voz nas Sombras).
*   **Avatar B:** Seto Kaiba.

#### Pré-Duelo
**(Background: Torre de Duelos - Estádio Supremo)**
*   **Kaiba [Deboche]:** Então você é o lixo que o Yugi tanto elogia? "Forbidden Chaos"... que nome patético para um deck que não passa de uma falha no meu sistema. Eu construí esta cidade, eu defini as regras e eu sou o ápice da evolução dos duelos!
*   **Kaiba [Confiança]:** Não me importa o que o místico ou o destino dizem. No meu mundo, o poder é definido pelo meu Dragão Branco de Olhos Azuis! Eu vou esmagar o seu deck de "Caos" e provar que a tecnologia da Kaiba Corp é a única força absoluta neste mundo. Prepare-se para ser apagado da história!
*   *(Uma voz ecoa no fundo, o Avatar A de KAIRO aparece levemente transparente/glitch)*
*   **Kairo (Voz) [Séria]:** [Nome do Jogador]... não deixe a arrogância dele te cegar. Kaiba é a personificação da ordem rígida. O seu Caos é a única coisa que ele não consegue prever ou controlar. Esta é a batalha final pela alma de Battle City. Liberte tudo!
*   **Kaiba [Raiva]:** Pare de falar com as paredes e saque suas cartas! Eu vou te mostrar o que acontece com quem desafia um Deus! DUELO!

#### Pós-Duelo (Fim do Ato 3)
*   **Kaiba [Tristeza]:** Impossível! O meu Blue-Eyes... superado por essa... essa anomalia?! Isso não é o fim! Eu vou recalibrar tudo, eu vou encontrar uma forma de... de...
*   *(Kaiba soca o painel de controle e sai de cena)*
*   **[Sistema]:** O CÉU DA CIDADE MUDA DE AZUL PARA UM VERDE MÍSTICO.
*   **Kairo [Confiança]:** (Voltando ao normal) Você conseguiu. O império de Kaiba foi abalado. Mas olhe para o horizonte... o céu está mudando. A energia que você liberou para vencer atraiu a atenção de guardiões que não pertencem a este tempo.
*   **Kairo [Séria]:** Magos antigos e místicas do deserto estão se manifestando. Saímos da tecnologia de Battle City e estamos entrando no domínio dos Guardiões Místicos. Prepare-se, pois agora a mágica é real.

---

## Ato 4: Guardiões Místicos
**Contexto:** Para obter o poder antigo, você deve derrotar os guardiões dos elementos.

### 031: Novice GBC (O Despertar do Passado)
*   **Local:** Entrada do Templo Ancestral.
*   **Avatar A:** Kairo.
*   **Avatar B:** Novice GBC.

#### Pré-Duelo
**(Background: Entrada do Templo)**
*   **Kairo [Séria]:** Sente esse peso no ar? Não estamos mais em Battle City. A energia do seu deck de Caos nos trouxe a um plano onde o tempo flui de maneira diferente. Aqui, as regras são ancestrais.
*   **Novice GBC [Confiança]:** Parado aí, viajante! Você pisou em solo sagrado. Eu sou o guardião dos portões externos. No meu tempo, não precisávamos de hologramas complicados para duelar!
*   **Novice GBC [Deboche]:** Meu deck é simples, mas eficaz. Ele carrega a essência dos primeiros duelos que o mundo conheceu. Se você não consegue vencer um "novato" da era original, os magos lá dentro vão transformar sua alma em areia!
*   **Kairo [Confiança]:** (Para o Protagonista) Ele pode parecer simples, mas os duelistas desta era usam cartas com efeitos diretos e poderosos. É um retorno às raízes.
*   **Kairo [Alegria]:** Mostre a ele que o seu "Forbidden Chaos" é uma força que transcende as eras!
*   **Novice GBC [Confiança]:** Prepare o seu disco de duelo antigo! É HORA DO DUELO!

#### Pós-Duelo (Vitória)
*   **Novice GBC [Tristeza]:** Pixel por pixel, você me desmontou. Pode passar, viajante.
*   **Kairo [Confiança]:** O passado respeita a força. Vamos em frente.

---

### 032: Female GBC (A Guardiã das Relíquias)
*   **Local:** Sala dos Hieróglifos.
*   **Avatar A:** Kairo.
*   **Avatar B:** Female GBC.

#### Pré-Duelo
**(Background: Sala dos Hieróglifos)**
*   **Kairo [Séria]:** Veja as paredes... os hieróglifos estão reagindo à sua presença. O Caos está tentando se comunicar com este lugar. Mas parece que temos companhia.
*   **Female GBC [Confiança]:** Impressionante. Poucos conseguem passar pelo portão sem perder a sanidade. Eu protejo as memórias deste templo. Para você continuar sua busca pelos Magos, terá que provar que seu coração é tão firme quanto a pedra dessas paredes.
*   **Female GBC [Deboche]:** Meu deck utiliza a sabedoria das cartas clássicas de suporte. Eu vou testar sua paciência e sua capacidade de adaptação. Se o seu Caos for apenas fúria descontrolada, ele será selado nestas ruínas para sempre!
*   **Kairo [Confiança]:** Ela é ágil e conhece cada armadilha escondida nestas salas.
*   **Kairo [Séria]:** (Para o Protagonista) Mantenha o foco. Este é um teste de resistência. Se você vencer, os Magos Elementais sentirão sua chegada.
*   **Female GBC [Confiança]:** Que os Deuses antigos julguem suas jogadas! DUELO!

#### Pós-Duelo (Vitória)
*   **Female GBC [Alegria]:** Você tem a marca... o brilho nos seus olhos confirma o que as lendas diziam. O caminho está aberto.
*   **Kairo [Séria]:** O ar está ficando mais quente. O primeiro dos grandes guardiões está se manifestando. Prepare-se. O Desert Mage nos aguarda nas areias escaldantes além deste corredor.

---

### 033: Desert Mage (A Sentinela das Areias)
*   **Local:** Deserto Infinito (Meio-Dia).
*   **Avatar A:** Kairo.
*   **Avatar B:** Desert Mage.

#### Pré-Duelo
**(Background: Deserto Infinito)**
*   **Kairo [Séria]:** O deserto não é apenas areia e calor; é um teste de perseverança. O Desert Mage controla as tempestades e os monstros que se escondem sob as dunas. Ele vai tentar enterrar sua estratégia antes mesmo de você invocar seu primeiro monstro.
*   **Desert Mage [Confiança]:** Viajante, você sobreviveu ao templo, mas as areias não perdoam erros. Aqui, o "Forbidden Chaos" é apenas um grão de poeira diante da imensidão. Sinta o peso de milênios de isolamento!
*   **Desert Mage [Séria]:** Meu deck de Zumbis e Criaturas do Deserto se fortalece com o desgate do oponente. Eu não preciso te vencer rápido... eu só preciso esperar que o sol e a minha areia drenem sua vontade de lutar. Prepare-se para virar parte da paisagem!
*   **Kairo [Confiança]:** (Para o Protagonista) Ele usa cartas que reduzem o ataque e travam o campo. Use a natureza explosiva do Caos para romper essa paralisia! Não deixe o duelo se arrastar, ou a tempestade de areia dele será sua ruína.
*   **Desert Mage [Confiança]:** Que o Saara consuma o seu destino! DUELO!

#### Pós-Duelo (Vitória)
*   **Desert Mage [Tristeza]:** A areia cobriu meus monstros... você é o novo oásis neste deserto.
*   **Kairo [Séria]:** Um a menos. Faltam cinco guardiões.

---

### 034: Forest Mage (O Sussurro da Selva Antiga)
*   **Local:** Floresta Mística (Noite).
*   **Avatar A:** Kairo.
*   **Avatar B:** Forest Mage.

#### Pré-Duelo
**(Background: Floresta Mística)**
*   **Kairo [Alegria]:** Do calor extremo para a umidade profunda. A Forest Mage é a guardiã da vida e do crescimento. Ela vê o "Forbidden Chaos" como uma praga que precisa ser contida e podada.
*   **Forest Mage [Confiança]:** Sua presença desequilibra a harmonia da minha floresta. O Caos é instável e perigoso, e as minhas plantas e insetos detestam o que não podem consumir.
*   **Forest Mage [Deboche]:** Meu deck é um ecossistema de regeneração e armadilhas naturais. Cada carta que você ativa serve de adubo para as minhas criaturas. Você acha que pode queimar minha floresta? Tente... e verá que a natureza sempre recupera o que é dela!
*   **Kairo [Séria]:** Cuidado! Ela usa efeitos que se ativam quando monstros são destruídos ou enviados ao cemitério. É um ciclo de vida infinito. Você precisará de uma força que apague a existência das cartas dela, não apenas as destrua.
*   **Forest Mage [Confiança]:** A floresta decidiu o seu fim. DUELO!

#### Pós-Duelo (Vitória)
*   **Forest Mage [Tristeza]:** O Caos... ele não pode ser podado. Ele cresce como uma raiz que atravessa a própria realidade. Siga em frente... os picos gelados e as águas profundas te esperam.
*   **Kairo [Confiança]:** Você está dominando os elementos. Mas os próximos guardiões são mais temperamentais. O Mountain Mage e o Meadow Mage possuem estilos de jogo opostos: um foca no poder bruto das alturas e o outro na nobreza tática dos campos abertos.

---

### 035: Mountain Mage (O Trovão dos Picos Isolados)
*   **Local:** Pico da Montanha Sagrada.
*   **Avatar A:** Kairo.
*   **Avatar B:** Mountain Mage.

#### Pré-Duelo
**(Background: Pico da Montanha)**
*   **Kairo [Séria]:** Cuidado onde pisa. Aqui, a gravidade e o clima jogam contra você. O Mountain Mage é o mestre dos ventos e das feras aladas. No topo do mundo, o seu "Forbidden Chaos" precisa ser tão sólido quanto a rocha para não ser soprado para o abismo.
*   **Mountain Mage [Confiança]:** Você escalou até aqui apenas para cair, duelista! Minhas feras dominam os céus e meu poder é tão imenso quanto a cordilheira que nos cerca. O Caos é uma força sem direção, mas o meu trovão tem um alvo certo: você!
*   **Mountain Mage [Deboche]:** Meu deck de Dragões e Bestas Aladas foca em ataques rápidos e devastadores. Se você demorar para preparar sua defesa, minhas criaturas vão te rasgar antes que você possa dizer "duelo". Sinta a pressão das alturas!
*   **Kairo [Confiança]:** (Para o Protagonista) Ele busca o bônus de campo e o poder de ataque puro. Não tente competir apenas na força bruta; use a instabilidade do Caos para desviar os raios dele e contra-atacar quando ele baixar a guarda.
*   **Mountain Mage [Alegria]:** O céu está desabando sobre você! DUELO!

#### Pós-Duelo (Vitória)
*   **Mountain Mage [Raiva]:** Como?! Minha montanha desmoronou! O trovão foi silenciado!
*   **Kairo [Deboche]:** Tudo o que sobe, desce. Até mesmo as montanhas.

---

### 036: Meadow Mage (A Estratégia dos Campos Infinitos)
*   **Local:** Prados de Ouro.
*   **Avatar A:** Kairo.
*   **Avatar B:** Meadow Mage.

#### Pré-Duelo
**(Background: Prados de Ouro)**
*   **Kairo [Alegria]:** Finalmente, um pouco de ar puro. Mas não baixe a guarda. O Meadow Mage é o estrategista mais refinado entre os guardiões. Para ele, o duelo é uma partida de xadrez, e cada carta é um soldado em seu exército real.
*   **Meadow Mage [Confiança]:** Bem-vindo aos meus domínios. O Caos é uma energia bárbara e desordenada, desprovida de tática. Eu comando a elite dos Guerreiros e Cavaleiros que já caminharam por estas terras. Vou te mostrar que a disciplina e a formação de batalha são superiores a qualquer força proibida.
*   **Meadow Mage [Séria]:** Meu deck foca em monstros Guerreiros com alta defesa e efeitos de suporte mútuo. Eu não ataco sem um plano, e minha defesa é quase impenetrável. Você tem a nobreza necessária para enfrentar um exército de elite ou é apenas um destruidor de mundos?
*   **Kairo [Séria]:** (Para o Protagonista) Ele vai tentar cercar você com monstros que protegem uns aos outros. Você precisará encontrar uma brecha na formação dele. O "Forbidden Chaos" deve agir como um golpe decisivo no coração do exército inimigo!
*   **Meadow Mage [Confiança]:** Cavaleiros, em posição! Que a honra dite o vencedor! DUELO!

#### Pós-Duelo (Vitória)
*   **Meadow Mage [Tristeza]:** Minha formação... quebrada pela pura imprevisibilidade do seu jogo. Talvez a ordem precise de um pouco de caos para evoluir. Você provou seu valor.
*   **Kairo [Séria]:** Você venceu os guardiões dos elementos básicos. Agora, a temperatura vai subir... e cair drasticamente. O Ocean Mage e o Labyrinth Mage estão à frente.

---

### 037: Ocean Mage (O Abismo das Águas Sombrias)
*   **Local:** Templo Submerso.
*   **Avatar A:** Kairo.
*   **Avatar B:** Ocean Mage.

#### Pré-Duelo
**(Background: Templo Submerso)**
*   **Kairo [Séria]:** Prenda a respiração. O Ocean Mage não luta apenas com cartas, ele luta com a pressão das profundezas. Aqui, o seu "Forbidden Chaos" pode acabar diluído se você não concentrar sua energia.
*   **Ocean Mage [Confiança]:** O Caos é como uma tempestade na superfície... barulhenta, mas passageira. Aqui no fundo, apenas o silêncio e a correnteza importam. Você está pronto para ver suas estratégias afundarem?
*   **Ocean Mage [Deboche]:** Meu deck utiliza monstros que atacam diretamente através das águas e efeitos que devolvem suas cartas para a mão. Você não pode destruir o que não consegue tocar. Deixe o oceano levar suas esperanças!
*   **Kairo [Séria]:** (Para o Protagonista) Ele vai tentar "limpar" o seu campo sem destruir suas cartas, apenas removendo-as. Você precisa de invocações rápidas e monstros que tenham resistência a efeitos de retorno. Não deixe a maré dele te arrastar!
*   **Ocean Mage [Confiança]:** Sinta o frio do abismo! DUELO!

#### Pós-Duelo (Vitória)
*   **Ocean Mage [Tristeza]:** A maré baixou... eu me rendo. O abismo não conseguiu te conter.
*   **Kairo [Séria]:** Respire fundo. O labirinto é o próximo desafio.

---

### 038: Labyrinth Mage (O Enigma dos Corredores Infinitos)
*   **Local:** Labirinto de Pedra.
*   **Avatar A:** Kairo.
*   **Avatar B:** Labyrinth Mage.

#### Pré-Duelo
**(Background: Labirinto de Pedra)**
*   **Kairo [Medo]:** Este lugar é uma armadilha viva. O Labyrinth Mage é o arquiteto das confusões mentais. Ele não quer apenas te vencer, ele quer que você se perca dentro do seu próprio deck.
*   **Labyrinth Mage [Deboche]:** Esquerda ou direita? Vida ou morte? Cada carta que você joga é uma curva em um labirinto que não tem saída. O Caos é desordem, e eu sou o mestre da estrutura que aprisiona a desordem!
*   **Labyrinth Mage [Confiança]:** Meu deck foca em monstros de parede com defesa absurda e cartas que mudam a posição de batalha dos seus monstros. Enquanto você bate a cabeça contra as minhas paredes, eu preparo a saída... para o cemitério!
*   **Kairo [Raiva]:** (Para o Protagonista) Ele joga na defensiva, esperando você se cansar. Use o poder de perfuração do Caos! Se ele quer construir paredes, mostre que o seu deck é a marreta que vai derrubar tudo de uma vez!
*   **Labyrinth Mage [Alegria]:** Boa sorte tentando encontrar a saída! DUELO!

#### Pós-Duelo (Vitória)
*   **Labyrinth Mage [Tristeza]:** Você... você simplesmente atravessou as paredes? O Caos não respeita a arquitetura do meu medo... o caminho para o Sumo Sacerdote está... logo ali.
*   **Kairo [Séria]:** O ar está ficando carregado de incenso e eletricidade estática. Vencemos os guardiões, mas agora o desafio muda de nível. Estamos no limiar do santuário interno. O High Mage e o lendário Heishin estão esperando.

---

### 039: Simon Muran (A Voz do Santuário)
*   **Local:** Grande Altar do Caos.
*   **Avatar A:** Kairo.
*   **Avatar B:** Simon Muran.

#### Pré-Duelo
**(Background: Grande Altar do Caos)**
*   **Kairo [Medo]:** Sinta a pressão... O High Mage é o elo direto entre o mundo dos homens e o poder que você carrega. Ele não vê o seu deck como uma ferramenta, mas como uma herança que você ainda não provou merecer.
*   **Simon Muran [Confiança]:** Silêncio! Suas vitórias sobre os guardiões elementais foram apenas o primeiro passo. O Caos não é algo que se "possui", é algo que se serve. Você está pronto para sacrificar suas certezas em nome do poder verdadeiro?
*   **Simon Muran [Séria]:** Meu deck é uma combinação de todos os elementos que você enfrentou, refinados em uma estratégia de controle absoluto. Eu vou banir suas esperanças e selar seus monstros nas dobras do tempo. Se você falhar aqui, o "Forbidden Chaos" retornará ao altar e sua memória será apagada!
*   **Kairo [Séria]:** (Para o Protagonista) Ele mistura as táticas de todos os Magos anteriores. É um duelo de resistência mental. Não se deixe levar pela imponência dele; o Caos dentro de você é a única coisa que ele não pode prever totalmente.
*   **Simon Muran [Confiança]:** Que o julgamento final comece! DUELO!

#### Pós-Duelo (Vitória)
*   **Simon Muran [Alegria]:** Magnífico. Você tem a sabedoria dos antigos. O santuário aceita sua presença.
*   **Kairo [Confiança]:** O selo está ao nosso alcance. Só resta o guardião final.

---

### 040: Shadi (O Guardião do Selo Proibido - Chefe do Ato 4)
*   **Local:** Câmara Proibida.
*   **Avatar A:** Kairo (Voz Trêmula).
*   **Avatar B:** Shadi.

#### Pré-Duelo
**(Background: Câmara Proibida)**
*   **Kairo [Medo]:** Shadi... o arquiteto da queda do Egito. Ele é o responsável por selar o poder do Caos há milênios. Se ele vencer, o ciclo se repetirá e o mundo entrará em trevas eternas.
*   **Shadi [Deboche]:** Patético. Milênios se passaram e a humanidade ainda envia crianças para tentar domar o indomável. Eu conheço a origem desse Caos melhor do que qualquer um. Eu fui aquele que o trancou, e serei aquele que o destruirá em suas mãos!
*   **Shadi [Confiança]:** Meu deck não conhece limites de raridade ou custo. Eu invoco monstros das estrelas e utilizo magias que desafiam a própria existência das suas cartas. Você é apenas um erro na minha história perfeita. Prepare-se para ser consumido pelo verdadeiro vazio!
*   **Kairo [Raiva]:** (Para o Protagonista) Ignore a aura dele! Shadi joga com cartas proibidas e de altíssimo poder de ataque. Você terá que ser mais rápido do que a própria luz para vencê-lo. É tudo ou nada... pela sua alma e pelo futuro do Caos!
*   **Shadi [Alegria]:** Desapareça na escuridão eterna! DUELO!

#### Pós-Duelo (Fim do Ato 4)
*   **Shadi [Tristeza]:** Não... o selo... ele se quebrou! O Caos... ele escolheu você em vez de mim?! Maldição... a luz está voltando...
*   *(Shadi se dissolve em partículas de luz violeta)*
*   **[Sistema]:** O TEMPLO EGÍPCIO COMEÇA A DESMORONAR E RECONSTRUIR-SE EM UMA ESTRUTURA MODERNA E BRANCA.
*   **Kairo [Alegria]:** Você conseguiu o impossível. Shadi foi derrotado e o selo do "Forbidden Chaos" foi finalmente liberado por completo. Mas veja... o templo está mudando.
*   **Kairo [Séria]:** As sombras do passado deram lugar a um brilho artificial. Saímos da era dos mistérios egípcios e estamos sendo transportados para uma realidade simulada.
*   **Kairo [Confiança]:** O Ato 5 nos aguarda. Onde a lógica domina e seus inimigos controlam o próprio sistema. Bem-vindo ao Pesadelo Virtual.

---

## Ato 5: Pesadelo Virtual
**Contexto:** Você está preso no ciberespaço. Os Big Five e a família Kaiba digital querem seu corpo para escapar.

### 041: Intermediate GBC (A Primeira Barreira Digital)
*   **Local:** Cidade Cibernética Abandonada.
*   **Avatar A:** Joey Wheeler.
*   **Avatar B:** Intermediate GBC.

#### Pré-Duelo
**(Background: Cidade Cibernética)**
*   **Joey [Medo]:** Que lugar esquisito! Onde é que o Kaiba nos meteu dessa vez? Parece que entrei dentro de um videogame de bolso!
*   **Intermediate GBC [Confiança]:** Vocês são os novos convidados do Mestre Noah. Eu sou apenas uma rotina de segurança intermediária. Se não conseguirem passar por mim, como esperam sobreviver ao julgamento dos Big Five?
*   **Joey [Raiva]:** Big Five? Aqueles velhotes da Kaiba Corp? [Nome do Jogador], esse cara de GBC está bloqueando o caminho. O deck dele tem um nível de dificuldade maior do que os anteriores. Não facilita!
*   **Intermediate GBC [Séria]:** Iniciando protocolo de teste. DUELO!

#### Pós-Duelo (Vitória)
*   **Intermediate GBC [Tristeza]:** Erro de sistema... Derrota registrada. Reiniciando...
*   **Joey [Alegria]:** Game Over pra você! Vamos nessa!

---

### 042: Expert GBC (O Código Especialista)
*   **Local:** Floresta de Pixels.
*   **Avatar A:** Mai Valentine.
*   **Avatar B:** Expert GBC.

#### Pré-Duelo
**(Background: Floresta de Pixels)**
*   **Mai [Confiança]:** Noah está tentando nos cansar com esses programas de treinamento. Mas este aqui... ele parece ter uma programação muito mais refinada.
*   **Expert GBC [Confiança]:** Minhas jogadas são calculadas para a vitória absoluta. Eu não sou um mero programa, sou o ápice da estratégia portátil! Seu "Forbidden Chaos" é apenas uma variável que eu vou aprender a anular em três turnos.
*   **Mai [Deboche]:** Três turnos? Ele é convencido! [Nome do Jogador], ele joga como um profissional. Fique atento aos combos de cartas mágicas que ele usa para acelerar o deck.
*   **Expert GBC [Séria]:** Executando estratégia mestre. DUELO!

#### Pós-Duelo (Vitória)
*   **Expert GBC [Raiva]:** Impossível! Meus algoritmos eram perfeitos! Onde foi que eu errei?
*   **Mai [Deboche]:** A perfeição é chata. O Caos é muito mais divertido.

---

### 043: Student GBC (O Estudante do Sistema)
*   **Local:** Biblioteca de Dados Virtual.
*   **Avatar A:** Kairo (Recuperando dados).
*   **Avatar B:** Student GBC.

#### Pré-Duelo
**(Background: Biblioteca de Dados)**
*   **Kairo [Séria]:** Estou tentando rastrear a saída, mas o sistema é um labirinto. Este estudante aqui... ele está analisando nossos duelos passados para criar o deck perfeito contra nós!
*   **Student GBC [Alegria]:** Eu li tudo sobre você! Suas vitórias na Ilha, em Battle City... eu estudei cada fraqueza do seu deck de Caos. Agora, vou usar esse conhecimento para ganhar meu lugar ao lado de Noah!
*   **Kairo [Confiança]:** Ele pode ter os dados, mas ele não tem o seu instinto! Mostre a ele que a teoria é bem diferente da prática no campo de batalha.
*   **Student GBC [Confiança]:** Aula prática iniciada! DUELO!

#### Pós-Duelo (Vitória)
*   **Student GBC [Tristeza]:** Meus dados... estavam incompletos? Como você previu minha jogada?
*   **Kairo [Séria]:** A experiência real supera qualquer simulação. Volte para os livros.

---

### 044: Gansley (O Retorno dos Big Five - Negócios Mortais)
*   **Local:** Sala de Reuniões Virtual.
*   **Avatar A:** Seto Kaiba (Intervenção "Vegeta Style").
*   **Avatar B:** Gansley.

#### Pré-Duelo
**(Background: Sala de Reuniões Virtual)**
*   **Kaiba [Raiva]:** Gansley! Eu deveria ter imaginado que você estaria por trás dessa palhaçada digital. Você falhou em me derrubar no mundo real e vai falhar aqui também!
*   **Gansley [Confiança]:** Seto... sempre tão arrogante. Mas aqui no mundo virtual, as regras mudaram. Eu sou o Deck Master! E meu objetivo é simples: tomar o corpo deste duelista e voltar para a Kaiba Corp para te tirar do trono!
*   **Kaiba [Deboche]:** Você não conseguiria gerenciar uma banca de jornal, quanto mais o meu corpo. [Nome do Jogador], esmague esse verme. Ele usa monstros guerreiros e táticas defensivas de covarde. Mostre a ele como um verdadeiro duelista luta!
*   **Gansley [Confiança]:** Vou ensinar uma lição de economia que você nunca esquecerá: a sua derrota é o meu lucro! DUELO!

#### Pós-Duelo (Vitória)
*   **Gansley [Medo]:** Minhas ações... despencaram! Estou falido!
*   **Kaiba [Deboche]:** Você está demitido, Gansley. De novo. Saia da minha frente.

---

### 045: Crump (O Pesadelo Gelado)
*   **Local:** Oceano Congelado.
*   **Avatar A:** Tea Gardner.
*   **Avatar B:** Crump.

#### Pré-Duelo
**(Background: Oceano Congelado)**
*   **Tea [Medo]:** Está muito frio aqui! E quem é aquele pinguim gigante vindo em nossa direção?
*   **Crump [Alegria]:** Pinguim? Eu sou Crump, o antigo contador da Kaiba Corp! E neste mundo, eu sou o mestre dos pinguins! Eu sempre quis um corpo jovem e bonito como o seu, minha querida!
*   **Tea [Raiva]:** Que nojo! [Nome do Jogador], por favor, mande esse pinguim de volta para o zoológico!
*   **Crump [Confiança]:** Meu deck de Pesadelo Pinguim vai congelar suas cartas e devolvê-las para a sua mão. Prepare-se para tremer! DUELO!

#### Pós-Duelo (Vitória)
*   **Crump [Tristeza]:** Não é justo! Eu queria ser jovem de novo! O gelo derreteu...
*   **Tea [Alívio]:** Ainda bem que acabou. Pinguins me dão arrepios agora.

---

### 046: Johnson (A Justiça Distorcida)
*   **Local:** Tribunal Digital.
*   **Avatar A:** Joey Wheeler.
*   **Avatar B:** Johnson.

#### Pré-Duelo
**(Background: Tribunal Digital)**
*   **Joey [Confiança]:** Olha só, um juiz! Vai nos dar uma multa por excesso de velocidade?
*   **Johnson [Séria]:** Silêncio no tribunal! Eu sou Johnson, o consultor jurídico. E vocês são culpados de invadir nosso domínio. A sentença é a perda de seus corpos!
*   **Johnson [Confiança]:** Meu deck de Juiz vai ditar as regras deste duelo. Eu posso manipular a sorte e garantir que a justiça... a MINHA justiça... prevaleça!
*   **Joey [Raiva]:** Justiça de araque! [Nome do Jogador], mostra pra ele que a única lei aqui é a do mais forte! DUELO!

#### Pós-Duelo (Vitória)
*   **Johnson [Raiva]:** Objeção! Isso foi sorte! Eu exijo um novo julgamento!
*   **Joey [Deboche]:** Caso encerrado, meritíssimo! A sentença é a sua derrota.

---

### 047: Leichter (O Jogo de Poder)
*   **Local:** Cassino Virtual.
*   **Avatar A:** Seto Kaiba.
*   **Avatar B:** Leichter.

#### Pré-Duelo
**(Background: Cassino Virtual)**
*   **Kaiba [Deboche]:** Leichter... o homem que tentou comprar minha empresa pelas minhas costas. Ainda jogando sujo?
*   **Leichter [Confiança]:** Seto, meu jovem. O mundo é um jogo de poder, e eu tenho as melhores cartas. Com o poder do Jinzo, eu vou silenciar todas as suas armadilhas patéticas.
*   **Kaiba [Raiva]:** Jinzo? Uma carta poderosa, mas nas mãos de um amador, é inútil. [Nome do Jogador], destrua-o. Ele não merece usar o nome da Kaiba Corp!
*   **Leichter [Alegria]:** Vamos ver quem vai à falência primeiro! DUELO!

#### Pós-Duelo (Vitória)
*   **Leichter [Tristeza]:** A banca quebrou... perdi tudo.
*   **Kaiba [Confiança]:** Nunca aposte contra mim ou meus aliados. O jogo acabou para você.

---

### 048: Nezbitt (O Gigante de Aço)
*   **Local:** Usina de Energia Virtual.
*   **Avatar A:** Joey Wheeler.
*   **Avatar B:** Nezbitt.

#### Pré-Duelo
**(Background: Usina de Energia)**
*   **Joey [Raiva]:** Este cara levou a sério o papo de "homem-máquina"! Ele não quer apenas seu corpo, [Nome do Jogador], ele quer transformar tudo isso aqui em um ferro-velho!
*   **Nezbitt [Confiança]:** A carne é fraca e o código é eterno! Eu sou Nezbitt, o mestre da engenharia da Kaiba Corp. Eu vou esmagar suas cartas com o peso do meu exército mecânico. Quando eu terminar, o seu "Forbidden Chaos" será apenas sucata processada!
*   **Joey [Confiança]:** (Para o Protagonista) Não se assusta com o tamanho dele! Máquinas grandes fazem muito barulho, mas o Caos é mais rápido. Destrói o núcleo de energia dele antes que ele invoque o Cavaleiro Perfeito!
*   **Nezbitt [Séria]:** Poder total às máquinas! DUELO!

#### Pós-Duelo (Vitória)
*   **Nezbitt [Raiva]:** Sistema... crítico... Desligando. A humanidade... é ilógica.
*   **Joey [Alegria]:** Virou sucata! Mais um pro ferro-velho.

---

### 049: Noah Kaiba (O Herdeiro Rejeitado)
*   **Local:** Jardim do Éden Digital.
*   **Avatar A:** Seto Kaiba (Rivalidade Extrema).
*   **Avatar B:** Noah Kaiba.

#### Pré-Duelo
**(Background: Jardim do Éden Digital)**
*   **Kaiba [Raiva]:** Noah... uma memória esquecida tentando brincar de Deus. Você criou este mundo porque não pôde conquistar o real. Mas este duelista aqui é a prova de que o seu "Mundo Perfeito" é apenas uma ilusão frágil!
*   **Noah [Alegria]:** Seto, você sempre foi tão limitado. Eu não sou uma memória, eu sou a evolução! E você, [Nome do Jogador], é apenas um convidado indesejado que se tornou perigoso demais. Meu deck da Arca de Noé vai purificar este mundo, começando pela sua derrota!
*   **Kaiba [Deboche]:** Purificar? Você vai é ser deletado. [Nome do Jogador], Noah usa um deck de Monstros Espíritos que retornam para a mão. É um estilo irritante e evasivo. Acerte-o com tudo antes que ele consiga se esconder atrás dos efeitos dele!
*   **Noah [Séria]:** O dilúvio digital começou. DUELO!

#### Pós-Duelo (Vitória)
*   **Noah [Tristeza]:** Pai! Por que você escolheu eles?! Eu criei tudo isso para você!
*   **Kaiba [Séria]:** Porque eles lutam pelo futuro, não pelo passado. Adeus, Noah.

---

### 050: Gozaburo Kaiba (O Espectro do Passado - Chefe do Ato 5)
*   **Local:** O Abismo do Código Fonte.
*   **Avatar A:** Kairo (A Convergência Final).
*   **Avatar B:** Gozaburo Kaiba.

#### Pré-Duelo
**(Background: Abismo do Código Fonte)**
*   **Kairo [Séria]:** Este é o fim da linha. Gozaburo Kaiba... o homem que deu início a tudo isso. Ele corrompeu o próprio sistema para sobreviver como um vírus de ódio puro. Se perdermos aqui, o Mundo Virtual colapsa com todos nós dentro!
*   **Gozaburo [Confiança]:** Eu construí o império Kaiba com mãos de ferro, e vou reconstruí-lo sobre as cinzas deste seu duelo insignificante! O "Forbidden Chaos" foi uma criação que fugiu ao meu controle, mas Exodia... o Exodia Necross é a minha vontade manifestada!
*   **Kairo [Confiança]:** (Para o Protagonista) [Nome do Jogador], ele trouxe a arma proibida! O Exodia Necross é quase indestrutível enquanto as peças estiverem no cemitério dele. Esta é a batalha final pelo destino da nossa realidade. Use cada gota de poder do seu deck. É hora de mostrar ao passado que o futuro pertence ao Caos!
*   **Gozaburo [Alegria]:** Ajoelhe-se diante do verdadeiro imperador! DUELO FINAL!

#### Pós-Duelo (Final Épico)
*   **Gozaburo [Tristeza/Medo]:** NÃO! O Exodia... superado?! O sistema está se desintegrando... Eu... eu não posso ser apagado novamente!
*   *(O cenário explode em luz branca e códigos verdes)*
*   **Kairo [Alegria]:** VOCÊ CONSEGUIU! O vínculo de Gozaburo com o sistema foi cortado! Estamos voltando para o mundo real, e o "Forbidden Chaos" está finalmente estabilizado no seu deck.
*   *(Cena final: O protagonista acorda na frente do computador com o logo "Forbidden Chaos - Concluído" na tela)*
*   **Kairo (Voz em Off):** "Sua lenda apenas começou. O mundo dos duelos nunca mais será o mesmo."

---

## Ato 6: Ascensão das Trevas
**Contexto:** Heishin despertou e corrompeu os duelistas com o Selo de Orichalcos. Enfrente suas versões sombrias.

### 051: Tea Gardner Adv (O Lado Sombrio da Amizade)
*   **Local:** Praça da Cidade sob o Eclipse.
*   **Avatar A:** Yugi Muto (Preocupado).
*   **Avatar B:** Tea Gardner Adv.

#### Pré-Duelo
**(Background: Praça da Cidade - Eclipse)**
*   **Yugi [Medo]:** Téa! O que aconteceu com você? Esse poder... ele está drenando a bondade do seu coração! [Nome do Jogador], precisamos trazê-la de volta, mas tome cuidado: o Orichalcos aumenta a fúria de qualquer deck!
*   **Tea [Confiança]:** Amizade? Cartas de fadas bonitinhas? Isso era para os fracos. O Orichalcos me mostrou que a única forma de proteger quem eu amo é esmagando quem se opõe a nós. Meu deck de Contra-Fadas agora tem o poder de silenciar qualquer jogada sua!
*   **Yugi [Séria]:** O deck dela agora foca em anular suas cartas mágicas e armadilhas. Ela vai tentar te travar completamente enquanto o Selo fortalece os monstros dela. Não deixe a culpa te impedir de duelar com tudo!
*   **Tea [Séria]:** O selo foi ativado. Não há escapatória! DUELO!

#### Pós-Duelo (Vitória)
*   **Tea [Tristeza]:** (O selo desaparece) O que... o que eu fiz? Minha cabeça dói...
*   **Yugi [Alegria]:** Você voltou, Téa! Está tudo bem agora. O pesadelo acabou.

---

### 052: Tristan Taylor Adv (A Fúria do Protetor)
*   **Local:** Beco Escuro com Runas Verdes.
*   **Avatar A:** Yugi Muto.
*   **Avatar B:** Tristan Taylor Adv.

#### Pré-Duelo
**(Background: Beco Escuro - Runas)**
*   **Yugi [Tristeza]:** Até o Tristan... O Orichalcos está se espalhando como uma praga. Ele está tentando proteger a Téa, mas esse poder está transformando sua lealdade em violência pura!
*   **Tristan [Raiva]:** Saiam da frente! Vocês não entendem o poder que estamos recebendo. Chega de ser o cara que só assiste das arquibancadas. Com este selo, meu deck de Guerreiros vai mostrar quem é que manda nas ruas de Battle City!
*   **Tristan [Confiança]:** [Nome do Jogador], você se acha especial com seu Caos? Vamos ver como ele se sai contra a força bruta de um guerreiro que não tem mais nada a perder!
*   **Yugi [Confiança]:** (Para o Protagonista) Ele está usando monstros de alto nível e cartas de equipamento sombrias. O Orichalcos dá a ele uma linha extra de defesa. Você precisa quebrar o escudo dele para chegar ao coração do duelo!
*   **Tristan [Séria]:** Pelo poder do Selo... DUELO!

#### Pós-Duelo (Vitória)
*   **Tristan [Alívio]:** Minha cabeça... parece que levei uma surra. Onde eu estou?
*   **Yugi [Alegria]:** Bem-vindo de volta, amigo. Você lutou bravamente, mas agora descanse.

---

### 053: Rex Raptor Adv (O Predador Jurássico Corrompido)
*   **Local:** Ruínas Urbanas em Chamas.
*   **Avatar A:** Joey Wheeler (Recuperado/Furioso).
*   **Avatar B:** Rex Raptor Adv.

#### Pré-Duelo
**(Background: Ruínas em Chamas)**
*   **Joey [Raiva]:** Rex! Você nunca aprende? Vendeu sua alma por um pouquinho de poder extra? Isso é rasteiro até para um duelista de segunda como você!
*   **Rex Raptor [Deboche]:** Diz o cara que vai perder a alma em cinco minutos! Meus dinossauros não são mais fósseis, Joey. Eles são máquinas de matar alimentadas pelo Orichalcos! Eu vou devorar seu deck de Caos e cuspir os ossos!
*   **Joey [Confiança]:** (Para o Protagonista) O deck dele está carregado de monstros com ATK absurdo. Com o bônus do Selo, ele vai tentar te atropelar no primeiro turno. Mostra pra esse lagarto que o Caos é o predador alfa aqui!
*   **Rex Raptor [Alegria]:** Hora da extinção! DUELO!

#### Pós-Duelo (Vitória)
*   **Rex Raptor [Tristeza]:** Meus dinos... eles estavam sofrendo com aquele poder. O que aconteceu?
*   **Joey [Séria]:** Agora eles podem descansar. E você também. Chega de Orichalcos.

---

### 054: Weevil Underwood Adv (O Enxame das Trevas)
*   **Local:** Parque Infestado.
*   **Avatar A:** Joey Wheeler.
*   **Avatar B:** Weevil Underwood Adv.

#### Pré-Duelo
**(Background: Parque Infestado)**
*   **Joey [Nojo]:** Eca! O cheiro de inseto esmagado ficou ainda pior com esse poder verde. Weevil, você é uma praga que nunca morre, hein?
*   **Weevil [Alegria]:** Hehehe! O Orichalcos transformou meus pequenos insetos em monstros imparáveis! Cada vez que você destrói um, dez novos aparecem para sugar seus pontos de vida. Você vai ficar preso na minha teia eterna!
*   **Joey [Séria]:** Cuidado! Ele usa táticas de "infecção" — ele vai colocar cartas de inseto no seu deck para travar seus saques. Limpa o campo dele rápido antes que a infestação saia do controle!
*   **Weevil [Deboche]:** Zumbido de morte para você! DUELO!

#### Pós-Duelo (Vitória)
*   **Weevil [Medo]:** O poder se foi... não me machuque! Eu só queria ser forte!
*   **Joey [Deboche]:** Só sai da minha frente antes que eu pise em você. Inseto.

---

### 055: Mako Tsunami Adv (O Tsunami Negro)
*   **Local:** Docas da Cidade (Céu Verde).
*   **Avatar A:** Serenity Wheeler (Representando a Torcida).
*   **Avatar B:** Mako Tsunami Adv.

#### Pré-Duelo
**(Background: Docas - Céu Verde)**
*   **Serenity [Preocupação]:** Mako! Por favor, pare! Você sempre falou sobre a honra do mar, esse poder não tem honra nenhuma!
*   **Duke Devlin (Voz) [Séria]:** Esquece, Serenity. O Orichalcos pegou ele. [Nome do Jogador], o deck dele de monstros marinhos está furioso. As correntes estão jogando a favor dele!
*   **Mako [Raiva]:** O oceano não tem piedade dos fracos! O Selo me deu a força para conquistar qualquer abismo. Prepare-se, duelista do Caos... vou te arrastar para onde a luz não chega!
*   **Mokuba (Voz) [Raiva]:** Não deixa ele te intimidar! Mostra que o seu deck é um maremoto que ele não pode controlar!
*   **Mako [Séria]:** Maremoto de Trevas! DUELO!

#### Pós-Duelo (Vitória)
*   **Mako [Tristeza]:** O mar se acalmou. Obrigado por me libertar dessa tempestade.
*   **Serenity [Alegria]:** Mako! Você está salvo! Eu sabia que você voltaria.

---

### 056: Joey Wheeler Adv (O Irmão Possuído)
*   **Local:** Ponte de Duelo (Crepúsculo Sombrio).
*   **Avatar A:** Yugi Muto.
*   **Avatar B:** Joey Wheeler Adv.

#### Pré-Duelo
**(Background: Ponte de Duelo)**
*   **Yugi [Tristeza]:** Joey... não... Por que você aceitou esse poder? Nós somos uma equipe!
*   **Joey [Confiança]:** Equipe? Eu cansei de ser o "segundo melhor". O Orichalcos me deu a visão que eu precisava. O "Forbidden Chaos" é forte, mas o meu Dragão Negro de Olhos Vermelhos agora tem chamas infernais!
*   **Tea (Voz) [Medo]:** [Nome do Jogador], ele não está blefando. O deck dele agora foca em punir cada jogada sua com dano direto. É um duelo contra o seu melhor amigo... você consegue aguentar o peso disso?
*   **Joey [Raiva]:** Chega de conversa! Eu vou tomar sua alma e provar que sou o melhor! DUELO!

#### Pós-Duelo (Vitória)
*   **Joey [Alívio]:** Cara... eu tive um pesadelo horrível. Eu estava atacando vocês...
*   **Yugi [Alegria]:** Acabou, Joey. Você é um de nós de novo. O Orichalcos não te controla mais.

---

### 057: Mai Valentine Adv (A Rainha das Harpias das Trevas)
*   **Local:** Topo de um Arranha-Céu.
*   **Avatar A:** Joey Wheeler (Liberto).
*   **Avatar B:** Mai Valentine Adv.

#### Pré-Duelo
**(Background: Topo do Arranha-Céu)**
*   **Joey [Tristeza]:** Mai... eu acabei de sair desse pesadelo. Por favor, larga essa carta! A gente pode resolver isso de outro jeito!
*   **Mai [Deboche]:** Resolver? Joey, eu nunca me senti tão viva! A solidão não me machuca mais porque o Orichalcos é o meu único parceiro. Suas Harpias agora são caçadoras de almas.
*   **Mai [Confiança]:** [Nome do Jogador], você venceu o Joey, mas eu sou muito mais rápida. Meu deck de Harpias vai despedaçar sua estratégia antes mesmo de você sacar seu trunfo. O Caos vai cair do céu hoje!
*   **Joey [Séria]:** (Para o Protagonista) Ela está fora de controle. O deck dela é focado em destruir suas cartas mágicas e armadilhas sem parar. Você vai ter que lutar no "seco"! Vai com tudo por ela!
*   **Mai [Alegria]:** O banquete das sombras começou! DUELO!

#### Pós-Duelo (Vitória)
*   **Mai [Tristeza]:** Eu me senti tão poderosa... mas tão sozinha. O frio passou.
*   **Joey [Alegria]:** Você nunca está sozinha, Mai. Nós estamos aqui.

---

### 058: Arkana Dark (O Teatro do Horror Digital)
*   **Local:** Circo das Trevas.
*   **Avatar A:** Seto Kaiba.
*   **Avatar B:** Arkana Dark.

#### Pré-Duelo
**(Background: Circo das Trevas)**
*   **Kaiba [Raiva]:** Outro palhaço usando truques de mágica de baixo nível? Esse Orichalcos só atrai perdedores desesperados.
*   **Arkana Dark [Alegria]:** (Surgindo das sombras) Desta vez não há truques, apenas a morte! Meu Mago Negro das Trevas foi banhado no sangue do Orichalcos. O show agora é obrigatório... e o ingresso é a sua vida!
*   **Yugi (Voz) [Séria]:** Ele está usando versões "Dark" de todas as magias. Ele vai tentar banir seu deck inteiro! [Nome do Jogador], confie no equilíbrio do Caos para resistir à loucura dele!
*   **Arkana Dark [Deboche]:** Que as cortinas se fechem para você! DUELO!

#### Pós-Duelo (Vitória)
*   **Arkana Dark [Medo]:** As sombras... elas estão me puxando de volta! Não! O show não pode acabar assim!
*   **Kaiba [Séria]:** Um mágico medíocre até o fim. Desapareça.

---

### 059: Bakura Spirit (O Último Suspiro do Espírito)
*   **Local:** Cemitério de Dados e Memórias.
*   **Avatar A:** Odion (Guardião Silencioso).
*   **Avatar B:** Bakura Spirit.

#### Pré-Duelo
**(Background: Cemitério de Dados)**
*   **Odion [Séria]:** O barulho da cidade desapareceu... O Orichalcos não busca mais apenas o poder, ele busca o vazio. Bakura não é mais um duelista, é a manifestação de um espírito que deseja que nada mais exista.
*   **Bakura Spirit [Tristeza]:** (Voz ecoante) Por que lutar contra o inevitável? O Caos que você carrega e a Escuridão que eu sou... no fim, ambos levam ao mesmo lugar. O silêncio eterno. O Selo não é uma arma, é um túmulo aberto esperando por nós dois.
*   **Odion [Séria]:** (Para o Protagonista) Ele joga com o "Deck de Ocultismo". Cada carta que você envia ao cemitério é um passo em direção à sua própria condenação. Não deixe a melancolia dele apagar a chama do seu deck. Se o Caos é destruição, que seja para criar uma saída desta névoa.
*   **Bakura Spirit [Séria]:** Sinta o frio do esquecimento. DUELO.

#### Pós-Duelo (Vitória)
*   **Bakura Spirit [Neutro]:** O vazio... recua por enquanto. A luz persiste.
*   **Odion [Séria]:** O equilíbrio foi restaurado. O espírito maligno foi contido.

---

### 060: Heishin (O Sacrifício do Sumo Sacerdote - Chefe do Ato 6)
*   **Local:** Ruínas do Altar Supremo.
*   **Avatar A:** Kairo (Unidos em silêncio).
*   **Avatar B:** Heishin.

#### Pré-Duelo
**(Background: Ruínas do Altar Supremo)**
*   **Kairo [Séria]:** (Voz calma e cansada) Chegamos ao fim da Ascensão. Heishin fundiu o misticismo proibido do Egito com a corrupção do Selo. Ele não quer apenas vencer... ele quer sacrificar toda a história dos duelos para se tornar um deus do nada.
*   **Heishin [Séria]:** Olhe para este mundo... ele é frágil, feito de papel e hologramas. O Orichalcos me deu a verdade. O "Forbidden Chaos" que você protege é a chave para abrir o portão final. Eu não odeio você, duelista... eu apenas aceito que sua alma é o combustível necessário para o novo amanhecer de trevas.
*   **Yugi (Voz) [Séria]:** Não há ódio nas palavras dele, apenas uma determinação fria. Isso o torna o oponente mais perigoso que já enfrentamos.
*   **Kairo [Confiança]:** [Nome do Jogador]... deixe a melodia desta batalha guiar seus dedos. Não lute com raiva, lute com a memória de todos que o Orichalcos tentou apagar. É hora de silenciar o mestre das trevas.
*   **Heishin [Confiança]:** Que o selo se feche. Pelo fim de todas as eras... DUELO.

#### Pós-Duelo (Fim do Ato 6)
*   **Heishin [Tristeza]:** A harmonia... foi quebrada. O Caos não se deixou sacrificar... ele escolheu... viver.
*   *(O Selo de Orichalcos racha e se dissolve em pétalas de luz branca. O céu verde dá lugar a um amanhecer límpido e azul.)*
*   **Joey [Alegria]:** CONSEGUIMOS! O ar... está puro de novo! Sinto que acordei de um pesadelo horrível!
*   **Kairo [Alegria]:** O pesadelo acabou, mas ele deixou uma lição. Vocês não são mais os mesmos. Suas habilidades atingiram o ápice através do sofrimento.
*   **Kairo [Séria]:** A elite dos duelistas agora nos aguarda para um teste final. Sem selos, sem trevas... apenas o mais puro nível de estratégia. Bem-vindo ao Ato 7: O Desafio da Elite.

---

## Ato 7: O Desafio da Elite
**Contexto:** Os melhores do mundo retornaram, mais fortes do que nunca. Kaiba busca vingança com seu deck definitivo.

### 061: Rare Hunter Rematch (A Revanche dos Caçadores)
*   **Local:** Arena de Duelos Kaiba Corp (Dia).
*   **Avatar A:** Serenity Wheeler.
*   **Avatar B:** Rare Hunter Rematch.

#### Pré-Duelo
**(Background: Arena Kaiba Corp)**
*   **Serenity [Alegria]:** Eu vi como meu irmão lutou contra esses caras. Hoje, eu não vou fechar os olhos. Eu vou ver você mostrar que a estratégia deles não passa de um truque barato comparado ao seu deck!
*   **Rare Hunter [Confiança]:** Você teve sorte da última vez. Meu deck de busca de peças agora é infalível. Em poucos turnos, o proibido será invocado e sua lenda terminará aqui, diante de milhares de pessoas!
*   **Serenity [Séria]:** (Para o Protagonista) Ele vai tentar travar o jogo para comprar cartas. Seja rápido e agressivo! Não deixe ele completar o quebra-cabeça. Eu confio em você!
*   **Rare Hunter [Séria]:** A contagem regressiva começou! DUELO!

---

### 062: Rare Elite Rematch (O Despertar da Elite)
*   **Local:** Estádio Principal - Campo de Batalha.
*   **Avatar A:** Duke Devlin.
*   **Avatar B:** Rare Elite Rematch.

#### Pré-Duelo
**(Background: Estádio Principal)**
*   **Duke [Confiança]:** O nível subiu, parceiro! Se esses caras acham que podem ganhar com força bruta, eles não conhecem o estilo do Dungeon Dice Monsters aplicado às cartas. Vamos dar um show!
*   **Rare Elite [Confiança]:** Chega de amadores. Eu represento a elite dos Caçadores de Raras. Meu deck foca em controle total do cemitério. O seu "Forbidden Chaos" vai encontrar o seu mestre hoje!
*   **Duke [Deboche]:** Falar é fácil. Quero ver você lidar com o caos imprevisível desse deck aqui! Jogue os dados e veja sua sorte acabar!
*   **Rare Elite [Séria]:** Protocolo de elite ativado. DUELO!

---

### 063: Odion Rematch (A Honra do Guardião)
*   **Local:** Sala de Duelos Reais.
*   **Avatar A:** Ishizu Ishtar.
*   **Avatar B:** Odion Rematch.

#### Pré-Duelo
**(Background: Sala de Duelos Reais)**
*   **Ishizu [Alegria]:** Odion não luta mais por ordens sombrias. Ele luta pela própria honra como duelista. É um privilégio ver dois guerreiros tão dedicados se enfrentarem.
*   **Odion [Séria]:** Sua jornada foi longa e difícil. Eu testemunhei sua força no deserto e no mundo digital. Agora, sem deuses ou sombras, vamos testar apenas nossa habilidade. Meu deck de Armadilhas está pronto para o seu maior desafio!
*   **Ishizu [Séria]:** Cuidado. Odion é um mestre da paciência. Cada carta que você ativa pode ser um gatilho para a derrota. Use o instinto do Caos para prever o que está escondido!
*   **Odion [Confiança]:** Que a melhor estratégia vença. DUELO!

---

### 064: Strings Rematch (O Silêncio das Cordas)
*   **Local:** Aquário Municipal - Arena de Vidro.
*   **Avatar A:** Marik Ishtar (Redimido).
*   **Avatar B:** Strings Rematch.

#### Pré-Duelo
**(Background: Aquário Municipal)**
*   **Marik [Confiança]:** Strings era minha marionete. Hoje, ele é apenas um reflexo do poder que eu costumava controlar. Deixe-me ver como você lida com a pressão de um deck infinito sem a minha influência maligna.
*   **Strings [Séria]:** *(Nenhuma palavra é dita, apenas o disco de duelo se ativa com um brilho intenso)*
*   **Marik [Séria]:** Ele vai tentar manter o Slifer no campo e encher a mão de cartas. Se ele conseguir, o poder de ataque será imparável. Corte o fluxo de cartas dele e a vitória será sua!
*   **Strings [Séria]:** ...DUELO!

---

### 065: Bandit Keith Adv (Máquinas de Trapaça)
*   **Local:** Hangar de Aviões Kaiba Corp.
*   **Avatar A:** Joey Wheeler.
*   **Avatar B:** Bandit Keith Adv.

#### Pré-Duelo
**(Background: Hangar de Aviões)**
*   **Joey [Raiva]:** Você de novo, Keith?! Pensei que você já tinha sido expulso de todos os torneios do planeta. Dessa vez, não tem alçapão ou trapaça que te salve!
*   **Bandit Keith [Deboche]:** Ora, se não é o cachorrinho do Yugi. Escuta aqui, moleque: eu não preciso de trapaça quando tenho o deck de máquinas mais destrutivo da América. Esse seu amigo aí vai aprender que, contra o meu "Barrel Dragon", a única regra que importa é a do revólver!
*   **Joey [Confiança]:** [Nome do Jogador], ele joga com cartas de "Gamble" (Sorte) e efeitos de destruição. O deck dele é perigoso porque pode virar o jogo em um segundo. Não dê espaço para ele girar os tambores!
*   **Bandit Keith [Alegria]:** Na América, a gente chama isso de... XEQUE-MATE! DUELO!

---

### 066: Espa Roba Adv (O Olhar Cibernético)
*   **Local:** Estação de Rádio e Televisão.
*   **Avatar A:** Mai Valentine.
*   **Avatar B:** Espa Roba Adv.

#### Pré-Duelo
**(Background: Estação de Rádio)**
*   **Mai [Confiança]:** Desta vez não há irmãos escondidos nos prédios ajudando você, não é, Espa? Vamos ver o quão "psíquico" você é quando enfrenta o instinto puro.
*   **Espa Roba [Confiança]:** Minha conexão com o "Jinzo" nunca foi tão forte! Eu não preciso de truques quando posso ver através da sua estratégia. O Caos é barulhento, mas o meu silêncio tecnológico vai apagar sua voz!
*   **Mai [Séria]:** (Para o Protagonista) Cuidado com as Armadilhas! Ou melhor, com a falta delas. Ele vai tentar invocar o Jinzo para travar suas defesas. Use efeitos de monstros para passar por cima da tecnologia dele!
*   **Espa Roba [Séria]:** Frequência de vitória sintonizada. DUELO!

---

### 067: Pegasus Adv (A Ilusão do Criador)
*   **Local:** Salão de Arte Moderna.
*   **Avatar A:** Téa Gardner.
*   **Avatar B:** Pegasus Adv.

#### Pré-Duelo
**(Background: Salão de Arte Moderna)**
*   **Téa [Alegria]:** Sr. Pegasus! É estranho ver o criador do jogo desafiando a gente assim. Mas não vamos recuar, a gente aprendeu muito desde o Reino dos Duelistas!
*   **Pegasus [Alegria]:** Oh, Kaiba-boy... Digo, [Nome do Jogador]-boy! Seu deck de Caos é uma obra de arte fascinante, mas um pouco... desorganizada. Que tal se eu desse um toque de "Toon" nessa sua realidade?
*   **Téa [Medo]:** Ele vai tentar usar o "Mundo Toon" para tornar os monstros dele intocáveis! Você precisa destruir a magia de campo dele o mais rápido possível, ou vai acabar virando um desenho animado!
*   **Pegasus [Confiança]:** Preparem-se para o show mais fabuloso das suas vidas! DUELO!

---

### 068: Ishizu Adv (O Destino nas Cartas)
*   **Local:** Observatório Astronômico.
*   **Avatar A:** Odion.
*   **Avatar B:** Ishizu Adv.

#### Pré-Duelo
**(Background: Observatório Astronômico)**
*   **Odion [Séria]:** Minha senhora Ishizu... o seu colar pode não prever mais o futuro, mas seu deck ainda carrega a sabedoria de milênios.
*   **Ishizu [Confiança]:** O futuro não é mais um caminho traçado, mas uma escolha que fazemos a cada turno. [Nome do Jogador], seu deck de Caos desafia o destino. Vamos ver se você pode superar a guardiã das memórias ancestrais!
*   **Odion [Séria]:** Ela foca em manipular o cemitério e trocar os decks. Se você não tomar cuidado, ficará sem cartas antes de perceber. Mantenha o equilíbrio!
*   **Ishizu [Alegria]:** Que as estrelas guiem suas jogadas. DUELO!

---

### 069: Yugi Adv (O Rei dos Jogos no Ápice)
*   **Local:** Coliseu Kaiba Corp (Pôr do Sol).
*   **Avatar A:** Kairo.
*   **Avatar B:** Yugi Adv.

#### Pré-Duelo
**(Background: Coliseu Kaiba Corp)**
*   **Kairo [Alegria]:** Olhe para ele... Não há brechas. O Yugi atingiu o nível onde o deck e o duelista são um só. Este é o penúltimo passo para o seu "Forbidden Chaos" se tornar eterno.
*   **Kaiba (Voz) [Deboche]:** Humph! Não me diga que você vai tremer agora, [Nome do Jogador]! Se você perder para o Yugi antes de me enfrentar, eu mesmo confisco o seu deck por incompetência! Mostre que o Caos que eu ajudei a forjar é superior ao misticismo barato dele!
*   **Yugi [Confiança]:** Kaiba, você nunca entende que o verdadeiro poder vem do elo com as cartas. [Nome do Jogador], você provou ser digno de chegar até aqui. Meu deck não tem apenas magos e dragões; ele tem a confiança de todos os amigos que fizemos. Você está pronto para enfrentar o potencial máximo do Mago Negro?
*   **Kairo [Séria]:** (Para o Protagonista) Ele usa o deck "Dark Magician" mais rápido e consistente que existe. Ele vai tentar banir suas cartas no turno dele. Você precisa ser imprevisível. O Caos deve ser a resposta para a magia dele!
*   **Yugi [Confiança]:** Que o Coração das Cartas guie nosso destino. DUELO!

---

### 070: Kaiba Adv (O Imperador do Caos - Chefe do Ato 7)
*   **Local:** Topo da Torre Kaiba (Noite Estrelada).
*   **Avatar A:** Kairo.
*   **Avatar B:** Kaiba Adv.

#### Pré-Duelo
**(Background: Topo da Torre Kaiba)**
*   **Kairo [Séria]:** Este é o momento. O duelo que definirá quem é o verdadeiro mestre da era moderna. Kaiba trouxe o deck definitivo de "Blue-Eyes Alternative".
*   **Yugi (Voz) [Alegria]:** Você chegou ao topo, [Nome do Jogador]! O Kaiba pode parecer invencível com sua tecnologia e seu poder bruto, mas lembre-se: o seu "Forbidden Chaos" nasceu da sua vontade de superar limites. Eu estarei assistindo... mostre a ele a força da sua alma!
*   **Kaiba [Confiança]:** CHEGA DE DISCURSOS! Yugi, guarde seu sentimentalismo para os seus amigos. [Nome do Jogador], você é o único que sobrou no meu caminho. Eu não aceito nada menos que a perfeição absoluta. Eu vou esmagar o seu Caos com a força de um deus e provar que eu sou o único Rei neste mundo de dados e metal!
*   **Kaiba [Alegria]:** MEU DECK NÃO CONHECE A DERROTA! MEU DRAGÃO BRANCO DE OLHOS AZUIS VAI APAGAR A SUA EXISTÊNCIA!
*   **Kairo [Séria]:** (Para o Protagonista) É agora ou nunca. Ele vai invocar monstros de 3000 ATK ou mais a cada turno. Use toda a mecânica do seu projeto. Quebre o orgulho do Imperador com a pureza do seu Caos!
*   **Kaiba [Confiança]:** VOU TE MOSTRAR O VERDADEIRO SIGNIFICADO DO PODER! DUELO FINAL!

#### Pós-Duelo (Fim do Ato 7)
*   **Kaiba [Tristeza]:** ...Inacreditável. O meu Blue-Eyes... caiu diante do seu Caos. Eu admito... você não é apenas um duelista comum. Você é uma anomalia que eu não posso ignorar.
*   **Yugi (Voz) [Alegria]:** Você conseguiu, [Nome do Jogador]! Você uniu a estratégia, o poder e o coração. Hoje, uma nova lenda nasceu.
*   **Kairo [Séria]:** A vitória é sua, mas a origem do seu poder ainda é um mistério. O "Forbidden Chaos" não nasceu na Kaiba Corp, nem na Ilha de Pegasus.
*   **Ishizu (Voz) [Séria]:** Kairo tem razão. Para entender a verdadeira natureza do que você controla, você deve olhar para trás. Muito para trás.
*   **Kairo [Confiança]:** O Vale dos Reis nos chama. O passado do Faraó guarda o segredo final. Prepare-se para viajar no tempo. Bem-vindo ao Ato 8.

---

## Ato 8: O Vale dos Reis
**Contexto:** Viaje ao passado para enfrentar os Sumos Sacerdotes e seus poderes divinos.

### 071: Desert Mage Rematch (O Reinado das Areias Eternas)
*   **Local:** Entrada do Vale dos Reis (Pôr do Sol).
*   **Avatar A:** Ishizu Ishtar.
*   **Avatar B:** Desert Mage Rematch.

#### Pré-Duelo
**(Background: Entrada do Vale - Pôr do Sol)**
*   **Ishizu [Séria]:** Voltamos ao solo sagrado. Mas não se engane: este não é o mesmo mago que você enfrentou no templo. Ele agora canaliza a força de todos os que foram enterrados sob estas areias.
*   **Desert Mage [Confiança]:** O tempo é um círculo, duelista. Você provou seu valor no mundo moderno, mas será que sua alma aguenta o julgamento do deserto real? Meu deck de Zumbis Ancestrais não conhece o cansaço. Cada carta que você destrói apenas alimenta a minha necrópole!
*   **Ishizu [Séria]:** (Para o Protagonista) Ele refinou sua estratégia de "Mill" e invocação do cemitério. O deserto vai tentar engolir suas cartas. Mantenha o fluxo do seu Caos focado no banimento para que os mortos dele não possam retornar!
*   **Desert Mage [Séria]:** As areias vão julgar seu destino. DUELO!

---

### 072: Forest Mage Rematch (A Selva dos Espíritos)
*   **Local:** Oásis Místico.
*   **Avatar A:** Mai Valentine.
*   **Avatar B:** Forest Mage Rematch.

#### Pré-Duelo
**(Background: Oásis Místico)**
*   **Mai [Alegria]:** Esse lugar me dá calafrios, mas ao mesmo tempo... que energia incrível! Parece que a natureza aqui está viva e faminta.
*   **Forest Mage [Confiança]:** O ciclo da vida e da morte se acelera aqui no Vale. Minhas criaturas evoluíram. O "Forbidden Chaos" é um fogo que eu pretendo sufocar com as raízes da terra primordial. Meu enxame de Insetos e Plantas é agora uma força da natureza imparável!
*   **Mai [Séria]:** Cuidado! Ela agora usa combos de invocação especial sincronizada. Se você deixar uma única semente no campo, no próximo turno terá uma floresta inteira bloqueando seu caminho. Use o poder destrutivo do Caos para limpar o terreno!
*   **Forest Mage [Séria]:** A natureza não perdoa invasores. DUELO!

---

### 073: Mountain Mage Rematch (O Trovão dos Ancestrais)
*   **Local:** Altar dos Picos Sagrados.
*   **Avatar A:** Joey Wheeler.
*   **Avatar B:** Mountain Mage Rematch.

#### Pré-Duelo
**(Background: Altar dos Picos)**
*   **Joey [Confiança]:** Isso que é adrenalina! O cara lá em cima parece que engoliu um trovão. Vamos mostrar pra ele que o nosso estilo de luta é um raio que cai duas vezes no mesmo lugar!
*   **Mountain Mage [Confiança]:** Minhas Bestas Aladas agora cavalgam as tempestades do Egito Antigo! Seu Caos pode ser vasto, mas ele conseguirá alcançar as nuvens onde eu governo? Meu deck de Dragões e Trovões foi forjado no topo do mundo!
*   **Joey [Séria]:** (Para o Protagonista) Ele aumentou a velocidade de ataque. O bônus de campo dele agora é permanente e afeta diretamente seus pontos de vida. Não perca tempo na defesa; esse duelo é uma corrida até o topo!
*   **Mountain Mage [Alegria]:** Sinta a fúria dos céus! DUELO!

---

### 074: Meadow Mage Rematch (O Exército do Sol)
*   **Local:** Planície dos Guerreiros.
*   **Avatar A:** Odion.
*   **Avatar B:** Meadow Mage Rematch.

#### Pré-Duelo
**(Background: Planície dos Guerreiros)**
*   **Odion [Séria]:** Este é o teste de disciplina. O Meadow Mage agora comanda a guarda real do próprio Faraó. Cada jogada dele é uma formação de guerra milenar.
*   **Meadow Mage [Confiança]:** Minha estratégia não é mais apenas defesa. Meus Cavaleiros e Guerreiros agora possuem o brilho do sol. O Caos é desordem, e a desordem morre diante da minha lâmina! Meu deck de Guerreiros de Elite vai testar a resistência da sua alma!
*   **Odion [Séria]:** Ele usa cartas que aumentam o ATK baseadas no número de monstros no campo. Não deixe que ele forme uma linha de frente completa. Golpeie com precisão!
*   **Meadow Mage [Confiança]:** Pela glória do Egito! DUELO!

---

### 075: Ocean Mage Rematch (O Abismo do Nilo Eterno)
*   **Local:** Templo Inundado de Luxor.
*   **Avatar A:** Mako Tsunami (Espírito Ancestral).
*   **Avatar B:** Ocean Mage Rematch.

#### Pré-Duelo
**(Background: Templo Inundado)**
*   **Mako [Confiança]:** Sinto a força das marés antigas aqui! Este mago não controla apenas a água, ele controla a vida que flui por este vale. Mas o mar não tem dono, e o seu Caos é uma tempestade que nenhuma barragem pode segurar!
*   **Ocean Mage [Séria]:** As águas do Nilo limpam o mundo da impureza. O seu deck de Caos é uma mancha no espelho de água do Faraó. Prepare-se para ser submerso pelo meu novo exército de Serpentes Marinhas e Leviatãs Sagrados!
*   **Mako [Séria]:** (Para o Protagonista) Ele agora usa o "Ocean" como uma arma de negação. Se ele invocar o Neo-Daedalus, o campo inteiro será destruído. Ataca como um tubarão: rápido e sem aviso!
*   **Ocean Mage [Confiança]:** Afunda no esquecimento! DUELO!

---

### 076: Isis (A Guardiã do Ritual)
*   **Local:** Câmara do Destino.
*   **Avatar A:** Ishizu Ishtar.
*   **Avatar B:** Isis.

#### Pré-Duelo
**(Background: Câmara do Destino)**
*   **Ishizu [Tristeza]:** Estou diante da minha própria ancestral... A Suma Sacerdotisa Isis. Ela não usa apenas cartas, ela lê a alma do seu deck.
*   **Isis [Confiança]:** O futuro é um livro que eu já li, jovem viajante. Eu vi a sua chegada nas estrelas há cinco mil anos. O seu "Forbidden Chaos" é a peça que falta para o equilíbrio, mas apenas se a sua vontade for mais forte que o destino que eu tracei para você.
*   **Ishizu [Séria]:** O deck dela foca em banimento e controle temporal. Ela vai tentar fazer com que as tuas melhores cartas nunca cheguem à tua mão. Confia no teu instinto, pois é a única coisa que ela não pode prever!
*   **Isis [Séria]:** Que o veredito do tempo seja dado. DUELO!

---

### 077: Secmeton (O Carniceiro do Deserto)
*   **Local:** Arena de Sacrifício.
*   **Avatar A:** Seto Kaiba (Desdenhoso).
*   **Avatar B:** Secmeton.

#### Pré-Duelo
**(Background: Arena de Sacrifício)**
*   **Kaiba [Raiva]:** Outro bárbaro que acha que força bruta supera a estratégia. [Nome do Jogador], não percas tempo com as ameaças dele. Esmaga este animal e vamos seguir para o que realmente importa.
*   **Secmeton [Raiva]:** O rugido de Secmeton fará a terra tremer! Eu sou a fúria do sol! O meu deck de Bestas Guerreiras vai estraçalhar a tua defesa. Não haverá nada para enterrar quando eu terminar contigo!
*   **Kaiba [Deboche]:** Ele foca em destruir os teus monstros por batalha para ganhar ATK infinito. Patético. Usa o efeito das tuas cartas de Caos para removê-lo do jogo antes que ele consiga sequer atacar!
*   **Secmeton [Alegria]:** Sangue e areia! DUELO!

---

### 078: Martis (O Guardião da Tumba)
*   **Local:** Corredor das Almas Perdidas.
*   **Avatar A:** Kairo.
*   **Avatar B:** Martis.

#### Pré-Duelo
**(Background: Corredor das Almas)**
*   **Kairo [Séria]:** Estamos nos aproximando do santuário interior. Martis é o guardião que nunca dorme. Ele protege os segredos do Faraó com uma defesa impenetrável.
*   **Martis [Confiança]:** Nenhum vivo deve passar por aqui. Minha tumba é o fim da sua jornada. O Caos é uma energia vibrante demais para este lugar de descanso eterno. Eu vou silenciar o seu deck para sempre.
*   **Martis [Séria]:** Meu deck de "Gravekeepers" (Coveiros) vai selar o seu cemitério. Sem acesso aos seus mortos, o seu Caos perderá a força. Prepare-se para ser mumificado vivo!
*   **Kairo [Confiança]:** (Para o Protagonista) Ele usa o "Necrovalley" para impedir qualquer interação com o cemitério. Você precisa destruir essa magia de campo imediatamente, ou sua estratégia vai desmoronar!
*   **Martis [Séria]:** O selo da tumba está fechado. DUELO!

---

### 079: Kepura (O Escorpião do Deserto)
*   **Local:** Fosso dos Escorpiões.
*   **Avatar A:** Joey Wheeler.
*   **Avatar B:** Kepura.

#### Pré-Duelo
**(Background: Fosso dos Escorpiões)**
*   **Joey [Medo]:** Cara, olha o tamanho daquele ferrão! Esse tal de Kepura não parece muito amigável.
*   **Kepura [Deboche]:** O veneno do deserto é lento, mas fatal. Vocês chegaram longe, mas agora sentirão a picada da realidade. O Caos é forte, mas até os gigantes caem quando o veneno atinge o coração.
*   **Kepura [Confiança]:** Meu deck de Insetos e Bestas foca em destruir seus monstros e causar dano direto a cada turno. Vocês vão sangrar lentamente até não restar nada além de cascas vazias.
*   **Joey [Raiva]:** Ele quer ganhar pelo cansaço! [Nome do Jogador], não deixa ele te envenenar. Ataca com tudo e esmaga esse escorpião antes que ele te pique!
*   **Kepura [Alegria]:** Sinta o veneno correr nas suas veias! DUELO!

---

### 080: Anubisius (O Senhor do Submundo - Chefe do Ato 8)
*   **Local:** Portão de Anúbis.
*   **Avatar A:** Kairo.
*   **Avatar B:** Anubisius.

#### Pré-Duelo
**(Background: Portão de Anúbis)**
*   **Kairo [Medo]:** A pressão aqui é insuportável... Anubisius é o juiz final deste vale. Ele não quer apenas vencer um duelo, ele quer pesar o teu coração contra uma pena. Se fores pesado demais com o poder do Caos, serás consumido!
*   **Anubisius [Confiança]:** Mortais e as suas ambições... O Caos que carregas é uma chama que consome o seu hospedeiro. Eu sou o silêncio que vem depois da tempestade. O meu deck de "End of the World" trará o apocalipse para o teu campo de batalha!
*   **Kairo [Séria]:** (Para o Protagonista) Ele usa rituais de nível supremo. O Demise, King of Armageddon é o trunfo dele. Se ele entrar em campo, tudo o que construíste será reduzido a zero. Este é o duelo pela tua existência!
*   **Anubisius [Séria]:** O julgamento final começou. DUELO!

#### Pós-Duelo (Fim do Ato 8)
*   **Anubisius [Tristeza]:** O julgamento... foi revertido. O seu coração é puro caos, mas não há peso de maldade nele. O portão... ele se abre para você.
*   *(O Vale dos Reis começa a rachar, revelando um abismo de sombras puras)*
*   **Kairo [Medo]:** O que é isso?! Vencer Anubisius deveria ter nos libertado, mas... as sombras estão nos puxando para baixo!
*   **Bakura (Voz) [Deboche]:** (Risada maligna) Vocês acharam que o passado era o fim? O verdadeiro jogo começa agora, no labirinto da minha alma!
*   **Kairo [Séria]:** Segure-se! Estamos sendo arrastados para o Domínio das Trevas. Bem-vindo ao Ato 9: O Labirinto Final.