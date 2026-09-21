using System.Collections.Generic;
using UnityEngine;
using ArenaSurvivor.Enemies;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Weapons
{
    public class AutoAttackComponent : MonoBehaviour
    {
        [SerializeField] private float attackInterval = 1f;
        [SerializeField] private float range = 8f;
        [SerializeField] private GameObject projectilePrefab;

        private Cooldown cooldown;

        private void Awake()
        {
            cooldown = new Cooldown(attackInterval);
        }

        private void Update()
        {
            if (!cooldown.TryConsume(Time.time))
            {
                return;
            }

            EnemyChaseComponent[] enemies = Object.FindObjectsByType<EnemyChaseComponent>(FindObjectsSortMode.None);
            if (enemies.Length == 0)
            {
                return;
            }

            var positions = new List<Vector2>(enemies.Length);
            foreach (EnemyChaseComponent enemy in enemies)
            {
                positions.Add(enemy.transform.position);
            }

            Vector2 origin = transform.position;
            int nearestIndex = NearestTargetSelector.FindNearestIndex(origin, positions);
            Vector2 targetPosition = positions[nearestIndex];
            if (Vector2.Distance(origin, targetPosition) > range)
            {
                return;
            }

            Vector2 direction = (targetPosition - origin).normalized;
            GameObject projectileInstance = Instantiate(projectilePrefab, origin, Quaternion.identity);
            projectileInstance.GetComponent<ProjectileComponent>().Launch(direction);
        }
    }
}
