using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FieldStatUI : MonoBehaviour
{
    public TextMeshProUGUI statText;

    private CardDisplay attachedCard;
    private GameManager gm;

    // Cache para evitar updates desnecessários
    private int lastAtk;
    private int lastDef;
    private CardDisplay.BattlePosition lastPos;
    private bool lastFlipped;

    // Método público para receber a referência da carta que deve seguir
    public void Setup(CardDisplay cardToFollow)
    {
        this.attachedCard = cardToFollow;
        gm = GameManager.Instance;

        if (statText == null) statText = GetComponentInChildren<TextMeshProUGUI>();

        if (attachedCard == null || gm == null)
        {
            Destroy(gameObject); // Se não tem o que seguir, se autodestrói.
            return;
        }

        // Garante que a UI de stats não bloqueie cliques nas cartas abaixo
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;
    }

    // Usamos LateUpdate para garantir que a carta já se moveu neste frame
    void LateUpdate()
    {
        if (attachedCard == null || gm == null)
        {
            Destroy(gameObject); // Limpa a UI se a carta for destruída
            return;
        }

        // 1. Segue a posição local da carta dentro da Zona (seguro para UI / Canvas)
        transform.localPosition = attachedCard.transform.localPosition;

        float yOffset = (gm.monsterStatDisplayMode == StatDisplayMode.AboveCard) ? gm.statDisplayYOffset : -gm.statDisplayYOffset;
        
        // 2. Aplica o espelhamento exato de perspectiva (Rotação e Inversão de Eixo Y)
        if (gm.statDisplayMatchCardRotation && !attachedCard.isPlayerCard)
        {
            transform.localEulerAngles = new Vector3(0, 0, 180f);
            // Como o oponente está do outro lado, "baixo" (-yOffset) vira "cima" (+Y) na tela do jogador
            transform.localPosition += new Vector3(0, -yOffset, 0);
        }
        else
        {
            transform.localEulerAngles = Vector3.zero;
            transform.localPosition += new Vector3(0, yOffset, 0);
        }
        
        UpdateStats(); // Atualização inicial
    }

    public void UpdateStats()
    {
        if (attachedCard == null || gm == null) return;

        // Otimização: Só atualiza o texto se os valores ou a posição mudaram
        if (attachedCard.currentAtk == lastAtk && attachedCard.currentDef == lastDef && attachedCard.position == lastPos && attachedCard.isFlipped == lastFlipped) return;

        // Oculta os stats se a carta estiver virada para baixo e for do oponente
        if (attachedCard.isFlipped && !attachedCard.isPlayerCard)
        {
            statText.text = "";
            lastAtk = attachedCard.currentAtk; lastDef = attachedCard.currentDef; lastPos = attachedCard.position; lastFlipped = attachedCard.isFlipped;
            return;
        }

        string atkColorHex = ColorUtility.ToHtmlStringRGB(GetStatColor(attachedCard.currentAtk, attachedCard.originalAtk));
        string defColorHex = ColorUtility.ToHtmlStringRGB(GetStatColor(attachedCard.currentDef, attachedCard.originalDef));
        string inactiveColorHex = ColorUtility.ToHtmlStringRGB(gm.statInactiveColor);

        string atkString = (attachedCard.position == CardDisplay.BattlePosition.Attack)
            ? $"<color=#{atkColorHex}>ATK/{attachedCard.currentAtk}</color>"
            : $"<color=#{inactiveColorHex}>ATK/{attachedCard.currentAtk}</color>";

        string defString = (attachedCard.position == CardDisplay.BattlePosition.Defense)
            ? $"<color=#{defColorHex}>DEF/{attachedCard.currentDef}</color>"
            : $"<color=#{inactiveColorHex}>DEF/{attachedCard.currentDef}</color>";

        statText.text = $"{atkString}  {defString}";

        lastAtk = attachedCard.currentAtk; lastDef = attachedCard.currentDef; lastPos = attachedCard.position; lastFlipped = attachedCard.isFlipped;
    }

    private Color GetStatColor(int current, int original) => current > original ? gm.statBuffColor : (current < original ? gm.statDebuffColor : Color.white);
}