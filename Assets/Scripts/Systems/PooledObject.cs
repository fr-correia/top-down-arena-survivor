using System;
using UnityEngine;

namespace ArenaSurvivor.Systems
{
    public class PooledObject : MonoBehaviour
    {
        private Action<GameObject> releaseAction;

        public void Initialize(Action<GameObject> releaseAction)
        {
            this.releaseAction = releaseAction;
        }

        public void ReturnToPool()
        {
            releaseAction?.Invoke(gameObject);
        }
    }
}
