using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace ColorSwipeGame
{

    [System.Serializable]
    public class LevelSaveData
    {
        public List<SaveData> saveDatas = new();
    }

    public class SaveManager : MonoBehaviour
    {
        [SerializeField] private LevelDataSO _levelDataSO;

        private LevelSaveData _saveData;

        private string _filePath;

        private void Awake()
        {
            _filePath = Path.Combine(Application.persistentDataPath, "currentSaveState.json");
            LoadAllLevels();
        }

        private LevelSaveData LoadLevelsData()
        {
            if (File.Exists(_filePath))
            {
                // Read the JSON file
                string json = File.ReadAllText(_filePath);

                // Deserialize the JSON string back into AllTexturesData
                LevelSaveData levelData = JsonUtility.FromJson<LevelSaveData>(json);

                Debug.Log("CurrentTextures data loaded from: " + _filePath);
                return levelData;
            }
            else
            {
                Debug.LogWarning("Save file not found");
                return null;
            }
        }

        private void LoadAllLevels()
        {
            LevelSaveData levelsData = LoadLevelsData();
            if (levelsData == null) return;

            for (int i = 0; i < levelsData.saveDatas.Count; i++)
            {
                if (levelsData.saveDatas[i].IsEdited)
                {
                    _levelDataSO.RestoreSaveData(levelsData.saveDatas[i]);
                }
            }
        }

        public void SaveLevelsData()
        {
            LevelSaveData levelsData = new();
            Debug.Log("chk save");

            for (int i = 0; i < _levelDataSO.Levels.levelsData.Length; i++)
            {
                if (_levelDataSO.Levels.levelsData[i].IsEdited)
                {
                    SaveData savdata = _levelDataSO.GetSaveData(i);
                    levelsData.saveDatas.Add(savdata);
                }
                else
                {
                    SaveData newData = new SaveData(i);
                    levelsData.saveDatas.Add(newData);
                }
            }

            // Serialize the CurrentTextures field to JSON
            string json = JsonUtility.ToJson(levelsData, true);  // Pretty print for readability

            // Write the JSON to a file
            File.WriteAllText(_filePath, json);

            Debug.Log("Level data saved to: " + _filePath);
        }

    }
}
