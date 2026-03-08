using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ArenaLibraryManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform listContent;
    public GameObject arenaItemPrefab;
    
    [Header("Preview")]
    public Image previewImage;
    public TextMeshProUGUI arenaNameText;

    void OnEnable()
    {
        LoadArenas();
    }

    public void LoadArenas()
    {
        if (GameManager.Instance == null || GameManager.Instance.campaignDatabase == null) return;

        foreach (Transform child in listContent) Destroy(child.gameObject);

        // Itera pelos Atos da campanha
        var acts = GameManager.Instance.campaignDatabase.acts;
        for (int i = 0; i < acts.Count; i++)
        {
            var act = acts[i];
            // Verifica se o ato foi completado (ou se é o ato 1 que sempre aparece, ou lógica de desbloqueio)
            // TODO: Integrar com CampaignManager.maxUnlockedLevel
            // int actStartLevel = (i * 10) + 1;
            // bool unlocked = CampaignManager.Instance.maxUnlockedLevel > actStartLevel;
            bool unlocked = true; // DEBUG

            if (unlocked)
            {
                GameObject item = Instantiate(arenaItemPrefab, listContent);
                TextMeshProUGUI txt = item.GetComponentInChildren<TextMeshProUGUI>();
                if (txt) txt.text = act.actName;

                Button btn = item.GetComponent<Button>();
                if (btn)
                {
                    btn.onClick.AddListener(() => ShowArenaPreview(act));
                }
            }
        }
    }

    void ShowArenaPreview(CampaignDatabase.ActData act)
    {
        if (arenaNameText) arenaNameText.text = act.actName;
        
        if (previewImage && act.theme != null)
        {
            // Mostra o background do tabuleiro como preview
            previewImage.sprite = act.theme.boardBackground;
            // Mantém a proporção
            previewImage.preserveAspect = true;
        }
    }
}
