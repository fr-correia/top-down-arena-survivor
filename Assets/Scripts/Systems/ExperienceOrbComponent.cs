using UnityEngine;

namespace ArenaSurvivor.Systems
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class ExperienceOrbComponent : MonoBehaviour
    {
        [SerializeField] private int xpValue = 5;
        [SerializeField] private float magnetRadius = 3f;
        [SerializeField] private float magnetSpeed = 8f;

        private Rigidbody2D rb;
        private ExperienceOrb orb;
        private PlayerLevelingComponent playerLeveling;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            orb = new ExperienceOrb(xpValue, magnetRadius);
            playerLeveling = Object.FindFirstObjectByType<PlayerLevelingComponent>();
        }

        private void FixedUpdate()
        {
            if (playerLeveling == null)
            {
                return;
            }

            Vector2 nextPosition = orb.ComputeNextPosition(rb.position, playerLeveling.transform.position, magnetSpeed, Time.fixedDeltaTime);
            rb.MovePosition(nextPosition);
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
