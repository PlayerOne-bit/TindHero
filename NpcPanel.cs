using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NpcPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject npcRowPrefab;

    private List<NpcRowPanel> rows = new List<NpcRowPanel>();
    [Header("Search")]
    [SerializeField] private TMP_InputField searchField;

    private void Start()
    {
        if (searchField != null)
            searchField.onValueChanged.AddListener(OnSearchChanged);
    }

    private void OnSearchChanged(string query)
    {
        query = query.ToLower().Trim();

        foreach (NpcRowPanel row in rows)
        {
            if (row == null) continue;

            if (string.IsNullOrEmpty(query))
                row.gameObject.SetActive(true);
            else
            {
                bool matches = row.npcData.npcName.ToLower().Contains(query);
                row.gameObject.SetActive(matches);
            }
        }
    }
    private void OnEnable()
    {
        if (NpcManager.Instance == null) return;
        PopulateRows();
    }

    public void OnNpcManagerReady()
    {
        PopulateRows();
    }

    private void PopulateRows()
    {
        ClearRows();

        // ✅ Customs after
        foreach (NpcData npc in NpcManager.Instance.CustomNpcs)
            SpawnRow(npc);
    }

    private void SpawnRow(NpcData npc)
    {
        GameObject rowObj = Instantiate(npcRowPrefab, content);
        NpcRowPanel row = rowObj.GetComponent<NpcRowPanel>();
        row.Initialize(npc, this);
        rows.Add(row);
    }

    private void ClearRows()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);
        rows.Clear();
    }

    public void RefreshPanel()
    {
        PopulateRows();
    }

    public void AddCustomRow(NpcData npc)
    {
        SpawnRow(npc);
    }
}