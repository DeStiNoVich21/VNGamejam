using UnityEngine;
using UnityEngine.EventSystems;

public class Slot : MonoBehaviour, IDropHandler
{
    protected DragableElement dragableElement;
    public virtual void OnDrop(PointerEventData eventData)
    {
        var dropped = eventData.pointerDrag;
        dragableElement = dropped.GetComponent<DragableElement>();
        dragableElement.parentAfterDrag = transform;
    }
}
