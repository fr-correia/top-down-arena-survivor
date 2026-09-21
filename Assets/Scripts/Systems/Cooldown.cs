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

        public bool IsReady(float currentTime)
        {
            return currentTime - lastTriggerTime >= interval;
        }

        public bool TryConsume(float currentTime)
        {
            if (!IsReady(currentTime))
            {
                return false;
            }

            lastTriggerTime = currentTime;
            return true;
        }
    }
}
