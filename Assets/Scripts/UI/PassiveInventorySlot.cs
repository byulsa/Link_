using UnityEngine;
using UnityEngine.EventSystems;

public class PassiveInventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
{
    private PassiveItemDefinition passive;
    private bool hovered;

    public void Setup(PassiveItemDefinition item) => passive = item;

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
