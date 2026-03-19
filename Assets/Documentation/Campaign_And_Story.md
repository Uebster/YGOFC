# 7. Campanha, História e Roteiro (Campaign & Story)

## Visão Geral
A campanha é o coração do jogo, estruturada como um RPG de cartas. O jogador viaja por um mapa mundi dividido em **10 Atos**, enfrentando oponentes progressivamente mais difíceis, vivenciando uma narrativa no estilo Visual Novel, ganhando cartas (Drop System) e fortalecendo seu baralho.

---

## 7.1 Lógica do Mapa e Progressão (`CampaignManager.cs`)

O `CampaignManager.cs` é o Singleton persistente que gerencia o estado global da progressão do jogador no mapa mundi e controla o acesso aos duelos.

### 1. Controle de Desbloqueio e Nós do Mapa
*   O sistema mantém a variável `maxUnlockedLevel` (int). A matemática segue a regra: Nível 1 = Oponente 1 do Ato 1. Nível 11 = Oponente 1 do Ato 2.
*   **Validação:** O método `IsLevelUnlocked(int level)` é usado pelos botões do mapa (`CampaignNode`) para saber se devem estar interativos ou bloqueados (cinza/cadeado).
*   **Tipos de Nós (`CampaignNode`):**
    *   **HOME (Acampamento):** Onde o jogador salva o jogo, edita o deck, vê a biblioteca de cartas e seus troféus.
    *   **ARENA (Free Duel):** Local para "grind". Permite enfrentar qualquer oponente já desbloqueado repetidamente para farmar cartas (Drop Rate) sem avançar a história.
    *   **ACT NODES (1-10):** Botões que levam para a tela de seleção de oponentes daquele ato.

### 2. Persistência e Cheat
*   Ao vencer um duelo na Campanha, o `GameManager` notifica o `CampaignManager`. Se o nível vencido for o maior já alcançado, `maxUnlockedLevel` é incrementado e o `SaveLoadSystem` é acionado para gravar no disco.
*   **DevMode:** Se `GameManager.devMode` estiver ativo, o `CampaignManager` libera o acesso a todos os nós do mapa simultaneamente, ignorando a verificação de nível.

### 3. Sistema de "Walkthrough" e Fluxo Contínuo
*   **Pré-Duelo (`Panel_Walkthrough`):** Antes do combate, uma tela de história carrega o texto (Visual Novel), aplica o tema musical do Ato (`DuelTheme`) e mostra a dificuldade.
*   **Pós-Duelo:** A tela de Conclusão exibe Rank e Drops. O botão `[Continue]` avança imediatamente para o próximo roteiro, criando um loop de jogabilidade fluida, enquanto `[Back]` retorna ao mapa.

---

## 7.2 Sistema de Diálogos (Visual Novel)

A cena de diálogo é composta por quatro elementos principais:
1.  **Background (Fundo):** Imagem estática do local (ex: Academia, Praça). Muda dinamicamente.
2.  **Avatar A (Esquerda):** Personagem de suporte/coadjuvante. Geralmente é o Mentor (**Yuto**), narrador ou um aliado.
3.  **Avatar B (Direita):** O Oponente do nível atual.
4.  **Caixa de Texto:** Exibe o nome do falante e a respectiva fala.

### Sistema de Expressões (Tags Faciais)
Os personagens possuem sprites fixos na Unity com variações faciais. O script dita qual emoção usar:
*   **[Neutro/Confiança]:** Estado padrão, explicação, seriedade.
*   **[Alegria]:** Vitória, elogio, risada.
*   **[Raiva]:** Provocação, frustração, dano.
*   **[Tristeza]:** Derrota, preocupação, decepção.
*   **[Medo]:** Surpresa, intimidação, choque.
*   **[Deboche]:** Sarcasmo, arrogância, desprezo.

---

## 7.3 Lista de Atos e Oponentes Oficiais

> [!WARNING] 
> **AVISO DE REFATORAÇÃO DE NOMES (PYTHON & JSON):**  
> Os personagens genéricos abaixo foram renomeados e receberam identidade própria. Ao gerar o `characters.json` ou usar os scripts de Python, certifique-se de usar os novos nomes:
> *   `001`: Novice Duelist -> **Ren**
> *   `002`: Student -> **Hiroki**
> *   `003`: Female Student -> **Nina**
> *   `004`: Intermediate -> **Katsu**
> *   `005`: Expert -> **Sensei Kenji**
> *   `027`: Female Duelist GBA -> **Maki**
> *   `028`: Grandpa GBA -> **Sr. Takagi**
> *   `031`: Novice GBC -> **Niko**
> *   `032`: Female GBC -> **Amy**
> *   `041`: Intermediate GBC -> **Sora**
> *   `042`: Expert GBC -> **Capitão Ryuu**
> *   `043`: Student GBC -> **Tony**

### Escalonamento de Drops (Pools 1.1 a 5.5)
*   **Ato 1 e 2:** Pools 1.x (Iniciais) | **Ato 3 e 4:** Pools 2.x (Médias)
*   **Ato 5 e 6:** Pools 3.x (Fortes) | **Ato 7 e 8:** Pools 4.x (Elite) | **Ato 9 e 10:** Pools 5.x (Lendárias/Deuses)

### Resumo dos 100 Duelos:
*   **Ato 1 (A Academia):** 001 Ren, 002 Hiroki, 003 Nina, 004 Katsu, 005 Sensei Kenji, 006 Mokuba, 007 Tristan, 008 Tea, 009 Grandpa, 010 **Duke Devlin**.
*   **Ato 2 (O Reino):** 011 Rex, 012 Weevil, 013 Mako, 014 Joey, 015 Mai, 016 Keith, 017 Espa, 018 Bakura, 019 Yami Bakura, 020 **Pegasus**.
*   **Ato 3 (Battle City):** 021 Rare Hunter, 022 Rare Hunter Elite, 023 Strings, 024 Arkana, 025 Odion, 026 Ishizu, 027 Maki, 028 Sr. Takagi, 029 Yugi, 030 **Kaiba**.
*   **Ato 4 (Magos Místicos):** 031 Niko, 032 Amy, 033 Desert Mage, 034 Forest Mage, 035 Mountain Mage, 036 Meadow Mage, 037 Ocean Mage, 038 Labyrinth Mage, 039 Simon, 040 **Shadi**.
*   **Ato 5 (Mundo Virtual):** 041 Sora, 042 Capitão Ryuu, 043 Tony, 044 Gansley, 045 Crump, 046 Johnson, 047 Leichter, 048 Nezbitt, 049 Noah, 050 **Gozaburo**.
*   **Ato 6 (Trevas/Orichalcos):** 051 Tea Adv, 052 Tristan Adv, 053 Rex Adv, 054 Weevil Adv, 055 Mako Adv, 056 Joey Adv, 057 Mai Adv, 058 Arkana Dark, 059 Bakura Spirit, 060 **Heishin**.
*   **Ato 7 (Elite):** 061 Rare Rematch, 062 Elite Rematch, 063 Odion Rematch, 064 Strings Rematch, 065 Keith Adv, 066 Espa Adv, 067 Pegasus Adv, 068 Ishizu Adv, 069 Yugi Adv, 070 **Kaiba Adv**.
*   **Ato 8 (Egito Antigo):** 071 Desert Rematch, 072 Forest Rematch, 073 Mountain Rematch, 074 Meadow Rematch, 075 Ocean Rematch, 076 Isis, 077 Secmeton, 078 Martis, 079 Kepura, 080 **Anubisius**.
*   **Ato 9 (Labirinto de Bakura):** 081 Labyrinth Rematch, 082 Gansley Rematch, 083 Crump Rematch, 084 Johnson Rematch, 085 Leichter Rematch, 086 Nezbitt Rematch, 087 Simon Rematch, 088 Shadi Rematch, 089 Bakura Final, 090 **Dark Bakura Final**.
*   **Ato 10 (Finais Supremas):** 091 Joey Final, 092 Mai Final, 093 Rex Final, 094 Weevil Final, 095 Mako Final, 096 Keith Final, 097 Yugi Final, 098 Pegasus Final, 099 Kaiba Final, 100 **Marik Ishtar**.

---

## 7.4 Roteiro Completo da Campanha (Visual Novel Scripts)

*(O Protagonista = O Jogador. O Mentor = Yuto)*

### Ato 1: O Início (Academia de Duelos)

#### 001: Ren (O Portão da Academia)
*   **Local:** Entrada da Academia (Dia). | **Avatar A:** Yuto | **Avatar B:** Ren
**(Pré-Duelo)**
*   **Yuto [Confiança]:** Finalmente você chegou. Estava te observando desde que cruzou os portões. Eu me chamo Yuto, e serei seus olhos e ouvidos nesta jornada.
*   **Yuto [Alegria]:** Você carrega um deck... sinto uma energia familiar nele. Aqui na Academia, não ensinamos apenas regras; buscamos o domínio sobre o "Forbidden Chaos".
*   **Ren [Deboche]:** Ei, Yuto! Já está tentando recrutar mais um perdedor para as suas teorias de "energia de cartas"?
*   **Ren [Confiança]:** Escuta aqui, calouro. Eu sou o Ren. Se você não conseguir passar nem pelo meu Deck Estrutural novo, é melhor voltar pra casa. Está pronto para ser humilhado?
*   **Yuto [Raiva]:** Ele é sempre assim. Confia demais em cartas compradas prontas.
*   **Yuto [Confiança]:** Mostre a ele, [Nome do Jogador]. Use sua estratégia para silenciar essa arrogância!
**(Pós-Duelo - Vitória)**
*   **Ren [Medo]:** M-mas o quê?! Eu perdi para um novato? Meu deck de Guerreiros era infalível! Isso... isso foi sorte!
*   **Yuto [Deboche]:** Sorte é o nome que os derrotados dão à estratégia dos vencedores. Saia da frente, Ren.

#### 002: Hiroki (O Corredor dos Veteranos)
*   **Local:** Corredor Principal. | **Avatar A:** Yuto | **Avatar B:** Hiroki
**(Pré-Duelo)**
*   **Yuto [Confiança]:** Nada mal para o começo. Mas à medida que entramos na escola, os oponentes ficam mais técnicos.
*   **Hiroki [Confiança]:** Eu vi o que você fez lá fora. Impressionante, mas eu não sou o Ren. Eu estudo as probabilidades de cada invocação... quando não estou matando aula, claro.
*   **Hiroki [Deboche]:** Dizem que você tem algo especial... Se você me vencer, eu te mostro onde ficam as salas avançadas. Caso contrário, confiscarei sua carta rara!
*   **Yuto [Confiança]:** Hiroki é calculista. Eles são perigosos porque raramente cometem erros. Não deixe que ele dite o ritmo!
*   **Hiroki [Confiança]:** Deck embaralhado. Fones no máximo. Vamos nessa!
**(Pós-Duelo - Vitória)**
*   **Hiroki [Tristeza]:** Meus cálculos... você introduziu uma variável que eu não previ. Seu jogo é caótico, mas preciso.
*   **Yuto [Confiança]:** O "Caos" não é falta de ordem, Hiroki. Vamos em frente.

#### 003: Nina (O Jardim do Conhecimento)
*   **Local:** Jardim da Academia. | **Avatar A:** Yuto | **Avatar B:** Nina
**(Pré-Duelo)**
*   **Yuto [Alegria]:** Sente esse ar? Aqui a energia é diferente.
*   **Nina [Deboche]:** Yuto, sempre poético. Mas você esqueceu de avisar que este jardim é o meu território de anotações.
*   **Nina [Confiança]:** Você deve ser o novato barulhento. Aposto que confia apenas na força bruta. Na minha estratégia, a sutileza vence o poder.
*   **Yuto [Confiança]:** A Nina utiliza armadilhas para controlar o campo. Exige pensar dois passos à frente.
*   **Nina [Deboche]:** Exatamente. Se você perder, terá que reorganizar o clube de duelos por uma semana!
*   **Yuto [Alegria]:** Não dê esse prazer a ela!
**(Pós-Duelo - Vitória)**
*   **Nina [Tristeza]:** Minhas armadilhas... você as desarmou como se pudesse ler meu tablet. Que deck é esse?
*   **Yuto [Confiança]:** Não são as anotações, Nina. É o instinto.

#### 004: Katsu (O Salão dos Desafiantes)
*   **Local:** Sala Intermediária. | **Avatar A:** Yuto | **Avatar B:** Katsu
**(Pré-Duelo)**
*   **Yuto [Neutro]:** Aqui a brincadeira acaba. Katsu é do nível Obelisk Blue e não aceita perder para novatos.
*   **Katsu [Confiança]:** Então você é o "fenômeno"? Francamente, não vejo nada de especial. Apenas mais um novato com sorte.
*   **Katsu [Deboche]:** Eu refinei meu deck de equipamentos. Não jogo por diversão, jogo para subir no ranking. Vai correr pro colo do Yuto?
*   **Yuto [Raiva]:** A arrogância dele é um problema, mas ele transforma monstros fracos em gigantes rapidamente. Se ele equipar algo, destrua-o na mesma hora!
*   **Katsu [Confiança]:** Vou te mostrar a diferença entre a ralé e a elite! DUELO!
**(Pós-Duelo - Vitória)**
*   **Katsu [Raiva]:** Isso não pode estar certo! Minha jaqueta azul... manchada por um calouro?! Eu exijo revanche!
*   **Yuto [Alegria]:** A derrota é sua melhor professora hoje, Katsu.

#### 005: Sensei Kenji (O Terraço dos Especialistas)
*   **Local:** Terraço da Academia. | **Avatar A:** Yuto | **Avatar B:** Sensei Kenji
**(Pré-Duelo)**
*   **Yuto [Neutro]:** Este terraço é para quem busca a perfeição técnica. O Sensei Kenji não luta para humilhar, mas a lição dele é duríssima.
*   **Sensei Kenji [Confiança]:** Yuto. Vejo que trouxe o novo aluno. Sabe, a paixão é boa, mas o duelo exige uma mente fria.
*   **Sensei Kenji [Deboche]:** Meu deck não depende do "coração das cartas", mas de probabilidade exata. Se você não terminar este duelo rápido, sua derrota será matematicamente inevitável.
*   **Yuto [Confiança]:** Especialistas jogam com o tempo. Eles te cansam. Mostre a ele que o Caos quebra calculadoras!
*   **Sensei Kenji [Confiança]:** A aula começou. Demonstre seu valor!
**(Pós-Duelo - Vitória)**
*   **Sensei Kenji [Medo]:** A probabilidade de você sacar aquela carta... era nula. Como você pôde...?
*   **Yuto [Alegria]:** Você confiou demais nos números, Sensei.

#### 006: Mokuba Kaiba (A Visita da Kaiba Corp)
*   **Local:** Heliponto da Academia / Área VIP. | **Avatar A:** Yuto | **Avatar B:** Mokuba Kaiba.
**(Pré-Duelo)**
*   **Yuto [Medo]:** (Sussurrando) Tome cuidado. Aquele ali é Mokuba Kaiba. Se ele está aqui, significa que o próprio Seto Kaiba está de olho no que está acontecendo nesta Academia.
*   **Mokuba [Confiança]:** Ora, ora! Então é você o novato que está causando todo esse barulho? Meu irmão me contou que alguém estava vencendo todos os especialistas da escola de uma vez só.
*   **Mokuba [Deboche]:** Eu vim ver de perto se você é realmente tudo isso ou se esses duelistas daqui é que ficaram moles. Eu tenho acesso às cartas mais raras da Kaiba Corp, sabia? Vencer você vai ser mais fácil do que ganhar um videogame novo!
*   **Yuto [Neutro]:** Mokuba pode parecer apenas uma criança, mas ele tem recursos que nenhum outro aluno tem. O deck dele é imprevisível e cheio de truques da corporação.
*   **Mokuba [Confiança]:** Se você me vencer, talvez eu deixe você entrar no banco de dados da Kaiba Corp. Mas se eu vencer... você vai ter que admitir que meu irmão é o maior duelista de todos os tempos! Preparado?
*   **Yuto [Confiança]:** É a sua chance de provar que o "Forbidden Chaos" é superior até mesmo à tecnologia de ponta. Vá em frente!
*   **Mokuba [Alegria]:** É hora do duelo! Não chora depois, hein!
**(Pós-Duelo - Vitória)**
*   **Mokuba [Raiva]:** Ei! Isso não vale! Você usou algum truque estranho! Eu vou contar tudo pro Seto... ele não vai gostar nada de saber que um novato me venceu!
*   **Yuto [Confiança]:** (Sorrindo) Diga a ele o que quiser, Mokuba. Os fatos estão no campo. Você acabou de derrotar um Kaiba.
*   **Yuto [Neutro]:** Prepare-se... depois disso, a dificuldade vai escalar para os veteranos lendários. Tristan e Tea não vão deixar isso passar em branco.

#### 007: Tristan Taylor (O Desafio do Veterano)
*   **Local:** Pátio dos Fundos (Entardecer). | **Avatar A:** Yuto | **Avatar B:** Tristan Taylor.
**(Pré-Duelo)**
*   **Yuto [Neutro]:** A notícia correu rápido. Derrotar um Kaiba colocou um alvo nas suas costas.
*   **Tristan [Raiva]:** Ei! Você aí! Então você é o cara que fez o Mokuba sair correndo chorando? O garoto pode ser mimado, mas ele faz parte da nossa turma.
*   **Tristan [Confiança]:** Eu não sou um "especialista" em cálculos como aqueles caras do terraço, mas eu tenho algo que eles não têm: garra! Meu deck de Guerreiros não recua diante de nenhum "Caos". Se você quer continuar na Academia, vai ter que passar pela minha defesa primeiro!
*   **Yuto [Confiança]:** (Para o Protagonista) Este é o seu momento. Tristan joga com o coração e com monstros de alto ataque. Eu vou ficar por aqui apenas observando... desta vez, a resposta ao desafio deve ser inteiramente sua.
*   **Tristan [Deboche]:** Pode vir com tudo! Vou te mostrar como um verdadeiro veterano duela!
**(Pós-Duelo - Vitória)**
*   **Tristan [Tristeza]:** Caramba... você é bom mesmo. Minha defesa não aguentou.
*   **Yuto [Alegria]:** Viu? Força bruta não é tudo. O equilíbrio entre ataque e estratégia é o que define um campeão.

#### 008: Téa Gardner (A Amizade e o Espírito)
*   **Local:** Praça da Amizade. | **Avatar A:** Tristan Taylor | **Avatar B:** Téa Gardner.
**(Pré-Duelo)**
*   **Tristan [Alegria]:** Cara, eu tenho que admitir... você tem estilo! Aquele seu último combo foi bruto. Deixa eu te apresentar a Téa. Ela é o coração do nosso grupo.
*   **Téa [Confiança]:** Então esse é o duelista que o Yuto está treinando? Prazer em te conhecer! O Tristan me contou sobre o duelo de vocês.
*   **Téa [Neutro]:** Mas sabe... duelo não é só sobre vencer ou perder. É sobre o que você sente pelas suas cartas. Eu uso um deck de Fadas e Magos que foca no suporte mútuo. Quero ver se o seu "Caos" tem lugar para a amizade ou se é apenas destruição!
*   **Tristan [Deboche]:** Cuidado, hein! Ela parece gentil, mas o deck dela recupera pontos de vida mais rápido do que você consegue tirar!
*   **Téa [Alegria]:** Pronto para testar seu espírito? Vamos lá!
**(Pós-Duelo - Vitória)**
*   **Téa [Alegria]:** Uau! Você joga com tanto coração! Foi um duelo lindo, mesmo eu perdendo.
*   **Tristan [Confiança]:** Ele é bom, né? Acho que ele está pronto para conhecer o Vovô.

#### 009: Grandpa Muto (A Sabedoria do Mestre)
*   **Local:** Loja de Cartas do Vovô. | **Avatar A:** Téa Gardner | **Avatar B:** Grandpa Muto.
**(Pré-Duelo)**
*   **Téa [Alegria]:** Você passou no meu teste! Consigo ver que você respeita o seu deck. Por isso, decidi te trazer ao melhor lugar da cidade: a loja do Vovô do Yugi.
*   **Grandpa [Confiança]:** Ho ho ho! Então este é o jovem de quem todos estão falando? Yuto me mandou uma mensagem dizendo que você carrega uma energia muito parecida com as relíquias que eu coleciono.
*   **Grandpa [Neutro]:** O "Forbidden Chaos" é um poder antigo, meu jovem. Antes de você enfrentar o desafio final deste Ato, eu preciso ter certeza de que você é digno de carregar esse fardo. Meu deck é cheio de segredos e cartas raras que guardo há anos. Mostre-me o seu valor!
*   **Téa [Confiança]:** O Vovô é uma lenda! Preste atenção em cada jogada dele, é uma aula de Yu-Gi-Oh!
*   **Grandpa [Alegria]:** Vamos ver se a nova geração está pronta para o que vem por aí!
**(Pós-Duelo - Vitória)**
*   **Grandpa [Alegria]:** Ho ho! Excelente! Você tem o toque de um mestre. Suas cartas confiam em você.
*   **Téa [Alegria]:** Sabia que ele conseguiria! Yuto escolheu bem.

#### 010: Duke Devlin (O Mestre dos Dados - Chefe do Ato 1)
*   **Local:** Arena Principal da Academia (Noite). | **Avatar A:** Yuto (Retorno e Despedida) | **Avatar B:** Duke Devlin.
**(Pré-Duelo)**
*   **Yuto [Neutro]:** Chegamos ao fim da sua primeira etapa. Duke Devlin é o atual campeão deste setor. Ele não usa apenas cartas; ele usa a sorte e o risco a seu favor.
*   **Duke Devlin [Deboche]:** Bem-vindo ao show principal! Eu vi seus duelos contra os amadores lá fora, mas aqui na minha arena, as regras são outras.
*   **Duke Devlin [Confiança]:** Meu estilo é visual, é arriscado, é... perfeito! Seus monstros de "Caos" não significam nada se os meus dados não permitirem que eles ataquem. Prepare-se para ser a estrela do meu próximo vídeo de derrota!
*   **Yuto [Confiança]:** [Nome do Jogador], este é o momento em que nossos caminhos se dividem. Eu te ensinei a ouvir as cartas. Agora, você deve aprender a guiar o seu próprio destino.
*   **Yuto [Alegria]:** Vença o Duke, e o portão para o Reino dos Duelistas se abrirá para você. Não olhe para trás. Confie no seu deck. Adeus por enquanto... nos veremos novamente quando o Caos despertar de verdade.
*   *(Yuto sai da tela. O Avatar A fica vazio por um segundo)*
*   **Duke Devlin [Deboche]:** Parece que seu mentor te deixou sozinho. Que pena! Vai ser mais divertido te esmagar assim. VAMOS NESSA!
**(Pós-Duelo - Fim do Ato 1)**
*   **Duke Devlin [Tristeza]:** Eu não acredito... Meus dados nunca falham assim! Você... você forçou a sorte ao seu favor? Que tipo de duelista é você?
*   **[Sistema]:** O LOGO "ACT 2: DUELIST KINGDOM" APARECE NO CENTRO.
*   **Yuto (Voz em Off):** "O primeiro selo foi quebrado. A jornada para a Ilha de Pegasus começou. Boa sorte, Duelista do Caos."

---

### Ato 2: Reino dos Duelistas
**Contexto:** O protagonista viaja para a ilha de Pegasus.

#### 011: Rex Raptor (O Desembarque na Ilha)
*   **Local:** Costa da Ilha (Dia). | **Avatar A:** Joey Wheeler | **Avatar B:** Rex Raptor.
**(Pré-Duelo)**
*   **Joey [Alegria]:** Ei, amigão! Conseguimos chegar na ilha! Mas olha só a recepção que temos... parece que os dinossauros ainda não foram extintos por aqui.
*   **Rex Raptor [Deboche]:** Ora, ora... se não é o Joey e o seu novo "guarda-costas". Vocês acham que o Reino dos Duelistas é um piquenique?
*   **Rex Raptor [Confiança]:** Este território é meu! Meus dinossauros estão famintos e o seu deck de "Caos" parece um ótimo lanche. Se vocês querem sair dessa praia vivos, vão ter que passar por cima do meu exército pré-histórico!
*   **Joey [Raiva]:** Escuta aqui, cabeça de lagarto! O meu amigo aqui acabou de chegar e já derrotou o Duke Devlin. Você não tem chance!
*   **Joey [Confiança]:** (Para o Protagonista) Vai lá! Mostra pra ele que o tamanho do monstro não importa se a estratégia for maior. Detona esses dinossauros!
*   **Rex Raptor [Confiança]:** Preparem-se para serem pisoteados! É HORA DO DUELO!
**(Pós-Duelo - Vitória)**
*   **Rex Raptor [Raiva]:** Meus dinossauros... extintos de novo?! Isso não vai ficar assim!
*   **Joey [Deboche]:** Volta pra era do gelo, Rex! A gente tem um torneio pra vencer.

#### 012: Weevil Underwood (Armadilha na Floresta)
*   **Local:** Floresta Densa. | **Avatar A:** Joey Wheeler | **Avatar B:** Weevil Underwood.
**(Pré-Duelo)**
*   **Joey [Medo]:** Que lugar bizarro... estou sentindo algo rastejando pela minha bota. Eu odeio insetos!
*   **Weevil [Deboche]:** Hi-hi-hi-hi! Vocês caíram direitinho na minha rede! Bem-vindos ao meu jardim, onde os insetos dominam e os duelistas tolos servem de alimento.
*   **Weevil [Confiança]:** Eu vi o seu duelo na praia. Muito barulhento... muita força bruta. Mas aqui na floresta, meus insetos atacam das sombras. Quando você perceber, seus pontos de vida já estarão em zero! Hi-hi-hi!
*   **Joey [Raiva]:** Esse nanico de óculos é um trapaceiro! Ele usa efeitos de veneno e cartas que travam o campo.
*   **Joey [Confiança]:** Não deixa ele te cercar! Esmaga esses bichos antes que eles se multipliquem!
*   **Weevil [Deboche]:** Vejam o poder da minha Grande Mariposa! O seu caos vai virar casulo! DUELO!
**(Pós-Duelo - Vitória)**
*   **Weevil [Tristeza]:** Minha rainha... meus insetos... esmagados! Como você escapou da minha teia?
*   **Joey [Alegria]:** Nada como um bom inseticida logo de manhã! Vamos sair dessa floresta antes que apareça mais algum bicho.

#### 013: Mako Tsunami (O Terror dos Sete Mares)
*   **Local:** Penhasco à Beira-Mar. | **Avatar A:** Mai Valentine (Mudança de Aliado) | **Avatar B:** Mako Tsunami.
**(Pré-Duelo)**
*   **Mai [Confiança]:** Joey e seus amigos... sempre metidos em confusão. Deixem que eu assumo daqui. Esse próximo duelista tem um código de honra que vocês, garotos, mal entendem.
*   **Mako [Confiança]:** Uma duelista que respeita o mar? Raro de se ver. Mas eu luto pelo sustento da minha tripulação e pela memória do meu pai. O oceano é um mestre impiedoso, e meu deck reflete a sua fúria!
*   **Mako [Neutro]:** As águas estão agitadas hoje. Elas pressagiam uma batalha épica. Mostre-me se o seu espírito é tão profundo quanto o abismo marítimo ou se você vai apenas afogar-se na minha maré!
*   **Mai [Deboche]:** Ele é dramático, mas o deck de Água dele é sólido. Ele usa a neblina para esconder seus monstros.
*   **Mai [Confiança]:** Mostre a ele que o seu "Caos" pode vaporizar até o oceano mais profundo. Estou assistindo!
*   **Mako [Alegria]:** Pelo mar e pela glória! DUELO!
**(Pós-Duelo - Vitória)**
*   **Mako [Tristeza]:** Você duela com a força de um tsunami... Minhas criaturas marinhas foram superadas. Foi um duelo honrado.
*   **Mai [Alegria]:** Nada mal, novato. Você está começando a ficar interessante. Mas guarde o fôlego... ouvi dizer que o Joey está em apuros em algum lugar da ilha. Vamos, temos que encontrá-lo!

#### 014: Joey Wheeler (A Prova de Amizade)
*   **Local:** Campo de Treino da Ilha (Dia). | **Avatar A:** Mai Valentine | **Avatar B:** Joey Wheeler.
**(Pré-Duelo)**
*   **Mai [Neutro]:** Olha só quem está ali. O Joey. Parece que ele está treinando sozinho, mas algo está errado... ele está agindo de forma estranha, como se estivesse sob algum tipo de pressão.
*   **Joey [Raiva]:** (Falando para si mesmo) Eu preciso vencer! O Seto Kaiba não vai ficar rindo da minha cara se eu conseguir as estrelas suficientes para o torneio final!
*   **Joey [Confiança]:** Ei! Você! Eu não sei quem te colocou no meu caminho, mas eu preciso dessas estrelas para avançar. Não é nada pessoal, amigão, mas eu vou ter que te derrotar aqui e agora!
*   **Mai [Deboche]:** (Para o Protagonista) Ele está perdendo a cabeça com essa obsessão por estrelas. Mostre a ele que o verdadeiro valor de um duelista não está em números, mas na qualidade do jogo. Eu vou ficar aqui apenas como espectadora desta vez.
*   **Joey [Confiança]:** Meu "Red-Eyes" está faminto por um desafio. Vamos ver se o seu deck de "Caos" aguenta o calor! DUELO!
**(Pós-Duelo - Vitória)**
*   **Joey [Alegria]:** (Rindo) Cara, que duelo! Eu quase esqueci como é bom enfrentar alguém que me obriga a dar o meu máximo. Você venceu, e minhas estrelas são suas. Mas ó... no torneio, eu não vou facilitar da próxima vez!
*   **Mai [Confiança]:** Ele é um bobão, mas tem um coração de ouro. Agora que resolvemos isso, vamos seguir. O próximo desafio é comigo, não é? Vamos testar se você é realmente digno da minha atenção.

#### 015: Mai Valentine (O Desafio da Dama)
*   **Local:** Jardim de Rosas da Ilha (Noite). | **Avatar A:** Joey Wheeler | **Avatar B:** Mai Valentine.
**(Pré-Duelo)**
*   **Joey [Confiança]:** Pronto! Agora é a vez da Mai. Ela é durona, não se deixe levar pelo charme dela, ou você vai ver as suas cartas desaparecerem num piscar de olhos!
*   **Mai [Confiança]:** Joey, você fala demais.
*   **Mai [Deboche]:** Estava ansiosa por este momento. Você derrotou o Joey, o que é um feito e tanto. Mas agora você vai enfrentar a verdadeira estratégia. O meu deck de "Harpies" é como um vento cortante: você nem verá de onde veio o ataque antes de ser derrotado.
*   **Joey [Medo]:** Ela não está brincando! As cartas de suporte dela tornam as "Harpies" uma unidade imparável.
*   **Joey [Confiança]:** (Para o Protagonista) Você consegue! Use a fraqueza dela contra ela mesma!
*   **Mai [Confiança]:** Prepare-se para voar para fora deste torneio! É HORA DO DUELO!
**(Pós-Duelo - Vitória)**
*   **Mai [Alegria]:** Impressionante... você realmente sabe como ler as intenções do oponente. Eu subestimei o seu deck de "Caos". Você provou que merece seguir em frente.
*   **Joey [Alegria]:** Isso aí! Ninguém segura o nosso time! Agora estamos a um passo de enfrentar os figurões mais perigosos desta ilha. O Bandit Keith já está nos observando de longe...

#### 016: Bandit Keith (A Emboscada das Máquinas)
*   **Local:** Entrada das Cavernas (Crepúsculo). | **Avatar A:** Joey Wheeler | **Avatar B:** Bandit Keith.
**(Pré-Duelo)**
*   **Joey [Raiva]:** Eu reconheceria esse cheiro de óleo de motor em qualquer lugar... Keith! Apareça e lute como um duelista de verdade, em vez de se esconder nas sombras!
*   **Bandit Keith [Deboche]:** Ora, se não é o pequeno Joey e o seu novo "cachorrinho" de estimação. Eu não luto por honra, garoto. Eu luto por dinheiro e poder. E as suas estrelas valem uma fortuna no mercado negro desta ilha.
*   **Bandit Keith [Confiança]:** Meu deck de Máquinas é blindado. Enquanto você tenta entender o que está acontecendo, meus canhões já vão estar apontados para os seus pontos de vida. "Forbidden Chaos"? Para mim, isso parece apenas mais sucata para o meu ferro-velho!
*   **Joey [Medo]:** Cuidado! Esse cara não joga limpo. Ele esconde cartas na manga e usa monstros que não podem ser destruídos facilmente por batalha!
*   **Joey [Raiva]:** (Para o Protagonista) Não dê chances para ele armar os combos. Detone essas máquinas antes que elas se tornem uma fortaleza!
*   **Bandit Keith [Confiança]:** Prepare-se para ser esmagado pelas engrenagens do destino! DUELO!
**(Pós-Duelo - Vitória)**
*   **Bandit Keith [Raiva]:** Trapaça! Você deve ter trapaceado! Ninguém vence minhas máquinas!
*   **Joey [Deboche]:** Aceita, Keith. Na América ou aqui, você perdeu. E sem trapaças, só talento.

#### 017: Espa Roba (A Farsa dos Poderes Psíquicos)
*   **Local:** Acampamento Abandonado. | **Avatar A:** Mai Valentine | **Avatar B:** Espa Roba.
**(Pré-Duelo)**
*   **Mai [Deboche]:** (Olhando para os lados) Dizem que há um duelista por aqui que consegue ler a mente dos oponentes. Ele afirma ter poderes extrasensoriais... mas eu só vejo um amador tentando assustar os outros.
*   **Espa Roba [Confiança]:** Eu posso sentir os seus pensamentos, "Dama das Harpias". E posso sentir o medo que emana do seu deck de "Caos", novato. Não adianta esconder suas cartas mágicas... minhas ondas cerebrais já as detectaram!
*   **Espa Roba [Deboche]:** Meus irmãos estão espalhados pela ilha, transmitindo cada movimento seu para a minha mente. Você está jogando com as cartas viradas para cima para mim! Meu Jinzo vai silenciar todas as suas armadilhas antes mesmo de você pensar em ativá-las!
*   **Mai [Raiva]:** É um truque! Ele deve estar usando binóculos ou algum sistema de comunicação. Não se deixe levar pela pressão psicológica dele.
*   **Mai [Confiança]:** (Para o Protagonista) Confie no seu instinto. Se ele diz que sabe o que você tem, mude a sua estratégia no último segundo e quebre a "visão" dele!
*   **Espa Roba [Confiança]:** A transmissão começou! Vamos ver se o seu cérebro aguenta o choque! DUELO!
**(Pós-Duelo - Vitória)**
*   **Espa Roba [Medo]:** Minhas... minhas leituras falharam?! A energia desse "Caos" é tão instável que eu não consegui prever nada! Meus irmãos... me desculpem... eu perdi.
*   **Mai [Alegria]:** Viu só? Nada de poderes psíquicos, apenas um bom e velho blefe quebrado pela força bruta.
*   **Mai [Neutro]:** Mas fique alerta. A temperatura da ilha está caindo... e o próximo duelista que nos aguarda não usa truques de rádio. Ele usa algo muito mais sombrio. Bakura está à nossa espera.

#### 018: Bakura Ryou (O Grito de Socorro)
*   **Local:** Cemitério da Ilha (Nevoeiro). | **Avatar A:** Joey Wheeler | **Avatar B:** Bakura Ryou.
**(Pré-Duelo)**
*   **Joey [Medo]:** Bakura! É você? Cara, o que você está fazendo aqui sozinho no meio desse cemitério? Você parece pálido... mais do que o normal!
*   **Bakura [Tristeza]:** Joey... [Nome do Jogador]... por favor, saiam daqui. Eu sinto que algo está para acontecer. O meu Anel do Milênio... ele está vibrando de uma forma terrível.
*   **Bakura [Medo]:** Ele quer duelar. Ele quer as vossas almas! Eu não consigo controlar... as minhas mãos estão se mexendo sozinhas! Por favor, derrotem-me rápido antes que a sombra assuma o controle total!
*   **Joey [Raiva]:** Aguenta firme, Bakura!
*   **Joey [Confiança]:** (Para o Protagonista) Ele está sendo possuído por aquele objeto estranho! Temos que vencer o duelo para quebrar o transe dele. O deck dele usa espíritos e monstros de ocultismo. Não tenha pena, lute com tudo para o salvar!
*   **Bakura [Tristeza]:** Sinto muito... aqui vou eu! DUELO!
**(Pós-Duelo - Vitória)**
*   **Bakura [Alívio]:** Obrigado... a voz parou. Eu me sinto mais leve. Desculpem por causar problemas.
*   **Joey [Confiança]:** Descansa, amigo. Nós cuidamos do resto. O Anel do Milênio não vai mais te controlar hoje.

#### 019: Yami Bakura (O Despertar do Espírito do Anel)
*   **Local:** Reino das Sombras (Vórtice Roxo). | **Avatar A:** Mai Valentine | **Avatar B:** Yami Bakura.
**(Pré-Duelo)**
*   **Mai [Medo]:** O que é isto?! O Bakura mudou completamente... o olhar dele, a voz... não é mais aquele rapaz gentil. A temperatura baixou tanto que consigo ver a minha própria respiração!
*   **Yami Bakura [Deboche]:** (Risada sinistra) O hospedeiro foi dormir. Agora, vocês estão lidando com o verdadeiro senhor do Anel do Milênio. Bem-vindos ao meu Jogo das Trevas!
*   **Yami Bakura [Confiança]:** [Nome do Jogador], o seu "Forbidden Chaos" é uma energia curiosa... mas não é nada comparado ao vazio das sombras. O meu deck de "Destiny Board" vai escrever o seu fim, letra por letra. Se não me derrotar a tempo, sua alma ficará presa neste tabuleiro para sempre!
*   **Mai [Raiva]:** Eu não acredito em fantasmas, mas sinto uma pressão enorme no peito!
*   **Mai [Neutro]:** (Para o Protagonista) Ele está tentando vencer por contagem de turnos ou efeitos de deck. Tem que ser agressivo! Quebre o tabuleiro dele antes que seja tarde demais!
*   **Yami Bakura [Deboche]:** As trevas estão famintas... Vamos ver quem será o primeiro a ser consumido! DUELO!
**(Pós-Duelo - Vitória)**
*   **Yami Bakura [Raiva]:** Maldição! O poder do Caos interferiu com as sombras... Mas não pensem que isto acabou. Eu voltarei quando menos esperarem.
*   *(Yami Bakura desaparece. Bakura Ryou volta, desmaiado no chão)*
*   **Joey [Alegria]:** Conseguimos! O Bakura está respirando normal agora. Mas olha lá para cima...
*   *(Destaque de Background: O Castelo de Pegasus surge no horizonte)*
*   **Joey [Neutro]:** O portão está aberto. Todas as estrelas que acumulamos até agora nos levaram aqui. O Pegasus está nos esperando para o banquete final. Estás pronto para o duelo mais difícil da tua vida?

#### 020: Maximillion Pegasus (O Criador e o Olho que Tudo Vê - Chefe do Ato 2)
*   **Local:** Salão Real do Castelo. | **Avatar A:** Joey Wheeler | **Avatar B:** Maximillion Pegasus.
**(Pré-Duelo)**
*   **Joey [Raiva]:** Finalmente te encontramos, Pegasus! Chega de joguinhos e de sequestrar almas! O meu amigo aqui veio pegar o que é dele e acabar com esse seu torneio maluco!
*   **Pegasus [Alegria]:** Welcome! Sejam muito bem-vindos ao meu santuário. Eu estava observando seus duelos através das câmeras... e através de algo muito mais eficiente.
*   *(Pegasus levanta o cabelo, revelando o Olho do Milênio brilhando)*
*   **Pegasus [Confiança]:** [Nome do Jogador]-boy... você trouxe uma energia fascinante para a minha ilha. O "Forbidden Chaos" é como uma mancha de tinta em um quadro perfeito. Mas você deve entender uma coisa: eu criei este jogo. Eu conheço cada carta, cada efeito... e agora, eu conheço cada pensamento seu.
*   **Mai [Medo]:** Cuidado! Dizem que ele consegue ver as cartas na sua mão antes mesmo de você pensar em jogá-las! É como duelar contra um espelho!
*   **Pegasus [Deboche]:** Exatamente, Mlle. Valentine. Por que você não se junta aos seus amigos e assiste ao nascimento de uma nova obra de arte? O meu Mundo Toon vai transformar seus monstros de caos em meras caricaturas!
*   **Joey [Confiança]:** Não escuta ele, parceiro! O coração das cartas é mais forte do que qualquer olho mágico! Acredita no seu deck e acaba com a graça desse palhaço!
*   **Pegasus [Confiança]:** It's showtime! Vamos ver se o seu Caos consegue sobreviver à minha imaginação! DUELO!
**(Pós-Duelo - Fim do Ato 2)**
*   **Pegasus [Tristeza]:** Incredible... Eu não consegui ver... a escuridão do seu deck era tão profunda que meu Olho do Milênio só via o vazio. Você... você derrotou o criador no seu próprio domínio.
*   **Joey [Alegria]:** ISSO! Conseguimos! O campeão da ilha é você! Pegasus, agora cumpra sua palavra e liberte todos!
*   **Mai [Confiança]:** Você foi incrível. Mas olhe para o céu... algo está mudando. A vitória aqui é só o começo de algo maior.
*   **[Sistema]:** O LOGO "ACT 3: BATTLE CITY" APARECE.
*   **Yuto (Voz em Off):** "A ilha foi apenas o teste de fogo. Agora, o mundo todo se tornará um campo de batalha. Prepare suas cartas, pois em Battle City, os Deuses estão à espreita."

---

### Ato 3: Batalha na Cidade
**Contexto:** O torneio mudou para as ruas de Domino City. Cuidado com os Rare Hunters.

#### 021: Rare Hunter (Ameaça nas Sombras)
*   **Local:** Beco Escuro da Cidade (Noite). | **Avatar A:** Yugi Muto | **Avatar B:** Rare Hunter.
**(Pré-Duelo)**
*   **Yugi [Confiança]:** Fico feliz que tenha aceitado o convite para o torneio de Battle City. Mas tome cuidado. Há duelistas aqui que não jogam pelas regras da Kaiba Corp. Eles buscam apenas cartas raras... a qualquer custo.
*   **Rare Hunter [Deboche]:** (Rosto coberto) Hehe... falando em cartas raras... eu sinto o cheiro de uma relíquia vindo desse seu deck de "Caos". Meu mestre, Marik, ficaria muito satisfeito em tê-la em nossa coleção.
*   **Rare Hunter [Confiança]:** Você pode ter vencido no Reino dos Duelistas, mas aqui nas ruas, a sobrevivência é para poucos. Meu deck não precisa de monstros gigantes para te esmagar... eu só preciso das cinco peças certas. Se eu completar o Exodia, sua alma e seu deck serão meus!
*   **Yugi [Séria]:** Um Caçador de Raros! Eles usam táticas de compra acelerada para invocar o Exodia. Você precisa ser rápido e agressivo antes que ele junte todas as partes!
*   **Yugi [Confiança]:** (Para o Protagonista) Não deixe o medo do Exodia te travar. O "Forbidden Chaos" é imprevisível demais para ser contido por correntes. Ataque com tudo!
*   **Rare Hunter [Deboche]:** As peças estão se movendo... o fim está próximo para você! DUELO!
**(Pós-Duelo - Vitória)**
*   **Rare Hunter [Medo]:** O mestre Marik não vai gostar disso... Exodia falhou... Minhas peças...
*   **Yugi [Séria]:** Diga ao seu mestre que estamos indo atrás dele. Battle City não pertence aos Rare Hunters.

#### 022: Rare Hunter Elite (O Caçador de Elite)
*   **Local:** Praça Central da Cidade (Dia). | **Avatar A:** Yugi Muto | **Avatar B:** Rare Hunter Elite.
**(Pré-Duelo)**
*   **Yugi [Raiva]:** Eles não param! Derrotar um deles só atraiu a atenção de alguém mais perigoso. Veja aquele homem ali... a aura dele é muito mais sombria do que a do anterior.
*   **Rare Hunter Elite [Confiança]:** Aquele fracassado não era digno de representar os Rare Hunters. Eu, por outro lado, entendo a verdadeira natureza do poder. Nós não queremos apenas suas cartas raras... queremos testar o limite desse seu "Caos" proibido.
*   **Rare Hunter Elite [Deboche]:** O meu deck é projetado para anular estratégias baseadas em efeitos especiais. Você se sente seguro com suas cartas mágicas? Veremos quanto tempo elas duram sob o meu controle. Se você perder, entregue seu localizador de Battle City e desapareça desta cidade!
*   **Yugi [Séria]:** Ele parece mais experiente. Ele vai tentar travar suas jogadas e te forçar a cometer erros por frustração.
*   **Yugi [Confiança]:** (Para o Protagonista) Confie no elo que você construiu com o seu deck. O "Caos" não pode ser controlado por quem não o entende. Vamos mostrar a ele o que um verdadeiro duelista pode fazer!
*   **Rare Hunter Elite [Confiança]:** Prepare-se para o seu julgamento. DUELO!
**(Pós-Duelo - Vitória)**
*   **Rare Hunter Elite [Medo]:** Como?! Meus bloqueios foram despedaçados! Marik não vai acreditar nisso... você é uma anomalia neste torneio!
*   **Yugi [Alegria]:** Bom trabalho! Você está limpando as ruas desses mercenários. Mas sinto que o Marik está usando alguém como marionete para se aproximar de nós.
*   **Yugi [Séria]:** Um duelista silencioso chamado Strings foi visto perto do porto. Dizem que ele carrega uma carta de poder divino... Tenha muito cuidado.

#### 023: Strings (O Boneco Silencioso)
*   **Local:** Docas da Cidade (Noite). | **Avatar A:** Yugi Muto | **Avatar B:** Strings.
**(Pré-Duelo)**
*   **Yugi [Medo]:** (Sussurrando) Ele não diz uma palavra... Olhe nos olhos dele. Estão vazios. Ele está sendo controlado mentalmente pelo Marik através de uma relíquia milenar!
*   **Strings (Marik) [Tristeza]:** Finalmente nos encontramos, portador do Caos. Strings é apenas minha marionete, mas através dele, eu vou drenar cada gota de energia do seu deck.
*   **Strings (Marik) [Confiança]:** Você acha que suas cartas de Caos são especiais? Meu deck de Slime é imortal. Ele se regenera, ele se multiplica e ele vai sufocar sua estratégia até que você não tenha mais nada para sacar. Prepare-se para ver seu "Forbidden Chaos" ser devorado pelo meu abismo infinito!
*   **Yugi [Raiva]:** Ele usa cartas de regeneração constante! Se você não destruir os monstros dele de uma vez, eles voltarão turno após turno.
*   **Yugi [Confiança]:** (Para o Protagonista) Não deixe o silêncio dele te desconcentrar. Foque no ponto cego da defesa dele. O Caos pode romper qualquer ciclo de regeneração!
*   **Strings [Séria]:** O duelo começa agora. E será o seu último.
**(Pós-Duelo - Vitória)**
*   **Strings [Silêncio]:** ... (O boneco cai de joelhos)
*   **Yugi [Tristeza]:** A conexão mental foi cortada. Ele está livre agora. Marik perdeu seu controle sobre ele.

#### 024: Arkana (O Mestre das Ilusões Sombrias)
*   **Local:** Teatro Abandonado. | **Avatar A:** Yugi Muto | **Avatar B:** Arkana.
**(Pré-Duelo)**
*   **Yugi [Raiva]:** Esse lugar... sinto uma presença maligna vinda do palco. Arkana se diz o verdadeiro mestre do Mago Negro, mas ele não tem respeito pelas suas próprias cartas!
*   **Arkana [Deboche]:** (Aparece em uma nuvem de fumaça) Sejam bem-vindos ao meu último show! Yugi Muto... e o seu novo assistente de palco. Dizem que o Caos é a força suprema, mas o que é o Caos diante da verdadeira magia das sombras?
*   **Arkana [Confiança]:** Meu Mago Negro é impiedoso. Diferente do seu, Yugi, o meu não conhece a palavra "lealdade", apenas a "vitória". Eu preparei um campo de duelo onde cada erro seu será punido com lâminas reais! Vamos ver se o seu deck de Caos consegue escapar do meu truque final!
*   **Yugi [Séria]:** Cuidado! Arkana usa versões alteradas de cartas mágicas e armadilhas letais. Ele vai tentar banir seus monstros antes mesmo deles atacarem.
*   **Yugi [Confiança]:** Mostre a ele que um verdadeiro duelista confia no seu deck, enquanto ele só confia em truques baratos. É hora de fechar as cortinas para ele!
*   **Arkana [Alegria]:** Que comece o espetáculo! DUELO!
**(Pós-Duelo - Vitória)**
*   **Arkana [Medo]:** Não! Meus magos... minha mágica perfeita... tudo despedaçado! O Caos... ele não segue as regras do meu show! Marik... me ajude!
*   **Yugi [Tristeza]:** Ele era apenas mais uma peça no jogo do Marik. O próximo passo nos levará ao coração da organização dos Rare Hunters.
*   **Yugi [Confiança]:** O caminho está livre para enfrentarmos o braço direito de Marik, Odion, e a misteriosa Ishizu Ishtar. O destino da cidade está sendo decidido agora!

#### 025: Odion (A Lealdade de Ferro)
*   **Local:** Pátio de um Templo Antigo. | **Avatar A:** Yugi Muto | **Avatar B:** Odion.
**(Pré-Duelo)**
*   **Yugi [Séria]:** Diferente dos outros Rare Hunters, este homem não exala malícia... ele exala disciplina. Odion é o protetor da família Ishtar. Ele não luta por ganância, mas por um dever que jurou cumprir até a morte.
*   **Odion [Confiança]:** Sua jornada foi impressionante até aqui, jovem duelista. Você derrotou os impuros, mas agora está diante do guardião das sombras. Meu mestre Marik ordenou que eu testasse a resistência do seu espírito.
*   **Odion [Séria]:** O meu deck é uma fortaleza de armadilhas. Cada passo que você der no campo será um convite para a sua própria destruição. O Caos é uma força explosiva, mas contra a minha paciência milenar, as explosões costumam se voltarem contra quem as criou. Mostre-me sua convicção!
*   **Yugi [Medo]:** Cuidado! Odion raramente invoca monstros de forma direta. Ele prefere cartas de armadilha que se transformam em monstros ou que causam dano direto.
*   **Yugi [Confiança]:** (Para o Protagonista) Você terá que ser cirúrgico. Se atacar sem pensar, cairá nas garras dele. Use o Caos para desestabilizar a defesa perfeita de Odion!
*   **Odion [Séria]:** Que o destino decida o vencedor. DUELO!
**(Pós-Duelo - Vitória)**
*   **Odion [Respeito]:** Você lutou com honra. Talvez haja esperança para o meu mestre através de você.
*   **Yugi [Séria]:** Nós vamos salvá-lo, Odion. Eu prometo. A escuridão não vai consumir a família Ishtar.

#### 026: Ishizu Ishtar (O Olhar do Futuro)
*   **Local:** Sala das Antiguidades (Museu). | **Avatar A:** Yugi Muto | **Avatar B:** Ishizu Ishtar.
**(Pré-Duelo)**
*   **Yugi [Séria]:** Ishizu... a portadora do Colar do Milênio. Ela possui a habilidade de prever o futuro. Duelo contra ela é como lutar contra um destino que já foi escrito.
*   **Ishizu [Confiança]:** Tudo o que aconteceu até agora foi previsto pelas sombras do Colar. Seu encontro com o Caos, suas vitórias... e até este momento. Eu vim para alertá-lo: o poder que você carrega pode ser a salvação ou a ruína deste mundo.
*   **Ishizu [Séria]:** Meu deck foca no controle do cemitério e na manipulação do tempo. Se você confia apenas no que está na sua mão, já perdeu. Eu vi o fim deste duelo, e ele termina com o seu deck vazio e suas esperanças esmagadas. Você é capaz de mudar o que já foi escrito?
*   **Yugi [Raiva]:** Não aceite o destino dela! O futuro é algo que construímos com cada carta que sacamos!
*   **Yugi [Confiança]:** (Para o Protagonista) O deck dela usa cartas como "Exchange of the Spirit" para virar o jogo contra você. Mantenha o seu cemitério sob controle e não deixe que ela preveja o seu próximo passo. Surpreenda o futuro!
*   **Ishizu [Confiança]:** Mostre-me a luz que o Colar não conseguiu enxergar. DUELO!
**(Pós-Duelo - Vitória)**
*   **Ishizu [Alegria]:** Incrível... pela primeira vez, as visões do Colar do Milênio falharam. Você criou um novo caminho. Talvez haja esperança contra a escuridão que Marik está invocando.
*   **Yugi [Alegria]:** Você conseguiu o impossível! Mas a nossa trégua termina aqui. Os próximos duelistas que restam em Battle City são veteranos que não se importam com o destino do mundo, apenas com o poder.
*   **Yugi [Séria]:** Prepare-se. O caminho para enfrentar Seto Kaiba está sendo bloqueado por duelistas vindos de outras regiões... e o próprio Vovô parece estar envolvido em um desafio estranho no setor leste.

#### 027: Maki (A Desafiante das Sombras GBA)
*   **Local:** Parque de Diversões Abandonado. | **Avatar A:** Yugi Muto | **Avatar B:** Maki.
**(Pré-Duelo)**
*   **Yugi [Séria]:** Algo está errado... essa duelista não parece estar registrada no sistema da Kaiba Corp. Sinto uma energia vinda dela que remete a duelos de uma era diferente... como se ela tivesse viajado através do tempo ou de uma memória.
*   **Maki [Confiança]:** Então você é o famoso mestre do "Forbidden Chaos"? No meu mundo, as regras são mais rígidas e o poder é medido pela raridade da alma, não apenas das cartas. Eu vim de longe para ver se esse Caos é real ou apenas um truque de luzes. Meu nome é Maki.
*   **Maki [Deboche]:** Meu deck é composto por cartas que desafiam o meta atual. Eu não sigo as modas de Battle City. Se você não conseguir acompanhar o meu ritmo, sua jornada terminará neste parque esquecido. Vamos ver se o seu Caos brilha no escuro!
*   **Yugi [Confiança]:** (Para o Protagonista) Ela joga com uma precisão cirúrgica. Os duelistas dessa "linhagem" costumam usar cartas de efeito que punem jogadores agressivos demais.
*   **Yugi [Séria]:** Mantenha a guarda alta. O "Forbidden Chaos" precisa de equilíbrio agora mais do que nunca!
*   **Maki [Confiança]:** Que a melhor estratégia vença. DUELO!
**(Pós-Duelo - Vitória)**
*   **Maki [Alegria]:** Nada mal para esta dimensão! Vou levar essa derrota como lição para o meu mundo.
*   **Yugi [Alegria]:** Foi um duelo incrível. É sempre bom ver estilos diferentes de duelo.

#### 028: Sr. Takagi (O Reencontro do Mestre GBA)
*   **Local:** Rua Lateral do Museu (Entardecer). | **Avatar A:** Yugi Muto | **Avatar B:** Sr. Takagi.
**(Pré-Duelo)**
*   **Yugi [Alegria]:** Sr. Takagi?! O que o senhor está fazendo aqui? Achei que tivesse se aposentado da Liga Clássica!
*   **Sr. Takagi [Confiança]:** Ho ho ho! Yugi, às vezes um velho veterano precisa mostrar que ainda tem alguns truques na manga! [Nome do Jogador], eu estive observando sua evolução de longe. Mas a nova geração costuma ser apressada demais.
*   **Sr. Takagi [Séria]:** Para enfrentar o que vem a seguir — Kaiba e o próprio Yugi — você precisa enfrentar a sabedoria dos clássicos. Meu deck é focado em combos de cartas que você provavelmente nunca viu em Battle City. Se você não puder vencer um mestre das antigas, você não terá chance no topo da Torre de Duelos!
*   **Yugi [Alegria]:** O Sr. Takagi está falando sério! Ele é uma lenda viva do formato clássico.
*   **Yugi [Confiança]:** (Para o Protagonista) Não facilite só porque ele é mais velho. Ele quer ver o brilho total do seu poder. Mostre a ele que a nova era superou os mestres!
*   **Sr. Takagi [Alegria]:** Prepare-se, jovem! Vamos ver se o "Forbidden Chaos" aguenta o peso da experiência! DUELO!
**(Pós-Duelo - Vitória)**
*   **Sr. Takagi [Alegria]:** Incrível! Simplesmente magnífico! Seus movimentos são fluidos como a água e destrutivos como o fogo. Você está pronto.
*   **Yugi [Séria]:** O Sr. Takagi tem razão. O caminho à frente se divide.
*   **Yugi [Confiança]:** As finais de Battle City estão começando. No topo daquele arranha-céu, o Rei dos Jogos e o Imperador da Kaiba Corp estão esperando. Eu serei o seu próximo oponente, [Nome do Jogador]. E saiba que, no campo de duelo, não haverá amizade que me impeça de dar o meu melhor!

#### 029: Yugi Muto (O Desafio do Rei dos Jogos)
*   **Local:** Topo do Dirigível da Kaiba Corp (Noite). | **Avatar A:** NENHUM (Espaço Vazio) | **Avatar B:** Yugi Muto.
**(Pré-Duelo)**
*   **Yugi [Confiança]:** Chegamos ao ponto onde as palavras não são mais necessárias. Você derrotou os Rare Hunters, superou os Ishtar e provou que o seu "Forbidden Chaos" não é uma maldição, mas uma extensão da sua alma.
*   **Yugi [Séria]:** Mas para ser o Rei dos Jogos, você precisa enfrentar o meu Mago Negro. Eu não vou lutar apenas com cartas; vou lutar com o elo que tenho com cada monstro no meu deck. Se o seu Caos for apenas destruição, ele será consumido pela minha luz. Você está pronto para o duelo que definirá sua lenda?
*   **[Sistema]:** CONEXÃO COM MENTOR INDISPONÍVEL. O DUELISTA ESTÁ POR CONTA PRÓPRIA.
*   **Yugi [Confiança]:** Não procure por ajuda nos lados. Olhe apenas para o seu deck. É hora do duelo!
**(Pós-Duelo - Vitória)**
*   **Yugi [Alegria]:** Você me superou! O título de Rei dos Jogos está em boas mãos... por enquanto!
*   **[Sistema]:** CONEXÃO COM MENTOR RESTABELECIDA. YUTO RETORNA.

#### 030: Seto Kaiba (O Imperador da Kaiba Corp - Chefe do Ato 3)
*   **Local:** Torre de Duelos - Estádio Supremo. | **Avatar A:** Yuto (Interferência/Voz nas Sombras) | **Avatar B:** Seto Kaiba.
**(Pré-Duelo)**
*   **Kaiba [Deboche]:** Então você é o lixo que o Yugi tanto elogia? "Forbidden Chaos"... que nome patético para um deck que não passa de uma falha no meu sistema. Eu construí esta cidade, eu defini as regras e eu sou o ápice da evolução dos duelos!
*   **Kaiba [Confiança]:** Não me importa o que o místico ou o destino dizem. No meu mundo, o poder é definido pelo meu Dragão Branco de Olhos Azuis! Eu vou esmagar o seu deck de "Caos" e provar que a tecnologia da Kaiba Corp é a única força absoluta neste mundo. Prepare-se para ser apagado da história!
*   *(Uma voz ecoa no fundo, o Avatar A de YUTO aparece levemente transparente/glitch)*
*   **Yuto (Voz) [Séria]:** [Nome do Jogador]... não deixe a arrogância dele te cegar. Kaiba é a personificação da ordem rígida. O seu Caos é a única coisa que ele não consegue prever ou controlar. Esta é a batalha final pela alma de Battle City. Liberte tudo!
*   **Kaiba [Raiva]:** Pare de falar com as paredes e saque suas cartas! Eu vou te mostrar o que acontece com quem desafia um Deus! DUELO!
**(Pós-Duelo - Fim do Ato 3)**
*   **Kaiba [Tristeza]:** Impossível! O meu Blue-Eyes... superado por essa... essa anomalia?! Isso não é o fim! Eu vou recalibrar tudo, eu vou encontrar uma forma de... de...
*   *(Kaiba soca o painel de controle e sai de cena)*
*   **[Sistema]:** O CÉU DA CIDADE MUDA DE AZUL PARA UM VERDE MÍSTICO.
*   **Yuto [Confiança]:** (Voltando ao normal) Você conseguiu. O império de Kaiba foi abalado. Mas olhe para o horizonte... o céu está mudando. A energia que você liberou para vencer atraiu a atenção de guardiões que não pertencem a este tempo.
*   **Yuto [Séria]:** Magos antigos e místicas do deserto estão se manifestando. Saímos da tecnologia de Battle City e estamos entrando no domínio dos Guardiões Místicos. Prepare-se, pois agora a mágica é real.

---

### Ato 4: Guardiões Místicos
**Contexto:** Para obter o poder antigo, você deve derrotar os guardiões dos elementos.

#### 031: Niko (O Despertar do Passado)
*   **Local:** Entrada do Templo Ancestral. | **Avatar A:** Yuto | **Avatar B:** Niko.
**(Pré-Duelo)**
*   **Yuto [Séria]:** Sente esse peso no ar? Não estamos mais em Battle City. A energia do seu deck de Caos nos trouxe a um plano onde o tempo flui de maneira diferente. Aqui, as regras são ancestrais.
*   **Niko [Confiança]:** Parado aí, viajante! Você pisou em solo sagrado. Eu sou Niko, o guardião dos portões externos. No meu tempo, não precisávamos de hologramas complicados para duelar!
*   **Niko [Deboche]:** Meu deck é simples, mas eficaz. Ele carrega a essência dos primeiros duelos que o mundo conheceu. Se você não consegue vencer um novato da era original, os magos lá dentro vão transformar sua alma em areia!
*   **Yuto [Confiança]:** (Para o Protagonista) Ele pode parecer simples, mas os duelistas desta era usam cartas com efeitos diretos e poderosos. É um retorno às raízes.
*   **Yuto [Alegria]:** Mostre a ele que o seu "Forbidden Chaos" é uma força que transcende as eras!
*   **Niko [Confiança]:** Prepare o seu disco de duelo antigo! É HORA DO DUELO!
**(Pós-Duelo - Vitória)**
*   **Niko [Tristeza]:** Pixel por pixel, você me desmontou. Pode passar, viajante.
*   **Yuto [Confiança]:** O passado respeita a força. Vamos em frente.

#### 032: Amy (A Guardiã das Relíquias)
*   **Local:** Sala dos Hieróglifos. | **Avatar A:** Yuto | **Avatar B:** Amy.
**(Pré-Duelo)**
*   **Yuto [Séria]:** Veja as paredes... os hieróglifos estão reagindo à sua presença. O Caos está tentando se comunicar com este lugar. Mas parece que temos companhia.
*   **Amy [Confiança]:** Impressionante. Poucos conseguem passar pelo Niko sem perder a sanidade. Eu me chamo Amy, e protejo as memórias deste templo. Para você continuar sua busca pelos Magos, terá que provar que seu coração é tão firme quanto a pedra dessas paredes.
*   **Amy [Deboche]:** Meu deck utiliza a sabedoria das cartas clássicas de suporte. Eu vou testar sua paciência e sua capacidade de adaptação. Se o seu Caos for apenas fúria descontrolada, ele será selado nestas ruínas para sempre!
*   **Yuto [Confiança]:** Ela é ágil e conhece cada armadilha escondida nestas salas.
*   **Yuto [Séria]:** (Para o Protagonista) Mantenha o foco. Este é um teste de resistência. Se você vencer, os Magos Elementais sentirão sua chegada.
*   **Amy [Confiança]:** Que os Deuses antigos julguem suas jogadas! DUELO!
**(Pós-Duelo - Vitória)**
*   **Amy [Alegria]:** Você tem a marca... o brilho nos seus olhos confirma o que as lendas diziam. O caminho está aberto.
*   **Yuto [Séria]:** O ar está ficando mais quente. O primeiro dos grandes guardiões está se manifestando. Prepare-se. O Desert Mage nos aguarda nas areias escaldantes além deste corredor.

#### 033: Desert Mage (A Sentinela das Areias)
*   **Local:** Deserto Infinito (Meio-Dia). | **Avatar A:** Yuto | **Avatar B:** Desert Mage.
**(Pré-Duelo)**
*   **Yuto [Séria]:** O deserto não é apenas areia e calor; é um teste de perseverança. O Desert Mage controla as tempestades e os monstros que se escondem sob as dunas. Ele vai tentar enterrar sua estratégia antes mesmo de você invocar seu primeiro monstro.
*   **Desert Mage [Confiança]:** Viajante, você sobreviveu ao templo, mas as areias não perdoam erros. Aqui, o "Forbidden Chaos" é apenas um grão de poeira diante da imensidão. Sinta o peso de milênios de isolamento!
*   **Desert Mage [Séria]:** Meu deck de Zumbis e Criaturas do Deserto se fortalece com o desgate do oponente. Eu não preciso te vencer rápido... eu só preciso esperar que o sol e a minha areia drenem sua vontade de lutar. Prepare-se para virar parte da paisagem!
*   **Yuto [Confiança]:** (Para o Protagonista) Ele usa cartas que reduzem o ataque e travam o campo. Use a natureza explosiva do Caos para romper essa paralisia! Não deixe o duelo se arrastar, ou a tempestade de areia dele será sua ruína.
*   **Desert Mage [Confiança]:** Que o Saara consuma o seu destino! DUELO!
**(Pós-Duelo - Vitória)**
*   **Desert Mage [Tristeza]:** A areia cobriu meus monstros... você é o novo oásis neste deserto.
*   **Yuto [Séria]:** Um a menos. Faltam cinco guardiões.

#### 034: Forest Mage (O Sussurro da Selva Antiga)
*   **Local:** Floresta Mística (Noite). | **Avatar A:** Yuto | **Avatar B:** Forest Mage.
**(Pré-Duelo)**
*   **Yuto [Alegria]:** Do calor extremo para a umidade profunda. A Forest Mage é a guardiã da vida e do crescimento. Ela vê o "Forbidden Chaos" como uma praga que precisa ser contida e podada.
*   **Forest Mage [Confiança]:** Sua presença desequilibra a harmonia da minha floresta. O Caos é instável e perigoso, e as minhas plantas e insetos detestam o que não podem consumir.
*   **Forest Mage [Deboche]:** Meu deck é um ecossistema de regeneração e armadilhas naturais. Cada carta que você ativa serve de adubo para as minhas criaturas. Você acha que pode queimar minha floresta? Tente... e verá que a natureza sempre recupera o que é dela!
*   **Yuto [Séria]:** Cuidado! Ela usa efeitos que se ativam quando monstros são destruídos ou enviados ao cemitério. É um ciclo de vida infinito. Você precisará de uma força que apague a existência das cartas dela, não apenas as destrua.
*   **Forest Mage [Confiança]:** A floresta decidiu o seu fim. DUELO!
**(Pós-Duelo - Vitória)**
*   **Forest Mage [Tristeza]:** O Caos... ele não pode ser podado. Ele cresce como uma raiz que atravessa a própria realidade. Siga em frente... os picos gelados e as águas profundas te esperam.
*   **Yuto [Confiança]:** Você está dominando os elementos. Mas os próximos guardiões são mais temperamentais. O Mountain Mage e o Meadow Mage possuem estilos de jogo opostos: um foca no poder bruto das alturas e o outro na nobreza tática dos campos abertos.

#### 035: Mountain Mage (O Trovão dos Picos Isolados)
*   **Local:** Pico da Montanha Sagrada. | **Avatar A:** Yuto | **Avatar B:** Mountain Mage.
**(Pré-Duelo)**
*   **Yuto [Séria]:** Cuidado onde pisa. Aqui, a gravidade e o clima jogam contra você. O Mountain Mage é o mestre dos ventos e das feras aladas. No topo do mundo, o seu "Forbidden Chaos" precisa ser tão sólido quanto a rocha para não ser soprado para o abismo.
*   **Mountain Mage [Confiança]:** Você escalou até aqui apenas para cair, duelista! Minhas feras dominam os céus e meu poder é tão imenso quanto a cordilheira que nos cerca. O Caos é uma força sem direção, mas o meu trovão tem um alvo certo: você!
*   **Mountain Mage [Deboche]:** Meu deck de Dragões e Bestas Aladas foca em ataques rápidos e devastadores. Se você demorar para preparar sua defesa, minhas criaturas vão te rasgar antes que você possa dizer "duelo". Sinta a pressão das alturas!
*   **Yuto [Confiança]:** (Para o Protagonista) Ele busca o bônus de campo e o poder de ataque puro. Não tente competir apenas na força bruta; use a instabilidade do Caos para desviar os raios dele e contra-atacar quando ele baixar a guarda.
*   **Mountain Mage [Alegria]:** O céu está desabando sobre você! DUELO!
**(Pós-Duelo - Vitória)**
*   **Mountain Mage [Raiva]:** Como?! Minha montanha desmoronou! O trovão foi silenciado!
*   **Yuto [Deboche]:** Tudo o que sobe, desce. Até mesmo as montanhas.

#### 036: Meadow Mage (A Estratégia dos Campos Infinitos)
*   **Local:** Prados de Ouro. | **Avatar A:** Yuto | **Avatar B:** Meadow Mage.
**(Pré-Duelo)**
*   **Yuto [Alegria]:** Finalmente, um pouco de ar puro. Mas não baixe a guarda. O Meadow Mage é o estrategista mais refinado entre os guardiões. Para ele, o duelo é uma partida de xadrez, e cada carta é um soldado em seu exército real.
*   **Meadow Mage [Confiança]:** Bem-vindo aos meus domínios. O Caos é uma energia bárbara e desordenada, desprovida de tática. Eu comando a elite dos Guerreiros e Cavaleiros que já caminharam por estas terras. Vou te mostrar que a disciplina e a formação de batalha são superiores a qualquer força proibida.
*   **Meadow Mage [Séria]:** Meu deck foca em monstros Guerreiros com alta defesa e efeitos de suporte mútuo. Eu não ataco sem um plano, e minha defesa é quase impenetrável. Você tem a nobreza necessária para enfrentar um exército de elite ou é apenas um destruidor de mundos?
*   **Yuto [Séria]:** (Para o Protagonista) Ele vai tentar cercar você com monstros que protegem uns aos outros. Você precisará encontrar uma brecha na formação dele. O "Forbidden Chaos" deve agir como um golpe decisivo no coração do exército inimigo!
*   **Meadow Mage [Confiança]:** Cavaleiros, em posição! Que a honra dite o vencedor! DUELO!
**(Pós-Duelo - Vitória)**
*   **Meadow Mage [Tristeza]:** Minha formação... quebrada pela pura imprevisibilidade do seu jogo. Talvez a ordem precise de um pouco de caos para evoluir. Você provou seu valor.
*   **Yuto [Séria]:** Você venceu os guardiões dos elementos básicos. Agora, a temperatura vai subir... e cair drasticamente. O Ocean Mage e o Labyrinth Mage estão à frente.

#### 037: Ocean Mage (O Abismo das Águas Sombrias)
*   **Local:** Templo Submerso. | **Avatar A:** Yuto | **Avatar B:** Ocean Mage.
**(Pré-Duelo)**
*   **Yuto [Séria]:** Prenda a respiração. O Ocean Mage não luta apenas com cartas, ele luta com a pressão das profundezas. Aqui, o seu "Forbidden Chaos" pode acabar diluído se você não concentrar sua energia.
*   **Ocean Mage [Confiança]:** O Caos é como uma tempestade na superfície... barulhenta, mas passageira. Aqui no fundo, apenas o silêncio e a correnteza importam. Você está pronto para ver suas estratégias afundarem?
*   **Ocean Mage [Deboche]:** Meu deck utiliza monstros que atacam diretamente através das águas e efeitos que devolvem suas cartas para a mão. Você não pode destruir o que não consegue tocar. Deixe o oceano levar suas esperanças!
*   **Yuto [Séria]:** (Para o Protagonista) Ele vai tentar "limpar" o seu campo sem destruir suas cartas, apenas removendo-as. Você precisa de invocações rápidas e monstros que tenham resistência a efeitos de retorno. Não deixe a maré dele te arrastar!
*   **Ocean Mage [Confiança]:** Sinta o frio do abismo! DUELO!
**(Pós-Duelo - Vitória)**
*   **Ocean Mage [Tristeza]:** A maré baixou... eu me rendo. O abismo não conseguiu te conter.
*   **Yuto [Séria]:** Respire fundo. O labirinto é o próximo desafio.

#### 038: Labyrinth Mage (O Enigma dos Corredores Infinitos)
*   **Local:** Labirinto de Pedra. | **Avatar A:** Yuto | **Avatar B:** Labyrinth Mage.
**(Pré-Duelo)**
*   **Yuto [Medo]:** Este lugar é uma armadilha viva. O Labyrinth Mage é o arquiteto das confusões mentais. Ele não quer apenas te vencer, ele quer que você se perca dentro do seu próprio deck.
*   **Labyrinth Mage [Deboche]:** Esquerda ou direita? Vida ou morte? Cada carta que você joga é uma curva em um labirinto que não tem saída. O Caos é desordem, e eu sou o mestre da estrutura que aprisiona a desordem!
*   **Labyrinth Mage [Confiança]:** Meu deck foca em monstros de parede com defesa absurda e cartas que mudam a posição de batalha dos seus monstros. Enquanto você bate a cabeça contra as minhas paredes, eu preparo a saída... para o cemitério!
*   **Yuto [Raiva]:** (Para o Protagonista) Ele joga na defensiva, esperando você se cansar. Use o poder de perfuração do Caos! Se ele quer construir paredes, mostre que o seu deck é a marreta que vai derrubar tudo de uma vez!
*   **Labyrinth Mage [Alegria]:** Boa sorte tentando encontrar a saída! DUELO!
**(Pós-Duelo - Vitória)**
*   **Labyrinth Mage [Tristeza]:** Você... você simplesmente atravessou as paredes? O Caos não respeita a arquitetura do meu medo... o caminho para o Sumo Sacerdote está... logo ali.
*   **Yuto [Séria]:** O ar está ficando carregado de incenso e eletricidade estática. Vencemos os guardiões, mas agora o desafio muda de nível. Estamos no limiar do santuário interno. O High Mage e o lendário Heishin estão esperando.

#### 039: Simon Muran (A Voz do Santuário)
*   **Local:** Grande Altar do Caos. | **Avatar A:** Yuto | **Avatar B:** Simon Muran.
**(Pré-Duelo)**
*   **Yuto [Medo]:** Sinta a pressão... O High Mage é o elo direto entre o mundo dos homens e o poder que você carrega. Ele não vê o seu deck como uma ferramenta, mas como uma herança que você ainda não provou merecer.
*   **Simon Muran [Confiança]:** Silêncio! Suas vitórias sobre os guardiões elementais foram apenas o primeiro passo. O Caos não é algo que se "possui", é algo que se serve. Você está pronto para sacrificar suas certezas em nome do poder verdadeiro?
*   **Simon Muran [Séria]:** Meu deck é uma combinação de todos os elementos que você enfrentou, refinados em uma estratégia de controle absoluto. Eu vou banir suas esperanças e selar seus monstros nas dobras do tempo. Se você falhar aqui, o "Forbidden Chaos" retornará ao altar e sua memória será apagada!
*   **Yuto [Séria]:** (Para o Protagonista) Ele mistura as táticas de todos os Magos anteriores. É um duelo de resistência mental. Não se deixe levar pela imponência dele; o Caos dentro de você é a única coisa que ele não pode prever totalmente.
*   **Simon Muran [Confiança]:** Que o julgamento final comece! DUELO!
**(Pós-Duelo - Vitória)**
*   **Simon Muran [Alegria]:** Magnífico. Você tem a sabedoria dos antigos. O santuário aceita sua presença.
*   **Yuto [Confiança]:** O selo está ao nosso alcance. Só resta o guardião final.

#### 040: Shadi (O Guardião do Selo Proibido - Chefe do Ato 4)
*   **Local:** Câmara Proibida. | **Avatar A:** Yuto (Voz Trêmula) | **Avatar B:** Shadi.
**(Pré-Duelo)**
*   **Yuto [Medo]:** Shadi... o arquiteto da queda do Egito. Ele é o responsável por selar o poder do Caos há milênios. Se ele vencer, o ciclo se repetirá e o mundo entrará em trevas eternas.
*   **Shadi [Deboche]:** Patético. Milênios se passaram e a humanidade ainda envia crianças para tentar domar o indomável. Eu conheço a origem desse Caos melhor do que qualquer um. Eu fui aquele que o trancou, e serei aquele que o destruirá em suas mãos!
*   **Shadi [Confiança]:** Meu deck não conhece limites de raridade ou custo. Eu invoco monstros das estrelas e utilizo magias que desafiam a própria existência das suas cartas. Você é apenas um erro na minha história perfeita. Prepare-se para ser consumido pelo verdadeiro vazio!
*   **Yuto [Raiva]:** (Para o Protagonista) Ignore a aura dele! Shadi joga com cartas proibidas e de altíssimo poder de ataque. Você terá que ser mais rápido do que a própria luz para vencê-lo. É tudo ou nada... pela sua alma e pelo futuro do Caos!
*   **Shadi [Alegria]:** Desapareça na escuridão eterna! DUELO!
**(Pós-Duelo - Fim do Ato 4)**
*   **Shadi [Tristeza]:** Não... o selo... ele se quebrou! O Caos... ele escolheu você em vez de mim?! Maldição... a luz está voltando...
*   *(Shadi se dissolve em partículas de luz violeta)*
*   **[Sistema]:** O TEMPLO EGÍPCIO COMEÇA A DESMORONAR E RECONSTRUIR-SE EM UMA ESTRUTURA MODERNA E BRANCA.
*   **Yuto [Alegria]:** Você conseguiu o impossível. Shadi foi derrotado e o selo do "Forbidden Chaos" foi finalmente liberado por completo. Mas veja... o templo está mudando.
*   **Yuto [Séria]:** As sombras do passado deram lugar a um brilho artificial. Saímos da era dos mistérios egípcios e estamos sendo transportados para uma realidade simulada.
*   **Yuto [Confiança]:** O Ato 5 nos aguarda. Onde a lógica domina e seus inimigos controlam o próprio sistema. Bem-vindo ao Pesadelo Virtual.

---

### Ato 5: Pesadelo Virtual
**Contexto:** Você está preso no ciberespaço. Os Big Five e a família Kaiba digital querem seu corpo para escapar.

#### 041: Sora (A Primeira Barreira Digital)
*   **Local:** Cidade Cibernética Abandonada. | **Avatar A:** Joey Wheeler | **Avatar B:** Sora.
**(Pré-Duelo)**
*   **Joey [Medo]:** Que lugar esquisito! Onde é que o Kaiba nos meteu dessa vez? Parece que entrei dentro de um videogame de bolso!
*   **Sora [Confiança]:** Vocês são os novos convidados do Mestre Noah. Eu sou Sora, uma rotina de segurança intermediária. Se não conseguirem passar por mim, como esperam sobreviver ao julgamento dos Big Five?
*   **Joey [Raiva]:** Big Five? Aqueles velhotes da Kaiba Corp? [Nome do Jogador], esse cara, o Sora, está bloqueando o caminho. O deck dele tem um nível de dificuldade maior do que os anteriores. Não facilita!
*   **Sora [Séria]:** Iniciando protocolo de teste. DUELO!
**(Pós-Duelo - Vitória)**
*   **Sora [Tristeza]:** Erro de sistema... Derrota registrada. Reiniciando...
*   **Joey [Alegria]:** Game Over pra você! Vamos nessa!

#### 042: Capitão Ryuu (O Código Especialista)
*   **Local:** Floresta de Pixels. | **Avatar A:** Mai Valentine | **Avatar B:** Capitão Ryuu.
**(Pré-Duelo)**
*   **Mai [Confiança]:** Noah está tentando nos cansar com esses programas de treinamento. Mas este aqui, o Capitão Ryuu... ele parece ter uma programação muito mais refinada.
*   **Capitão Ryuu [Confiança]:** Minhas jogadas são calculadas para a vitória absoluta. Eu não sou um mero programa, sou o Capitão Ryuu, o ápice da estratégia portátil! Seu "Forbidden Chaos" é apenas uma variável que eu vou aprender a anular em três turnos.
*   **Mai [Deboche]:** Três turnos? Ele é convencido! [Nome do Jogador], ele joga como um profissional. Fique atento aos combos de cartas mágicas que ele usa para acelerar o deck.
*   **Capitão Ryuu [Séria]:** Executando estratégia mestre. DUELO!
**(Pós-Duelo - Vitória)**
*   **Capitão Ryuu [Raiva]:** Impossível! Meus algoritmos eram perfeitos! Onde foi que eu errei?
*   **Mai [Deboche]:** A perfeição é chata. O Caos é muito mais divertido.

#### 043: Tony (O Estudante do Sistema)
*   **Local:** Biblioteca de Dados Virtual. | **Avatar A:** Yuto (Recuperando dados) | **Avatar B:** Tony.
**(Pré-Duelo)**
*   **Yuto [Séria]:** Estou tentando rastrear a saída, mas o sistema é um labirinto. Este rapaz aqui, o Tony... ele está analisando nossos duelos passados para criar o deck perfeito contra nós!
*   **Tony [Alegria]:** Eu li tudo sobre você! Suas vitórias na Ilha, em Battle City... eu estudei cada fraqueza do seu deck de Caos. Agora, vou usar esse conhecimento para ganhar meu lugar ao lado de Noah!
*   **Yuto [Confiança]:** Ele pode ter os dados, mas ele não tem o seu instinto! Mostre a ele que a teoria é bem diferente da prática no campo de batalha.
*   **Tony [Confiança]:** Aula prática iniciada! DUELO!
**(Pós-Duelo - Vitória)**
*   **Tony [Tristeza]:** Meus dados... estavam incompletos? Como você previu minha jogada?
*   **Yuto [Séria]:** A experiência real supera qualquer simulação. Volte para os livros.

#### 044: Gansley (O Retorno dos Big Five - Negócios Mortais)
*   **Local:** Sala de Reuniões Virtual. | **Avatar A:** Seto Kaiba | **Avatar B:** Gansley.
**(Pré-Duelo)**
*   **Kaiba [Raiva]:** Gansley! Eu deveria ter imaginado que você estaria por trás dessa palhaçada digital. Você falhou em me derrubar no mundo real e vai falhar aqui também!
*   **Gansley [Confiança]:** Seto... sempre tão arrogante. Mas aqui no mundo virtual, as regras mudaram. Eu sou o Deck Master! E meu objetivo é simples: tomar o corpo deste duelista e voltar para a Kaiba Corp para te tirar do trono!
*   **Kaiba [Deboche]:** Você não conseguiria gerenciar uma banca de jornal, quanto mais o meu corpo. [Nome do Jogador], esmague esse verme. Ele usa monstros guerreiros e táticas defensivas de covarde. Mostre a ele como um verdadeiro duelista luta!
*   **Gansley [Confiança]:** Vou ensinar uma lição de economia que você nunca esquecerá: a sua derrota é o meu lucro! DUELO!
**(Pós-Duelo - Vitória)**
*   **Gansley [Medo]:** Minhas ações... despencaram! Estou falido!
*   **Kaiba [Deboche]:** Você está demitido, Gansley. De novo. Saia da minha frente.

#### 045: Crump (O Pesadelo Gelado)
*   **Local:** Oceano Congelado. | **Avatar A:** Tea Gardner | **Avatar B:** Crump.
**(Pré-Duelo)**
*   **Tea [Medo]:** Está muito frio aqui! E quem é aquele pinguim gigante vindo em nossa direção?
*   **Crump [Alegria]:** Pinguim? Eu sou Crump, o antigo contador da Kaiba Corp! E neste mundo, eu sou o mestre dos pinguins! Eu sempre quis um corpo jovem e bonito como o seu, minha querida!
*   **Tea [Raiva]:** Que nojo! [Nome do Jogador], por favor, mande esse pinguim de volta para o zoológico!
*   **Crump [Confiança]:** Meu deck de Pesadelo Pinguim vai congelar suas cartas e devolvê-las para a sua mão. Prepare-se para tremer! DUELO!
**(Pós-Duelo - Vitória)**
*   **Crump [Tristeza]:** Não é justo! Eu queria ser jovem de novo! O gelo derreteu...
*   **Tea [Alívio]:** Ainda bem que acabou. Pinguins me dão arrepios agora.

#### 046: Johnson (A Justiça Distorcida)
*   **Local:** Tribunal Digital. | **Avatar A:** Joey Wheeler | **Avatar B:** Johnson.
**(Pré-Duelo)**
*   **Joey [Confiança]:** Olha só, um juiz! Vai nos dar uma multa por excesso de velocidade?
*   **Johnson [Séria]:** Silêncio no tribunal! Eu sou Johnson, o consultor jurídico. E vocês são culpados de invadir nosso domínio. A sentença é a perda de seus corpos!
*   **Johnson [Confiança]:** Meu deck de Juiz vai ditar as regras deste duelo. Eu posso manipular a sorte e garantir que a justiça... a MINHA justiça... prevaleça!
*   **Joey [Raiva]:** Justiça de araque! [Nome do Jogador], mostra pra ele que a única lei aqui é a do mais forte! DUELO!
**(Pós-Duelo - Vitória)**
*   **Johnson [Raiva]:** Objeção! Isso foi sorte! Eu exijo um novo julgamento!
*   **Joey [Deboche]:** Caso encerrado, meritíssimo! A sentença é a sua derrota.

#### 047: Leichter (O Jogo de Poder)
*   **Local:** Cassino Virtual. | **Avatar A:** Seto Kaiba | **Avatar B:** Leichter.
**(Pré-Duelo)**
*   **Kaiba [Deboche]:** Leichter... o homem que tentou comprar minha empresa pelas minhas costas. Ainda jogando sujo?
*   **Leichter [Confiança]:** Seto, meu jovem. O mundo é um jogo de poder, e eu tenho as melhores cartas. Com o poder do Jinzo, eu vou silenciar todas as suas armadilhas patéticas.
*   **Kaiba [Raiva]:** Jinzo? Uma carta poderosa, mas nas mãos de um amador, é inútil. [Nome do Jogador], destrua-o. Ele não merece usar o nome da Kaiba Corp!
*   **Leichter [Alegria]:** Vamos ver quem vai à falência primeiro! DUELO!
**(Pós-Duelo - Vitória)**
*   **Leichter [Tristeza]:** A banca quebrou... perdi tudo.
*   **Kaiba [Confiança]:** Nunca aposte contra mim ou meus aliados. O jogo acabou para você.

#### 048: Nezbitt (O Gigante de Aço)
*   **Local:** Usina de Energia Virtual. | **Avatar A:** Joey Wheeler | **Avatar B:** Nezbitt.
**(Pré-Duelo)**
*   **Joey [Raiva]:** Este cara levou a sério o papo de "homem-máquina"! Ele não quer apenas seu corpo, [Nome do Jogador], ele quer transformar tudo isso aqui em um ferro-velho!
*   **Nezbitt [Confiança]:** A carne é fraca e o código é eterno! Eu sou Nezbitt, o mestre da engenharia da Kaiba Corp. Eu vou esmagar suas cartas com o peso do meu exército mecânico. Quando eu terminar, o seu "Forbidden Chaos" será apenas sucata processada!
*   **Joey [Confiança]:** (Para o Protagonista) Não se assusta com o tamanho dele! Máquinas grandes fazem muito barulho, mas o Caos é mais rápido. Destrói o núcleo de energia dele antes que ele invoque o Cavaleiro Perfeito!
*   **Nezbitt [Séria]:** Poder total às máquinas! DUELO!
**(Pós-Duelo - Vitória)**
*   **Nezbitt [Raiva]:** Sistema... crítico... Desligando. A humanidade... é ilógica.
*   **Joey [Alegria]:** Virou sucata! Mais um pro ferro-velho.

#### 049: Noah Kaiba (O Herdeiro Rejeitado)
*   **Local:** Jardim do Éden Digital. | **Avatar A:** Seto Kaiba | **Avatar B:** Noah Kaiba.
**(Pré-Duelo)**
*   **Kaiba [Raiva]:** Noah... uma memória esquecida tentando brincar de Deus. Você criou este mundo porque não pôde conquistar o real. Mas este duelista aqui é a prova de que o seu "Mundo Perfeito" é apenas uma ilusão frágil!
*   **Noah [Alegria]:** Seto, você sempre foi tão limitado. Eu não sou uma memória, eu sou a evolução! E você, [Nome do Jogador], é apenas um convidado indesejado que se tornou perigoso demais. Meu deck da Arca de Noé vai purificar este mundo, começando pela sua derrota!
*   **Kaiba [Deboche]:** Purificar? Você vai é ser deletado. [Nome do Jogador], Noah usa um deck de Monstros Espíritos que retornam para a mão. É um estilo irritante e evasivo. Acerte-o com tudo antes que ele consiga se esconder atrás dos efeitos dele!
*   **Noah [Séria]:** O dilúvio digital começou. DUELO!
**(Pós-Duelo - Vitória)**
*   **Noah [Tristeza]:** Pai! Por que você escolheu eles?! Eu criei tudo isso para você!
*   **Kaiba [Séria]:** Porque eles lutam pelo futuro, não pelo passado. Adeus, Noah.

#### 050: Gozaburo Kaiba (O Espectro do Passado - Chefe do Ato 5)
*   **Local:** O Abismo do Código Fonte. | **Avatar A:** Yuto (A Convergência Final) | **Avatar B:** Gozaburo Kaiba.
**(Pré-Duelo)**
*   **Yuto [Séria]:** Este é o fim da linha. Gozaburo Kaiba... o homem que deu início a tudo isso. Ele corrompeu o próprio sistema para sobreviver como um vírus de ódio puro. Se perdermos aqui, o Mundo Virtual colapsa com todos nós dentro!
*   **Gozaburo [Confiança]:** Eu construí o império Kaiba com mãos de ferro, e vou reconstruí-lo sobre as cinzas deste seu duelo insignificante! O "Forbidden Chaos" foi uma criação que fugiu ao meu controle, mas Exodia... o Exodia Necross é a minha vontade manifestada!
*   **Yuto [Confiança]:** (Para o Protagonista) [Nome do Jogador], ele trouxe a arma proibida! O Exodia Necross é quase indestrutível enquanto as peças estiverem no cemitério dele. Esta é a batalha final pelo destino da nossa realidade. Use cada gota de poder do seu deck. É hora de mostrar ao passado que o futuro pertence ao Caos!
*   **Gozaburo [Alegria]:** Ajoelhe-se diante do verdadeiro imperador! DUELO FINAL!
**(Pós-Duelo - Final Épico)**
*   **Gozaburo [Tristeza/Medo]:** NÃO! O Exodia... superado?! O sistema está se desintegrando... Eu... eu não posso ser apagado novamente!
*   *(O cenário explode em luz branca e códigos verdes)*
*   **Yuto [Alegria]:** VOCÊ CONSEGUIU! O vínculo de Gozaburo com o sistema foi cortado! Estamos voltando para o mundo real, e o "Forbidden Chaos" está finalmente estabilizado no seu deck.
*   *(Cena final: O protagonista acorda na frente do computador)*
*   **Yuto (Voz em Off):** "Sua lenda apenas começou. O mundo dos duelos nunca mais será o mesmo."

---

### Ato 6: Ascensão das Trevas
**Contexto:** Heishin despertou e corrompeu os duelistas com o Selo de Orichalcos. Enfrente suas versões sombrias.

#### 051: Tea Gardner Adv (O Lado Sombrio da Amizade)
*   **Local:** Praça da Cidade sob o Eclipse. | **Avatar A:** Yugi Muto | **Avatar B:** Tea Gardner Adv.
**(Pré-Duelo)**
*   **Yugi [Medo]:** Téa! O que aconteceu com você? Esse poder... ele está drenando a bondade do seu coração! [Nome do Jogador], precisamos trazê-la de volta, mas tome cuidado: o Orichalcos aumenta a fúria de qualquer deck!
*   **Tea [Confiança]:** Amizade? Cartas de fadas bonitinhas? Isso era para os fracos. O Orichalcos me mostrou que a única forma de proteger quem eu amo é esmagando quem se opõe a nós. Meu deck de Contra-Fadas agora tem o poder de silenciar qualquer jogada sua!
*   **Yugi [Séria]:** O deck dela agora foca em anular suas cartas mágicas e armadilhas. Ela vai tentar te travar completamente enquanto o Selo fortalece os monstros dela. Não deixe a culpa te impedir de duelar com tudo!
*   **Tea [Séria]:** O selo foi ativado. Não há escapatória! DUELO!
**(Pós-Duelo - Vitória)**
*   **Tea [Tristeza]:** (O selo desaparece) O que... o que eu fiz? Minha cabeça dói...
*   **Yugi [Alegria]:** Você voltou, Téa! Está tudo bem agora. O pesadelo acabou.

#### 052: Tristan Taylor Adv (A Fúria do Protetor)
*   **Local:** Beco Escuro com Runas Verdes. | **Avatar A:** Yugi Muto | **Avatar B:** Tristan Taylor Adv.
**(Pré-Duelo)**
*   **Yugi [Tristeza]:** Até o Tristan... O Orichalcos está se espalhando como uma praga. Ele está tentando proteger a Téa, mas esse poder está transformando sua lealdade em violência pura!
*   **Tristan [Raiva]:** Saiam da frente! Vocês não entendem o poder que estamos recebendo. Chega de ser o cara que só assiste das arquibancadas. Com este selo, meu deck de Guerreiros vai mostrar quem é que manda nas ruas de Battle City!
*   **Tristan [Confiança]:** [Nome do Jogador], você se acha especial com seu Caos? Vamos ver como ele se sai contra a força bruta de um guerreiro que não tem mais nada a perder!
*   **Yugi [Confiança]:** (Para o Protagonista) Ele está usando monstros de alto nível e cartas de equipamento sombrias. O Orichalcos dá a ele uma linha extra de defesa. Você precisa quebrar o escudo dele para chegar ao coração do duelo!
*   **Tristan [Séria]:** Pelo poder do Selo... DUELO!
**(Pós-Duelo - Vitória)**
*   **Tristan [Alívio]:** Minha cabeça... parece que levei uma surra. Onde eu estou?
*   **Yugi [Alegria]:** Bem-vindo de volta, amigo. Você lutou bravamente, mas agora descanse.

#### 053: Rex Raptor Adv (O Predador Jurássico Corrompido)
*   **Local:** Ruínas Urbanas em Chamas. | **Avatar A:** Joey Wheeler | **Avatar B:** Rex Raptor Adv.
**(Pré-Duelo)**
*   **Joey [Raiva]:** Rex! Você nunca aprende? Vendeu sua alma por um pouquinho de poder extra? Isso é rasteiro até para um duelista de segunda como você!
*   **Rex Raptor [Deboche]:** Diz o cara que vai perder a alma em cinco minutos! Meus dinossauros não são mais fósseis, Joey. Eles são máquinas de matar alimentadas pelo Orichalcos! Eu vou devorar seu deck de Caos e cuspir os ossos!
*   **Joey [Confiança]:** (Para o Protagonista) O deck dele está carregado de monstros com ATK absurdo. Com o bônus do Selo, ele vai tentar te atropelar no primeiro turno. Mostra pra esse lagarto que o Caos é o predador alfa aqui!
*   **Rex Raptor [Alegria]:** Hora da extinção! DUELO!
**(Pós-Duelo - Vitória)**
*   **Rex Raptor [Tristeza]:** Meus dinos... eles estavam sofrendo com aquele poder. O que aconteceu?
*   **Joey [Séria]:** Agora eles podem descansar. E você também. Chega de Orichalcos.

#### 054: Weevil Underwood Adv (O Enxame das Trevas)
*   **Local:** Parque Infestado. | **Avatar A:** Joey Wheeler | **Avatar B:** Weevil Underwood Adv.
**(Pré-Duelo)**
*   **Joey [Nojo]:** Eca! O cheiro de inseto esmagado ficou ainda pior com esse poder verde. Weevil, você é uma praga que nunca morre, hein?
*   **Weevil [Alegria]:** Hehehe! O Orichalcos transformou meus pequenos insetos em monstros imparáveis! Cada vez que você destrói um, dez novos aparecem para sugar seus pontos de vida. Você vai ficar preso na minha teia eterna!
*   **Joey [Séria]:** Cuidado! Ele usa táticas de "infecção" — ele vai colocar cartas de inseto no seu deck para travar seus saques. Limpa o campo dele rápido antes que a infestação saia do controle!
*   **Weevil [Deboche]:** Zumbido de morte para você! DUELO!
**(Pós-Duelo - Vitória)**
*   **Weevil [Medo]:** O poder se foi... não me machuque! Eu só queria ser forte!
*   **Joey [Deboche]:** Só sai da minha frente antes que eu pise em você. Inseto.

#### 055: Mako Tsunami Adv (O Tsunami Negro)
*   **Local:** Docas da Cidade (Céu Verde). | **Avatar A:** Serenity Wheeler | **Avatar B:** Mako Tsunami Adv.
**(Pré-Duelo)**
*   **Serenity [Preocupação]:** Mako! Por favor, pare! Você sempre falou sobre a honra do mar, esse poder não tem honra nenhuma!
*   **Duke Devlin (Voz) [Séria]:** Esquece, Serenity. O Orichalcos pegou ele. [Nome do Jogador], o deck dele de monstros marinhos está furioso. As correntes estão jogando a favor dele!
*   **Mako [Raiva]:** O oceano não tem piedade dos fracos! O Selo me deu a força para conquistar qualquer abismo. Prepare-se, duelista do Caos... vou te arrastar para onde a luz não chega!
*   **Mokuba (Voz) [Raiva]:** Não deixa ele te intimidar! Mostra que o seu deck é um maremoto que ele não pode controlar!
*   **Mako [Séria]:** Maremoto de Trevas! DUELO!
**(Pós-Duelo - Vitória)**
*   **Mako [Tristeza]:** O mar se acalmou. Obrigado por me libertar dessa tempestade.
*   **Serenity [Alegria]:** Mako! Você está salvo! Eu sabia que você voltaria.

#### 056: Joey Wheeler Adv (O Irmão Possuído)
*   **Local:** Ponte de Duelo (Crepúsculo Sombrio). | **Avatar A:** Yugi Muto | **Avatar B:** Joey Wheeler Adv.
**(Pré-Duelo)**
*   **Yugi [Tristeza]:** Joey... não... Por que você aceitou esse poder? Nós somos uma equipe!
*   **Joey [Confiança]:** Equipe? Eu cansei de ser o "segundo melhor". O Orichalcos me deu a visão que eu precisava. O "Forbidden Chaos" é forte, mas o meu Dragão Negro de Olhos Vermelhos agora tem chamas infernais!
*   **Tea (Voz) [Medo]:** [Nome do Jogador], ele não está blefando. O deck dele agora foca em punir cada jogada sua com dano direto. É um duelo contra o seu melhor amigo... você consegue aguentar o peso disso?
*   **Joey [Raiva]:** Chega de conversa! Eu vou tomar sua alma e provar que sou o melhor! DUELO!
**(Pós-Duelo - Vitória)**
*   **Joey [Alívio]:** Cara... eu tive um pesadelo horrível. Eu estava atacando vocês...
*   **Yugi [Alegria]:** Acabou, Joey. Você é um de nós de novo. O Orichalcos não te controla mais.

#### 057: Mai Valentine Adv (A Rainha das Harpias das Trevas)
*   **Local:** Topo de um Arranha-Céu. | **Avatar A:** Joey Wheeler | **Avatar B:** Mai Valentine Adv.
**(Pré-Duelo)**
*   **Joey [Tristeza]:** Mai... eu acabei de sair desse pesadelo. Por favor, larga essa carta! A gente pode resolver isso de outro jeito!
*   **Mai [Deboche]:** Resolver? Joey, eu nunca me senti tão viva! A solidão não me machuca mais porque o Orichalcos é o meu único parceiro. Suas Harpias agora são caçadoras de almas.
*   **Mai [Confiança]:** [Nome do Jogador], você venceu o Joey, mas eu sou muito mais rápida. Meu deck de Harpias vai despedaçar sua estratégia antes mesmo de você sacar seu trunfo. O Caos vai cair do céu hoje!
*   **Joey [Séria]:** (Para o Protagonista) Ela está fora de controle. O deck dela é focado em destruir suas cartas mágicas e armadilhas sem parar. Você vai ter que lutar no "seco"! Vai com tudo por ela!
*   **Mai [Alegria]:** O banquete das sombras começou! DUELO!
**(Pós-Duelo - Vitória)**
*   **Mai [Tristeza]:** Eu me senti tão poderosa... mas tão sozinha. O frio passou.
*   **Joey [Alegria]:** Você nunca está sozinha, Mai. Nós estamos aqui.

#### 058: Arkana Dark (O Teatro do Horror Digital)
*   **Local:** Circo das Trevas. | **Avatar A:** Seto Kaiba | **Avatar B:** Arkana Dark.
**(Pré-Duelo)**
*   **Kaiba [Raiva]:** Outro palhaço usando truques de mágica de baixo nível? Esse Orichalcos só atrai perdedores desesperados.
*   **Arkana Dark [Alegria]:** (Surgindo das sombras) Desta vez não há truques, apenas a morte! Meu Mago Negro das Trevas foi banhado no sangue do Orichalcos. O show agora é obrigatório... e o ingresso é a sua vida!
*   **Yugi (Voz) [Séria]:** Ele está usando versões "Dark" de todas as magias. Ele vai tentar banir seu deck inteiro! [Nome do Jogador], confie no equilíbrio do Caos para resistir à loucura dele!
*   **Arkana Dark [Deboche]:** Que as cortinas se fechem para você! DUELO!
**(Pós-Duelo - Vitória)**
*   **Arkana Dark [Medo]:** As sombras... elas estão me puxando de volta! Não! O show não pode acabar assim!
*   **Kaiba [Séria]:** Um mágico medíocre até o fim. Desapareça.

#### 059: Bakura Spirit (O Último Suspiro do Espírito)
*   **Local:** Cemitério de Dados e Memórias. | **Avatar A:** Odion | **Avatar B:** Bakura Spirit.
**(Pré-Duelo)**
*   **Odion [Séria]:** O barulho da cidade desapareceu... O Orichalcos não busca mais apenas o poder, ele busca o vazio. Bakura não é mais um duelista, é a manifestação de um espírito que deseja que nada mais exista.
*   **Bakura Spirit [Tristeza]:** (Voz ecoante) Por que lutar contra o inevitável? O Caos que você carrega e a Escuridão que eu sou... no fim, ambos levam ao mesmo lugar. O silêncio eterno. O Selo não é uma arma, é um túmulo aberto esperando por nós dois.
*   **Odion [Séria]:** (Para o Protagonista) Ele joga com o "Deck de Ocultismo". Cada carta que você envia ao cemitério é um passo em direção à sua própria condenação. Não deixe a melancolia dele apagar a chama do seu deck. Se o Caos é destruição, que seja para criar uma saída desta névoa.
*   **Bakura Spirit [Séria]:** Sinta o frio do esquecimento. DUELO.
**(Pós-Duelo - Vitória)**
*   **Bakura Spirit [Neutro]:** O vazio... recua por enquanto. A luz persiste.
*   **Odion [Séria]:** O equilíbrio foi restaurado. O espírito maligno foi contido.

#### 060: Heishin (O Sacrifício do Sumo Sacerdote - Chefe do Ato 6)
*   **Local:** Ruínas do Altar Supremo. | **Avatar A:** Yuto | **Avatar B:** Heishin.
**(Pré-Duelo)**
*   **Yuto [Séria]:** (Voz calma e cansada) Chegamos ao fim da Ascensão. Heishin fundiu o misticismo proibido do Egito com a corrupção do Selo. Ele não quer apenas vencer... ele quer sacrificar toda a história dos duelos para se tornar um deus do nada.
*   **Heishin [Séria]:** Olhe para este mundo... ele é frágil, feito de papel e hologramas. O Orichalcos me deu a verdade. O "Forbidden Chaos" que você protege é a chave para abrir o portão final. Eu não odeio você, duelista... eu apenas aceito que sua alma é o combustível necessário para o novo amanhecer de trevas.
*   **Yugi (Voz) [Séria]:** Não há ódio nas palavras dele, apenas uma determinação fria. Isso o torna o oponente mais perigoso que já enfrentamos.
*   **Yuto [Confiança]:** [Nome do Jogador]... deixe a melodia desta batalha guiar seus dedos. Não lute com raiva, lute com a memória de todos que o Orichalcos tentou apagar. É hora de silenciar o mestre das trevas.
*   **Heishin [Confiança]:** Que o selo se feche. Pelo fim de todas as eras... DUELO.
**(Pós-Duelo - Fim do Ato 6)**
*   **Heishin [Tristeza]:** A harmonia... foi quebrada. O Caos não se deixou sacrificar... ele escolheu... viver.
*   *(O Selo de Orichalcos racha e se dissolve em pétalas de luz branca)*
*   **Joey [Alegria]:** CONSEGUIMOS! O ar... está puro de novo! Sinto que acordei de um pesadelo horrível!
*   **Yuto [Alegria]:** O pesadelo acabou, mas ele deixou uma lição. Vocês não são mais os mesmos. Suas habilidades atingiram o ápice através do sofrimento.
*   **Yuto [Séria]:** A elite dos duelistas agora nos aguarda para um teste final. Sem selos, sem trevas... apenas o mais puro nível de estratégia. Bem-vindo ao Ato 7: O Desafio da Elite.

---

### Ato 7: O Desafio da Elite
**Contexto:** Os melhores do mundo retornaram, mais fortes do que nunca.

#### 061: Rare Hunter Rematch (A Revanche dos Caçadores)
*   **Local:** Arena de Duelos Kaiba Corp (Dia). | **Avatar A:** Serenity Wheeler | **Avatar B:** Rare Hunter Rematch.
**(Pré-Duelo)**
*   **Serenity [Alegria]:** Eu vi como meu irmão lutou contra esses caras. Hoje, eu não vou fechar os olhos. Eu vou ver você mostrar que a estratégia deles não passa de um truque barato comparado ao seu deck!
*   **Rare Hunter [Confiança]:** Você teve sorte da última vez. Meu deck de busca de peças agora é infalível. Em poucos turnos, o proibido será invocado e sua lenda terminará aqui, diante de milhares de pessoas!
*   **Serenity [Séria]:** (Para o Protagonista) Ele vai tentar travar o jogo para comprar cartas. Seja rápido e agressivo! Não deixe ele completar o quebra-cabeça. Eu confio em você!
*   **Rare Hunter [Séria]:** A contagem regressiva começou! DUELO!
**(Pós-Duelo - Vitória)**
*   **Rare Hunter [Medo]:** Impossível... Exodia... falhou novamente? Como você foi mais rápido que o proibido?
*   **Serenity [Alegria]:** Você conseguiu! O pesadelo acabou. A estratégia dele desmoronou!

#### 062: Rare Elite Rematch (O Despertar da Elite)
*   **Local:** Estádio Principal - Campo de Batalha. | **Avatar A:** Duke Devlin | **Avatar B:** Rare Elite Rematch.
**(Pré-Duelo)**
*   **Duke [Confiança]:** O nível subiu, parceiro! Se esses caras acham que podem ganhar com força bruta, eles não conhecem o estilo do Dungeon Dice Monsters aplicado às cartas. Vamos dar um show!
*   **Rare Elite [Confiança]:** Chega de amadores. Eu represento a elite dos Caçadores de Raras. Meu deck foca em controle total do cemitério. O seu "Forbidden Chaos" vai encontrar o seu mestre hoje!
*   **Duke [Deboche]:** Falar é fácil. Quero ver você lidar com o caos imprevisível desse deck aqui! Jogue os dados e veja sua sorte acabar!
*   **Rare Elite [Séria]:** Protocolo de elite ativado. DUELO!
**(Pós-Duelo - Vitória)**
*   **Rare Elite [Raiva]:** Minha elite... derrotada por dados? Isso não estava nos cálculos!
*   **Duke [Alegria]:** Sorte ou habilidade? Talvez os dois. É assim que se joga Dungeon Dice Monsters... digo, Duel Monsters!

#### 063: Odion Rematch (A Honra do Guardião)
*   **Local:** Sala de Duelos Reais. | **Avatar A:** Ishizu Ishtar | **Avatar B:** Odion Rematch.
**(Pré-Duelo)**
*   **Ishizu [Alegria]:** Odion não luta mais por ordens sombrias. Ele luta pela própria honra como duelista. É um privilégio ver dois guerreiros tão dedicados se enfrentarem.
*   **Odion [Séria]:** Sua jornada foi longa e difícil. Eu testemunhei sua força no deserto e no mundo digital. Agora, sem deuses ou sombras, vamos testar apenas nossa habilidade. Meu deck de Armadilhas está pronto para o seu maior desafio!
*   **Ishizu [Séria]:** Cuidado. Odion é um mestre da paciência. Cada carta que você ativa pode ser um gatilho para a derrota. Use o instinto do Caos para prever o que está escondido!
*   **Odion [Confiança]:** Que a melhor estratégia vença. DUELO!
**(Pós-Duelo - Vitória)**
*   **Odion [Respeito]:** Você é um verdadeiro guerreiro. Minhas armadilhas não foram suficientes para conter seu espírito.
*   **Ishizu [Alegria]:** Obrigada por honrar este duelo. Odion encontrou a paz na batalha.

#### 064: Strings Rematch (O Silêncio das Cordas)
*   **Local:** Aquário Municipal - Arena de Vidro. | **Avatar A:** Marik Ishtar (Redimido) | **Avatar B:** Strings Rematch.
**(Pré-Duelo)**
*   **Marik [Confiança]:** Strings era minha marionete. Hoje, ele é apenas um reflexo do poder que eu costumava controlar. Deixe-me ver como você lida com a pressão de um deck infinito sem a minha influência maligna.
*   **Strings [Séria]:** *(Nenhuma palavra é dita, apenas o disco de duelo se ativa com um brilho intenso)*
*   **Marik [Séria]:** Ele vai tentar manter o Slifer no campo e encher a mão de cartas. Se ele conseguir, o poder de ataque será imparável. Corte o fluxo de cartas dele e a vitória será sua!
*   **Strings [Séria]:** ...DUELO!
**(Pós-Duelo - Vitória)**
*   **Strings [Silêncio]:** ... (Strings faz uma reverência lenta)
*   **Marik [Alegria]:** Ele está livre agora. O Slifer não o controla mais, e ele encontrou sua própria voz no silêncio da derrota.

#### 065: Bandit Keith Adv (Máquinas de Trapaça)
*   **Local:** Hangar de Aviões Kaiba Corp. | **Avatar A:** Joey Wheeler | **Avatar B:** Bandit Keith Adv.
**(Pré-Duelo)**
*   **Joey [Raiva]:** Você de novo, Keith?! Pensei que você já tinha sido expulso de todos os torneios do planeta. Dessa vez, não tem alçapão ou trapaça que te salve!
*   **Bandit Keith [Deboche]:** Ora, se não é o cachorrinho do Yugi. Escuta aqui, moleque: eu não preciso de trapaça quando tenho o deck de máquinas mais destrutivo da América. Esse seu amigo aí vai aprender que, contra o meu "Barrel Dragon", a única regra que importa é a do revólver!
*   **Joey [Confiança]:** [Nome do Jogador], ele joga com cartas de "Gamble" (Sorte) e efeitos de destruição. O deck dele é perigoso porque pode virar o jogo em um segundo. Não dê espaço para ele girar os tambores!
*   **Bandit Keith [Alegria]:** Na América, a gente chama isso de... XEQUE-MATE! DUELO!
**(Pós-Duelo - Vitória)**
*   **Bandit Keith [Raiva]:** Trapaça! Só pode ser trapaça! Ninguém vence minhas máquinas blindadas!
*   **Joey [Deboche]:** Chora mais, Keith. Na América ou aqui, perdedor é perdedor.

#### 066: Espa Roba Adv (O Olhar Cibernético)
*   **Local:** Estação de Rádio e Televisão. | **Avatar A:** Mai Valentine | **Avatar B:** Espa Roba Adv.
**(Pré-Duelo)**
*   **Mai [Confiança]:** Desta vez não há irmãos escondidos nos prédios ajudando você, não é, Espa? Vamos ver o quão "psíquico" você é quando enfrenta o instinto puro.
*   **Espa Roba [Confiança]:** Minha conexão com o "Jinzo" nunca foi tão forte! Eu não preciso de truques quando posso ver através da sua estratégia. O Caos é barulhento, mas o meu silêncio tecnológico vai apagar sua voz!
*   **Mai [Séria]:** (Para o Protagonista) Cuidado com as Armadilhas! Ou melhor, com a falta delas. Ele vai tentar invocar o Jinzo para travar suas defesas. Use efeitos de monstros para passar por cima da tecnologia dele!
*   **Espa Roba [Séria]:** Frequência de vitória sintonizada. DUELO!
**(Pós-Duelo - Vitória)**
*   **Espa Roba [Medo]:** Meus sensores... sobrecarregaram. Eu não consegui prever essa jogada!
*   **Mai [Confiança]:** A intuição vence a máquina. Você precisa de mais do que truques eletrônicos para nos parar.

#### 067: Pegasus Adv (A Ilusão do Criador)
*   **Local:** Salão de Arte Moderna. | **Avatar A:** Téa Gardner | **Avatar B:** Pegasus Adv.
**(Pré-Duelo)**
*   **Téa [Alegria]:** Sr. Pegasus! É estranho ver o criador do jogo desafiando a gente assim. Mas não vamos recuar, a gente aprendeu muito desde o Reino dos Duelistas!
*   **Pegasus [Alegria]:** Oh, Kaiba-boy... Digo, [Nome do Jogador]-boy! Seu deck de Caos é uma obra de arte fascinante, mas um pouco... desorganizada. Que tal se eu desse um toque de "Toon" nessa sua realidade?
*   **Téa [Medo]:** Ele vai tentar usar o "Mundo Toon" para tornar os monstros dele intocáveis! Você precisa destruir a magia de campo dele o mais rápido possível, ou vai acabar virando um desenho animado!
*   **Pegasus [Confiança]:** Preparem-se para o show mais fabuloso das suas vidas! DUELO!
**(Pós-Duelo - Vitória)**
*   **Pegasus [Tristeza]:** Oh my... meu mundo Toon perdeu a cor. Sua realidade é forte demais para minhas fantasias.
*   **Téa [Alegria]:** A realidade é mais bonita, Sr. Pegasus. E a amizade é a melhor mágica de todas!

#### 068: Ishizu Adv (O Destino nas Cartas)
*   **Local:** Observatório Astronômico. | **Avatar A:** Odion | **Avatar B:** Ishizu Adv.
**(Pré-Duelo)**
*   **Odion [Séria]:** Minha senhora Ishizu... o seu colar pode não prever mais o futuro, mas seu deck ainda carrega a sabedoria de milênios.
*   **Ishizu [Confiança]:** O futuro não é mais um caminho traçado, mas uma escolha que fazemos a cada turno. [Nome do Jogador], seu deck de Caos desafia o destino. Vamos ver se você pode superar a guardiã das memórias ancestrais!
*   **Odion [Séria]:** Ela foca em manipular o cemitério e trocar os decks. Se você não tomar cuidado, ficará sem cartas antes de perceber. Mantenha o equilíbrio!
*   **Ishizu [Alegria]:** Que as estrelas guiem suas jogadas. DUELO!
**(Pós-Duelo - Vitória)**
*   **Ishizu [Alegria]:** O futuro mudou... para melhor. Você reescreveu o destino que as cartas mostravam.
*   **Odion [Respeito]:** Você superou o destino. Minha irmã pode sorrir novamente.

#### 069: Yugi Adv (O Rei dos Jogos no Ápice)
*   **Local:** Coliseu Kaiba Corp (Pôr do Sol). | **Avatar A:** Yuto | **Avatar B:** Yugi Adv.
**(Pré-Duelo)**
*   **Yuto [Alegria]:** Olhe para ele... Não há brechas. O Yugi atingiu o nível onde o deck e o duelista são um só. Este é o penúltimo passo para o seu "Forbidden Chaos" se tornar eterno.
*   **Kaiba (Voz) [Deboche]:** Humph! Não me diga que você vai tremer agora, [Nome do Jogador]! Se você perder para o Yugi antes de me enfrentar, eu mesmo confisco o seu deck por incompetência! Mostre que o Caos que eu ajudei a forjar é superior ao misticismo barato dele!
*   **Yugi [Confiança]:** Kaiba, você nunca entende que o verdadeiro poder vem do elo com as cartas. [Nome do Jogador], você provou ser digno de chegar até aqui. Meu deck não tem apenas magos e dragões; ele tem a confiança de todos os amigos que fizemos. Você está pronto para enfrentar o potencial máximo do Mago Negro?
*   **Yuto [Séria]:** (Para o Protagonista) Ele usa o deck "Dark Magician" mais rápido e consistente que existe. Ele vai tentar banir suas cartas no turno dele. Você precisa ser imprevisível. O Caos deve ser a resposta para a magia dele!
*   **Yugi [Confiança]:** Que o Coração das Cartas guie nosso destino. DUELO!
**(Pós-Duelo - Vitória)**
*   **Yugi [Alegria]:** Incrível! Você está pronto para o Kaiba. Seu duelo foi perfeito.
*   **Yuto [Confiança]:** O último degrau antes do topo. Respire fundo, o Imperador aguarda.

#### 070: Kaiba Adv (O Imperador do Caos - Chefe do Ato 7)
*   **Local:** Topo da Torre Kaiba (Noite Estrelada). | **Avatar A:** Yuto | **Avatar B:** Kaiba Adv.
**(Pré-Duelo)**
*   **Yuto [Séria]:** Este é o momento. O duelo que definirá quem é o verdadeiro mestre da era moderna. Kaiba trouxe o deck definitivo de "Blue-Eyes".
*   **Yugi (Voz) [Alegria]:** Você chegou ao topo, [Nome do Jogador]! O Kaiba pode parecer invencível com sua tecnologia e seu poder bruto, mas lembre-se: o seu "Forbidden Chaos" nasceu da sua vontade de superar limites. Eu estarei assistindo... mostre a ele a força da sua alma!
*   **Kaiba [Confiança]:** CHEGA DE DISCURSOS! Yugi, guarde seu sentimentalismo para os seus amigos. [Nome do Jogador], você é o único que sobrou no meu caminho. Eu não aceito nada menos que a perfeição absoluta. Eu vou esmagar o seu Caos com a força de um deus e provar que eu sou o único Rei neste mundo de dados e metal!
*   **Kaiba [Alegria]:** MEU DECK NÃO CONHECE A DERROTA! MEU DRAGÃO BRANCO DE OLHOS AZUIS VAI APAGAR A SUA EXISTÊNCIA!
*   **Yuto [Séria]:** (Para o Protagonista) É agora ou nunca. Ele vai invocar monstros de 3000 ATK ou mais a cada turno. Use toda a mecânica do seu projeto. Quebre o orgulho do Imperador com a pureza do seu Caos!
*   **Kaiba [Confiança]:** VOU TE MOSTRAR O VERDADEIRO SIGNIFICADO DO PODER! DUELO FINAL!
**(Pós-Duelo - Fim do Ato 7)**
*   **Kaiba [Tristeza]:** ...Inacreditável. O meu Blue-Eyes... caiu diante do seu Caos. Eu admito... você não é apenas um duelista comum. Você é uma anomalia que eu não posso ignorar.
*   **Yugi (Voz) [Alegria]:** Você conseguiu, [Nome do Jogador]! Você uniu a estratégia, o poder e o coração. Hoje, uma nova lenda nasceu.
*   **Yuto [Séria]:** A vitória é sua, mas a origem do seu poder ainda é um mistério. O "Forbidden Chaos" não nasceu na Kaiba Corp, nem na Ilha de Pegasus.
*   **Ishizu (Voz) [Séria]:** Yuto tem razão. Para entender a verdadeira natureza do que você controla, você deve olhar para trás. Muito para trás.
*   **Yuto [Confiança]:** O Vale dos Reis nos chama. O passado do Faraó guarda o segredo final. Prepare-se para viajar no tempo. Bem-vindo ao Ato 8.

---

### Ato 8: O Vale dos Reis
**Contexto:** Viaje ao passado para enfrentar os Sumos Sacerdotes e seus poderes divinos.

#### 071: Desert Mage Rematch (O Reinado das Areias Eternas)
*   **Local:** Entrada do Vale dos Reis (Pôr do Sol). | **Avatar A:** Ishizu Ishtar | **Avatar B:** Desert Mage Rematch.
**(Pré-Duelo)**
*   **Ishizu [Séria]:** Voltamos ao solo sagrado. Mas não se engane: este não é o mesmo mago que você enfrentou no templo. Ele agora canaliza a força de todos os que foram enterrados sob estas areias.
*   **Desert Mage [Confiança]:** O tempo é um círculo, duelista. Você provou seu valor no mundo moderno, mas será que sua alma aguenta o julgamento do deserto real? Meu deck de Zumbis Ancestrais não conhece o cansaço. Cada carta que você destrói apenas alimenta a minha necrópole!
*   **Ishizu [Séria]:** (Para o Protagonista) Ele refinou sua estratégia de "Mill" e invocação do cemitério. O deserto vai tentar engolir suas cartas. Mantenha o fluxo do seu Caos focado no banimento para que os mortos dele não possam retornar!
*   **Desert Mage [Séria]:** As areias vão julgar seu destino. DUELO!
**(Pós-Duelo - Vitória)**
*   **Desert Mage [Tristeza]:** A areia... parou. O tempo volta a fluir.
*   **Ishizu [Séria]:** O caminho está limpo. As almas antigas reconhecem sua força.

#### 072: Forest Mage Rematch (A Selva dos Espíritos)
*   **Local:** Oásis Místico. | **Avatar A:** Mai Valentine | **Avatar B:** Forest Mage Rematch.
**(Pré-Duelo)**
*   **Mai [Alegria]:** Esse lugar me dá calafrios, mas ao mesmo tempo... que energia incrível! Parece que a natureza aqui está viva e faminta.
*   **Forest Mage [Confiança]:** O ciclo da vida e da morte se acelera aqui no Vale. Minhas criaturas evoluíram. O "Forbidden Chaos" é um fogo que eu pretendo sufocar com as raízes da terra primordial. Meu enxame de Insetos e Plantas é agora uma força da natureza imparável!
*   **Mai [Séria]:** Cuidado! Ela agora usa combos de invocação especial sincronizada. Se você deixar uma única semente no campo, no próximo turno terá uma floresta inteira bloqueando seu caminho. Use o poder destrutivo do Caos para limpar o terreno!
*   **Forest Mage [Séria]:** A natureza não perdoa invasores. DUELO!
**(Pós-Duelo - Vitória)**
*   **Forest Mage [Tristeza]:** A floresta se curva... O Caos é a nova força da natureza.
*   **Mai [Alegria]:** Nada como um pouco de jardinagem para abrir caminho.

#### 073: Mountain Mage Rematch (O Trovão dos Ancestrais)
*   **Local:** Altar dos Picos Sagrados. | **Avatar A:** Joey Wheeler | **Avatar B:** Mountain Mage Rematch.
**(Pré-Duelo)**
*   **Joey [Confiança]:** Isso que é adrenalina! O cara lá em cima parece que engoliu um trovão. Vamos mostrar pra ele que o nosso estilo de luta é um raio que cai duas vezes no mesmo lugar!
*   **Mountain Mage [Confiança]:** Minhas Bestas Aladas agora cavalgam as tempestades do Egito Antigo! Seu Caos pode ser vasto, mas ele conseguirá alcançar as nuvens onde eu governo? Meu deck de Dragões e Trovões foi forjado no topo do mundo!
*   **Joey [Séria]:** (Para o Protagonista) Ele aumentou a velocidade de ataque. O bônus de campo dele agora é permanente e afeta diretamente seus pontos de vida. Não perca tempo na defesa; esse duelo é uma corrida até o topo!
*   **Mountain Mage [Alegria]:** Sinta a fúria dos céus! DUELO!
**(Pós-Duelo - Vitória)**
*   **Mountain Mage [Raiva]:** O pico... foi conquistado. O trovão se calou diante do seu rugido.
*   **Joey [Alegria]:** A vista daqui de cima é ótima! E sem levar choque!

#### 074: Meadow Mage Rematch (O Exército do Sol)
*   **Local:** Planície dos Guerreiros. | **Avatar A:** Odion | **Avatar B:** Meadow Mage Rematch.
**(Pré-Duelo)**
*   **Odion [Séria]:** Este é o teste de disciplina. O Meadow Mage agora comanda a guarda real do próprio Faraó. Cada jogada dele é uma formação de guerra milenar.
*   **Meadow Mage [Confiança]:** Minha estratégia não é mais apenas defesa. Meus Cavaleiros e Guerreiros agora possuem o brilho do sol. O Caos é desordem, e a desordem morre diante da minha lâmina! Meu deck de Guerreiros de Elite vai testar a resistência da sua alma!
*   **Odion [Séria]:** Ele usa cartas que aumentam o ATK baseadas no número de monstros no campo. Não deixe que ele forme uma linha de frente completa. Golpeie com precisão!
*   **Meadow Mage [Confiança]:** Pela glória do Egito! DUELO!
**(Pós-Duelo - Vitória)**
*   **Meadow Mage [Respeito]:** Uma vitória honrada. Meus guerreiros baixam as armas para você.
*   **Odion [Séria]:** A disciplina venceu. Você tem o porte de um general.

#### 075: Ocean Mage Rematch (O Abismo do Nilo Eterno)
*   **Local:** Templo Inundado de Luxor. | **Avatar A:** Mako Tsunami | **Avatar B:** Ocean Mage Rematch.
**(Pré-Duelo)**
*   **Mako [Confiança]:** Sinto a força das marés antigas aqui! Este mago não controla apenas a água, ele controla a vida que flui por este vale. Mas o mar não tem dono, e o seu Caos é uma tempestade que nenhuma barragem pode segurar!
*   **Ocean Mage [Séria]:** As águas do Nilo limpam o mundo da impureza. O seu deck de Caos é uma mancha no espelho de água do Faraó. Prepare-se para ser submerso pelo meu novo exército de Serpentes Marinhas e Leviatãs Sagrados!
*   **Mako [Séria]:** (Para o Protagonista) Ele agora usa o "Ocean" como uma arma de negação. Se ele invocar o Neo-Daedalus, o campo inteiro será destruído. Ataca como um tubarão: rápido e sem aviso!
*   **Ocean Mage [Confiança]:** Afunda no esquecimento! DUELO!
**(Pós-Duelo - Vitória)**
*   **Ocean Mage [Tristeza]:** O mar recua... As águas do Nilo abrem passagem.
*   **Mako [Confiança]:** Navegue em frente, campeão. O horizonte é seu.

#### 076: Isis (A Guardiã do Ritual)
*   **Local:** Câmara do Destino. | **Avatar A:** Ishizu Ishtar | **Avatar B:** Isis.
**(Pré-Duelo)**
*   **Ishizu [Tristeza]:** Estou diante da minha própria ancestral... A Suma Sacerdotisa Isis. Ela não usa apenas cartas, ela lê a alma do seu deck.
*   **Isis [Confiança]:** O futuro é um livro que eu já li, jovem viajante. Eu vi a sua chegada nas estrelas há cinco mil anos. O seu "Forbidden Chaos" é a peça que falta para o equilíbrio, mas apenas se a sua vontade for mais forte que o destino que eu tracei para você.
*   **Ishizu [Séria]:** O deck dela foca em banimento e controle temporal. Ela vai tentar fazer com que as tuas melhores cartas nunca cheguem à tua mão. Confia no teu instinto, pois é a única coisa que ela não pode prever!
*   **Isis [Séria]:** Que o veredito do tempo seja dado. DUELO!
**(Pós-Duelo - Vitória)**
*   **Isis [Alegria]:** O destino aceita sua força. Você é aquele que as estrelas aguardavam.
*   **Ishizu [Alegria]:** Minha ancestral está orgulhosa. O passado e o futuro estão em harmonia.

#### 077: Secmeton (O Carniceiro do Deserto)
*   **Local:** Arena de Sacrifício. | **Avatar A:** Seto Kaiba | **Avatar B:** Secmeton.
**(Pré-Duelo)**
*   **Kaiba [Raiva]:** Outro bárbaro que acha que força bruta supera a estratégia. [Nome do Jogador], não percas tempo com as ameaças dele. Esmaga este animal e vamos seguir para o que realmente importa.
*   **Secmeton [Raiva]:** O rugido de Secmeton fará a terra tremer! Eu sou a fúria do sol! O meu deck de Bestas Guerreiras vai estraçalhar a tua defesa. Não haverá nada para enterrar quando eu terminar contigo!
*   **Kaiba [Deboche]:** Ele foca em destruir os teus monstros por batalha para ganhar ATK infinito. Patético. Usa o efeito das tuas cartas de Caos para removê-lo do jogo antes que ele consiga sequer atacar!
*   **Secmeton [Alegria]:** Sangue e areia! DUELO!
**(Pós-Duelo - Vitória)**
*   **Secmeton [Raiva]:** Minha força... quebrada! Como um mortal pode ser mais forte que a fúria do sol?
*   **Kaiba [Deboche]:** Apenas um animal enjaulado. Força sem cérebro não vale nada.

#### 078: Martis (O Guardião da Tumba)
*   **Local:** Corredor das Almas Perdidas. | **Avatar A:** Yuto | **Avatar B:** Martis.
**(Pré-Duelo)**
*   **Yuto [Séria]:** Estamos nos aproximando do santuário interior. Martis é o guardião que nunca dorme. Ele protege os segredos do Faraó com uma defesa impenetrável.
*   **Martis [Confiança]:** Nenhum vivo deve passar por aqui. Minha tumba é o fim da sua jornada. O Caos é uma energia vibrante demais para este lugar de descanso eterno. Eu vou silenciar o seu deck para sempre.
*   **Martis [Séria]:** Meu deck de "Gravekeepers" (Coveiros) vai selar o seu cemitério. Sem acesso aos seus mortos, o seu Caos perderá a força. Prepare-se para ser mumificado vivo!
*   **Yuto [Confiança]:** (Para o Protagonista) Ele usa o "Necrovalley" para impedir qualquer interação com o cemitério. Você precisa destruir essa magia de campo imediatamente, ou sua estratégia vai desmoronar!
*   **Martis [Séria]:** O selo da tumba está fechado. DUELO!
**(Pós-Duelo - Vitória)**
*   **Martis [Tristeza]:** A tumba foi violada... mas por alguém digno.
*   **Yuto [Séria]:** Não violada, superada. O segredo está seguro conosco.

#### 079: Kepura (O Escorpião do Deserto)
*   **Local:** Fosso dos Escorpiões. | **Avatar A:** Joey Wheeler | **Avatar B:** Kepura.
**(Pré-Duelo)**
*   **Joey [Medo]:** Cara, olha o tamanho daquele ferrão! Esse tal de Kepura não parece muito amigável.
*   **Kepura [Deboche]:** O veneno do deserto é lento, mas fatal. Vocês chegaram longe, mas agora sentirão a picada da realidade. O Caos é forte, mas até os gigantes caem quando o veneno atinge o coração.
*   **Kepura [Confiança]:** Meu deck de Insetos e Bestas foca em destruir seus monstros e causar dano direto a cada turno. Vocês vão sangrar lentamente até não restar nada além de cascas vazias.
*   **Joey [Raiva]:** Ele quer ganhar pelo cansaço! [Nome do Jogador], não deixa ele te envenenar. Ataca com tudo e esmaga esse escorpião antes que ele te pique!
*   **Kepura [Alegria]:** Sinta o veneno correr nas suas veias! DUELO!
**(Pós-Duelo - Vitória)**
*   **Kepura [Medo]:** O veneno... não funcionou? Você é imune à dor?
*   **Joey [Alívio]:** Ufa! Achei que ia precisar de um antídoto. Vamos sair logo desse buraco.

#### 080: Anubisius (O Senhor do Submundo - Chefe do Ato 8)
*   **Local:** Portão de Anúbis. | **Avatar A:** Yuto | **Avatar B:** Anubisius.
**(Pré-Duelo)**
*   **Yuto [Medo]:** A pressão aqui é insuportável... Anubisius é o juiz final deste vale. Ele não quer apenas vencer um duelo, ele quer pesar o teu coração contra uma pena. Se fores pesado demais com o poder do Caos, serás consumido!
*   **Anubisius [Confiança]:** Mortais e as suas ambições... O Caos que carregas é uma chama que consome o seu hospedeiro. Eu sou o silêncio que vem depois da tempestade. O meu deck de "End of the World" trará o apocalipse para o teu campo de batalha!
*   **Yuto [Séria]:** (Para o Protagonista) Ele usa rituais de nível supremo. O Demise, King of Armageddon é o trunfo dele. Se ele entrar em campo, tudo o que construíste será reduzido a zero. Este é o duelo pela tua existência!
*   **Anubisius [Séria]:** O julgamento final começou. DUELO!
**(Pós-Duelo - Fim do Ato 8)**
*   **Anubisius [Tristeza]:** O julgamento... foi revertido. O seu coração é puro caos, mas não há peso de maldade nele. O portão... ele se abre para você.
*   *(O Vale dos Reis começa a rachar, revelando um abismo de sombras puras)*
*   **Yuto [Medo]:** O que é isso?! Vencer Anubisius deveria ter nos libertado, mas... as sombras estão nos puxando para baixo!
*   **Bakura (Voz) [Deboche]:** (Risada maligna) Vocês acharam que o passado era o fim? O verdadeiro jogo começa agora, no labirinto da minha alma!
*   **Yuto [Séria]:** Segure-se! Estamos sendo arrastados para o Domínio das Trevas. Bem-vindo ao Ato 9: O Labirinto Final.

---

### Ato 9: O Labirinto Final
**Contexto:** O mal se reúne para uma última tentativa de pará-lo. Bakura revela sua verdadeira forma no labirinto.

#### 081: Labyrinth Mage Rematch (O Enigma das Paredes Vivas)
*   **Local:** Corredores Infinitos de Pedra. | **Avatar A:** Joey Wheeler | **Avatar B:** Labyrinth Mage Rematch.
**(Pré-Duelo)**
*   **Joey [Medo]:** Ei! Aquela parede acabou de piscar pra mim? Esse lugar tá me dando um nó na cabeça! [Nome do Jogador], a gente tá andando em círculos ou o labirinto tá jogando contra a gente?
*   **Labyrinth Mage [Confiança]:** Bem-vindos ao meu domínio refeito. No passado, você apenas atravessou minhas paredes... Agora, você faz parte delas!
*   **Labyrinth Mage [Séria]:** Meu deck de "Labyrinth Wall" foi fortalecido pelas sombras de Bakura. Você nunca encontrará a saída, pois eu sou o próprio caminho para a sua perdição!
*   **Joey [Raiva]:** Escuta aqui, seu mestre de obras das trevas! O meu amigo aqui já derrubou deuses, não vai ser um punhado de tijolo que vai parar a gente!
*   **Joey [Confiança]:** (Para o Protagonista) Ele usa monstros que se escondem na defesa e atacam das sombras. Quebra essas paredes com a força bruta do seu Caos antes que a gente fique preso aqui pra sempre!
*   **Labyrinth Mage [Confiança]:** Perca-se na eternidade! DUELO!
**(Pós-Duelo - Vitória)**
*   **Labyrinth Mage [Tristeza]:** As paredes... elas estão sangrando... um código negro está escorrendo pelas frestas! Como a luz desse Caos pôde encontrar uma saída no meu labirinto perfeito? Bakura... você disse que eu seria eterno aqui!
*   **Joey [Alegria]:** Olha só! O labirinto dele está desmoronando! Parece que o seu deck de Caos é como uma britadeira gigante pra esse amontoado de pedras!
*   **Labyrinth Mage [Medo]:** NÃO! O teto... ele está vindo abaixo! Eu não quero ser esmagado pelo meu próprio mundo! ARGHHHH!

#### 082: Gansley Rematch (O Acordo das Sombras)
*   **Local:** Escritório de Pedra no Vazio. | **Avatar A:** Seto Kaiba | **Avatar B:** Gansley Rematch.
**(Pré-Duelo)**
*   **Kaiba [Raiva]:** Gansley! Nem a morte ou o colapso do mundo virtual livram você da sua mediocridade? Você se aliou ao Bakura agora? Que queda patética para um ex-diretor da minha empresa.
*   **Gansley [Confiança]:** Seto... no Labirinto de Bakura, as almas são a única moeda que importa. Eu recuperei meu cargo aqui como o coletor de impostos do submundo!
*   **Gansley [Deboche]:** O seu deck de "Caos" parece um ativo muito valioso, [Nome do Jogador]. Vou tomá-lo como bônus pela minha eficiência! Meu "Deep Sea Warrior" vai afogar suas esperanças!
*   **Kaiba [Deboche]:** Tente a sorte, verme. (Para o Protagonista) Ele usa um deck de controle que pune você por cada invocação. Não deixe que ele dite as regras desse negócio. Esmague os ativos dele!
*   **Gansley [Confiança]:** A fusão hostil começou! DUELO!
**(Pós-Duelo - Vitória)**
*   **Gansley [Medo]:** O balanço... ele deu negativo?! Meus ativos... minha autoridade... sendo deletados da existência?! Como você pode ter tanto poder sem um pingo de estrutura corporativa?
*   **Kaiba [Alegria]:** Hahaha! Parece que sua "fusão hostil" foi um fracasso total, Gansley. No mundo real ou nas sombras, você continua sendo um burocrata de segunda categoria.
*   **Gansley [Tristeza]:** O sistema... ele está me ejetando! Bakura me prometeu o controle... mas agora... eu sou apenas um erro contábil sendo apagado! SOCORROOO!

#### 083: Crump Rematch (O Recarrego do Pinguim)
*   **Local:** Geleira de Ossos. | **Avatar A:** Mai Valentine | **Avatar B:** Crump Rematch.
**(Pré-Duelo)**
*   **Mai [Nojo]:** Esse pinguim asqueroso de novo? Bakura realmente não tem bom gosto para recrutar aliados. O cheiro de peixe podre e magia negra aqui é insuportável!
*   **Crump [Confiança]:** Hehehe... Senhorita Valentine, você continua charmosa mesmo no fim do mundo. Mas Bakura me deu um novo oceano para patinar.
*   **Crump [Alegria]:** Meu deck de Pinguins agora tem o toque do pesadelo! Cada vez que você tentar atacar, eu vou devolver suas cartas para a mão e rir enquanto você congela de desespero! O "Forbidden Chaos" vai virar um picolé!
*   **Mai [Séria]:** (Para o Protagonista) Ele refinou o controle de campo. Se você não for rápido, ele vai travar todas as suas zonas de monstros com efeitos de "Bounce". Quebre o gelo dele com o calor escaldante do seu deck!
*   **Crump [Confiança]:** O inverno eterno chegou para você! DUELO!
**(Pós-Duelo - Vitória)**
*   **Crump [Raiva]:** O gelo... ele não está rachando... ele está DERRETENDO?! Mas Bakura disse que este seria o frio absoluto das trevas! O calor desse seu deck... ele está fervendo a minha alma!
*   **Mai [Alegria]:** Parece que o seu "inverno eterno" acabou de virar uma poça de água suja, Crump. O Caos do meu amigo aqui é quente demais para um perdedor como você!
*   **Crump [Medo]:** Eu não quero voltar para o vazio escuro! Alguém me dê um casaco... está ficando... tão quente... POR QUE OS MEUS PINGUINS ESTÃO SUMINDO?! NÃOOO!

#### 084: Johnson Rematch (A Lei do Caos no Tribunal)
*   **Local:** Tribunal de Obsidiana. | **Avatar A:** Joey Wheeler | **Avatar B:** Johnson Rematch.
**(Pré-Duelo)**
*   **Joey [Raiva]:** Eu já disse que odeio tribunais! E esse aqui parece que foi decorado pelo próprio ceifador. Johnson, desiste logo dessa peruca de juiz e luta como um duelista de verdade!
*   **Johnson [Confiança]:** Silêncio no recinto! No Labirinto de Bakura, a única lei é a dor. Eu fui nomeado o carrasco oficial deste setor e minha palavra é final!
*   **Johnson [Séria]:** Você foi considerado culpado de existir... e a sentença é a aniquilação total! Meu deck de "Mudança de Vida" vai drenar sua energia vital para alimentar as sombras!
*   **Joey [Confiança]:** (Para o Protagonista) Ele vai tentar trocar os pontos de vida dele pelos seus quando estiver perdendo. É uma tática de trapaceiro disfarçada de lei! Mantém a pressão e não deixa ele bater o martelo!
*   **Johnson [Confiança]:** Veredito imediato! DUELO!
**(Pós-Duelo - Vitória)**
*   **Johnson [Tristeza]:** Anulado... o julgamento foi anulado por uma força que eu não posso processar! O martelo... ele virou cinzas nas minhas mãos! O Caos... ele não segue leis... ele não segue precedentes!
*   **Joey [Alegria]:** É isso aí! O tribunal fechou cedo hoje, "Sr. Juiz"! No mundo do Caos, a gente é que faz as regras! Sua sentença acaba de ser revogada!
*   **Johnson [Medo]:** Bakura, a lei falhou! A sentença de morte que eu preparei para ele... ela está voltando contra mim! O veredito... EU SOU O CULPADO?! NÃO! ISSO É CONTRA OS PROTOCOLOS! ARGHHHH!

#### 085: Leichter Rematch (A Artilharia do Fim do Mundo)
*   **Local:** Arsenal de Ossos e Metal. | **Avatar A:** Mai Valentine | **Avatar B:** Leichter Rematch.
**(Pré-Duelo)**
*   **Mai [Séria]:** Esse Leichter não parece mais um homem, parece uma máquina de guerra possuída. Bakura realmente deu a ele brinquedos perigosos desta vez. Cuidado com os estilhaços!
*   **Leichter [Alegria]:** O "Forbidden Chaos" é a bateria perfeita para as minhas armas! Bakura me permitiu converter a energia do seu deck em munição pura para o meu exército.
*   **Leichter [Confiança]:** Meu deck de Máquinas e Dano Direto foi calibrado para te destruir antes mesmo de você declarar o primeiro ataque. Sinta o peso da artilharia das trevas! O seu destino será escrito em pólvora e sangue!
*   **Mai [Confiança]:** (Para o Protagonista) Ele vai tentar tirar seus pontos de vida com efeitos de cartas mágicas a cada turno. É um duelo contra o relógio! Não dê tempo para ele carregar os canhões. Destrua a base de operações dele agora!
*   **Leichter [Confiança]:** Fogo total! DUELO!
**(Pós-Duelo - Vitória)**
*   **Leichter [Raiva]:** ALERTA! Superaquecimento total! O poder do Caos... ele não é combustível... ele é uma SOBRECARGA! Meus canhões... eles estão disparando contra mim mesmo!
*   **Mai [Alegria]:** Parece que a sua "artilharia pesada" era apenas fogos de artifício molhados. O Caos do meu amigo aqui é instável demais para as suas máquinas velhas, Leichter!
*   **Leichter [Medo]:** Eu deveria ser o destruidor de mundos... mas eu sou apenas sucata no labirinto de outro homem! Bakura, você me usou como uma arma descartável! SISTEMA EM COLAPSO! EU VOU EXPLODIR... NÃOOO!

#### 086: Nezbitt Rematch (O Engenheiro do Apocalipse)
*   **Local:** Forja das Almas. | **Avatar A:** Yuto | **Avatar B:** Nezbitt Rematch.
**(Pré-Duelo)**
*   **Yuto [Séria]:** Este é o último dos Big Five. Nezbitt fundiu sua alma com o mecanismo central deste pesadelo. Se ele vencer, as engrenagens vão esmagar nossa conexão com o mundo real para sempre!
*   **Nezbitt [Confiança]:** A evolução final! Meu deck de Máquinas agora utiliza a energia das almas para se auto-reparar e evoluir infinitamente.
*   **Nezbitt [Séria]:** Você destrói uma peça, e eu construo um deus mecânico no lugar! O seu Caos é uma variável primitiva diante da minha lógica de destruição total! Eu sou o arquiteto do seu fim!
*   **Yuto [Confiança]:** (Para o Protagonista) Mostre a ele que nenhuma máquina pode simular a alma de um duelista e a imprevisibilidade do Caos! Destrua o processador central e pare esse mecanismo infernal de uma vez por todas!
*   **Nezbitt [Séria]:** Iniciando sequência de aniquilação! DUELO!
**(Pós-Duelo - Vitória)**
*   **Nezbitt [Tristeza]:** Lógica... fragmentada. Engrenagens... paralisadas. Como o Caos pode ser tão... cirúrgico na destruição? Eu tentei construir a eternidade com metal e ódio...
*   **Yuto [Alegria]:** A sua "perfeição mecânica" não passa de um castelo de cartas diante do poder do Caos. As engrenagens pararam porque o seu ódio não tem mais onde se apoiar!
*   **Nezbitt [Medo]:** O Labirinto está parando... e sem o movimento, não há vida para nós. Bakura... a forja apagou... e o frio... o frio está voltando... ADEUS, MUNDO DE DADOS!

#### 087: Simon Rematch (O Arquiteto das Memórias)
*   **Local:** Sala do Trono de Pedra. | **Avatar A:** Yami Yugi | **Avatar B:** Simon Rematch.
**(Pré-Duelo)**
*   **Yami Yugi [Séria]:** Simon... o conselheiro real de cinco mil anos atrás. Bakura o trouxe de volta como um espectro para testar se somos dignos. Sinto que ele não luta por ódio, mas por um dever sagrado que o escraviza.
*   **Simon [Confiança]:** Você caminha pelo Labirinto de Bakura, mas sua alma ainda brilha com a luz do Faraó. Eu sou o guardião das chaves do passado e o protetor do que é proibido!
*   **Simon [Séria]:** Meu deck de "Exodia" e "Defesa Absoluta" não é um truque, é uma barreira contra o fim do mundo. Se você não puder romper minha guarda, as sombras de Bakura serão sua morada eterna!
*   **Yami Yugi [Confiança]:** (Para o Protagonista) Ele conhece cada segredo do jogo original e vai tentar ganhar tempo para invocar o Proibido. Você precisa ser a tempestade que derruba o castelo dele antes que o selo se complete!
*   **Simon [Séria]:** Mostre-me a força da sua era! DUELO!
**(Pós-Duelo - Vitória)**
*   **Simon [Tristeza]:** Incrível... O trono de pedra racha ao meio sob o impacto do seu Caos... Eu vi milênios de duelistas, mas sua vontade quebrou as correntes que me prendiam!
*   **Yami Yugi [Alegria]:** Você conseguiu! Simon está livre da influência das sombras. Veja, ele está apontando para o próximo portal com respeito.
*   **Simon [Alegria]:** Vá... o caminho para o mestre do Labirinto está aberto. Mas cuidado, duelista: o poder que você carrega despertou algo que nem mesmo a história pode apagar.

#### 088: Shadi Rematch (O Julgamento das Relíquias)
*   **Local:** O Vazio das Relíquias. | **Avatar A:** Ishizu Ishtar | **Avatar B:** Shadi Rematch.
**(Pré-Duelo)**
*   **Ishizu [Medo]:** Shadi... o guardião entre os mundos. Bakura corrompeu a neutralidade dele para usar a Balança do Milênio contra nós. Sinto que o peso de toda a sua jornada será colocado à prova agora!
*   **Shadi [Confiança]:** Não há segredo que a Balança não possa revelar. O seu deck de Caos é feito de glórias e destruição em partes iguais, [Nome do Jogador]. Eu invoco os espíritos das Relíquias para purificar este Labirinto!
*   **Shadi [Séria]:** Meu deck de "Controle de Mente" e "Justiça Divina" vai refletir cada jogada sua contra você mesmo! O equilíbrio será restaurado através da sua derrota!
*   **Ishizu [Séria]:** (Para o Protagonista) Ele vai tentar usar seus próprios monstros contra você! Não confie apenas no que vê no campo; confie na alma do seu deck. Quebre o equilíbrio dele ou seja julgado insuficiente!
*   **Shadi [Séria]:** Que o julgamento comece. DUELO!
**(Pós-Duelo - Vitória)**
*   **Shadi [Raiva]:** O peso... é impossível! A Balança do Milênio está explodindo em chamas purpúreas! Como o Caos em sua alma pode ser mais denso que a própria justiça das Relíquias?!
*   **Ishizu [Confiança]:** O julgamento dele falhou porque o seu Caos não é maldade, é potencial puro. Shadi não consegue medir algo que é infinito em sua essência!
*   **Shadi [Medo]:** Bakura... você criou um erro que nem o destino pode conter. A Balança pendeu para a destruição... e o que vem a seguir é o próprio eclipse da alma! FUJA!

#### 089: Bakura Ryou (A Alma Fragmentada)
*   **Local:** O Cemitério das Cartas Esquecidas. | **Avatar A:** Yuto (Aura Branca) | **Avatar B:** Bakura Ryou.
**(Pré-Duelo)**
*   **Yuto [Séria]:** Olhe para ele, [Nome do Jogador]. Este é o Bakura "humano", mas ele está sendo usado como um escudo vivo pelo espírito do anel. Ele joga com uma melancolia desesperada... cada carta que ele coloca no campo parece um pedido de socorro.
*   **Bakura [Tristeza]:** Eu sinto muito... Eu não queria que as coisas terminassem assim para você. Mas o Labirinto exige um sacrifício final para abrir os portões de Zorc.
*   **Bakura [Medo]:** O meu Anel do Milênio... ele está queimando o meu peito! Meu deck de "Ocultismo e Destruição de Deck" vai transformar sua vitória em cinzas. Se você não puder me derrotar agora, o outro... ele assumirá o controle total e não restará nada de mim!
*   **Yuto [Confiança]:** (Para o Protagonista) Ele usa a tática de "Bônus de Cemitério" e cartas que removem seu deck de jogo. É um duelo contra a sua própria mente. Vença-o para libertar o que resta da alma dele antes que o eclipse comece!
*   **Bakura [Tristeza]:** Perdoe-me... eu não tenho escolha! DUELO!
**(Pós-Duelo - Vitória)**
*   **Bakura [Medo]:** Você... você foi rápido demais... O brilho do seu Caos cortou as cordas que me controlavam... mas é tarde. O selo foi quebrado pelo impacto da nossa batalha!
*   **Yuto [Medo]:** Bakura! Aguenta firme! (Para o Protagonista) Olhe! O Anel do Milênio dele está emitindo uma fumaça negra colossal... a consciência dele está sendo engolida!
*   **Bakura [Tristeza]:** Corram... a escuridão absoluta... ela despertou... FUJAM ANTES QUE ELE—!
*   *(Bakura desmaia e seu corpo flutua, sendo envolto por uma aura roxa e negra.)*

#### 090: Dark Bakura Final (O Eclipse de Zorc - Chefe do Ato 9)
*   **Local:** O Abismo de Zorc Necrophades. | **Avatar A:** Yuto (Poder Máximo) | **Avatar B:** Dark Bakura Final.
**(Pré-Duelo)**
*   **Yami Yugi (Voz) [Séria]:** Bakura! Ou devo dizer... o fragmento de Zorc que habita este anel! O seu jogo macabro termina aqui e agora! Eu e este duelista não vamos permitir que você mergulhe o futuro nas trevas do passado!
*   **Dark Bakura [Alegria]:** (Risada histérica) Hahaha! O Faraó e seu pequeno erro de código! Vocês acham que cartas de papel podem deter a entidade que criou o próprio conceito de sombra? Meu deck de 'Zorc' e 'Dark Sanctuary' vai devorar cada esperança que vocês cultivaram!
*   **Dark Bakura [Confiança]:** Cada carta que você saca é um segundo a menos de vida para este mundo! O seu 'Forbidden Chaos' é apenas o tempero que faltava para o meu banquete de almas. Bem-vindos ao eclipse final da humanidade!
*   **Yuto [Confiança]:** (Para o Protagonista) Ele vai usar o "Dark Sanctuary" para tornar cada ataque seu um risco de morte por possessão. Esta é a batalha definitiva. Use todo o poder do seu deck de Caos para iluminar este abismo ou seremos esquecidos para sempre!
*   **Dark Bakura [Alegria]:** O dado da morte foi lançado e ele sempre cai no meu número! DUELO FINAL!
**(Pós-Duelo - Fim do Ato 9)**
*   **Dark Bakura [Raiva]:** NÃO! ISSO É IMPOSSÍVEL! O tabuleiro... ele está rachando! O meu mundo de trevas eternas... sendo consumido por esse brilho?! ZORC... NÃO ME DEIXE APAGAR!
*   **Yami Yugi (Voz) [Alegria]:** Acabou, Bakura! Onde há uma luz tão intensa quanto o Caos deste duelista, a sombra não tem onde se esconder! O seu RPG das Trevas foi vencido!
*   **Yuto [Alegria]:** Veja! O Anel do Milênio está se despedaçando! O Labirinto está colapsando e nos enviando de volta! Nós vencemos o impossível, [Nome do Jogador]!
*   **Dark Bakura [Tristeza]:** Malditos... vocês venceram esta rodada... mas o poder que você despertou... ele ecoou pelo mundo real... os portadores dos Deuses Egípcios... eles estão vindo... para o desafio final...
*   *(Uma explosão de energia branca consome a tela)*

---

## Ato 10: A Batalha Suprema
**Contexto:** O desafio final contra os portadores das Relíquias e Deuses.

#### 091: Joey Final (O Coração do Campeão)
*   **Local:** Arena do Torneio Final - Pôr do Sol. | **Avatar A:** Yuto (Forma Estável) | **Avatar B:** Joey Final.
**(Pré-Duelo)**
*   **Joey [Alegria]:** Caramba! Eu senti aquela explosão de energia lá do outro lado da cidade! [Nome do Jogador], eu sabia que você ia chutar o traseiro do Bakura, mas o que você trouxe de volta... é algo totalmente novo, não é?
*   **Joey [Confiança]:** Eu treinei como um louco enquanto você estava naquele labirinto. Meu "Red-Eyes Black Dragon" evoluiu e minha sorte nunca esteve tão afiada! Eu não sou mais aquele garoto que dependia só da coragem.
*   **Joey [Séria]:** Para a gente avançar contra o Marik, eu preciso saber se você consegue aguentar a pressão de um duelo de elite. Meu deck de "Guerreiros e Sorte Máxima" vai te levar ao limite! Não segura nada, amigão!
*   **Yuto [Confiança]:** (Para o Protagonista) O Joey alcançou o ápice. Ele combina instinto com cartas de fusão poderosas. Ele vai tentar te pegar com efeitos de dados e moedas que podem virar o jogo em um segundo. Mostre a ele o quanto você cresceu!
*   **Joey [Confiança]:** Vamos fazer desse o duelo das nossas vidas! É HORA DO DUELO!
**(Pós-Duelo - Vitória)**
*   **Joey [Alegria]:** (Limpando o suor da testa e rindo) Uau! Que pancada! Eu vi o brilho nos olhos do meu Dragão, mas o seu Caos... ele é simplesmente imparável. Você realmente é o cara, [Nome do Jogador]!
*   **Yuto [Alegria]:** Isso foi incrível. O Joey provou que tem o espírito de um verdadeiro campeão, e você provou que está pronto para o que vem a seguir.
*   **Joey [Confiança]:** Escuta, eu vou estar logo ali na arquibancada te dando cobertura. Mas se prepara, porque a Mai está vindo aí, e ela disse que o seu brilho está ofuscando as Harpias dela... e ela não gosta nada de perder o destaque!

#### 092: Mai Final (O Vento da Vitória)
*   **Local:** Jardim Suspenso de Luxo. | **Avatar A:** Joey Wheeler | **Avatar B:** Mai Final.
**(Pré-Duelo)**
*   **Mai [Confiança]:** Eu vi o seu duelo com o Joey. Muito barulhento, muita força bruta... típico de garotos. Mas um verdadeiro mestre sabe que a vitória se conquista com elegância e precisão cirúrgica.
*   **Mai [Deboche]:** O seu "Forbidden Chaos" é fascinante, mas será que ele consegue atingir o que não pode ver? Minhas Harpias atingiram um novo nível de sincronia. Elas não são apenas monstros, são uma tempestade que vai varrer o seu campo antes de você dizer "sacar"!
*   **Joey [Medo]:** Cuidado, parceiro! A Mai está usando cartas de suporte que eu nunca vi antes. Ela consegue invocar um bando inteiro de Harpias em um único turno e destruir todas as suas cartas mágicas!
*   **Mai [Confiança]:** (Para o Protagonista) Eu não vou deixar você passar sem testar se a sua mente é tão forte quanto o seu deck. Se você hesitar por um segundo, o vento vai te cortar ao meio. Está pronto para o show?
*   **Mai [Confiança]:** Prepare-se para cair do pedestal! DUELO!
**(Pós-Duelo - Vitória)**
*   **Mai [Alegria]:** (Guardando o deck) Impressionante... o seu Caos não é apenas força, é inteligência. Você previu cada movimento das minhas Harpias. Eu admito... você é mais do que apenas um "novato sortudo".
*   **Joey [Alegria]:** Eu te avisei, Mai! Esse cara é de outro planeta! Viu como ele lidou com a sua estratégia de campo?
*   **Mai [Confiança]:** Cale a boca, Joey. (Para o Protagonista) Você tem a minha bênção para seguir em frente. Mas fique de olho no céu... os predadores primitivos sentiram o seu cheiro. O Rex e o Weevil estão formando uma aliança desesperada para tentar te parar.

#### 093: Rex Final (O Rugido do Apocalipse)
*   **Local:** Canteiro de Obras - Crepúsculo. | **Avatar A:** Joey Wheeler | **Avatar B:** Rex Final.
**(Pré-Duelo)**
*   **Joey [Raiva]:** Rex! Eu devia saber que você estaria rastejando por aqui. Ainda não cansou de perder pros meus monstros de metal e pro Caos do meu parceiro?
*   **Rex Raptor [Confiança]:** Calado, Joey! Você e esse novato podem ter sorte, mas a evolução não para! Eu escavei os fósseis mais sombrios que existem para este momento.
*   **Rex Raptor [Séria]:** Meu novo deck de "Dinossauros Evoluídos" não conhece a piedade. Eu vou esmagar o seu "Forbidden Chaos" sob o peso de criaturas que dominaram o mundo por milhões de anos! [Nome do Jogador], prepare-se para a extinção!
*   **Joey [Confiança]:** (Para o Protagonista) Ele está usando monstros de nível alto que entram em campo sem sacrifício! É pura força bruta, parceiro. Mostra pra esse lagarto que o seu Caos é o meteoro que vai mandar ele de volta pra pré-história!
*   **Rex Raptor [Alegria]:** Sintam a fúria do Jurássico! DUELO!
**(Pós-Duelo - Vitória)**
*   **Rex Raptor [Tristeza]:** (Caindo de joelhos) Não... meus tiranossauros... eles foram pulverizados por uma luz que eu nem consigo descrever! A evolução... ela falhou contra você...
*   **Joey [Alegria]:** Haha! Eu te disse, Rex! O seu exército de lagartixas não é páreo para o poder que o meu amigo carrega. Melhor voltar pro museu!
*   **Rex Raptor [Medo]:** Riam enquanto podem... mas o Weevil está logo ali naquelas vigas acima de nós. Ele disse que se a força bruta falhasse... o veneno dele faria o serviço. Tomem cuidado com o que vocês respiram!

#### 094: Weevil Final (O Enxame Supremo)
*   **Local:** Topo das Vigas de Aço. | **Avatar A:** Mai Valentine | **Avatar B:** Weevil Final.
**(Pré-Duelo)**
*   **Mai [Nojo]:** Argh! Eu odeio ter que subir aqui. Weevil, apareça logo! Esse seu cheiro de inseticida barato está estragando o meu perfume.
*   **Weevil [Deboche]:** Hi-hi-hi-hi! Bem-vindos à minha teia, "Dama das Harpias"! E bem-vindo você também, "Rei do Caos". Vocês acham que podem ganhar com ataques diretos? Que tolos!
*   **Weevil [Confiança]:** Meu deck de "Infestação Total" foi aprimorado com as toxinas do Marik! Eu vou paralisar seus monstros e drenar seus pontos de vida gota a gota. Vocês vão implorar para o duelo acabar enquanto meus insetos devoram o seu deck!
*   **Mai [Séria]:** (Para o Protagonista) Ele não está blefando. O Weevil usa cartas que impedem o ataque e causam dano por efeito a cada turno. É uma estratégia covarde, mas mortal. Limpe esse enxame antes que o veneno se espalhe!
*   **Weevil [Alegria]:** Sintam a picada da morte! DUELO!
**(Pós-Duelo - Vitória)**
*   **Weevil [Medo]:** (Gritando) MEUS INSETOS! O brilho... ele está queimando as asas deles! Como você pode destruir um enxame de milhares com uma única jogada?!
*   **Mai [Confiança]:** Viu só, nanico? No fim das contas, você é apenas um inseto sendo esmagado pela bota do destino. Excelente trabalho, [Nome do Jogador].
*   **Weevil [Tristeza]:** Isso não é justo... hi-hi... mas não pensem que venceram. O oceano está subindo... Mako Tsunami está bloqueando o caminho para o porto, e ele não vai ser tão fácil de "limpar" quanto eu...

#### 095: Mako Final (O Tsunami do Destino)
*   **Local:** Porto de Battle City - Noite. | **Avatar A:** Mai Valentine | **Avatar B:** Mako Final.
**(Pré-Duelo)**
*   **Mai [Séria]:** A névoa está tão espessa que mal consigo ver as luzes do farol. Mako está escondido em algum lugar nessas águas... e ele não é do tipo que desiste sem uma tempestade.
*   **Mako [Confiança]:** O oceano aceitou o seu desafio, [Nome do Jogador]! Eu treinei sob as ondas mais violentas para este momento. Meu pai me ensinou que a calmaria é apenas o prelúdio do caos!
*   **Mako [Séria]:** Meu deck de "Leviatãs Lendários" foi despertado. Eu vou submergir o seu "Forbidden Chaos" nas profundezas do abismo marítimo! Aqui, no meu domínio, você é apenas um náufrago lutando por fôlego!
*   **Mai [Confiança]:** (Para o Protagonista) Ele usa a "Umi" para esconder seus monstros e atacar diretamente. É um jogo de esconde-esconde mortal. Use o poder do seu Caos para evaporar o oceano dele antes que a maré te engula!
*   **Mako [Alegria]:** Pela honra do mar e da minha linhagem! DUELO!
**(Pós-Duelo - Vitória)**
*   **Mako [Tristeza]:** Incrível... o calor da sua alma ferveu as águas do meu oceano. Minhas criaturas marinhas recuaram diante da sua luz. Foi um duelo que meu pai teria orgulho de presenciar.
*   **Mai [Alegria]:** Você lutou com honra, Mako. Mas o meu parceiro aqui é uma força que nem o Poseidon conseguiria segurar.
*   **Mako [Confiança]:** Verdade... Siga em frente, duelista do Caos. Mas tome cuidado ao entrar naquele armazém abandonado... ouvi o som de engrenagens e o cheiro de óleo. Bandit Keith está preparando uma emboscada que não tem nada de honrada!

#### 096: Keith Final (O Metal da Vingança)
*   **Local:** Armazém Industrial Abandonado. | **Avatar A:** Joey Wheeler | **Avatar B:** Keith Final.
**(Pré-Duelo)**
*   **Joey [Raiva]:** Keith! Eu sabia que você ia tentar alguma sujeira no escuro! Apareça e lute como um homem, seu trapaceiro de marca maior!
*   **Keith [Deboche]:** (Saindo das sombras) Ora, se não é o "cachorrinho" do Wheeler. Eu não luto por medalhas, garoto. Eu luto para destruir quem se atreve a brilhar mais do que eu.
*   **Keith [Confiança]:** Meu deck de "Máquinas de Assalto" foi blindado com tecnologia roubada da corporação Kaiba e fortalecido pelo ódio que eu guardei todos esses anos. O seu "Forbidden Chaos" vai ser triturado pelas minhas engrenagens e cuspido como sucata!
*   **Joey [Medo]:** Cuidado, [Nome do Jogador]! Ele usa monstros que não podem ser destruídos por batalha no primeiro turno e cartas de "Gambiarras" que tiram pontos de vida de surpresa. Ele não joga limpo!
*   **Keith [Alegria]:** Prepare-se para ser fuzilado pelos meus canhões! Não haverá testemunhas! DUELO!
**(Pós-Duelo - Vitória)**
*   **Keith [Raiva]:** (Socando uma parede de metal) MALDIÇÃO! Como esse lixo de Caos conseguiu atravessar a minha blindagem?! Eu tinha o melhor deck que o dinheiro e o roubo podiam comprar!
*   **Joey [Alegria]:** Sabe o que o dinheiro não compra, Keith? Coração! E o meu amigo aqui tem de sobra. Suas máquinas agora são apenas ferro-velho, assim como a sua reputação!
*   **Keith [Medo]:** Isso não acabou... Marik vai acabar com vocês... mas antes... hi-hi... o "Menino Prodígio" está esperando por vocês na praça central. O Yugi Muto quer testar se o Caos de vocês é digno de enfrentar os Deuses!

#### 097: Yugi Muto (O Coração das Cartas)
*   **Local:** Praça Central de Battle City - Noite. | **Avatar A:** Yuto (Forma de Luz) | **Avatar B:** Yugi Muto.
**(Pré-Duelo)**
*   **Yugi [Alegria]:** [Nome do Jogador]! Eu estive observando sua jornada desde o primeiro duelo. O Faraó me disse que sentia uma energia vinda do seu deck que nem mesmo as Relíquias do Milênio poderiam explicar completamente.
*   **Yugi [Confiança]:** Eu não preciso do poder do Faraó para ver que você é um duelista excepcional. Mas para enfrentar o que vem a seguir, você precisa de mais do que poder... você precisa de equilíbrio.
*   **Yugi [Séria]:** Meu deck de "Magos e Sincronia de Estratégia" vai testar cada fibra do seu raciocínio. Eu vou usar o "Mago Negro" e suas variações para mostrar que a verdadeira força do Caos reside na harmonia das cartas!
*   **Yuto [Séria]:** (Para o Protagonista) Este é o Yugi original. Ele não usa a força bruta do Faraó, ele usa a inteligência pura. Ele vai tentar prever suas jogadas com cartas mágicas de resposta rápida. Não subestime a doçura dele... ele é um mestre!
*   **Yugi [Confiança]:** Vamos descobrir juntos o que o seu deck de Caos tem a nos dizer! É HORA DO DUELO!
**(Pós-Duelo - Vitória)**
*   **Yugi [Alegria]:** (Estendendo a mão) Incrível... Você não apenas venceu, você fluiu com o seu deck como se fossem um só. O coração das cartas bate forte dentro de você, [Nome do Jogador].
*   **Yuto [Alegria]:** Vencer o Yugi é o maior selo de qualidade que um duelista pode receber. O caminho agora está iluminado pela verdade!
*   **Yugi [Confiança]:** Você provou ser digno. Mas olhe para o topo daquela torre... Pegasus está esperando. Ele diz que quer transformar o seu Caos na sua maior obra-prima... ou no seu maior pesadelo. Boa sorte!

#### 098: Pegasus Final (A Ilusão Final)
*   **Local:** Cobertura da Torre Kaiba - Salão de Artes. | **Avatar A:** Mai Valentine | **Avatar B:** Pegasus Final.
**(Pré-Duelo)**
*   **Mai [Medo]:** Pegasus... eu ainda sinto calafrios só de entrar na mesma sala que ele. Ele não precisa mais do Olho do Milênio para ser perigoso; o gênio dele por trás das cartas Toon é o suficiente para enlouquecer qualquer um!
*   **Pegasus [Alegria]:** Welcome back, [Nome do Jogador]-boy! Ou deveria dizer... o Grande Maestro do Caos? Suas vitórias recentes foram tão... dramáticas! Eu simplesmente tive que pintar um novo cenário para o nosso reencontro.
*   **Pegasus [Confiança]:** Meu "Mundo Toon" foi refeito com as cores do seu desespero! Eu vou transformar seus monstros de Caos em meras caricaturas cômicas sob o meu comando. O meu deck de "Ilusão e Controle de Mente" vai mostrar que a realidade é apenas o que eu decido desenhar!
*   **Mai [Raiva]:** Não escuta as piadinhas dele! O Pegasus usa cartas que removem seus monstros do jogo para invocar versões Toon deles. Ele vai tentar virar a sua própria força contra você!
*   **Pegasus [Deboche]:** It's showtime! Vamos ver se o seu Caos consegue sobreviver à minha imaginação sem limites! DUELO!
**(Pós-Duelo - Vitória)**
*   **Pegasus [Tristeza]:** Oh, no... minha obra-prima foi despedaçada! O seu Caos é uma cor que eu não consigo misturar na minha paleta... é algo que brilha com uma luz própria, além da minha compreensão!
*   **Mai [Alegria]:** Parece que o "Mundo Toon" não foi páreo para a realidade do Caos, Pegasus! O show acabou para você.
*   **Pegasus [Confiança]:** Indeed... você venceu o criador. Mas agora... você deve enfrentar a criatura que domina este império. Seto Kaiba está no heliporto acima de nós. Ele disse que se você não puder vencer o 'Blue-Eyes' dele, você não terá o direito de sequer olhar para o Marik!

#### 099: Kaiba Final (O Império do Dragão Branco)
*   **Local:** Heliporto da Torre Kaiba - Tempestade. | **Avatar A:** Yami Yugi | **Avatar B:** Kaiba Final.
**(Pré-Duelo)**
*   **Kaiba [Raiva]:** Então você finalmente chegou, [Nome do Jogador]! Eu vi você esmagar o "Mundo Toon" do Pegasus e brincar com o Yugi, mas comigo... a brincadeira acaba agora! Eu não acredito em "destino" ou em "Caos Proibido". Eu acredito em poder tecnológico e força bruta!
*   **Kaiba [Confiança]:** Meu "Blue-Eyes White Dragon" não é apenas uma carta; é o ápice da evolução dos duelos! Eu saturei meu deck com as cartas mais raras da Terra. Vou provar que o seu Caos é apenas uma falha estatística diante da minha supremacia!
*   **Yami Yugi [Séria]:** (Para o Protagonista) Kaiba está possuído por uma obsessão que beira a loucura. Ele vai invocar o "Blue-Eyes Ultimate Dragon" e usar o "Obelisco, o Atormentador" se tiver a chance! Você precisa ser mais rápido que a luz dele. Não deixe o orgulho dele te intimidar!
*   **Kaiba [Alegria]:** Sinta o rugido que atravessa o tempo e o espaço! Prepare-se para ser varrido do meu império! DUELO!
**(Pós-Duelo - Vitória)**
*   **Kaiba [Tristeza]:** (Caindo de joelhos) Impossível... O meu Dragão Branco... superado por uma energia que nem os meus supercomputadores conseguem calcular?! Como você... um simples duelista... pôde derrubar o meu legado?!
*   **Yami Yugi [Alegria]:** Kaiba, você buscou o poder no metal, mas este duelista buscou o poder na alma do Caos. O resultado estava escrito nas estrelas desde o início.
*   **Kaiba [Raiva]:** (Levantando-se) Cale-se, Faraó! [Nome do Jogador]... você venceu o meu corpo, mas o Marik... o Marik vai devorar a sua alma. Ele está no Dirigível de Comando, logo acima de nós. Se você perder para ele, eu mesmo te deletarei da história! VÁ!

#### 100: Marik Ishtar (O Sol Alado da Agonia - Grande Chefe Final)
*   **Local:** O Santuário das Trevas - Dirigível. | **Avatar A:** Yuto (Forma Divina) | **Avatar B:** Marik Ishtar.
**(Pré-Duelo)**
*   **Marik [Alegria]:** (Risada maníaca) Hahaha! Bem-vindo ao seu funeral, escolhido do Caos! Você percorreu um longo caminho para entregar a sua alma diretamente para o meu Dragão Alado de Rá! O eclipse está completo... e o meu reinado de dor está apenas começando!
*   **Marik [Confiança]:** O seu "Forbidden Chaos" é uma piada diante do Criador da Luz e das Trevas! Eu vou usar o seu sofrimento como combustível para o meu Deus Egípcio. Cada ponto de vida que você perder será sentido na sua pele! Este não é um duelo... é um sacrifício milenar!
*   **Yuto [Séria]:** (Voz telepática) [Nome do Jogador], este é o momento para o qual você nasceu. O Marik vai usar o "Dragão Alado de Rá" em sua forma de Fênix. Ele vai tentar remover cada carta sua e te deixar sem defesas. Concentre toda a energia do Caos em um único ponto! Nós estamos com você!
*   **Joey & Mai (Voz) [Confiança]:** Detona esse cara de olhos malucos, parceiro! Pelo Reino dos Duelistas! Mostre a ele que a verdadeira luz não pode ser engolida pelas sombras!
*   **Marik [Alegria]:** SURJAM AS TREVAS! RÁ, DESPERTE E INCINERE O MUNDO! É O DUELO FINAL!
**(Pós-Duelo - Final do Jogo)**
*   **Marik [Tristeza]:** (O Cetro do Milênio explode) NÃO! O meu Deus... o Sol Alado... ele está se curvando diante do Caos?! Como uma força mortal pode apagar a divindade egípcia?! A escuridão... ela está me levando... NÃOOOOO!
*   **Yuto [Alegria]:** (Brilhando com luz pura) VOCÊ CONSEGUIU! O ciclo de cinco mil anos de ódio foi quebrado! O Caos Proibido trouxe a ordem de volta ao mundo!
*   **Joey [Alegria]:** VOCÊ É O REI DOS JOGOS, CARA!
*   **Yugi [Alegria]:** Você salvou o mundo, meu amigo. A lenda do Caos agora viverá para sempre.
*   *(A cena corta para você e seus amigos na praia, olhando o sol nascer sobre Battle City. O seu deck brilha uma última vez.)*
*   **[Sistema]:** Parabéns! Você zerou "Yu-Gi-Oh! Forbidden Chaos".
#### 100: Marik Ishtar (O Sol Alado da Agonia - Grande Chefe Final)
*   **Local:** O Santuário das Trevas - Dirigível.
*   **Avatar A:** Yuto (Forma Divina).
*   **Avatar B:** Marik Ishtar.
**(Pré-Duelo)**
*   **Marik [Alegria]:** (Risada maníaca) Hahaha! Bem-vindo ao seu funeral, escolhido do Caos!
*   **Yuto [Séria]:** (Voz telepática) [Nome do Jogador], este é o momento para o qual você nasceu...
**(Pós-Duelo - Final do Jogo)**
*   **Marik [Tristeza]:** NÃO! O meu Deus... o Sol Alado... ele está se curvando diante do Caos?!
*   **Yuto [Alegria]:** (Brilhando com luz pura) VOCÊ CONSEGUIU! O ciclo de cinco mil anos de ódio foi quebrado! O Caos Proibido trouxe a ordem de volta ao mundo!
*   **[Sistema]:** Parabéns! Você zerou "Yu-Gi-Oh! Forbidden Chaos".
