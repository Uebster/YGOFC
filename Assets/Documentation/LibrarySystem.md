# Sistema de Biblioteca (Library System)

## Visão Geral
A **Biblioteca (Library)** é o museu pessoal do jogador, onde todo o progresso, colecionáveis e informações desbloqueadas são armazenados. Diferente do *Deck Builder* (que é funcional), a Biblioteca é informativa e celebratória.

O acesso é feito através do Menu Principal ou do botão "Library" no menu de Novo Jogo.

---

## 1. Menu Principal da Biblioteca (`Panel_Library`)
O hub central que permite navegar para as quatro seções principais.

### Estrutura de UI
*   **Panel_Library** `[Image]` - Fundo geral.
    *   **Panel_LibraryButtons** `[Image]` - Container dos botões.
        *   **Btn_LibCards** `[Button, MillenniumButton]` - Acessa a coleção de cartas.
            *   *Text:* "Cards"
        *   **Btn_LibDuelists** `[Button, MillenniumButton]` - Acessa perfis de oponentes.
            *   *Text:* "Duelists"
        *   **Btn_LibArenas** `[Button, MillenniumButton]` - Acessa temas e cenários.
            *   *Text:* "Arenas"
        *   **Btn_LibDecks** `[Button, MillenniumButton]` - Acessa receitas de decks inimigos.
            *   *Text:* "Decks"
        *   **Btn_BackToMenu** `[Button, MillenniumButton]` - Retorna ao menu anterior.

---

## 2. Biblioteca de Duelistas (`Panel_LibDuelists`)
Exibe informações detalhadas sobre os personagens encontrados no jogo.

### Lógica de Desbloqueio
O sistema de progressão de duelistas funciona em três estágios:
1.  **Bloqueado (Oculto):** O personagem não aparece na lista.
2.  **Desbloqueado na Arena:** Após vencer o personagem na **Campanha**, ele aparece na *Arena (Free Duel)*, mas ainda não na Biblioteca (ou aparece como silhueta).
3.  **Desbloqueado na Biblioteca:** O requisito para ver os dados completos (Avatar, Descrição, Lore) é **ganhar 1 duelo contra este oponente na Arena**.

### Estrutura de UI
*   **Panel_LibDuelists** `[Image]`
    *   **Panel_DuelistsViewer** `[Image]` - Painel de detalhes à esquerda.
        *   **DuelistViewer** `[CardViewerUI]` - Script reutilizado para exibir dados do duelista.
            *   **DuelistAvatar** `[RawImage]` - Foto do personagem.
            *   **DuelistNameText** `[TMP]` - Nome.
            *   **Panel_DuelistDescription** `[Image]` - Fundo do texto.
                *   **Scroll View** `[ScrollRect]`
                    *   **DuelistDescriptionText** `[TMP]` - Lore, Ato de origem, características.
    *   **Btn_BackToMenu** `[Button]`
    *   **Title_Text** `[TMP]` - "Duelist Library"
    *   **Panel_DuelistLibrary** `[Image]` - Área da grade à direita.
        *   **Panel_Header** `[Image]`
            *   **PageText** `[TMP]` - "Page 1/X"
        *   **Panel_MainArea** `[Image]`
            *   **Scroll View** `[ScrollRect]`
                *   **Content** `[GridLayoutGroup]` - Onde os ícones dos duelistas são instanciados.
        *   **Panel_Footer** `[Image]`
            *   **Btn_Previous** `[Button]`
            *   **Btn_Next** `[Button]`

---

## 3. Biblioteca de Cartas (`Panel_LibCards`)
A enciclopédia de todas as cartas disponíveis no jogo.

### Lógica de Desbloqueio e "New"
*   **Visualização Completa:** A biblioteca exibe **todas** as cartas do banco de dados (ex: 2147 cartas), divididas em páginas.
    *   **Cartas Não Obtidas:** Aparecem com o verso (Back) virado para cima. O *Card Viewer* não exibe detalhes para manter o mistério.
    *   **Cartas Obtidas:** Aparecem com a arte visível. O *Card Viewer* exibe todos os dados.
*   **Tag "New":**
    *   Toda carta recém-adquirida recebe uma tag/ícone "New" piscante sobre ela na grade.
    *   **Regra de Limpeza:** A tag "New" **NÃO** desaparece apenas ao passar o mouse ou clicar na Biblioteca. Ela só desaparece quando o jogador **adiciona a carta a um Deck** no menu *Deck Construction* (implementado via `SaveLoadSystem.MarkCardAsUsed`). Isso incentiva o uso das novas cartas.
    *   **Exceção (Deck Inicial):** As cartas geradas para o Deck Inicial do jogador já nascem marcadas como "Usadas", pois já fazem parte de um deck ativo.
    *   **Exceção (Cheat):** Se o jogador usar a opção `unlockAllCards` no GameManager, todas as cartas adicionadas serão marcadas como "Usadas" para evitar poluição visual na biblioteca.

### Estrutura de UI
*   **Panel_LibCards** `[Image, CardLibraryManager]` - Script principal de controle.
    *   **Panel_CardViewer** `[Image]` - Visualizador ampliado à esquerda.
        *   **CardViewer** `[CardViewerUI]`
            *   *Nota:* Este componente é atualizado dinamicamente ao passar o mouse sobre os itens da grade. Se a carta não foi obtida, ele é limpo.
            *   **Card2D** `[RawImage]` - Imagem da carta.
            *   **CardNameText** `[TMP]`
            *   **CardInfoText** `[TMP]` - Tipo/Raça/Atributo.
            *   **CardStatsText** `[TMP]` - ATK/DEF.
            *   **Panel_Description** `[Image]`
                *   **Scroll View** `[ScrollRect]`
                    *   **CardDescriptionText** `[TMP]` - Texto do efeito/flavor.
    *   **Btn_BackToMenu** `[Button]`
    *   **Title_Text** `[TMP]` - "Card Library"
    *   **Panel_CardLibrary** `[Image]` - Grade de cartas.
        *   **Panel_Header** `[Image]`
            *   **PageText** `[TMP]` - Exibe "Page X/Y".
        *   **Panel_MainArea** `[Image]`
            *   **Scroll View** `[ScrollRect]`
                *   **Content** `[GridLayoutGroup]` - Configurado para 10 colunas x 5 linhas (50 itens por página).
        *   **Panel_Footer** `[Image]`
            *   **Btn_Previous** `[Button]`
            *   **Btn_Next** `[Button]`

---

## 4. Biblioteca de Decks (`Panel_LibDecks`)
Permite visualizar as receitas (listas de cartas) dos decks usados pelos oponentes.

### Lógica de Desbloqueio (Grind System)
Inicialmente vazia. Os decks aparecem como "caixas de deck" com o nome do personagem após o personagem ser desbloqueado na *Lib Duelists* (1 vitória na Arena).

Ao clicar em um deck, o jogador pode tentar visualizar as variantes (A, B, C, Extra), mas o acesso depende do número de vitórias acumuladas contra aquele oponente específico na Arena:

*   **Deck A:** Requer **50 Vitórias**.
*   **Deck B:** Requer **100 Vitórias**.
*   **Deck C:** Requer **200 Vitórias**.
*   **Extra Deck / All Cards:** Requer **250 Vitórias**.

### Estrutura de UI
*   **Panel_LibDecks** `[Image]`
    *   **Panel_DeckViewer** `[Image]` - Área de detalhes à esquerda.
        *   **DeckViewer** `[CardViewerUI]` - Adaptado para mostrar info do deck.
            *   **DeckImage** `[RawImage]` - Ícone do deck/caixa.
            *   **DeckNameText** `[TMP]` - Ex: "Kaiba's Power Deck".
            *   **Panel_DeckDescription** `[Image]`
                *   **Scroll View** `[ScrollRect]`
                    *   **DeckListText** `[TMP]` - Lista de cartas (se desbloqueado) ou mensagem de requisito (se bloqueado).
            *   **Panel_VariantButtons** `[Image]` - Botões para trocar A/B/C (aparecem ao selecionar).
    *   **Btn_BackToMenu** `[Button]`
    *   **Title_Text** `[TMP]` - "Deck Library"
    *   **Panel_DeckLibrary** `[Image]` - Grade de seleção.
        *   **Panel_Header** `[Image]`
            *   **PageText** `[TMP]`
        *   **Panel_MainArea** `[Image]`
            *   **Scroll View** `[ScrollRect]`
                *   **Content** `[GridLayoutGroup]` - Itens clicáveis representando os oponentes.
        *   **Panel_Footer** `[Image]`
            *   **Btn_Previous** `[Button]`
            *   **Btn_Next** `[Button]`

---

## 5. Biblioteca de Arenas (`Panel_LibArenas`)
Galeria de arte dos cenários e temas do jogo.

### Lógica de Desbloqueio
O desbloqueio é vinculado ao progresso da **História (Campanha)**.
*   Ao concluir um **Ato** (derrotar o Chefe do Ato), todas as artes daquele tema são desbloqueadas na biblioteca.
*   Permite visualizar separadamente:
    *   Imagem de Fundo (Field).
    *   Moldura de Avatar.
    *   Moldura de Descrição de Carta.
    *   Indicador de Fases.

### Estrutura de UI
*   **Panel_LibArenas** `[Image]`
    *   **Panel_ArenaViewer** `[Image]` - Visualizador central grande.
        *   **PreviewImage** `[Image]` - Mostra o asset selecionado.
        *   **AssetNameText** `[TMP]` - Ex: "Kaiba Corp Logo".
    *   **Btn_BackToMenu** `[Button]`
    *   **Panel_ArenaList** `[Image]` - Lista lateral ou inferior.
        *   **Scroll View** `[ScrollRect]`
            *   **Content** `[GridLayoutGroup]` - Miniaturas dos temas desbloqueados.
    *   **Panel_ComponentFilter** `[Image]` - Abas para filtrar o tipo de asset.
        *   **Btn_Backgrounds** `[Button]`
        *   **Btn_Frames** `[Button]`
        *   **Btn_UIElements** `[Button]`

---

## Notas Técnicas
*   **Persistência:** O estado de "New" das cartas e contagem de vitórias na Arena é salvo no `SaveLoadSystem` dentro da estrutura `LibrarySaveData`.
*   **Reutilização:** O `CardViewerUI` é amplamente reutilizado aqui. Certifique-se de que ele pode ser configurado para mostrar dados estáticos sem depender do estado de um duelo ativo.

---

## 6. Scripts e Arquitetura

A implementação da Biblioteca é dividida em gerenciadores específicos para cada aba, além de estruturas de dados para persistência.

### Arquivos Principais
*   **`LibraryDataTypes.cs`**: Define as classes serializáveis para salvar o progresso da biblioteca.
    *   `DuelistWinRecord`: Armazena o ID do duelista e o número de vitórias.
    *   `LibrarySaveData`: Armazena a lista de cartas vistas (para remover a tag "New") e os registros de vitórias.
*   **`DuelistLibraryManager.cs`**: Gerencia a aba de Duelistas.
    *   Carrega a lista de personagens do `CharacterDatabase`.
    *   Verifica se o jogador tem pelo menos 1 vitória contra o oponente para exibi-lo na lista.
    *   Exibe detalhes (Lore, Avatar) ao selecionar.
*   **`DeckLibraryManager.cs`**: Gerencia a aba de Decks.
    *   Lista os oponentes desbloqueados.
    *   Ao selecionar um oponente, verifica o número de vitórias para habilitar os botões de Deck A (50), B (100) e C (200).
    *   Exibe a lista de cartas do deck selecionado.
*   **`ArenaLibraryManager.cs`**: Gerencia a aba de Arenas.
    *   Verifica o progresso da campanha (`CampaignManager.maxUnlockedLevel`) para determinar quais Atos foram concluídos.
    *   Exibe os temas visuais (`DuelTheme`) correspondentes aos Atos desbloqueados.