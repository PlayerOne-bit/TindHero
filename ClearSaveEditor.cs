// ClearSaveEditor.cs — put in Assets/Editor folder
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class ClearSaveEditor
{
    [MenuItem("TindHERO/Clear Save Data")]
    public static void ClearSave()
    {
        string path = Path.Combine(Application.persistentDataPath, "tindhero.dat");
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"Save cleared at: {path}");
        }
        else
        {
            Debug.Log("No save file found.");
        }
    }

    [MenuItem("TindHERO/Show Save Path")]
    public static void ShowSavePath()
    {
        Debug.Log($"Save path: {Application.persistentDataPath}");
    }
}
#endif