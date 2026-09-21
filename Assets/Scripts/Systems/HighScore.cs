using System;

namespace ArenaSurvivor.Systems
{
    public class HighScore
    {
        private readonly Func<float> load;
        private readonly Action<float> save;

        public HighScore(Func<float> load, Action<float> save)
        {
            this.load = load;
            this.save = save;
        }

        public float Best => load();

        public bool TrySubmit(float newScore)
        {
            if (newScore <= Best)
            {
                return false;
            }

            save(newScore);
            return true;
        }
    }
}
