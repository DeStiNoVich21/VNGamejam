using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragableElement : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    private RectTransform rectTransform;
    public Transform parentAfterDrag;
    public Transform parentBefDrag;
    private Canvas canvas;
    private Vector2 sizeBef;
    public Vector2 sizeAfter;
    public Vector2 moveOffset;
    public Image img;
    public TextMeshProUGUI text;
    public Characteristic characteristic;
    public Characters characters;
    public CanvasGroup canvasGroup,mainGroup;

    private RectTransform tempRect;
    private Transform originalParent;
    private Tween dragTween;
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        sizeBef = rectTransform.sizeDelta;
    }
    private void Start()
    {
        canvas = rectTransform.GetComponentInParent<Canvas>();
        originalParent = transform.parent;
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        parentBefDrag = transform.parent;
        parentAfterDrag = transform.parent;
        int indexWas = transform.GetSiblingIndex();
        rectTransform.SetParent(canvas.transform, worldPositionStays: false);
        transform.SetAsLastSibling();
        canvasGroup.alpha = 0;
        dragTween = rectTransform.DOSizeDelta(sizeAfter, 0.2f);
        tempRect = new GameObject($"Temp{name}",typeof(RectTransform)).GetComponent<RectTransform>();
        tempRect.SetParent(parentBefDrag);
        tempRect.SetSiblingIndex(indexWas);
        mainGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            canvas.worldCamera,
            out Vector2 localPoint
        );

        rectTransform.localPosition = localPoint + moveOffset;
    }


    public void OnEndDrag(PointerEventData eventData)
    {
        rectTransform.SetParent(parentAfterDrag, worldPositionStays: false);

        // ѕровер€ем, был ли изменен слот
        bool slotChanged = parentAfterDrag != parentBefDrag;
        bool returnedToOriginal = parentAfterDrag == originalParent;
        bool wasInOriginalBeforeDrag = parentBefDrag == originalParent;

        if (parentAfterDrag.TryGetComponent(out BigSlot bigSlot))
        {
            canvasGroup.alpha = 1;
            dragTween.Kill();
            rectTransform.sizeDelta = sizeBef;
            int indexWas = tempRect.GetSiblingIndex();
            rectTransform.SetSiblingIndex(indexWas);

            //  ислород добавл€етс€ только если перетащили в BigSlot
            ResourcesManagementData.Instance.managementResources.oxygen = Mathf.Min(
                (50 / characteristic.endurance) + ResourcesManagementData.Instance.managementResources.oxygen,
                ResourcesManagementData.Instance.managementResources.oxygen
            );
        }

        // Ћогика траты/возврата кислорода
        if (slotChanged)
        {
            if (wasInOriginalBeforeDrag && !returnedToOriginal)
            {
                // ”ходим из исходного слота в другой - тратим кислород
                ResourcesManagementData.Instance.managementResources.oxygen -= (200 / characteristic.endurance);
            }
            else if (!wasInOriginalBeforeDrag && returnedToOriginal)
            {
                // ¬озвращаемс€ в исходный слот из другого - возвращаем кислород
                ResourcesManagementData.Instance.managementResources.oxygen += (200 / characteristic.endurance);
            }
            // ≈сли перетаскиваем между не-исходными слотами - кислород не мен€етс€
        }

        mainGroup.blocksRaycasts = true;
        Destroy(tempRect.gameObject);
    }
}
