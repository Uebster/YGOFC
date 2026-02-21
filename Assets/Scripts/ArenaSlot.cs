using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ArenaSlot : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("UI References")]
    public Image avatarImage;
    public Image lockOverlay; // Imagem preta ou cadeado por cima
    public GameObject selectionHighlight; // Borda de seleção

    private CharacterData myCharacter;
    private ArenaManager manager;
    private bool isUnlocked;
    private int slotIndex;

    public void Setup(int index, CharacterData character, bool unlocked, ArenaManager arenaManager)
    {
        slotIndex = index;
        myCharacter = character;
        isUnlocked = unlocked;
        manager = arenaManager;

        // Configura visual de bloqueio
        if (lockOverlay != null) lockOverlay.gameObject.SetActive(!isUnlocked);
        
        // Tenta carregar a imagem do personagem
        if (avatarImage != null)
        {
            if (isUnlocked && myCharacter != null)
            {
                avatarImage.color = Color.white;
                // Tenta carregar sprite da pasta Resources/Characters/ID
                Sprite charSprite = Resources.Load<Sprite>($"Characters/{myCharacter.id}");
                if (charSprite != null) avatarImage.sprite = charSprite;
            }
            else
            {
                avatarImage.color = Color.black; // Silhueta se bloqueado
            }
        }

        if (selectionHighlight != null) selectionHighlight.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isUnlocked && manager != null)
        {
            // Opcional: Mostrar preview ao passar o mouse
            // manager.ShowPreview(myCharacter); 
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isUnlocked && manager != null)
        {
            manager.SelectCharacter(this, myCharacter);
            if (selectionHighlight != null) selectionHighlight.SetActive(true);
        }
        else
        {
            // Som de erro/bloqueado
            Debug.Log("Personagem bloqueado! Vença-o na campanha primeiro.");
        }
    }

    public void Deselect()
    {
        if (selectionHighlight != null) selectionHighlight.SetActive(false);
    }
}
