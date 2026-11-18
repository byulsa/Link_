using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PassiveInventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private Text label;
    private PassiveItemDefinition passive;
    private bool hovered;

    public void Setup(PassiveItemDefinition item)
    {
        passive = item;
        icon.sprite = item.icon;
        icon.gameObject.SetActive(item.icon != null);
        label.text = item.itemName;
        label.gameObject.SetActive(item.icon == null);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (passive == null) return;
        hovered = true;
        CodeBlockTooltip.Show(passive, eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (hovered) CodeBlockTooltip.Move(eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData) => HideTooltip();
    private void OnDisable() => HideTooltip();

    private void HideTooltip()
    {
        if (hovered) CodeBlockTooltip.Hide();
        hovered = false;
    }
}
