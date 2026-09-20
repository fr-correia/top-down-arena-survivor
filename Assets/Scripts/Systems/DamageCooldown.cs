namespace ArenaSurvivor.Systems
{
    public class DamageCooldown
    {
        private readonly float interval;
        private float lastTriggerTime = float.NegativeInfinity;

        public DamageCooldown(float interval)
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
