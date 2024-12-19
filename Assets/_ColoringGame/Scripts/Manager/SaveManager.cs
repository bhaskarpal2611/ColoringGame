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

        private string _jsonFilePath;

        private string _jsonFileName = "currentSaveState.json";

        private string _finalPath;

        private void Awake()
        {
#if UNITY_ANDROID
            _jsonFilePath = Path.Combine(Application.persistentDataPath, "JSONColoring");

            CreateFolder(_jsonFilePath);

            _finalPath = Path.Combine(_jsonFilePath, _jsonFileName);
            Debug.Log("Final Path: " + _finalPath);

            LoadAllLevels();
#endif
        }

        public void CreateFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                Debug.Log($"Created folder: {folderPath}");
            }
            else
            {
                Debug.Log($"Folder already exists: {folderPath}");
            }
        }

        private LevelSaveData LoadLevelsData()
        {
            if (File.Exists(_finalPath))
            {
                // Read the JSON file
                string json = File.ReadAllText(_finalPath);

                // Deserialize the JSON string back into AllTexturesData
                LevelSaveData levelData = JsonUtility.FromJson<LevelSaveData>(json);

                Debug.Log("CurrentTextures data loaded from: " + _finalPath);
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

        public bool CheckAllLevelsCompleted()
        {
            int count = 0;
            int length = _levelDataSO.Levels.levelsData.Length;
            for (int i = 0; i < length; i++)
            {
                if (_levelDataSO.IsEdited(i))
                {
                    count++;
                }
            }

            return count >= length - 1;
        }

        public void SaveLevelsData()
        {
            LevelSaveData AllLevelData = new();

            for (int i = 0; i < _levelDataSO.Levels.levelsData.Length; i++)
            {
                if (_levelDataSO.IsEdited(i))
                {
                    SaveData savedata = _levelDataSO.GetSaveData(i);
                    AllLevelData.saveDatas.Add(savedata);
                }
                else
                {
                    SaveData newData = new SaveData(i);
                    AllLevelData.saveDatas.Add(newData);
                }
            }

            // Serialize the CurrentTextures field to JSON
            string json = JsonUtility.ToJson(AllLevelData, true);  // Pretty print for readability

            // Write the JSON to a file
            File.WriteAllText(_finalPath, json);

            Debug.Log("Level data saved to: " + _finalPath);
        }

    }
}
