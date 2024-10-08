using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSwipeGame
{
    public class ButtonGlowHandler : MonoBehaviour
    {
        [SerializeField] private Image _glowingBG;
        [SerializeField] private Button _button;

        public void FadeGlow(float fadeValue) => _glowingBG.DOFade(fadeValue, 0.25f);

    }
}
