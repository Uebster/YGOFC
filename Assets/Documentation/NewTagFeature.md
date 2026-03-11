# Sistema da Tag "New" (Biblioteca e Deck Builder)

## Visão Geral
Este sistema foi projetado para notificar o jogador sobre cartas recém-adquiridas, incentivando a experimentação e a construção de novos decks. A tag "New" aparece tanto na Biblioteca de Cartas quanto na lista de Baú (Chest) do Deck Builder.

## Lógica de Exibição

Uma carta é considerada "nova" se o jogador a obteve (por exemplo, como recompensa de duelo) mas ainda não a "utilizou".

### 1. Biblioteca de Cartas (`CardLibraryManager`)
- Uma tag "New" visualmente destacada (piscando ou com um brilho) aparece sobre a miniatura da carta na grade da biblioteca.
- Esta tag permanece visível até que a carta seja "usada".

### 2. Baú do Construtor de Decks (`DeckBuilderManager`)
- A tag "New" também aparece no item correspondente da lista do Baú (`Chest`).
- **Lógica de Ocultação Temporária:** Se uma cópia de uma carta "nova" for adicionada ao Deck Principal, Deck Adicional ou Deck Auxiliar durante a sessão de construção, a tag "New" no Baú será **ocultada temporariamente** para aquela carta. Se a carta for removida de todos os decks, a tag "New" reaparecerá (assumindo que a carta ainda não foi permanentemente marcada como "usada").

## Lógica de Persistência: Quando uma carta deixa de ser "Nova"?

O ato de "usar" uma carta, que remove permanentemente a tag "New", é definido como **adicionar a carta a um deck e salvar esse deck**.

- **`SaveLoadSystem.IsCardNew(string cardId)`**: Este método verifica se o ID da carta está na lista de "cartas já usadas" do jogador no arquivo de save. Ele retorna `true` se a carta for nova.
- **`SaveLoadSystem.MarkCardAsUsed(string cardId)`**: Este método adiciona o ID da carta à lista de "cartas usadas" e deve ser chamado nos seguintes momentos:
    1.  **Ao Salvar o Deck:** No `DeckBuilderManager`, quando o jogador clica em "Save Deck", o sistema deve iterar por todas as cartas nos decks (Principal, Adicional, Auxiliar) e chamar `MarkCardAsUsed` para cada uma.
    2.  **Ao Gerar Deck Inicial:** Quando um novo jogo é iniciado e o `InitialDeckBuilder` cria o primeiro deck do jogador, todas as cartas nesse deck são automaticamente marcadas como "usadas".
    3.  **Cheat `unlockAllCards`:** Se a opção de desenvolvedor para desbloquear todas as cartas estiver ativa, todas as cartas são marcadas como "usadas" para evitar poluir a biblioteca com centenas de tags "New".

Este sistema garante que o jogador seja notificado sobre novas aquisições, mas a notificação desaparece de forma inteligente assim que o jogador demonstra intenção de uso, mantendo a interface limpa e funcional.