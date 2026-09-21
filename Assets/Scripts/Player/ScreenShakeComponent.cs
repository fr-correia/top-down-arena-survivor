using UnityEngine;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Player
{
    public class ScreenShakeComponent : MonoBehaviour
    {
        private ScreenShake shake;
        private System.Random random;
        private float shakeDuration;
        private float shakeMagnitude;
        private float shakeElapsed;
        private Vector3 currentOffset;

        private void Awake()
        {
            shake = new ScreenShake();
            random = new System.Random();
        }

        public void Shake(float duration, float magnitude)
        {
            shakeDuration = duration;
            shakeMagnitude = magnitude;
            shakeElapsed = 0f;
        }

        public Vector3 GetCurrentOffset()
        {
            return currentOffset;
        }

        private void Update()
        {
            if (shakeElapsed < shakeDuration)
            {
                shakeElapsed += Time.unscaledDeltaTime;
                Vector2 offset = shake.ComputeOffset(shakeElapsed, shakeDuration, shakeMagnitude, random);
                currentOffset = new Vector3(offset.x, offset.y, 0f);
            }
            else
            {
                currentOffset = Vector3.zero;
            }
        }
    }
}
