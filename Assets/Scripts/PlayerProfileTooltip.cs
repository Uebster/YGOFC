using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class PlayerProfileTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool isPlayerProfile = true;
    private GameObject tooltipObj;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (GameManager.Instance == null || !GameManager.Instance.enableProfileTooltip) return;
        
        string desc = isPlayerProfile ? GameManager.Instance.playerHintDesc : GameManager.Instance.opponentHintDesc;
        
        if (string.IsNullOrEmpty(desc)) return;

        // Cria o Tooltip dinamicamente
        if (tooltipObj == null)
        {
            tooltipObj = new GameObject("ProfileTooltip", typeof(RectTransform), typeof(Image));
            Transform canvas = GetComponentInParent<Canvas>().transform;
            tooltipObj.transform.SetParent(canvas, false);
            tooltipObj.transform.SetAsLastSibling();
            
            Image bg = tooltipObj.GetComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.9f);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(tooltipObj.transform, false);
            
            TextMeshProUGUI txt = textObj.GetComponent<TextMeshProUGUI>();
            txt.fontSize = 24;
            txt.color = Color.yellow;
            txt.alignment = TextAlignmentOptions.Center;
            txt.text = desc;
            
            RectTransform rtText = textObj.GetComponent<RectTransform>();
            rtText.sizeDelta = new Vector2(300, 100);
            
            RectTransform rtBg = tooltipObj.GetComponent<RectTransform>();
            rtBg.sizeDelta = new Vector2(320, 120);
            rtBg.position = transform.position + new Vector3(0, 100f, 0); // Fica um pouco acima do Avatar
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipObj != null) Destroy(tooltipObj);
    }
}