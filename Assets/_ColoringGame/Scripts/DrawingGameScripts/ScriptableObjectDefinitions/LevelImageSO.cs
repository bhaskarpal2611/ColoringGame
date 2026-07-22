using UnityEngine;

namespace DrawingGame
{
    [CreateAssetMenu(fileName = "SO/LevelImages", menuName = "SO/LevelImages")]
    public class LevelImageSO : ScriptableObject
    {
        public LevelImages data;
    }

    [System.Serializable]
    public struct LevelImages
    {
        public Sprite[] LevelSprites;
    }
}
