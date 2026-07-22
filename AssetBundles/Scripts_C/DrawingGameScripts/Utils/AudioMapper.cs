using UnityEngine;
using System.Reflection;

namespace DrawingGame
{
    /// <summary>
    /// Serves as a bridge between game logic and the runtime audio loading system.
    /// Manages string keys for various audio variations.
    /// </summary>
    public class AudioMapper : GenericSingleton<AudioMapper>
    {
        [Header("Tutorial")]
        public string[] Intro = { "Intro1", "Intro2" };

        [Header("Gameplay")]
        public string[] GameStart = { "Start1", "Start2" };
        public string[] GameEnd = { "End", "End2", "End3" };

        [Header("Feedback")]
        public string[] Cheer = { "Cheer1", "Cheer2", "Cheer3", "Cheer4", "Cheer5", "Cheer6", "Cheer7", "Cheer8", "Cheer9", "Cheer10" };

        #region Random Key Methods

        public string GetRandomIntro() => GetRandom(Intro);
        public string GetRandomGameStart() => GetRandom(GameStart);
        public string GetRandomGameEnd() => GetRandom(GameEnd);
        public string GetRandomCheer() => GetRandom(Cheer);

        private string GetRandom(string[] array)
        {
            if (array == null || array.Length == 0) return string.Empty;
            // Using exclusive upper bound for Random.Range
            return array[UnityEngine.Random.Range(0, array.Length)];
        }

        #endregion

        #region Functional Resolvers

        /// <summary>
        /// Retrieves a specific audio key by index from a logical group (array).
        /// </summary>
        public string GetKeyByIndex(string arrayName, int index)
        {
            FieldInfo field = GetType().GetField(arrayName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(string[]))
            {
                string[] array = (string[])field.GetValue(this);
                if (array != null && index >= 0 && index < array.Length)
                {
                    return array[index];
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Bridges existing Enum-based calls to the new string system.
        /// </summary>
        public string GetRandomKeyFor(Sounds soundType)
        {
            return soundType switch
            {
                Sounds.GameIntro => GetRandomIntro(),
                Sounds.GameStart => GetRandomGameStart(),
                Sounds.GameEnd => GetRandomGameEnd(),
                Sounds.Cheer => GetRandomCheer(),
                _ => string.Empty
            };
        }

        #endregion

#if UNITY_EDITOR
        /// <summary>
        /// Automatically populates newly added array elements in the Inspector.
        /// e.g., Adding an element to 'Cheer' will auto-assign 'Cheer11'.
        /// </summary>
        private void OnValidate()
        {
            FieldInfo[] fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(string[]))
                {
                    string[] array = (string[])field.GetValue(this);
                    if (array != null)
                    {
                        bool changed = false;
                        for (int i = 0; i < array.Length; i++)
                        {
                            // Only auto-fill if empty or null (newly added in Inspector)
                            if (string.IsNullOrEmpty(array[i]))
                            {
                                array[i] = field.Name + i;
                                changed = true;
                            }
                        }
                        if (changed) field.SetValue(this, array);
                    }
                }
            }
        }
#endif
    }
}
