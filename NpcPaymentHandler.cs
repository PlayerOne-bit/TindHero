using UnityEngine;
using System.Collections.Generic;

public static class NpcPaymentHandler
{
    private static readonly float[] denominations = new float[]
    {
        1000f, 500f, 200f, 100f, 50f, 20f, 10f, 5f, 1f, 0.25f, 0.05f, 0.01f
    };

    private const float CHANGE_TOLERANCE = 1.00f;

    public static List<(float denomination, int quantity)> GeneratePayment(float totalCost)
    {
        List<(float denomination, int quantity)> payment = new List<(float, int)>();
        float rounded = Mathf.Ceil(totalCost);

        // ✅ Build candidate bills that are strictly greater than total
        float[] bills = new float[] { 1000f, 500f, 200f, 100f, 50f, 20f, 10f, 5f, 1f };
        List<(float bill, float weight)> candidates = new List<(float, float)>();

        foreach (float bill in bills)
        {
            if (bill <= rounded) continue; // ✅ only bills that can actually pay

            float weight;

            // ✅ How "overshooting" is this bill relative to total?
            // The more it overshoots, the less likely NPC uses it
            float overshoot = bill - rounded;
            float overshootRatio = overshoot / bill; // 0 = barely over, 1 = massively over

            if (bill == 1000f)
            {
                // ✅ Very unlikely if total is small (e.g. ₱15 — why pay 1k?)
                // Only natural when total is close to 1000 (overshootRatio near 0)
                weight = Mathf.Lerp(0.05f, 0.6f, 1f - overshootRatio);
            }
            else if (bill == 500f)
            {
                weight = Mathf.Lerp(0.08f, 0.7f, 1f - overshootRatio);
            }
            else if (bill == 200f)
            {
                weight = Mathf.Lerp(0.15f, 0.8f, 1f - overshootRatio);
            }
            else
            {
                // ✅ Smaller bills (100 and below) — naturally preferred when they fit
                // Slight preference for bills closest to total (less change needed)
                weight = Mathf.Lerp(0.5f, 1.0f, 1f - overshootRatio);
            }

            candidates.Add((bill, weight));
        }

        // ✅ Fallback — if nothing qualifies (total > 1000), just use 1000
        if (candidates.Count == 0)
        {
            payment.Add((1000f, Mathf.CeilToInt(rounded / 1000f)));
            return payment;
        }

        // ✅ Weighted random selection
        float totalWeight = 0f;
        foreach (var (_, w) in candidates) totalWeight += w;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        float selectedBill = candidates[candidates.Count - 1].bill; // fallback

        foreach (var (bill, w) in candidates)
        {
            cumulative += w;
            if (roll <= cumulative)
            {
                selectedBill = bill;
                break;
            }
        }

        payment.Add((selectedBill, 1));
        return payment;
    }
    /// <summary>
    /// Generates a random bill strictly greater than totalCost
    /// that the player can make change for based on their current money.
    /// Higher denominations are rarer based on player money.
    /// </summary>
    public static List<(float denomination, int quantity)> GenerateAffordablePayment(
    float totalCost, float playerMoney)
    {
        List<(float denomination, int quantity)> payment = new List<(float, int)>();
        float[] bills = new float[] { 1000f, 500f, 200f, 100f, 50f, 20f, 10f, 5f, 1f };

        float rounded = Mathf.Ceil(totalCost);

        List<(float bill, float weight)> candidates = new List<(float, float)>();

        foreach (float bill in bills)
        {
            if (bill <= rounded) continue; // ✅ must be strictly greater than cost

            float changeNeeded = bill - rounded;

            // ✅ Player must have enough money to make exact change
            if (changeNeeded > playerMoney) continue;

            // ✅ Weight based on how rare the bill should be
            float weight;
            if (bill == 1000f)
                weight = Mathf.Lerp(0.02f, 0.1f, playerMoney / 10000f); // ✅ scales with wealth
            else if (bill == 500f)
                weight = Mathf.Lerp(0.05f, 0.2f, playerMoney / 5000f);
            else if (bill == 200f)
                weight = Mathf.Lerp(0.1f, 0.4f, playerMoney / 2000f);
            else if (bill == 100f)
                weight = 0.5f;
            else
                weight = 1.0f; // ✅ small bills always common

            candidates.Add((bill, weight));
        }

        // ✅ Fallback if no valid candidates
        if (candidates.Count == 0)
        {
            // ✅ Use exact payment instead
            return GenerateExactPayment(totalCost);
        }

        // ✅ Weighted random
        float totalWeight = 0f;
        foreach (var (_, w) in candidates) totalWeight += w;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        float selectedBill = candidates[0].bill;

        foreach (var (bill, w) in candidates)
        {
            cumulative += w;
            if (roll <= cumulative)
            {
                selectedBill = bill;
                break;
            }
        }

        payment.Add((selectedBill, 1));
        return payment;
    }
    public static bool IsChangeAcceptable(float changeGiven, float changeRequired)
    {
        float difference = Mathf.Abs(changeGiven - changeRequired);
        return difference <= CHANGE_TOLERANCE;
    }

    public static bool IsAffordable(float[] productPrices, float npcMaxPreferredPrice)
    {
        // ✅ Check each product's base price individually
        foreach (float price in productPrices)
        {
            if (price > npcMaxPreferredPrice)
                return false; // ✅ even one product too expensive = NPC leaves
        }
        return true;
    }
    public static List<(float denomination, int quantity)> GenerateExactPayment(float totalCost)
    {
        List<(float denomination, int quantity)> payment = new List<(float, int)>();
        float[] bills = new float[] { 1000f, 500f, 200f, 100f, 50f, 20f, 10f, 5f, 1f, 0.25f, 0.05f, 0.01f };

        float remaining = Mathf.Round(totalCost * 100f) / 100f;

        foreach (float bill in bills)
        {
            if (remaining <= 0f) break;
            int qty = Mathf.FloorToInt(remaining / bill);
            if (qty > 0)
            {
                payment.Add((bill, qty));
                remaining -= bill * qty;
                remaining = Mathf.Round(remaining * 100f) / 100f;
            }
        }

        return payment;
    }
 
}