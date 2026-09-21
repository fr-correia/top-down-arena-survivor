namespace ArenaSurvivor.Systems
{
    public class Cooldown
    {
        private const float MinInterval = 0.1f;

        private float lastTriggerTime = float.NegativeInfinity;

        public float Interval { get; private set; }

        public Cooldown(float interval)
        {
            Interval = interval;
        }

        public void SetInterval(float newInterval)
        {
            Interval = newInterval < MinInterval ? MinInterval : newInterval;
        }

        public bool IsReady(float currentTime)
        {
            return currentTime - lastTriggerTime >= Interval;
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
