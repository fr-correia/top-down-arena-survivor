using UnityEngine;

namespace ArenaSurvivor.Player
{
    public class CameraFollowComponent : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float followSpeed = 5f;

        private CameraFollow cameraFollow;
        private ScreenShakeComponent screenShake;
        private Vector3 basePosition;

        private void Awake()
        {
            cameraFollow = new CameraFollow();
            screenShake = GetComponent<ScreenShakeComponent>();
            basePosition = transform.position;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector2 next = cameraFollow.ComputeNextPosition(basePosition, target.position, followSpeed, Time.deltaTime);
            basePosition = new Vector3(next.x, next.y, basePosition.z);

            Vector3 finalPosition = basePosition;
            if (screenShake != null)
            {
                finalPosition += screenShake.GetCurrentOffset();
            }

            transform.position = finalPosition;
        }
    }
}
