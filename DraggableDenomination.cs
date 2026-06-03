using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableDenomination : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Denomination denomination;
    private RectTransform draggableAreaPanel;
    private RectTransform returnPanel;
    private RectTransform customerPanel;
    private TransactionPanel transactionPanel;

    private RectTransform rt;
    private Canvas canvas;
    public bool isNpcPayment = false;
    public void Initialize(
    Denomination denom,
    RectTransform draggableArea,
    RectTransform returnPnl,
    RectTransform customerPnl,
    TransactionPanel txPanel)
    {
        denomination = denom;
        draggableAreaPanel = draggableArea;
        returnPanel = returnPnl;
        customerPanel = customerPnl;
        transactionPanel = txPanel;

        rt = GetComponent<RectTransform>();

        // ✅ Cache canvas from draggableAreaPanel directly, not from self
        canvas = draggableAreaPanel.GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // ✅ Always re-cache canvas from root canvas
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = draggableAreaPanel.GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>(); // ✅ last resort fallback

        transform.SetParent(draggableAreaPanel, true);
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        rt.anchoredPosition += eventData.delta / canvas.scaleFactor;

        // ✅ Clamp position to stay inside draggableAreaPanel
        Vector2 clampedPos = rt.anchoredPosition;

        float halfW = (draggableAreaPanel.rect.width - rt.rect.width) * 0.5f;
        float halfH = (draggableAreaPanel.rect.height - rt.rect.height) * 0.5f;

        clampedPos.x = Mathf.Clamp(clampedPos.x, -halfW, halfW);
        clampedPos.y = Mathf.Clamp(clampedPos.y, -halfH, halfH);

        rt.anchoredPosition = clampedPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (IsOverlapping(returnPanel))
        {
            transactionPanel.OnReturnedFromDrag(denomination, 1, isNpcPayment);
            Destroy(gameObject);
        }
        else if (customerPanel != null && IsOverlapping(customerPanel))
        {
            transactionPanel.OnSentToCustomer(denomination, 1);
            Destroy(gameObject);
        }
        // else stays where dropped
    }

    /// <summary>
    /// Checks if this image's RectTransform overlaps with the target panel.
    /// </summary>
    private bool IsOverlapping(RectTransform panel)
    {
        if (panel == null) return false; // ✅ null check
        Rect selfRect = GetWorldRect(rt);
        Rect panelRect = GetWorldRect(panel);
        return selfRect.Overlaps(panelRect);
    }

    private Rect GetWorldRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        // corners: 0=bottomLeft, 1=topLeft, 2=topRight, 3=bottomRight
        return new Rect(
            corners[0].x,
            corners[0].y,
            corners[2].x - corners[0].x,
            corners[2].y - corners[0].y
        );
    }

    
}