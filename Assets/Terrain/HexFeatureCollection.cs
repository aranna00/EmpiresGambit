using UnityEngine;

namespace Terrain
{
    [System.Serializable]
    public struct HexFeatureCollection
    {
        public Transform[] Prefabs;

        public Transform Pick(float choice) {
            return Prefabs[(int) (choice * Prefabs.Length)];
        }
    }
}