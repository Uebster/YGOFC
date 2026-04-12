using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.IO;
using UnityEngine.Networking;

public class Jackpot7UI : MonoBehaviour
{
    [Header("Referências da UI")]
    public GameObject panel;
    public Image slot1; // Arraste o Card71
    public Image slot2; // Arraste o Card72
    public Image slot3; // Arraste o Card73
    public TextMeshProUGUI jackpotText; // Crie um texto "JACKPOT!" gigante no centro e arraste aqui
    
    [Header("Animação")]
    public float rollDuration = 0.6f;
    public float timeBetweenSlots = 0.15f;
    
    private Texture2D card7Texture;
    private Texture2D backTexture;

    void Awake()
    {
        if (panel == null) panel = gameObject;
        panel.SetActive(false);
        if (jackpotText != null) jackpotText.gameObject.SetActive(false);
    }

    public void ShowSequence(System.Action onComplete)
    {
        panel.SetActive(true);
        if (jackpotText != null)
        {
            jackpotText.gameObject.SetActive(false);
            
            // Copia o estilo Dourado/Outline do Phase Indicator
            if (GameManager.Instance != null)
            {
                var pSettings = GameManager.Instance.phaseAnnouncements;
                jackpotText.color = pSettings.textColor;
                jackpotText.fontStyle = FontStyles.Bold | FontStyles.Italic;
                
                if (DuelThemeManager.Instance != null && DuelThemeManager.Instance.currentTheme != null && DuelThemeManager.Instance.currentTheme.globalFont != null)
                    jackpotText.font = DuelThemeManager.Instance.currentTheme.globalFont;

                Outline outline = jackpotText.gameObject.GetComponent<Outline>();
                if (outline == null) outline = jackpotText.gameObject.AddComponent<Outline>();
                outline.effectColor = pSettings.outlineColor;
                outline.effectDistance = pSettings.outlineThickness;
            }
        }
        
        StartCoroutine(SequenceRoutine(onComplete));
    }

    private IEnumerator SequenceRoutine(System.Action onComplete)
    {
        // 1. Carrega as Texturas da Carta 7 e do Verso dinamicamente
        if (card7Texture == null && GameManager.Instance != null)
        {
            CardData cardData = GameManager.Instance.cardDatabase.GetCardById("0004");
            if (cardData == null) cardData = GameManager.Instance.cardDatabase.GetCardById("DM0004");
            
            if (cardData != null)
            {
                string path = Path.Combine(Application.streamingAssetsPath, cardData.image_filename);
                string url = "file://" + path;
                using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
                {
                    yield return uwr.SendWebRequest();
                    if (uwr.result == UnityWebRequest.Result.Success) card7Texture = DownloadHandlerTexture.GetContent(uwr);
                }
            }
        }
        backTexture = GameManager.Instance != null ? GameManager.Instance.GetCardBackTexture() : null;

        // 2. Prepara os Sprites
        Sprite backSprite = backTexture != null ? Sprite.Create(backTexture, new Rect(0,0,backTexture.width, backTexture.height), new Vector2(0.5f, 0.5f)) : null;
        Sprite frontSprite = card7Texture != null ? Sprite.Create(card7Texture, new Rect(0,0,card7Texture.width, card7Texture.height), new Vector2(0.5f, 0.5f)) : null;

        slot1.sprite = backSprite; slot2.sprite = backSprite; slot3.sprite = backSprite;

        // 3. Roda a Máquina Caça-Níqueis
        yield return StartCoroutine(RollSlot(slot1, frontSprite, backSprite));
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.spellSound);
        yield return new WaitForSeconds(timeBetweenSlots);
        
        yield return StartCoroutine(RollSlot(slot2, frontSprite, backSprite));
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.spellSound);
        yield return new WaitForSeconds(timeBetweenSlots);
        
        yield return StartCoroutine(RollSlot(slot3, frontSprite, backSprite));
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.summonSound); // Som de impacto pesado
        
        // 4. Animação do Texto JACKPOT
        if (jackpotText != null)
        {
            jackpotText.gameObject.SetActive(true);
            float t = 0;
            while (t < 0.2f) { t += Time.deltaTime; jackpotText.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t / 0.2f); yield return null; }
            
            // Efeito de piscar neon
            for (int i = 0; i < 3; i++) {
                jackpotText.color = Color.white; yield return new WaitForSeconds(0.05f);
                if (GameManager.Instance != null) jackpotText.color = GameManager.Instance.phaseAnnouncements.textColor;
                else jackpotText.color = new Color(1f, 0.84f, 0f); 
                yield return new WaitForSeconds(0.05f);
            }
        }

        yield return new WaitForSeconds(0.6f); // Pausa dramática mais rápida
        
        // 5. Encerra e repassa pro C# iniciar as explosões e o Draw!
        panel.SetActive(false);
        onComplete?.Invoke();
    }

    private IEnumerator RollSlot(Image slot, Sprite front, Sprite back)
    {
        // Máscara para que os cilindros "rodem" sem sair do slot
        Mask mask = slot.GetComponent<Mask>();
        if (mask == null) mask = slot.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        RectTransform slotRT = slot.rectTransform;
        float h = slotRT.rect.height > 0 ? slotRT.rect.height : 150f;

        // Cilindro 1
        GameObject img1Obj = new GameObject("Scroll1", typeof(RectTransform), typeof(Image));
        img1Obj.transform.SetParent(slot.transform, false);
        RectTransform rt1 = img1Obj.GetComponent<RectTransform>();
        rt1.anchorMin = Vector2.zero; rt1.anchorMax = Vector2.one;
        rt1.sizeDelta = Vector2.zero; rt1.anchoredPosition = Vector2.zero;
        Image img1 = img1Obj.GetComponent<Image>();
        img1.sprite = back;

        // Cilindro 2
        GameObject img2Obj = new GameObject("Scroll2", typeof(RectTransform), typeof(Image));
        img2Obj.transform.SetParent(slot.transform, false);
        RectTransform rt2 = img2Obj.GetComponent<RectTransform>();
        rt2.anchorMin = Vector2.zero; rt2.anchorMax = Vector2.one;
        rt2.sizeDelta = Vector2.zero; rt2.anchoredPosition = new Vector2(0, h);
        Image img2 = img2Obj.GetComponent<Image>();
        img2.sprite = front;

        float t = 0; float speed = h * 12f;
        while (t < rollDuration)
        {
            t += Time.deltaTime;
            rt1.anchoredPosition += Vector2.down * speed * Time.deltaTime;
            rt2.anchoredPosition += Vector2.down * speed * Time.deltaTime;
            
            // Reseta a posição pro topo quando vazar pelo fundo da carta
            if (rt1.anchoredPosition.y <= -h) rt1.anchoredPosition += new Vector2(0, h * 2);
            if (rt2.anchoredPosition.y <= -h) rt2.anchoredPosition += new Vector2(0, h * 2);
            yield return null;
        }
        
        mask.showMaskGraphic = true;
        slot.sprite = front; // Trava na carta 7
        Destroy(img1Obj); Destroy(img2Obj);
    }
}