using UnityEngine;
using System.Collections.Generic;

public class NpcManager : MonoBehaviour
{
    public static NpcManager Instance { get; private set; }

    [Header("Default NPCs")]
    [SerializeField] private List<DefaultNpcData> defaultNpcDataList;

    [Header("Sprite Library")]
    [SerializeField] private Sprite[] spriteLibrary;

    [Header("Audio Library")]
    [SerializeField] private NpcAudioSet audioLibrary;
    [System.Serializable]
    public class NpcAudioSet
    {
        public AudioClip happyAudio;
        public AudioClip angryAudio;
        public AudioClip talkingAudio;
    }

    // ✅ Separate lists like StockManager
    private List<NpcData> defaultNpcs = new List<NpcData>();
    private List<NpcData> customNpcs = new List<NpcData>();

    public List<NpcData> DefaultNpcs => defaultNpcs;
    public List<NpcData> CustomNpcs => customNpcs;

    // ✅ Combined for easy iteration like StockManager.Products
    public List<NpcData> AllNpcs
    {
        get
        {
            List<NpcData> all = new List<NpcData>(defaultNpcs);
            all.AddRange(customNpcs);
            return all;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (defaultNpcs.Count == 0)
            LoadOrInitialize();

        NpcPanel[] panels = FindObjectsByType<NpcPanel>(FindObjectsSortMode.None);
        foreach (NpcPanel panel in panels)
            panel.OnNpcManagerReady();

        // ✅ Always notify GameplayManager regardless
        if (GameplayManager.Instance != null)
            GameplayManager.Instance.OnManagersReady();
    }

    private void LoadOrInitialize()
    {
        InitializeDefaultNpcs();

        // ✅ Load custom NPCs from save
        SaveManager.GameSaveData saved = SaveManager.Instance?.LoadAll();
        if (saved != null && saved.npcs != null && saved.npcs.Count > 0)
            RebuildCustomNpcs(saved.npcs);
    }
    private void RebuildCustomNpcs(List<NpcSaveData> savedNpcs)
    {
        customNpcs.Clear();

        foreach (NpcSaveData data in savedNpcs)
        {
            // ✅ Restore sprite
            Sprite sprite = !string.IsNullOrEmpty(data.customSpriteBase64)
                ? SaveManager.Base64ToSprite(data.customSpriteBase64)
                : null;

            // ✅ Restore audio clips
            AudioClip happyAudio = SaveManager.Base64ToAudioClip(
                data.happyAudioBase64, $"{data.npcName}_happy");
            AudioClip angryAudio = SaveManager.Base64ToAudioClip(
                data.angryAudioBase64, $"{data.npcName}_angry");
            AudioClip talkingAudio = SaveManager.Base64ToAudioClip(
                data.talkingAudioBase64, $"{data.npcName}_talking");

            NpcData npc = new NpcData(
                data.npcName,
                sprite,
                data.maxPreferredPrice,
                data.happyDialogue,
                data.angryDialogue,
                happyAudio, -1,
                angryAudio, -1,
                talkingAudio, -1
            );

            customNpcs.Add(npc);
        }
    }
    private void InitializeDefaultNpcs()
    {
        defaultNpcs.Clear();

        foreach (var data in defaultNpcDataList)
        {
            NpcAudioSet audioSet = GetAudioSet();

            defaultNpcs.Add(new NpcData(
                data.npcName,
                GetSprite(data.spriteIndex),
                data.spriteIndex,
                data.maxPreferredPrice,
                "Thank You!",
                "Need more practice!",
                audioSet?.happyAudio,
                audioSet?.angryAudio,
                audioSet?.talkingAudio
            ));
        }
    }

    public bool AddCustomNpc(string name, Sprite sprite,
                         float maxPreferredPrice,
                         string happyDialogue,
                         string angryDialogue,
                         AudioClip happyAudio, int happyAudioIndex,
                         AudioClip angryAudio, int angryAudioIndex,
                         AudioClip talkingAudio, int talkingAudioIndex)
    {
        float payment = maxPreferredPrice * 1.25f;
        if (PlayerManager.Instance.CurrentMoney < payment) return false;

        PlayerManager.Instance.SubtractMoney(payment);

        NpcData custom = new NpcData(
            name, sprite, maxPreferredPrice,
            happyDialogue, angryDialogue,
            happyAudio, happyAudioIndex,
            angryAudio, angryAudioIndex,
            talkingAudio, talkingAudioIndex
        );

        customNpcs.Add(custom);
        Save();
        return true;
    }

    public Sprite GetSprite(int index)
    {
        if (index >= 0 && index < spriteLibrary.Length)
            return spriteLibrary[index];
        return null;
    }

    public NpcAudioSet GetAudioSet()
    {
        if (audioLibrary != null)
            return audioLibrary;
        return null;
    }

    public bool DeleteCustomNpc(NpcData npc)
    {
        if (!npc.isCustom) return false;

        if (npc.RefundAmount > 0f)
            PlayerManager.Instance.AddMoney(npc.RefundAmount);

        customNpcs.Remove(npc);
        Save();
        return true;
    }

    public void Save()
    {
        StockManager.Instance?.Save(); // ✅ piggyback on StockManager save
    }
}

[System.Serializable]
public class DefaultNpcData
{
    public string npcName;
    public int spriteIndex;
    public float maxPreferredPrice;
}