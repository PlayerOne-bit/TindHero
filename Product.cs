using UnityEngine;

[System.Serializable]
public class Product
{
    public string productName;
    public Sprite sprite;
    public float baseCost;
    public float sellingPrice;
    public int quantity;
    public bool isCustom;
    public int spriteIndex; // ✅ used for saving/loading sprite

    public static int MaxQuantity = 10;

    public Product(string name, Sprite sprite, int spriteIndex,
               float baseCost, float sellingPrice, bool isCustom = false)
    {
        this.productName = name;
        this.sprite = sprite;
        this.spriteIndex = spriteIndex; // -1 for custom uploaded
        this.baseCost = baseCost;
        this.sellingPrice = sellingPrice;
        this.quantity = 0;
        this.isCustom = isCustom;
    }

    public bool IsAtMaxQuantity => quantity >= MaxQuantity;
    public bool HasStock => quantity > 0;
    public float RefundAmount => baseCost * quantity * 0.25f;
}