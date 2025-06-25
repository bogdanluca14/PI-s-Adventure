using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    // Variabile Globale

    // Instanta Salvarii
    public static SaveData saveData;

    // Salvarea nivelului curent/progresului
    public static void SaveLevel(
        List<FunctionPlotter.GraphData> _graphDatas = null,
        int _efficiency = 0,
        int _level = 0
    )
    {
        if (saveData == null)
            LoadLevel();

        if (_graphDatas != null)
        {
            int _lastLevel = saveData.lastLevel;

            if (_lastLevel > _level || (_lastLevel == _level && saveData.efficiency.Count > _level))
            {
                if (saveData.efficiency[_level] > _efficiency)
                    return;
                saveData.graphDatas[_level].graphDatas = _graphDatas;

                saveData.efficiency[_level] = _efficiency;
            }
            else
            {
                saveData.graphDatas.Add(new GraphDataList { graphDatas = _graphDatas });
                saveData.efficiency.Add(_efficiency);

                saveData.lastLevel = _level;
            }
        }

        string json = JsonUtility.ToJson(saveData);
        string path = Application.persistentDataPath + "/gameSave.json";

        System.IO.File.WriteAllText(path, json);
    }

    // Incarcarea nivelului curent/progresului
    public static void LoadLevel(TextAsset file = null)
    {
        string path = Application.persistentDataPath + "/gameSave.json";

        if (File.Exists(path) || file != null)
        {
            string json = file != null ? file.text : System.IO.File.ReadAllText(path);

            saveData = JsonUtility.FromJson<SaveData>(json);
        }
        else
        {
            saveData = new SaveData
            {
                graphDatas = new List<GraphDataList>(),
                efficiency = new List<int>(),
                lastLevel = 0,
                firstLaunch = true,
            };
        }
    }

    // Prima lansare (finalul tutorialului)
    public static void FirstLaunched()
    {
        saveData.firstLaunch = false;
        SaveLevel();
    }
}
