using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AttributeChoiceUI : MonoBehaviour
{
    public static AttributeChoiceUI Instance;

    [Header("UI References")]
    public TextMeshProUGUI Text_PositionAsk;
    public Button Btn_SummonPosition; // Representa Ataque (ATK)
    public Button Btn_SetPosition;    // Representa Defesa (DEF)

    void Awake()
    {
        Instance = this;
        
        // Auto-configuração inteligente para pegar os botões do seu Prefab
        if (Text_PositionAsk == null) Text_PositionAsk = transform.Find("Text_PositionAsk")?.GetComponent<TextMeshProUGUI>();
        if (Btn_SummonPosition == null) Btn_SummonPosition = transform.Find("Btn_AtkChoice")?.GetComponent<Button>();
        if (Btn_SetPosition == null) Btn_SetPosition = transform.Find("Btn_DefChoice")?.GetComponent<Button>();
    }

    public void Show(System.Action<int> callback)
    {
        gameObject.SetActive(true);

        if (Text_PositionAsk) Text_PositionAsk.text = "Escolha um Atributo:";

        if (Btn_SummonPosition) {
            Btn_SummonPosition.onClick.RemoveAllListeners();
            Btn_SummonPosition.onClick.AddListener(() => { gameObject.SetActive(false); callback?.Invoke(0); });
        }
        if (Btn_SetPosition) {
            Btn_SetPosition.onClick.RemoveAllListeners();
            Btn_SetPosition.onClick.AddListener(() => { gameObject.SetActive(false); callback?.Invoke(1); });
        }
    }
}