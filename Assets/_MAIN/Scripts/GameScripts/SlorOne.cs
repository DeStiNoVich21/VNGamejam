
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SlorOne : Slot, IPointerEnterHandler, IPointerExitHandler
{
    public TooltipManager tooltipManager;
    [TextArea] public string tooltipTextIsNotWork;
    [TextArea] public string tooltipTextIsWork;

    public bool isWorking = true;

    private Canvas Rootcanvas;
    private Image image;
    private void Start()
    {
        Rootcanvas = GetComponentInParent<Canvas>();
        image = GetComponentInParent<Image>();
    }

    public void Update()
    {
        image.color = isWorking ? Color.green : Color.red;
    }

    public override void OnDrop(PointerEventData eventData)
    {
        if (transform.childCount > 0 || isWorking || ResourcesManagementData.Instance.managementResources.oxygen < 0)
            return;
        base.OnDrop(eventData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipManager == null)
            return;

        tooltipManager.Show(isWorking ? tooltipTextIsWork :  tooltipTextIsNotWork, eventData.position, Rootcanvas);

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltipManager.Hide();
    }
}
