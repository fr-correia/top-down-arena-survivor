using System.Collections.Generic;
using UnityEngine;

namespace ArenaSurvivor.Weapons
{
    public class AutoAttack
    {
        public bool TryGetShotDirection(Vector2 origin, IReadOnlyList<Vector2> targetPositions, float range, out Vector2 direction)
        {
            direction = Vector2.zero;

            int nearestIndex = NearestTargetSelector.FindNearestIndex(origin, targetPositions);
            if (nearestIndex < 0)
            {
                return false;
            }

            Vector2 targetPosition = targetPositions[nearestIndex];
            if (Vector2.Distance(origin, targetPosition) > range)
            {
                return false;
            }

            direction = (targetPosition - origin).normalized;
            return true;
        }
    }
}
