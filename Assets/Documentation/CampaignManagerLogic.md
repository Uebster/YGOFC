# Lógica do CampaignManager

## Visão Geral
O `CampaignManager.cs` é um Singleton persistente que gerencia o estado global da progressão do jogador no mapa mundi. Ele não participa do duelo em si, mas controla o acesso aos duelos.

## Responsabilidades

### 1. Controle de Desbloqueio
*   Mantém a variável `maxUnlockedLevel` (int).
*   **Lógica:** Nível 1 = Oponente 1 do Ato 1. Nível 11 = Oponente 1 do Ato 2.
*   **Validação:** O método `IsLevelUnlocked(int level)` é usado pelos botões do mapa (`CampaignNode`) para saber se devem estar ativos ou bloqueados (cinza/cadeado).

### 2. Navegação no Mapa
*   Gerencia a transição entre as telas de "Regiões" (Atos) e o Mapa Mundi.
*   Controla a visibilidade dos nós de conexão entre os atos.

### 3. Persistência
*   Ao vencer um duelo no modo Campanha, o `GameManager` notifica o `CampaignManager`.
*   Se o nível vencido for o maior já alcançado, `maxUnlockedLevel` é incrementado.
*   O `SaveLoadSystem` é acionado imediatamente para gravar o novo progresso.

### 4. Cheat / DevMode
*   Se `GameManager.devMode` estiver ativo, o `CampaignManager` libera o acesso a todos os nós do mapa, ignorando a verificação de nível.

## Relação com Outros Sistemas
*   **GameManager:** Informa vitória/derrota.
*   **CampaignDatabase:** Fornece os dados estáticos (quem é o oponente do nível X).
*   **UIManager:** É chamado para abrir as telas de seleção de oponente.