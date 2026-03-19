using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance;

    [Header("Referências")]
    public GameObject damagePopupPrefab; // O prefab que contém o script DamagePopup
    public Transform playerSpawnPoint; // Recomendado: O centro das zonas de Spell do Player
    public Transform opponentSpawnPoint; // Recomendado: O centro das zonas de Spell do Oponente

    [Header("Configurações Visuais")]
    [Tooltip("Se ativado, usa a lista de Sprites. Se desativado, usa as configurações de Texto (TMP).")]
    public bool useSpritesForNumbers = false;
    
    [Header("Configuração de Texto (Fallback)")]
    public TMP_FontAsset customFont;
    public Color textDamageColor = Color.red;
    public Color textHealColor = Color.green;
    public float fontSize = 60f;

    [Header("Configuração de Sprites")]
    public Vector2 spriteSize = new Vector2(50f, 60f);
    [Tooltip("Índices 0 a 9 = Números. Índice 10 = Sinal de Menos (-)")]
    public Sprite[] damageSprites = new Sprite[11];
    [Tooltip("Índices 0 a 9 = Números. Índice 10 = Sinal de Mais (+)")]
    public Sprite[] healSprites = new Sprite[11];

    void Awake()
    {
        Instance = this;
    }

    public void ShowPopup(int amount, bool isHeal, bool isPlayer)
    {
        if (damagePopupPrefab == null) return;

        Transform spawnPoint = isPlayer ? playerSpawnPoint : opponentSpawnPoint;
        if (spawnPoint == null)
        {
            // Fallback de segurança se não for definido no inspector
            if (GameManager.Instance != null && GameManager.Instance.duelFieldUI != null)
            {
                spawnPoint = isPlayer ? GameManager.Instance.duelFieldUI.playerMonsterZones[2] : GameManager.Instance.duelFieldUI.opponentMonsterZones[2];
            }
            if (spawnPoint == null) spawnPoint = transform;
        }

        GameObject go = Instantiate(damagePopupPrefab, spawnPoint.position, Quaternion.identity, spawnPoint);
        DamagePopup popup = go.GetComponent<DamagePopup>();
        
        if (popup != null)
        {
            popup.Setup(amount, isHeal, this);
        }
    }
}
