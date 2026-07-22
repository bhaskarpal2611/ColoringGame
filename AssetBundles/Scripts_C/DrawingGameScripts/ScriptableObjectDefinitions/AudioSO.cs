using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DrawingGame
{
    [CreateAssetMenu(fileName = "Audio", menuName = "SO/Audio")]
    public class AudioSO : ScriptableObject
    {
        public AudioInfo[] Audios;
    }
}
