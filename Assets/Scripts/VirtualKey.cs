using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class VirtualKey : MonoBehaviour
{
    public string character;
    private NameInputScreen inputScreen;
    private TextMeshProUGUI btnText;

    void Start()
    {
        inputScreen = GetComponentInParent<NameInputScreen>();
        btnText = GetComponentInChildren<TextMeshProUGUI>();
        GetComponent<Button>().onClick.AddListener(OnKeyPress);

        // Se não definir o caractere no Inspector, tenta pegar do texto do botão
        if (string.IsNullOrEmpty(character))
        {
            var text = GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) character = text.text;
        }
    }

    void OnKeyPress()
    {
        if (inputScreen != null)
        {
            inputScreen.ProcessKey(character);
        }
    }

    public void SetCharacter(string c)
    {
        character = c;
        if (btnText == null) btnText = GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null) btnText.text = c;
    }
}