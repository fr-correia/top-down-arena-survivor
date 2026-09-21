using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvivor.UI
{
    public class RunTimerComponent : MonoBehaviour
    {
        [SerializeField] private Text timerText;

        private float elapsed;
        private bool running;

        public float ElapsedSeconds => elapsed;

        public void StartTimer()
        {
            elapsed = 0f;
            running = true;
        }

        public void StopTimer()
        {
            running = false;
        }

        private void Update()
        {
            if (!running)
            {
                return;
            }

            elapsed += Time.deltaTime;
            int minutes = Mathf.FloorToInt(elapsed / 60f);
            int seconds = Mathf.FloorToInt(elapsed % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}
