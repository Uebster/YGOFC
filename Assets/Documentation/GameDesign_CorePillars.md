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
    *   **2.3.1** Sistema de Pools (Heurística, Banlist e Template CSV, `generate_pool_template.py` / `apply_pools_to_json.py`)
    *   **2.3.2** Geração Mestra (Conversão TSV/PoC/FM e Lazy Loading, `generate_assets.py`)
    *   **2.3.3** Aquisição e Scrapers (`download_cards_ultimate.py`, UI Flask, Multi-Task)
    *   **2.3.4** Geradores de Personagens e Bots (Temas, Cores, Dependências e Escalamento A/B/C, `generate_characters.py`, `generate_character_decks.py`, `generate_character_rewards.py`)
    *   **2.3.5** Validadores Externos (`test_card_viewer.py`, Pygame Viewer, `test_deck_system.py`, `generate_fields.py`)
    *   **2.3.6** Ferramentas de Editor Unity (`HierarchyDumper.cs`, `InspectorDumper.cs`, `VFXOptimizer.cs`)
    *   **2.3.7** Debug In-Game (`InGameDebugConsole.cs`, Ctrl+Shift+D)
    *   **2.3.8** Analisador Estático de Scripts Lua (`cardslua_analyzer.py`)
*   **2.4.** Sistema de Save e Carregamento (Persistência)
    *   **2.4.1** Estrutura de Memória (`GameSaveData`)
    *   **2.4.2** Interface de Save/Load (`SaveLoadMenu.cs`)
    *   **2.4.3** Receitas de Deck Locais (`DeckRecipe`, `DeckImportExportManager`)

### 3. ⚙️ `Engine_And_GameLoop.md` (O Motor Principal)
*   **3.1.** Visão Geral dos Gerenciadores
    *   Arquitetura de Singletons: `GameManager` (Orquestrador), `PhaseManager` (Máquina de Estados de Tempo), `CardEffectManager` (Motor de Efeitos e Correntes), `DuelFXManager` (VFX/SFX).
*   **3.2.** O Grande Cérebro Dividido (A Arquitetura Modular do `GameManager`)
    *   **3.2.1** `GameManager.cs` (O Core, Variáveis Globais e Modos de Jogo: `devMode`, `infiniteLP`, `EnsureCoreManagers`)
    *   **3.2.2** `GameManager_Phases.cs` (Orquestração: `StartDuel`, `EndDuel`, `SwitchTurn`, `TurnTransitionRoutine`, `OnDrawPhaseStart`)
    *   **3.2.3** `GameManager_BoardActions.cs` (Movimentação e Magias: `MoveCard`, `SendToGraveyard`, `PlaySpellTrap`, `EquipMonsterToMonster`)
    *   **3.2.4** `GameManager_Decks.cs` (Pilhas e Saque: `DrawCard`, `ShuffleDeck`, `InitializePlayerDeck`, `ViewGraveyard`)
    *   **3.2.5** `GameManager_Stats.cs` (Vida e UI: `DamagePlayer`, `UpdateCardViewer`, `RefreshAllCardsVisuals`, `Dev_InjectDependencies`)
    *   **3.2.6** `GameManager_Summons.cs` (Invocações: `TrySummonMonster`, `PerformSpecialSummon`, `BeginFusionSummon`, `SpawnToken`)
*   **3.2.7** `GameManager_Selections.cs` (Miras e Minigames: `RefreshAttackIndicators`, `Smart Select & Power-Set`, `TossCoin`, `RollDice`)
*   **3.3.** O Sistema de Fases e Turnos (`PhaseManager.cs`)
    *   **3.3.1** O Ciclo de Fases (`GamePhase` Enum, Hooks Automáticos e Manuais)
    *   **3.3.2** UI de Fases (Neon Effect e Avanço Manual)
*   **3.4.** O Sistema Assíncrono e Modais (Interrupções e Escolhas)
    *   **3.4.1** A Regra de Ouro (Padrão `While-Yield`)
    *   **3.4.2** API de Modais da Engine (`ShowConfirmation`, `StartTargetSelection`, `OpenCardMultiSelection`, `GlobalCardSearchUI`, `MultipleChoiceUI`, `NumericSelectionUI`, `ReorderCardsUI`)
    *   **3.4.3** Comportamento sob Simulação (O Bypass `isSimulating`)

### 4. ⚔️ `Rules_Combat_And_Board.md` (As Regras de Combate e Tabuleiro)
*   **4.1.** A Matemática Oculta do Combate (Fluxo de Batalha C# -> Lua)
    *   **4.1.1** O Fluxo de Ataque (UI -> `CardEffectManager` -> Lua `Core.Attack`)
    *   **4.1.2** Dano Perfurante (`hasPiercing`)
*   **4.2.** Arquitetura do Sistema de Correntes (`CardEffectManager.cs`)
    *   **4.2.1** A Anatomia de um Elo da Corrente (`ChainLink`)
    *   **4.2.2** A Lei das Velocidades (Spell Speeds)
    *   **4.2.3** O Fluxo de Construção e Resolução (LIFO)
    *   **4.2.4** Como as Negações Funcionam no Código (`isActivationNegated` Flag)
*   **4.3.** Arquitetura Lógica do Tabuleiro e Vínculos (`DuelFieldUI.cs` & `CardLink.cs`)
    *   **4.3.1** Busca e Alocação de Zonas (`GetFree...Zone`)
    *   **4.3.2** Sistema de Bloqueio Físico de Zonas (Zone Locks)
    *   **4.3.3** O Sistema de Vínculos e Equipamentos (`CardLink`)
*   **4.4.** Anatomia e Máquina de Estados da Carta (`CardDisplay.cs`)
    *   **4.4.1** Ciclo de Vida Visual (Lazy Loading)
    *   **4.4.2** Motor de Status Dinâmico (`RecalculateStats`)
    *   **4.4.3** As Flags de Memória (Estados de Turno)
    *   **4.4.4** Delegação de Cliques (Click Event Router, Menu Clássico vs One-Click, Response Chain)
*   **4.5.** Sistema de Special Summon
    *   **4.5.1** Fluxo de Código (`PerformSpecialSummon`, `PositionSelectionUI`, `FinalizeSummon`)
    *   **4.5.2** Tipos de Special Summon Suportados
*   **4.6.** Interfaces de Seleção (Smart Select & Power-Set, Right-Click to Finish, View-Only)

### 5. 📖 `Card_Programming_API.md` (A Bíblia de Programação de Cartas)
*   **5.1.** Arquitetura do Sistema de Efeitos (`CardEffectManager` e MoonSharp)
    *   **5.1.1** Estrutura de Arquivos e Componentes (A Ponte C# <-> LUA)
    *   **5.1.2** O Fluxo de Execução e o Sistema `chk` (Validação Silenciosa -> Ativação -> Corrente -> Resolução)
    *   **5.1.3** A Supremacia do `LuaEngineCore` e as Constantes
*   **5.2.** Referência de Gatilhos C# e Escutas LUA (Event Listeners)
    *   **5.2.1** Hooks de Fases e Turno (`OnPhaseStart`, `OnPreDrawPhase`)
    *   **5.2.2** Hooks de Batalha (Iniciados pela UI, `Core.Attack` em Lua, `EVENT_ATTACK_ANNOUNCE`)
    *   **5.2.3** Hooks de Dano e Pontos de Vida (`OnDamageTaken`, `OnLifePointsGained`)
    *   **5.2.4** Hooks de Movimentação e Destruição (`OnCardSentToGraveyard`, `OnCardLeavesField`)
    *   **5.2.5** Hooks de Estado de Campo e Magias (`OnSummon`, `OnSet`, `OnSpellActivated`)
*   **5.3.** API de Helpers e Caixa de Ferramentas C#
    *   **5.3.1** Vida e Dano (`Effect_DirectDamage`, `Effect_GainLP`)
    *   **5.3.2** Buscas e Filtros no Tabuleiro (`GetEquippedCards`)
    *   **5.3.3** Alteração de Status (Motor interno do `CardDisplay`)
    *   **5.3.4** Manipulação de Deck, Mão e Cemitério (`MillCards`, `BanishCard`, `ReturnToHand`)
    *   **5.3.5** Lógicas Empacotadas (`BeginFusionSummon`, `BeginRitualSummon`)
    *   **5.3.6** Interações de Negação (Flag `isActivationNegated` no `ChainLink`)
    *   **5.3.7** Fichas e Miscelânea (`SpawnToken`)
*   **5.4.** Dicionário Global de Estados e Variáveis de Memória
    *   **5.4.1** Estado Local da Instância (`CardDisplay` Flags: `hasAttackedThisTurn`, `isFlipped`)
    *   **5.4.2** Memória Global (`CardEffectManager` Flags: `currentChain`, `isChainResolving`)
    *   **5.4.3** Estado Central da Partida (`GameManager`: LPs, Turno, Contadores)
*   **5.5.** Sistemas Complexos e Efeitos Contínuos
    *   **5.5.1** Efeitos Contínuos e Restrições Globais (Locks)
    *   **5.5.2** Efeitos Retardados (Delayed Effects)
    *   **5.5.3** Buffs Dinâmicos (Dynamic Stat Buffs)
*   **5.6.** Interações de UI, Modais e Minigames
    *   **5.6.1** O Padrão Assíncrono e a Mágica da Engine LUA (`YieldReq`)
    *   **5.6.2** Tipos de Modais Oficiais da Engine (`ShowConfirmation`, `ShowCardSelection`, etc.)
    *   **5.6.3** O Bypass de IA e Simulação
    *   **5.6.4** Minigames de Sorte
*   **5.7.** Ferramenta de Validação em Massa (Mass Validator)
    *   **5.7.1** Como Executar
    *   **5.7.2** As Duas Fases de Validação (Compile-Time e Runtime)
    *   **5.7.3** O Relatório Agrupado
*   **5.8.** Casos de Estudo e Soluções Arquiteturais (Breakthroughs)
    *   **5.8.16** O Brilho Universal no Cemitério (Ghostbusters)
*   **5.9.** Core Pillars e A Regra Brutal das Constantes (Metatable Interceptor)
*   **5.10.** Dicionário OCGCore: A Anatomia das Variáveis LUA (e, tp, eg, chk, etc)

### 6. 🧠 `AI_And_Characters.md` (Inteligência Artificial e Personagens)
*   **6.1.** Design da Inteligência Artificial (`OpponentAI.cs`)
    *   **6.1.1** O Sistema de Pontuação (`Fear Score`, `Board Value`, `Panic Threshold`)
    *   **6.1.2** Perfis de Personalidade (Arquétipos Agressivo, Controle, etc.)
*   **6.1.3** As Regras de Ouro (Bypass LUA, Heurísticas de Combate, Gestão de Recursos e Paciência Visual)
    *   **6.1.4** Lógicas Específicas e Win-Cons (Interpretação de Categorias LUA)
    *   **6.1.5** Estrutura do Loop de Decisão (`AITurnRoutine`)
*   **6.2.** Sistema de Personagens e Decks (`CharacterDatabase.cs`)
    *   **6.2.1** Visão Geral e IDs
    *   **6.2.2** Estrutura de Dados (`CharacterData`, Drops)
    *   **6.2.3** Sistema de 3 Decks (Variantes A, B, C)

### 7. 🗺️ `Campaign_And_Story.md` (Campanha e História)
*   **7.1.** Lógica do Mapa e Progressão (`CampaignManager.cs`, Nós Home/Arena/Act)
*   **7.2.** Sistema de Diálogos (Visual Novel e Tags Faciais)
*   **7.3.** Lista de Atos e Oponentes Oficiais (10 Atos, 100 Duelos)
*   **7.4.** Roteiro Completo da Campanha (Visual Novel Scripts, Atos 1 ao 10)

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
*   **8.12.** A Cinemática de Extração (Pop-Out) e Bypass de Topo
*   **8.13.** Revelação de Mão e Embaralhamento Oculto (Hand Shuffle)

### 9. 🏆 `Progression_UI_And_Testing.md` (Progressão, Recompensas e Visuais)
*   **9.1.** Sistema de Pontuação e Drop Rate (`DuelScoreManager.cs`, `RewardPanelUI.cs`, Cálculo Base, Bônus, Ranks S+ a F, Drop Pools JSON)
*   **9.2.** Sistema de Troféus e Conquistas (`TrophyManager.cs`, As 7 Categorias Temáticas)
*   **9.3.** Sistema de Biblioteca (Library System)
    *   **9.3.1** Menu Principal (`Panel_Library`)
    *   **9.3.2** Biblioteca de Duelistas (`Panel_LibDuelists`, `DuelistLibraryManager.cs`, Desbloqueio)
    *   **9.3.3** Biblioteca de Cartas (`Panel_LibCards`, `CardLibraryManager.cs`, Cartas não obtidas e Visor)
    *   **9.3.4** Biblioteca de Decks (`Panel_LibDecks`, `DeckLibraryManager.cs`, Grind de Variantes A/B/C)
    *   **9.3.5** Biblioteca de Arenas (`Panel_LibArenas`, `ArenaLibraryManager.cs`)
    *   **9.3.6** Scripts de Dados (`LibraryDataTypes.cs`)
*   **9.4.** Mecânica Efêmera da Tag "NEW" (Lógica de Persistência, `SaveLoadSystem.MarkCardAsUsed`)
*   **9.5.** Sistemas de Teste, Debug e UI Dinâmica
    *   **9.5.1** Popup de Dano (`DamagePopupManager.cs`)
    *   **9.5.2** Tooltip de Mouse Dinâmico (`MouseTooltipUI.cs`)
    *   **9.5.3** Painel de Desenvolvedor (`FullTestManager.cs`, Toggles, Dev Card Menu, Ctrl+T)
    *   **9.5.4** In-Game Debug Console (`InGameDebugConsole.cs`, Ctrl+Shift+D)
*   **9.5.5** Laboratório de Teste de Efeitos (`EffectTestManager.cs`, Ctrl+E, Extração Cinemática, Contexto de Teste Dinâmico)
    *   **9.5.6** Gerador de Checklist QA (`generate_qa_checklist.py`, Python Tool)
    *   **9.5.7** Bloco de Notas do Desenvolvedor (`NotepadWindow.cs`, Ctrl+G)
*   **9.6.** Simulador de Caos (`SimulationManager.cs`, Bypass de UI, Visual Mode 1.5x, Fast Mode 50x)
*   **9.7.** Sistema de Temas de Duelo (`DuelThemeManager.cs`, `DuelTheme`)
    *   **9.7.1** Arquitetura de Temas (Console e Cartuchos)
    *   **9.7.2** Configurando o Relógio de Turnos (`TurnClockUI`, Rotação Dinâmica, Camadas e Pivot)
    *   **9.7.3** Minigames Dinâmicos (Moeda e Dados)
*   **9.8.** Efeitos Visuais e Sonoros (A Arquitetura Modular do `DuelFXManager`, Extração Cinemática, Pacotes de Invocação)
    *   **9.8.1** `DuelFXManager.cs` (Core, Inspector e Som)
    *   **9.8.2** `DuelFXManager_Combat.cs` (Ataques, Dano e Destruição)
    *   **9.8.3** `DuelFXManager_Summons.cs` (Invocações, Fichas e Cinemáticas)
    *   **9.8.4** `DuelFXManager_Flights.cs` (Voos, Equipamentos e Embaralhamento)
    *   **9.8.5** `DuelFXManager_Effects.cs` (Magias, Correntes, Auras e Ícones UI)
*   **9.9.** Personalização de UI e Preferências de Jogo (`PhaseAnnouncementSettings`, Modos de Flip, Velocidade de Jogo)

---

## 6. Protocolo de Manutenção da Base de Conhecimento (Instruções Estritas para a IA)

Para preservar a integridade desta "Bíblia" arquitetural e impedir a perda de contexto ou fragmentação de memória, a IA assistente deve operar **OBRIGATORIAMENTE** sob as seguintes diretrizes de agora em diante:

### 6.1. Leis Absolutas da Nova Arquitetura
*   **A Morte do LuaToC:** A arquitetura antiga baseada em métodos C# engessados (`CardEffectManager_Impl` / `LuaToC`) está **extinta**. O projeto opera 100% sob emulação OCGCore via MoonSharp. Os scripts originais `.lua` ditam a lógica; o C# apenas obedece e renderiza na tela.
*   **Gerenciadores Fragmentados (Partial Classes):** Nunca tente reescrever ou sugerir uma classe Core inteira em um único bloco de código. Atue sempre no submódulo responsável:
    *   `GameManager` está fragmentado em: `_BoardActions`, `_Decks`, `_Stats`, `_Summons`, `_Selections`, `_Phases`.
    *   `DuelFXManager` está fragmentado em: `_Combat`, `_Effects`, `_Flights`, `_Summons`.
    *   `LuaDuel` está fragmentado em: `_Core`, `_Actions`, `_Queries`, `_UI`, `_Stubs`.

### 6.2. A Lei de Ouro da Programação de Cartas (Análise LUA)
Ao introduzir ou debugar o efeito de uma carta, a IA deve cumprir este ritual estrito:
1.  **Análise de Raio-X:** Ler o script `.lua` nativo (YGOPro/EDOPro) da carta de ponta a ponta.
2.  **Mapeamento de Constantes:** Rastrear todas as constantes (`EVENT_*`, `REASON_*`, `EFFECT_*`, `HINTMSG_*`). Se a Engine C# (`LuaEngineCore.cs`) não tiver a constante nativa, **injetá-la imediatamente**.
3.  **Checagem de Funções Auxiliares (`utility.lua`):** Verificar se o script usa Closures como `aux.Filter` ou `aux.Next`. Acionar a `LuaUtilityBridge` (ou o próprio `LuaEngineCore`) para resolver a lógica no interpretador Lua antes de tentar recriar rodas complexas no C#.
4.  **Necessidade de UI (Interrupções de Interface):** Identificar se a carta exige escolhas visíveis (`Duel.SelectTarget`, `Duel.AnnounceRace`). Se faltar o modal, conectá-lo via **YieldReq** no `LuaDuel_UI.cs` e alertar o desenvolvedor para acoplar os painéis no `UIManager`.

### 6.3. Fluxo de Geração de Respostas
*   **Etapa 0 - Verificação e Contenção (Zero Suposições):** Nunca deduza lógicas ausentes e não aplique regras genéricas de TCG que não estejam documentadas. Se faltar contexto do código, **INTERROMPA** e exija o arquivo ao desenvolvedor.
*   **Etapa 1 - Enriquecimento Centralizado:** Ao documentar uma funcionalidade nova, não crie arquivos avulsos. Expanda as seções dos 9 Super Documentos existentes, mantendo a taxonomia e as notas anteriores intactas.
*   **Etapa 2 - Mapeamento Cirúrgico no Índice Mestre:** Atualize sempre o **Índice Mestre (Seção 5)** deste documento sempre que uma nova grande mecânica for consolidada.
*   **Etapa 3 - Relatório de Transparência:** No fim de cada resposta sistêmica, forneça um status claro do que foi alterado e o motivo de possíveis recusas técnicas.