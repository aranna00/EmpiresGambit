using UnityEngine;

namespace Terrain
{
    public class HexCell : MonoBehaviour
    {
        public HexCoordinates coordinates;
        public Color color;
        public RectTransform uiRect;

        public int Elevation
        {
            get => _elevation;
            set
            {
                _elevation = value;
                var position = transform.localPosition;
                position.y = value * HexMetrics.ElevationsStep;
                transform.localPosition = position;

                var uiPosition = uiRect.localPosition;
                uiPosition.z = _elevation * -HexMetrics.ElevationsStep;
                uiRect.localPosition = uiPosition;
            }
        }

        [SerializeField] private HexCell[] neighbors;
        private int _elevation;

        public void SetNeighbor(HexDirection direction, HexCell cell)
        {
            neighbors[(int) direction] = cell;
            cell.neighbors[(int)direction.Opposite()] = this;
        }
        public HexCell GetNeighbor (HexDirection direction) {
            return neighbors[(int)direction];
        }
    }
}
