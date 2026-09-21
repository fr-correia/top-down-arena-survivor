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
        [SerializeField] private int baseDamage = 10;

        private Cooldown cooldown;
        private AutoAttack autoAttack;
        private int damageBonus;

        private void Awake()
        {
            cooldown = new Cooldown(attackInterval);
            autoAttack = new AutoAttack();
        }

        private void Update()
        {
            if (!cooldown.IsReady(Time.time))
            {
                return;
            }

            EnemyChaseComponent[] enemies = Object.FindObjectsByType<EnemyChaseComponent>(FindObjectsSortMode.None);
            var positions = new List<Vector2>(enemies.Length);
            foreach (EnemyChaseComponent enemy in enemies)
            {
                positions.Add(enemy.transform.position);
            }

            Vector2 origin = transform.position;
            if (!autoAttack.TryGetShotDirection(origin, positions, range, out Vector2 direction))
            {
                return;
            }

            cooldown.TryConsume(Time.time);

            if (projectilePrefab == null)
            {
                Debug.LogError("AutoAttackComponent: projectilePrefab is not assigned on " + name);
                return;
            }

            GameObject projectileInstance = Instantiate(projectilePrefab, origin, Quaternion.identity);
            projectileInstance.GetComponent<ProjectileComponent>().Launch(direction, baseDamage + damageBonus);
        }

        public void IncreaseDamage(int amount)
        {
            damageBonus += amount;
        }

        public void ReduceAttackInterval(float amount)
        {
            cooldown.SetInterval(cooldown.Interval - amount);
        }
    }
}
