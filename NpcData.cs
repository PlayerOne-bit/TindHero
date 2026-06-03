using UnityEngine;

[System.Serializable]
public class NpcData
{
    public string npcName;
    public Sprite sprite;
    public int spriteIndex;
    public float maxPreferredPrice;
    public bool isCustom;
    public float creationCost;

    // ✅ Separated dialogue arrays
    public string happyDialogue;   // ✅ single string
    public string angryDialogue;   // ✅ single string

    public AudioClip happyAudio;
    public AudioClip angryAudio;
    public AudioClip talkingAudio;

    // ✅ Default NPC constructor (this is updated as well as the other related scripts like NpcManager the default has switched to one dialogue and 1 audio clip for default)
    public NpcData(string name, Sprite sprite, int spriteIndex,
                   float maxPreferredPrice,
                   string happyDialogue,
                   string angryDialogue,
                   AudioClip happyAudio,
                   AudioClip angryAudio, 
                   AudioClip talkingAudio)
    {
        this.npcName = name;
        this.sprite = sprite;
        this.spriteIndex = spriteIndex;
        this.maxPreferredPrice = maxPreferredPrice;
        this.isCustom = false;
        this.happyDialogue = happyDialogue;
        this.angryDialogue = angryDialogue;
        this.creationCost = 0f;
        this.happyAudio = happyAudio;
        this.angryAudio = angryAudio;
        this.talkingAudio = talkingAudio;
    }

    // ✅ Custom NPC constructor
    public NpcData(string name, Sprite sprite,
                   float maxPreferredPrice,
                   string happyDialogue,
                   string angryDialogue,
                   AudioClip happyAudio, int happyAudioIndex,
                   AudioClip angryAudio, int angryAudioIndex,
                   AudioClip talkingAudio, int talkingAudioIndex)
    {
        this.npcName = name;
        this.sprite = sprite;
        this.spriteIndex = -1;
        this.maxPreferredPrice = maxPreferredPrice;
        this.isCustom = true;
        this.happyDialogue = happyDialogue;
        this.angryDialogue = angryDialogue;
        this.creationCost = maxPreferredPrice * 1.25f;
        this.happyAudio = happyAudio;
        this.angryAudio = angryAudio;
        this.talkingAudio = talkingAudio;
    }

    public float RefundAmount => creationCost * 0.25f;
    public string PriceRangeDisplay => $"Max Preferred Price:\n₱ {maxPreferredPrice:N2}";

    // ✅ Prebuilt talking dialogue — game generates this
    public string GetTalkingDialogue(int[] quantities, string[] productNames)
    {
        return BuildDefaultDialogue(quantities, productNames);
    }

    public string GetTalkingDialogue(int quantity, string productName)
    {
        return BuildDefaultDialogue(new int[] { quantity }, new string[] { productName });
    }

    private string BuildDefaultDialogue(int[] quantities, string[] productNames)
    {
        if (quantities.Length == 1)
            return $"I want {quantities[0]} {productNames[0]}.";

        System.Text.StringBuilder sb = new System.Text.StringBuilder("I want ");

        for (int i = 0; i < quantities.Length; i++)
        {
            if (i == quantities.Length - 1)
                sb.Append($"and {quantities[i]} {productNames[i]}");
            else
                sb.Append($"{quantities[i]} {productNames[i]}, ");
        }

        sb.Append(".");
        return sb.ToString();
    }

    public string GetHappyDialogue() => happyDialogue;
    public string GetAngryDialogue() => angryDialogue;
    
}