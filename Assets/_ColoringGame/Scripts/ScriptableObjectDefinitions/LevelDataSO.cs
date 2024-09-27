using UnityEngine;

[CreateAssetMenu(fileName = "SO/Levels", menuName = "SO/Levels")]
public class LevelDataSO : ScriptableObject
{
    public LevelData[] LevelData;

    public GameObject GetLevelPrefab(int index)
    {
        if (index < 0 || index >= LevelData.Length)
        {
            Debug.LogError("Index out of bound");
            return null;
        }

        return LevelData[index].levelPrefab;
    }
}

[System.Serializable]
public class LevelData
{
    public int levelIndex;
    public GameObject levelPrefab;
    public bool isLevelCompleted = false;
}
