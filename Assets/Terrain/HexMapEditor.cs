using UnityEngine;
using UnityEngine.EventSystems;

namespace Terrain
{
    public class HexMapEditor : MonoBehaviour
    {
        public Color[] colors;
        public HexGrid hexGrid;

        private Color _activeColor;
        private int _activeElevation;
        private bool _applyColor;
        private bool _applyElevation = true;
        private int _brushSize;

        private void Awake() {
            SelectColor(0);
        }

        private void Update() {
            if (Input.GetMouseButton(0) &&
                !EventSystem.current.IsPointerOverGameObject()) {
                HandleInput();
            }
        }

        private void HandleInput() {
            var inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(inputRay, out hit)) {
                EditCells(hexGrid.GetCell(hit.point));
            }
        }

        private void EditCell(HexCell cell) {
            if (!cell) return;

            if (_applyColor) {
                cell.Color = _activeColor;
            }

            if (_applyElevation) {
                cell.Elevation = _activeElevation;
            }
        }

        public void SelectColor(int index) {
            _applyColor = index >= 0;
            if (_applyColor) {
                _activeColor = colors[index];
            }
        }

        public void SetElevation(float elevation) {
            _activeElevation = (int) elevation;
        }

        public void SetApplyElevation(bool toggle) {
            _applyElevation = toggle;
        }

        public void SetBrushSize(float size) {
            _brushSize = (int) size;
        }

        private void EditCells(HexCell center) {
            var centerX = center.coordinates.X;
            var centerZ = center.coordinates.Z;

            for (int r = 0, z = centerZ - _brushSize; z <= centerZ; z++, r++) {
                for (int x = centerX - r; x <= centerX + _brushSize; x++) {
                    EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
                }
            }

            for (int r = 0, z = centerZ + _brushSize; z > centerZ; z--, r++) {
                for (int x = centerX - _brushSize; x <= centerX + r; x++) {
                    EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
                }
            }
        }

        public void ShowUI(bool visible) {
            hexGrid.ShowUI(visible);
        }
    }
}