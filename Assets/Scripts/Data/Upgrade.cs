using UnityEngine;

namespace ArenaSurvivor.Data
{
    public abstract class Upgrade : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField] [TextArea] private string description;

        public string DisplayName => displayName;
        public string Description => description;

        public abstract void Apply(GameObject player);
    }
}
