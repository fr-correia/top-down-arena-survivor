using System.Collections.Generic;
using UnityEngine;

namespace ArenaSurvivor.Weapons
{
    public static class NearestTargetSelector
    {
        public static int FindNearestIndex(Vector2 origin, IReadOnlyList<Vector2> candidatePositions)
        {
            int nearestIndex = -1;
            float nearestSqrDistance = float.PositiveInfinity;

            for (int i = 0; i < candidatePositions.Count; i++)
            {
                float sqrDistance = (candidatePositions[i] - origin).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }
    }
}
