# Registro de Alterações (Changelog)

## Atualizações Recentes

### Sistema de Biblioteca e Coleção
*   **Visualização Completa:** A Biblioteca de Cartas agora exibe **todas** as cartas do banco de dados (2147+), não apenas as desbloqueadas.
    *   Cartas não obtidas aparecem com o verso virado (bloqueadas).
    *   Cartas obtidas aparecem com a arte visível.
*   **Paginação:** Implementado sistema de páginas com **50 cartas por página** (Grade 10x5).
*   **Tag "NEW" (Lógica Refinada):**
    *   **Na Biblioteca:** A tag "NEW" aparece em cartas que o jogador possui mas **nunca adicionou a um deck**. Ela desaparece permanentemente ao salvar um deck contendo a carta.
    *   **No Deck Inicial:** Cartas geradas para o Deck Inicial do jogador já nascem marcadas como "Usadas" (sem tag "NEW").
    *   **Cheat Unlock All:** Ao usar o cheat para desbloquear todas as cartas, elas são marcadas como "Usadas" para não poluir a visualização.

### Sistema de Recompensas (Rewards)
*   **Tag "NEW" (Lógica de Drop):** Na tela de vitória, a faixa "NEW" só aparece se a carta ganha **não existia** no Baú (Trunk) do jogador anteriormente. Se o jogador já tinha a carta (mesmo que nunca usada), a faixa não aparece.
*   **Correção de Drop:** A carta ganha agora é salva corretamente no arquivo de save imediatamente após o duelo.

### Ferramentas de Desenvolvimento
*   **Unlock All Cards:** Novo cheat no `GameManager` que adiciona 3 cópias de todas as cartas ao baú.
*   **Test Duel Direct:** Opção movida para o `GameManager` para pular menus e testar duelos rapidamente.
