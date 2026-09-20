using UnityEngine;

namespace ArenaSurvivor.Player
{
    public class CameraFollowComponent : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float followSpeed = 5f;

        private CameraFollow cameraFollow;

        private void Awake()
        {
            cameraFollow = new CameraFollow();
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

            Vector2 next = cameraFollow.ComputeNextPosition(transform.position, target.position, followSpeed, Time.deltaTime);
            transform.position = new Vector3(next.x, next.y, transform.position.z);
        }
    }
}
