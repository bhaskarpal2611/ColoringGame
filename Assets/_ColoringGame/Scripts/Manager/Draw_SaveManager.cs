using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace ColorSwipeGame
{

    [System.Serializable]
    public class SavedDrawnData
    {
        public List<DrawnData> drawnDatas = new();
    }
    public class Draw_SaveManager : MonoBehaviour
    {
        [SerializeField] private DrawnDataSO _drawnDataSO;

        private string _filePath;

        private void Awake()
        {
            _filePath = Path.Combine(Application.persistentDataPath, "drawingSaveState.json");

            LoadAllDrawings();
        }

        private SavedDrawnData LoadSavedData()
        {
            if (File.Exists(_filePath))
            {
                // Read the JSON file
                string json = File.ReadAllText(_filePath);

                // Deserialize the JSON string back 
                SavedDrawnData savedData = JsonUtility.FromJson<SavedDrawnData>(json);

                Debug.Log("Saved Draw Data loaded from: " + _filePath);
                return savedData;
            }
            else
            {
                Debug.LogWarning("Save file not found");
                return null;
            }
        }


        private void LoadAllDrawings()
        {
            SavedDrawnData savedData = LoadSavedData();
            if (savedData == null) return;

            for (int i = 0; i < savedData.drawnDatas.Count; i++)
            {
                _drawnDataSO.RestoreDrawnData(savedData.drawnDatas[i]);
            }
        }

        public void SaveDrawingsData()
        {
            SavedDrawnData savedDrawnData = new();

            for (int i = 0; i < _drawnDataSO.Levels.Count; i++)
            {
                var obj = _drawnDataSO.GetSaveData(i);
                savedDrawnData.drawnDatas.Add(obj);
            }

            // Serialize the CurrentTextures field to JSON
            string json = JsonUtility.ToJson(savedDrawnData, true);  // Pretty print for readability

            // Write the JSON to a file
            File.WriteAllText(_filePath, json);

            Debug.Log("Level data saved to: " + _filePath);
        }

    }
}
