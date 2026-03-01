# Efeitos Visuais e Sonoros (DuelFXManager)

O sistema de efeitos é centralizado no `DuelFXManager`. Ele gerencia partículas (VFX) e áudio (SFX/BGM). Os assets usados podem ser substituídos dinamicamente pelo `DuelTheme` do ato atual.

## Efeitos de Carta
| Ação | Descrição do Efeito |
| :--- | :--- |
| **Summon** | Partículas de luz/poeira na base da carta ao ser invocada. |
| **Set / Flip** | Som de carta virando e efeito de "poeira" sutil. |
| **Activate Spell** | Brilho verde/mágico ascendente. |
| **Activate Trap** | Brilho roxo/sinistro e som de armadilha. |
| **Tribute** | Efeito de "alma" ou luz azul saindo do monstro sacrificado. |
| **Fusion** | Espiral de fusão (Polymerization style) unindo materiais. |
| **Destruction** | Explosão e fumaça quando uma carta é destruída. |
| **Banish** | Vórtice negro ou dimensional sugando a carta. |

## Efeitos de Batalha
| Ação | Descrição do Efeito |
| :--- | :--- |
| **Attack** | Animação da carta recuando e avançando rapidamente (Strike). |
| **Impact** | Efeito de corte ou explosão no alvo. |
| **Reflect** | Escudo ou barreira quando um ataque atinge uma defesa maior. |
| **Damage** | Tremor de tela (Screen Shake) e som de impacto pesado. |
| **Direct Attack** | Animação de ataque em direção ao avatar do oponente. |

## Música Dinâmica (BGM)
A música de fundo muda conforme a situação dos Pontos de Vida (LP):

1.  **Normal:** Música padrão do tema. Toca no início.
2.  **Tense (Perigo):** Toca quando o Player tem < 50% dos LP do oponente (ou LP crítico).
3.  **Winning (Vantagem):** Toca quando o Player tem > 200% dos LP do oponente.

## Customização
Cada `DuelTheme` (ScriptableObject) pode sobrescrever os prefabs de VFX e os clipes de áudio, permitindo que o Ato 1 tenha efeitos "tecnológicos" e o Ato 2 tenha efeitos "mágicos", por exemplo.