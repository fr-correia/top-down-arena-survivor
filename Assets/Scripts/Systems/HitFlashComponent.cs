using UnityEngine;

namespace ArenaSurvivor.Systems
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(HealthComponent))]
    public class HitFlashComponent : MonoBehaviour
    {
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float flashDuration = 0.1f;

        private SpriteRenderer spriteRenderer;
        private HealthComponent healthComponent;
        private Color originalColor;
        private float flashTimeRemaining;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            healthComponent = GetComponent<HealthComponent>();
            originalColor = spriteRenderer.color;
        }

        private void OnEnable()
        {
            flashTimeRemaining = 0f;
            spriteRenderer.color = originalColor;
            healthComponent.OnDamaged += HandleDamaged;
        }

        private void OnDisable()
        {
            healthComponent.OnDamaged -= HandleDamaged;
        }

        private void HandleDamaged(int amount)
        {
            flashTimeRemaining = flashDuration;
            spriteRenderer.color = flashColor;
        }

        private void Update()
        {
            if (flashTimeRemaining <= 0f)
            {
                return;
            }

            flashTimeRemaining -= Time.deltaTime;
            if (flashTimeRemaining <= 0f)
            {
                spriteRenderer.color = originalColor;
            }
        }
    }
}
