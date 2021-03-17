using UnityEngine;

namespace Terrain
{
    public class HexFeatureManager : MonoBehaviour
    {
        public HexFeatureCollection[] urbanCollections;

        private Transform _container;

        public void Clear() {
            if (_container) {
                Destroy(_container.gameObject);
            }

            _container = new GameObject("Features Container").transform;
            _container.SetParent(transform, this);
        }

        public void Apply() {
        }

        public void AddFeature(HexCell cell, Vector3 position) {
            var hash = HexMetrics.SampleHashGrid(position);
            var prefab = PickPrefab(cell.UrbanLevel, hash.a, hash.b);
            if (!prefab) {
                return;
            }

            var instance = Instantiate(prefab, _container, false);
            position.y += instance.localScale.y * 0.5f;
            instance.localPosition = HexMetrics.Perturb(position);
            instance.localRotation = Quaternion.Euler(0f, 360f * hash.c, 0f);
        }

        Transform PickPrefab(int level, float hash, float choice) {
            if (level > 0) {
                var thresholds = HexMetrics.GetFeatureThresholds(level - 1);
                for (var i = 0; i < thresholds.Length; i++) {
                    if (hash < thresholds[i]) {
                        return urbanCollections[i].Pick(choice);
                    }
                }
            }

            return null;
        }
    }
}