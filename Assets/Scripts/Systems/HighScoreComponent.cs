using UnityEngine;

namespace ArenaSurvivor.Systems
{
    public class HighScoreComponent : MonoBehaviour
    {
        private const string PlayerPrefsKey = "HighScoreSeconds";

        private HighScore highScore;

        public float Best => highScore.Best;

        private void Awake()
        {
            highScore = new HighScore(
                () => PlayerPrefs.GetFloat(PlayerPrefsKey, 0f),
                value =>
                {
                    PlayerPrefs.SetFloat(PlayerPrefsKey, value);
                    PlayerPrefs.Save();
                });
        }

        public bool TrySubmit(float score)
        {
            return highScore.TrySubmit(score);
        }
    }
}
