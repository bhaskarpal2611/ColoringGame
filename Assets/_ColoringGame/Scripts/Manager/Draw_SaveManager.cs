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

        private string _completePath;

        private string _fileName  = "drawingSaveState.json";

        private void Awake()
        {
            _filePath = Path.Combine(Application.persistentDataPath, "JSONDrawing");

            if (!Directory.Exists(_filePath))
            {
                Directory.CreateDirectory(_filePath);
                Debug.Log($"Created folder: {_filePath}");
            }
            else
            {
                Debug.Log($"Folder already exists: {_filePath}");
            }

            _completePath = Path.Combine(_filePath, _fileName);

            LoadAllDrawings();
        }

        private SavedDrawnData LoadSavedData()
        {
            if (File.Exists(_completePath))
            {
                // Read the JSON file
                string json = File.ReadAllText(_completePath);

                // Deserialize the JSON string back 
                SavedDrawnData savedData = JsonUtility.FromJson<SavedDrawnData>(json);

                Debug.Log("Saved Draw Data loaded from: " + _completePath);
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
            File.WriteAllText(_completePath, json);

            Debug.Log("Level data saved to: " + _completePath);
        }

    }
}
