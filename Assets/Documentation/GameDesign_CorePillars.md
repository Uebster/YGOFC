# Game Design & Core Pillars (Yu-Gi-Oh! Forbidden Chaos)

## 1. O Propósito Deste Documento
Este é o **Documento Mestre (Hub)** do projeto. Caso todo o código-fonte seja perdido, a reconstrução deve começar por aqui. Este arquivo define as leis imutáveis do jogo e a arquitetura de alto nível, servindo como um mapa para os demais documentos técnicos.

---

## 2. Escopo e Limitações Estritas (A Regra de Ouro)
O jogo não é um simulador genérico de Yu-Gi-Oh!. Ele é uma "Cápsula do Tempo" desenhada com fronteiras matemáticas exatas:
*   **Data Limite:** O banco de dados abrange exclusivamente os lançamentos do formato **OCG até a data de 31/05/2005** (Fim da era *Cybernetic Revolution*).
*   **Capacidade do Banco de Dados:** Exatos **2147 IDs** de cartas únicos. Nenhuma carta de eras posteriores (Synchro, Xyz, Pendulum, Link) é reconhecida ou suportada pela engine.
*   **Textos e Erratas:** O jogo visa o comportamento clássico, utilizando textos pré-errata (Ex: *Sinister Serpent* retorna à mão sem se banir depois, *Ring of Destruction* pode causar empates).

---

## 3. Regras de Tabuleiro (Master Rule 1 / Goat Format)
O motor do jogo (`GameManager`) opera sob as seguintes restrições lógicas:
*   **Zonas:** 5 Zonas de Monstros, 5 Zonas de Magia/Armadilha, 1 Zona de Campo, 1 Cemitério, 1 Deck, 1 Extra Deck e 1 Zona de Banimento (Removidas). **Não existem** Extra Monster Zones ou Zonas de Pêndulo.
*   **Mão Inicial e Saque:** Cada jogador começa com 5 cartas. O jogador que começa o duelo **compra uma carta (Draw)** no seu primeiro turno (regra antiga).
*   **Limite de Mão:** 6 Cartas na End Phase (descarte obrigatório se exceder).
*   **Banlist Padrão:** Reflete o formato histórico de Setembro de 2005 (Goat Format limit), gerenciada ativamente no `DeckBuilderSystem`.

---

## 4. O Fluxo de Execução Assíncrono (A "Alma" do Código)
Se o código for perdido, o programador deve entender que **este jogo não roda em um fluxo linear tradicional**. Ele é amplamente impulsionado por *Corrotinas (IEnumerator)* e *Callbacks (Action)*.

Como o Yu-Gi-Oh! é um jogo de "respostas" (Chains) e seleções (Targeting), a thread principal da Unity **nunca pode travar**.
*   **Padrão de Espera:** Quando uma carta exige que o jogador escolha um alvo, a engine inicia uma corrotina, abre a UI de seleção e entra em um loop `while (isWaiting) yield return null;`.
*   **Delegação:** O jogo só prossegue quando a Interface do Usuário (UI) devolve a resposta através de um callback (ex: `onTargetSelected?.Invoke(card)`).

**Para entender como a UI e a Engine conversam, leia:** `DuelPanelStructure.md` e `ManagersOverview.md`.

---

## 5. Índice Mestre de Documentação (A Lista Completa)
Para garantir a integridade da IA e evitar fragmentação de contexto, o projeto foi consolidado de 35 pequenos arquivos para **9 Super Documentos**. 
Abaixo está o mapa exato e estruturado hierarquicamente de onde encontrar cada mecânica do jogo. **Nenhuma dedução deve ser feita fora do que está estritamente descrito nesta biblioteca.**

### 1. 🏛️ `GameDesign_CorePillars.md` (O Pilar de Design)
*   **1.1.** Escopo e Limitações Estritas (Regra das 2147 cartas, formato Goat/2005)
*   **1.2.** Regras de Tabuleiro (Master Rule 1)
*   **1.3.** O Fluxo de Execução Assíncrono (A "Alma" do Código)

### 2. 💾 `DB_And_SaveSystem.md` (Dados e Persistência)
*   **2.1.** Banco de Dados de Cartas (`cards.json`)
    *   **2.1.1** Estrutura Técnica e JSON (`CardData`)
    *   **2.1.2** Convenção de Imagens e Lazy Loading
    *   **2.1.3** Escopo de IDs (Classic 2147)
*   **2.2.** Ranking de Cartas e Pools (Tiers)
    *   **2.2.1** Classificação de Monstros (Tier 0 a 4)
    *   **2.2.2** Classificação de Magias e Armadilhas (Pool A, B, C)
*   **2.3.** Ferramentas e Pipeline de Dados (Python & Unity)
    *   **2.3.1** Sistema de Pools (`generate_pool_template.py` / `apply_pools_to_json.py`)
    *   **2.3.2** Geração Mestra (`generate_assets.py`, conversão TSV/Power of Chaos/Forbidden Memories)
    *   **2.3.3** Aquisição e Scrapers (`download_cards.py`, YGOPRODeck API)
    *   **2.3.4** Geradores de Personagens e Bots (`generate_characters.py`, `generate_character_decks.py`, `generate_character_rewards.py`)
    *   **2.3.5** Validadores Externos (`test_card_viewer.py`, Pygame Viewer, `test_deck_system.py`, `generate_fields.py`)
    *   **2.3.6** Ferramentas de Editor Unity (`HierarchyDumper.cs`)
    *   **2.3.7** Debug In-Game (`InGameDebugConsole.cs`, Ctrl+Shift+D)
*   **2.4.** Sistema de Save e Carregamento (Persistência)
    *   **2.4.1** Estrutura de Memória (`GameSaveData`)
    *   **2.4.2** Interface de Save/Load (`SaveLoadMenu.cs`)
    *   **2.4.3** Receitas de Deck Locais (`DeckRecipe`, `DeckImportExportManager`)

### 3. ⚙️ `Engine_And_GameLoop.md` (O Motor Principal)
*   **3.1.** Visão Geral dos Gerenciadores
    *   Singletons: `PhaseManager`, `SummonManager`, `BattleManager`, `SpellTrapManager`, `ChainManager`, `SpellCounterManager`, `CardEffectManager`, `DuelFXManager`.
*   **3.2.** Funções Vitais e Configurações (`GameManager.cs`)
    *   **3.2.1** Controle de Duelo, Vida e Dados (`StartDuel`, `EndDuel`, `DamagePlayer`, `GetPlayerMainDeck`)
    *   **3.2.2** Ações de Cartas e Tabuleiro (`TrySummonMonster`, `PlaySpellTrap`, `CreateCardLink`, `MillCards`)
    *   **3.2.3** Modos de Jogo, Debug e Testes In-Game (`devMode`, Hierarquia de Pulos `testDuelDirectly`, `unlockAllCards`, `infiniteLP`, `disableBanlist`)
    *   **3.2.4** Opções de Input, UX e Velocidade (`useMouseTooltipUI`, `quickSummonFromHand`, `playerDrawSpeed`)
*   **3.3.** O Sistema de Fases e Turnos (`PhaseManager.cs`)
    *   **3.3.1** O Ciclo de Fases (`GamePhase` Enum, Hooks Automáticos e Manuais)
    *   **3.3.2** UI de Fases (Neon Effect e Avanço Manual)
*   **3.4.** O Sistema Assíncrono e Modais (Interrupções e Escolhas)
    *   **3.4.1** A Regra de Ouro (Padrão `While-Yield`)
    *   **3.4.2** API de Modais da Engine (`ShowConfirmation`, `StartTargetSelection`, `OpenCardMultiSelection`, `GlobalCardSearchUI`, `MultipleChoiceUI`, `NumericSelectionUI`, `ReorderCardsUI`)
    *   **3.4.3** Comportamento sob Simulação (O Bypass `isSimulating`)

### 4. ⚔️ `Rules_Combat_And_Board.md` (As Regras de Combate e Tabuleiro)
*   **4.1.** A Matemática Oculta do Combate (`BattleManager.cs`)
    *   **4.1.1** O Fluxo de Ataque (Battle Step a End Step)
    *   **4.1.2** O Filtro de Dano (`DealBattleDamage`, Dano Nulo, Reflexão)
    *   **4.1.3** Dano Perfurante (`hasPiercing`)
    *   **4.1.4** Ataques Diretos
*   **4.2.** Arquitetura do Sistema de Correntes (`ChainManager.cs`)
    *   **4.2.1** A Anatomia de um Elo da Corrente (`ChainLink`)
    *   **4.2.2** A Lei das Velocidades (Spell Speeds)
    *   **4.2.3** O Fluxo de Construção (Building the Chain)
    *   **4.2.4** O Fluxo de Resolução (Pilha LIFO)
    *   **4.2.5** Como as Negações Funcionam no Código (`GetLinkToNegate`, `NegateAndDestroy`)
*   **4.3.** Arquitetura Lógica do Tabuleiro e Vínculos (`DuelFieldUI.cs` & `CardLink.cs`)
    *   **4.3.1** Busca e Alocação de Zonas (`GetFreeZone`)
    *   **4.3.2** Sistema de Bloqueio Físico de Zonas (`blockedZonesByCard`)
    *   **4.3.3** O Sistema de Vínculos e Equipamentos (`CardLink`)
*   **4.4.** Anatomia e Máquina de Estados da Carta (`CardDisplay.cs`)
    *   **4.4.1** Ciclo de Vida Visual (Lazy Loading, Eixos 3D, Hover)
    *   **4.4.2** Motor de Status Dinâmico (`StatModifier`, `RecalculateStats`)
    *   **4.4.3** As Flags de Memória (Restrições, `isTrapMonster`, Timers)
    *   **4.4.4** Delegação de Cliques (Roteador de Fase/Alvo)
*   **4.5.** Sistema de Special Summon
    *   **4.5.1** Fluxo de Código (`PerformSpecialSummon`, `PositionSelectionUI`)
    *   **4.5.2** Tipos de Special Summon Suportados

### 5. 📖 `Card_Programming_API.md` (A Bíblia de Programação de Cartas)
*   **5.1.** Arquitetura do Sistema de Efeitos (`CardEffectManager`)
    *   **5.1.1** Estrutura de Arquivos (Core, Impl, Registry, PartX)
    *   **5.1.2** Fluxo de Execução
*   **5.2.** Referência de Gatilhos e Hooks da Engine
    *   **5.2.1** Hooks de Fases e Turno (`OnPhaseStart`, `OnPreDrawPhaseImpl`)
    *   **5.2.2** Hooks de Batalha (`OnAttackDeclared`, `OnDamageCalculation`, `OnBattleEnd`, `IsAttackRestricted`)
    *   **5.2.3** Hooks de Dano e Pontos de Vida (`OnDamageDealtImpl`, `OnDamageTaken`, `OnLifePointsGained`)
    *   **5.2.4** Hooks de Movimentação e Destruição (`OnCardSentToGraveyard`, Missing Timing, `OnCardLeavesField`, `OnCardDiscardedImpl`)
    *   **5.2.5** Hooks de Estado de Campo e Magias (`OnSummonImpl`, `OnSetImpl`, `OnBattlePositionChangedImpl`, `OnSpellActivated`)
*   **5.3.** API de Helpers e Caixa de Ferramentas (Card Helpers)
    *   **5.3.1** Dano e Vida (`Effect_DirectDamage`, `Effect_GainLP`)
    *   **5.3.2** Buscas e Filtros no Tabuleiro (`CheckActiveCards`)
    *   **5.3.3** Alteração de Status (ATK / DEF) (`AddStatModifier`, `RemoveModifiersFromSource`)
    *   **5.3.4** Manipulação de Deck, Mão e Cemitério (`Effect_SearchDeck`, `BanishCard`, `ReturnToHand`)
    *   **5.3.5** Lógicas Empacotadas (Subtipos) (`Effect_Equip`, `Effect_Field`, `Effect_ChangeControl`, `BeginFusionSummon`)
    *   **5.3.6** Interações de Contra-Ataque e Negação (`GetLinkToNegate`, `NegateAndDestroy`)
    *   **5.3.7** Fichas, Armadilhas e Miscelânea (`SpawnToken`, `ConvertTrapToMonster`)
*   **5.4.** Dicionário Global de Estados e Variáveis de Memória
    *   **5.4.1** Estado Local da Instância (`CardDisplay` Flags)
    *   **5.4.2** Memória Global e Condições de Campo (`CardEffectManager` Flags)
    *   **5.4.3** Estado Central da Partida (`GameManager` e `SpellTrapManager`)
*   **5.5.** Sistemas Complexos e Efeitos Contínuos
    *   **5.5.1** Efeitos Contínuos e Restrições Globais (Locks)
    *   **5.5.2** Efeitos Retardados (Delayed Effects)
    *   **5.5.3** Buffs Dinâmicos (Dynamic Stat Buffs)
    *   **5.5.4** Bloqueio Físico de Zonas (Zone Blocking)
    *   **5.5.5** Interceptação de Dano de Efeito (Pre-Damage Hook)
    *   **5.5.6** Transferência Dinâmica de Equipamentos e Vínculos
    *   **5.5.7** Sistema de Vínculo Toon (Toon Link)
*   **5.6.** Interações de UI, Modais e Minigames
    *   **5.6.1** O Padrão Assíncrono (While-Yield e Delegação para IA)
    *   **5.6.2** Tipos de Modais Oficiais da Engine
    *   **5.6.3** O Bypass de Simulação (Crucial para o DevMode)
    *   **5.6.4** Sistema Global de Busca de Cartas (`GlobalCardSearchUI`)
    *   **5.6.5** Sistema de Reordenação e Seleção por Soma Matemática
    *   **5.6.6** Minigames Nativos (Moedas, Dados e Re-Rolagem)
    *   **5.6.7** Relógios e Turnos Virtuais (`TurnClockUI`)
    *   **5.6.8** Revelação Silenciosa (Silent Reveal)
    *   **5.6.9** Regras de Validação de Ativação (Activation Legality)

### 6. 🧠 `AI_And_Characters.md` (Inteligência Artificial e Personagens)
*   **6.1.** Design da Inteligência Artificial (`OpponentAI.cs`)
    *   **6.1.1** O Sistema de Pontuação (Scoring Engine)
    *   **6.1.2** Perfis de Personalidade (AI Archetypes)
    *   **6.1.3** As Regras de Ouro (Fog of War, Card Advantage, Suicide Crash, etc.)
    *   **6.1.4** Lógicas Específicas de Cartas e Win-Cons (Relinquished, Destiny Board)
    *   **6.1.5** Estrutura do Loop de Decisão (Decision Tree)
*   **6.2.** Sistema de Personagens e Decks (`CharacterDatabase.cs`)
    *   **6.2.1** Visão Geral e IDs
    *   **6.2.2** Estrutura de Dados (`CharacterData`, Drops)
    *   **6.2.3** Sistema de 3 Decks (Variantes A, B, C)

### 7. 🗺️ `Campaign_And_Story.md` (Campanha e História)
*   **7.1.** Lógica do Mapa e Progressão (`CampaignManager.cs`)
*   **7.2.** Sistema de Diálogos (Visual Novel)
*   **7.3.** Lista de Atos e Oponentes Oficiais
*   **7.4.** Roteiro Completo da Campanha (Visual Novel Scripts)

### 8. 🃏 `Deck_Management_System.md` (Construção e Gestão de Baralhos)
*   **8.1.** Sistema de Construção de Decks
    *   **8.1.1** Baú de Cartas / Virtual Scrolling (`DeckBuilderManager.cs`, `TrunkScrollManager.cs`, `TrunkCardScrollItem.cs`, `ChestCardItem.cs`, Troubleshoot de Flickering)
    *   **8.1.2** Estrutura de UI (Hierarquia: `Panel_DeckBuilder`, `Panel_CardViewer`, `Input_SearchCard`, `Panel_Filters`, `Panel_CardChest`, `Panel_MainDeck`, `Panel_SideDeck`, `Panel_ExtraDeck`)
    *   **8.1.3** Funcionalidades (Filtros Atributo/Raça/Tipo, Ordenação ABC/ATK/DEF, Indicador Tag "NEW", Validação de Limites)
    *   **8.1.4** Drag and Drop e Validação de Zonas (`DeckDragHandler.cs`, `DeckDropZone.cs`)
    *   **8.1.5** Layout Customizado (`CustomDeckLayout.cs`, Flow Layout dinâmico)
    *   **8.1.6** Estrutura do Prefab do Baú (`Card_PrefabChestList`, Ícones dinâmicos `AttributeIcon`/`RaceIcon`/`TypeIcon`/`SubTypeIcon`)
    *   **8.1.7** Banlist Ativa (Proibidas, Limitadas, Semi-Limitadas do formato Goat)
*   **8.2.** Sistema de Importação e Exportação
    *   **8.2.1** Estrutura da UI de Import/Export (`Panel_ImportExport`, `Panel_Export`, `Panel_Import`, `Input_DeckName`)
    *   **8.2.2** Arquitetura e Scripts (`DeckImportExportManager.cs`, Classe `DeckRecipe` em `SaveLoadSystem.cs`, `DeckSlotUI.cs`, Ponte `ExportCurrentDeck`/`ImportDeck`)
*   **8.3.** Sistema de Deck Inicial
    *   **8.3.1** O Gerador Procedural (`InitialDeckBuilder.cs`)
    *   **8.3.2** Estrutura das 5 Pools (Fodder, Warriors, Ace, Spells, Traps)
    *   **8.3.3** Regras de Filtragem Internas (`forbiddenIds`, Limite de Nível)

### 9. 🏆 `Progression_UI_And_Testing.md` (Progressão, Recompensas e Visuais)
*   **9.1.** Sistema de Pontuação e Drop Rate (`DuelScoreManager.cs`, `RewardPanelUI.cs`, Cálculo Base, Bônus, Ranks S+ a F, Drop Pools JSON)
*   **9.2.** Sistema de Troféus e Conquistas (`TrophyManager.cs`, As 7 Categorias Temáticas)
*   **9.3.** Sistema de Biblioteca (Library System)
    *   **9.3.1** Menu Principal (`Panel_Library`)
    *   **9.3.2** Biblioteca de Duelistas (`Panel_LibDuelists`, `DuelistLibraryManager.cs`, Desbloqueio)
    *   **9.3.3** Biblioteca de Cartas (`Panel_LibCards`, `CardLibraryManager.cs`, Cartas não obtidas e Visor)
    *   **9.3.4** Biblioteca de Decks (`Panel_LibDecks`, `DeckLibraryManager.cs`, Grind de Variantes A/B/C)
    *   **9.3.5** Biblioteca de Arenas (`Panel_LibArenas`, `ArenaLibraryManager.cs`)
*   **9.4.** Mecânica Efêmera da Tag "NEW" (Lógica de Persistência, `SaveLoadSystem.MarkCardAsUsed`)
*   **9.5.** Sistemas de Teste, Debug e UI Dinâmica (`DamagePopupManager.cs`, `MouseTooltipUI.cs`, `FullTestManager.cs`, Toggles, Dev Card Menu, `InGameDebugConsole.cs`)
*   **9.6.** Simulador de Caos (`SimulationManager.cs`, Bypass de UI, Visual Mode 1.5x, Fast Mode 50x)
*   **9.7.** Sistema de Temas de Duelo (`DuelThemeManager.cs`, ScriptableObject `DuelTheme`, Minigames visuais)
*   **9.8.** Efeitos Visuais e Sonoros (`DuelFXManager.cs`, Tabela de VFX de Cartas/Batalha, Música Dinâmica/BGM)

---

## 6. Protocolo de Manutenção da Base de Conhecimento (Instruções Estritas para a IA)

Para preservar a integridade desta "Bíblia" arquitetural e impedir a perda de contexto ou fragmentação de memória, a IA assistente deve operar **OBRIGATORIAMENTE** sob o seguinte fluxo ao criar documentações ou revisar grandes lógicas:

*   **Etapa 0 - Verificação e Contenção (Zero Suposições):** Nunca deduza lógicas ausentes, nunca aplique regras genéricas de TCG e não invente nomes de variáveis. Procure sempre nos documentos listados acima. Caso a informação, script ou contexto necessário para responder não seja encontrado, **INTERROMPA** a geração da resposta e peça orientações precisas ou o arquivo faltante ao usuário.
*   **Etapa 1 - Enriquecimento Centralizado:** Priorize utilizar a arquitetura dos 9 Super Documentos já criados. Ao documentar uma funcionalidade nova, não crie novos arquivos avulsos. Em vez disso, incorpore e expanda as seções existentes do documento apropriado. O foco é enriquecer e detalhar, nunca resumir, reduzir ou apagar dados antigos (mantendo taxonomia, assinaturas e notas intactas).
*   **Etapa 2 - Mapeamento Cirúrgico no Índice Mestre:** Com o arquivo atualizado, é mandatório retornar ao Pilar Central (`GameDesign_CorePillars.md`) e atualizar o **Índice Mestre (Seção 5)**. Toda nova função, citação, classe, script, gancho (hook), componente ou funcionalidade de UI criada deve constar no índice de forma detalhada e rastreável, utilizando o sistema de numeração decimal (ex: `2.1.3`). Isso garante que um simples "Ctrl+F" revele imediatamente o local exato de qualquer mecânica na documentação.
*   **Etapa 3 - Relatório de Transparência:** Na conclusão da resposta fornecida ao usuário, apresente um relatório (Status) informando clara e detalhadamente o que foi incluído no código/documento e, caso algo tenha sido intencionalmente excluído ou postergado, forneça a devida justificativa para essa tomada de decisão.