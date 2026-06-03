using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableProduct : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Product product;
    private RectTransform draggableAreaPanel;
    private RectTransform returnPanel;
    private RectTransform customerPanel;
    private TransactionPanel transactionPanel;
    private RectTransform rt;
    private Canvas canvas;

    public void Initialize(
        Product prod,
        RectTransform draggableArea,
        RectTransform returnPnl,
        RectTransform customerPnl,
        TransactionPanel txPanel)
    {
        product = prod;
        draggableAreaPanel = draggableArea;
        returnPanel = returnPnl;
        customerPanel = customerPnl;
        transactionPanel = txPanel;

        rt = GetComponent<RectTransform>();
        canvas = draggableAreaPanel.GetComponentInParent<Canvas>();

        // ✅ Assign sprite from product
        Image img = GetComponent<Image>();
        if (img != null && product.sprite != null)
        {
            img.sprite = product.sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = draggableAreaPanel.GetComponentInParent<Canvas>();

        transform.SetParent(draggableAreaPanel, true);
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        rt.anchoredPosition += eventData.delta / canvas.scaleFactor;

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
            transactionPanel.OnProductReturnedFromDrag(product, 1);
            Destroy(gameObject);
        }
        else if (IsOverlapping(customerPanel))
        {
            transactionPanel.OnProductSentToCustomer(product, 1);
            Destroy(gameObject);
        }
        // else stays where dropped
    }

    private bool IsOverlapping(RectTransform panel)
    {
        Rect selfRect = GetWorldRect(rt);
        Rect panelRect = GetWorldRect(panel);
        return selfRect.Overlaps(panelRect);
    }

    private Rect GetWorldRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return new Rect(
            corners[0].x,
            corners[0].y,
            corners[2].x - corners[0].x,
            corners[2].y - corners[0].y
        );
    }
}