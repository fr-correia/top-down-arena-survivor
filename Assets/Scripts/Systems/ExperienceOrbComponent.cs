using UnityEngine;

namespace ArenaSurvivor.Systems
{
    public class ExperienceOrbComponent : MonoBehaviour
    {
        [SerializeField] private int xpValue = 5;
        [SerializeField] private float magnetRadius = 3f;
        [SerializeField] private float magnetSpeed = 8f;

        private ExperienceOrb orb;
        private PlayerLevelingComponent playerLeveling;

        private void Awake()
        {
            orb = new ExperienceOrb(xpValue, magnetRadius);
            playerLeveling = Object.FindFirstObjectByType<PlayerLevelingComponent>();
        }

        private void Update()
        {
            if (playerLeveling == null)
            {
                return;
            }

            Vector2 nextPosition = orb.ComputeNextPosition(transform.position, playerLeveling.transform.position, magnetSpeed, Time.deltaTime);
            transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerLevelingComponent otherLeveling = other.GetComponent<PlayerLevelingComponent>();
            if (otherLeveling == null)
            {
                return;
            }

            otherLeveling.AddExperience(orb.XpValue);
            Destroy(gameObject);
        }
    }
}
