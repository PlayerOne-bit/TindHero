using UnityEngine;

[System.Serializable]
public class Denomination
{
    public float value;
    public Sprite moneyImage;

    /// <summary>
    /// All denomination values for Philippine Peso.
    /// </summary>
    public static readonly float[] Values = new float[]
    {
        0.01f,   // 1 centavo
        0.05f,   // 5 centavos
        0.25f,   // 25 centavos
        1f,      // ₱1
        5f,      // ₱5
        10f,     // ₱10
        20f,     // ₱20
        50f,     // ₱50
        100f,    // ₱100
        200f,    // ₱200
        500f,    // ₱500
        1000f    // ₱1000
    };

    public Denomination(float value, Sprite moneyImage)
    {
        this.value = value;
        this.moneyImage = moneyImage;
    }
}