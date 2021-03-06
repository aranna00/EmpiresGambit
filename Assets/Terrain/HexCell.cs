using UnityEngine;

namespace Terrain
{
    public class HexCell : MonoBehaviour
    {
        public HexCoordinates coordinates;

        public Color Color {
            get => _color;
            set {
                if(_color == value) return;
                _color = value;
                Refresh();
            }
        }

        public RectTransform uiRect;
        public HexGridChunk chunk;

        public Vector3 Position => transform.localPosition;

        public int Elevation {
            get => _elevation;
            set {
                if (_elevation == value) return;
                
                _elevation = value;
                var position = transform.localPosition;
                position.y = value * HexMetrics.ElevationsStep;
                position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.ElevationPerturbStrength;
                transform.localPosition = position;

                var uiPosition = uiRect.localPosition;
                uiPosition.z = -position.y;
                uiRect.localPosition = uiPosition;
                Refresh();
            }
        }

        [SerializeField] private HexCell[] neighbors;
        private int _elevation = int.MinValue;
        private Color _color;

        public void SetNeighbor(HexDirection direction, HexCell cell) {
            neighbors[(int) direction] = cell;
            cell.neighbors[(int) direction.Opposite()] = this;
        }

        public HexCell GetNeighbor(HexDirection direction) {
            return neighbors[(int) direction];
        }

        public HexEdgeType GETEdgeType(HexDirection direction) {
            return HexMetrics.GetEdgeType(_elevation, neighbors[(int) direction].Elevation);
        }

        public HexEdgeType GetEdgeType(HexCell otherCell) {
            return HexMetrics.GetEdgeType(_elevation, otherCell.Elevation);
        }

        private void Refresh() {
            if (!chunk) return;
            
            chunk.Refresh();
            foreach (var neighbor in neighbors) {
                if (neighbor != null && neighbor.chunk != chunk) {
                    neighbor.chunk.Refresh();
                }
            }
        }
    }
}