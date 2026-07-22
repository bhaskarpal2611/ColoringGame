using UnityEngine;
using UnityEngine.SceneManagement;

namespace DrawingGame
{
    public class TimeKeeper : MonoBehaviour
    {
        private float _totalPlayingTime;

        public void StartTimer() => _totalPlayingTime = 0f;

        public void AddTime() => _totalPlayingTime += Time.deltaTime;


        public void GoBackToMainMenu()
        {
            SceneManager.LoadScene(TMKOCPlaySchoolConstants.TMKOCPlayMainMenu);
        }
    }
}
