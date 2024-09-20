using UnityEngine;

[CreateAssetMenu(fileName = "SO/Levels", menuName = "SO/Levels")]
public class LevelDataSO : ScriptableObject
{
    public LevelData[] levels;
}

[System.Serializable]
public class LevelData
{
    public int levelIndex;
    public GameObject levelPrefab;
    public bool isLevelCompleted = false;
}
