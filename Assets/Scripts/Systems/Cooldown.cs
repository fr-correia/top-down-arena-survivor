namespace ArenaSurvivor.Systems
{
    public class Cooldown
    {
        private readonly float interval;
        private float lastTriggerTime = float.NegativeInfinity;

        public Cooldown(float interval)
        {
            this.interval = interval;
        }

        public bool TryConsume(float currentTime)
        {
            if (currentTime - lastTriggerTime < interval)
            {
                return false;
            }

            lastTriggerTime = currentTime;
            return true;
        }
    }
}
