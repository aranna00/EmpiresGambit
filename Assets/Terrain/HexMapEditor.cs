using UnityEngine;
using UnityEngine.EventSystems;

namespace Terrain
{
    public class HexMapEditor : MonoBehaviour
    {
        public HexGrid hexGrid;
        public Material terrainMaterial;

        private int _activeElevation,
            _activeWaterLevel,
            _activeTerrainTypeIndex,
            _activeUrbanLevel,
            _activeFarmLevel,
            _activePlantLevel,
            _activeSpecialIndex;

        private bool _applyElevation,
            _applyUrbanLevel,
            _applyFarmLevel,
            _applyPlantLevel,
            _applySpecialIndex,
            _applyWaterLevel;

        private int _brushSize;
        private HexDirection _dragDirection;
        private bool _editMode;
        private bool _isDrag;
        private HexCell _previousCell, _searchFromCell, _searchToCell;
        private OptionalToggle _riverMode, _roadMode, _walledMode;

        private void Awake() {
            terrainMaterial.DisableKeyword("GRID_ON");
        }


        private void Update() {
            if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject()) {
                HandleInput();
            }
            else {
                _previousCell = null;
            }
        }

        private void HandleInput() {
            var inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(inputRay, out var hit)) {
                var currentCell = hexGrid.GetCell(hit.point);
                if (_previousCell && _previousCell != currentCell) {
                    ValidateDrag(currentCell);
                }
                else {
                    _isDrag = false;
                }

                if (_editMode) {
                    EditCells(currentCell);
                }
                else if (Input.GetKey(KeyCode.LeftShift) && _searchToCell != currentCell) {
                    if (_searchFromCell) {
                        _searchFromCell.DisableHighlight();
                    }

                    _searchFromCell = currentCell;
                    _searchFromCell.EnableHighlight(Color.blue);
                    if (_searchToCell) {
                        hexGrid.FindPath(_searchFromCell, _searchToCell);
                    }
                }
                else if (_searchFromCell && _searchFromCell != currentCell) {
                    _searchToCell = currentCell;
                    hexGrid.FindPath(_searchFromCell, _searchToCell);
                }

                _previousCell = currentCell;
            }
            else {
                _previousCell = null;
            }
        }

        private void ValidateDrag(HexCell currentCell) {
            for (_dragDirection = HexDirection.NE; _dragDirection <= HexDirection.NW; _dragDirection++) {
                if (_previousCell.GetNeighbor(_dragDirection) == currentCell) {
                    _isDrag = true;
                    return;
                }
            }

            _isDrag = false;
        }

        private void EditCell(HexCell cell) {
            if (!cell) return;
            if (_activeTerrainTypeIndex >= 0) {
                cell.TerrainTypeIndex = _activeTerrainTypeIndex;
            }

            if (_applyElevation) {
                cell.Elevation = _activeElevation;
            }

            if (_applyWaterLevel) {
                cell.WaterLevel = _activeWaterLevel;
            }

            if (_applySpecialIndex) {
                cell.SpecialIndex = _activeSpecialIndex;
            }

            if (_applyUrbanLevel) {
                cell.UrbanLevel = _activeUrbanLevel;
            }

            if (_applyPlantLevel) {
                cell.PlantLevel = _activePlantLevel;
            }

            if (_applyFarmLevel) {
                cell.FarmLevel = _activeFarmLevel;
            }

            if (_riverMode == OptionalToggle.No) {
                cell.RemoveRiver();
            }

            if (_roadMode == OptionalToggle.No) {
                cell.RemoveRoads();
            }

            if (_walledMode != OptionalToggle.Ignore) {
                cell.Walled = _walledMode == OptionalToggle.Yes;
            }

            else if (_isDrag) {
                var otherCell = cell.GetNeighbor(_dragDirection.Opposite());
                if (!otherCell) return;

                if (_riverMode == OptionalToggle.Yes) {
                    otherCell.SetOutgoingRiver(_dragDirection);
                }

                if (_roadMode == OptionalToggle.Yes) {
                    otherCell.AddRoad(_dragDirection);
                }
            }
        }

        public void SetElevation(float elevation) {
            _activeElevation = (int)elevation;
        }

        public void SetApplyElevation(bool toggle) {
            _applyElevation = toggle;
        }

        public void SetBrushSize(float size) {
            _brushSize = (int)size;
        }

        private void EditCells(HexCell center) {
            var centerX = center.coordinates.X;
            var centerZ = center.coordinates.Z;

            for (int r = 0, z = centerZ - _brushSize; z <= centerZ; z++, r++) {
                for (var x = centerX - r; x <= centerX + _brushSize; x++) {
                    EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
                }
            }

            for (int r = 0, z = centerZ + _brushSize; z > centerZ; z--, r++) {
                for (var x = centerX - _brushSize; x <= centerX + r; x++) {
                    EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
                }
            }
        }

        public void SetRiverMode(int mode) {
            _riverMode = (OptionalToggle)mode;
        }

        public void setRoadMode(int mode) {
            _roadMode = (OptionalToggle)mode;
        }

        public void SetApplyWaterLevel(bool toggle) {
            _applyWaterLevel = toggle;
        }

        public void SetWaterLevel(float level) {
            _activeWaterLevel = (int)level;
        }

        public void SetApplyUrbanLevel(bool toggle) {
            _applyUrbanLevel = toggle;
        }

        public void SetUrbanLevel(float level) {
            _activeUrbanLevel = (int)level;
        }


        public void SetApplyFarmLevel(bool toggle) {
            _applyFarmLevel = toggle;
        }

        public void SetFarmLevel(float level) {
            _activeFarmLevel = (int)level;
        }

        public void SetApplyPlantLevel(bool toggle) {
            _applyPlantLevel = toggle;
        }

        public void SetPlantLevel(float level) {
            _activePlantLevel = (int)level;
        }

        public void SetWalledMode(int mode) {
            _walledMode = (OptionalToggle)mode;
        }

        public void SetApplySpecialIndex(bool toggle) {
            _applySpecialIndex = toggle;
        }

        public void SetSpecialIndex(float index) {
            _activeSpecialIndex = (int)index;
        }

        public void SetTerrainTypeIndex(int index) {
            _activeTerrainTypeIndex = index;
        }

        public void ShowGrid(bool visible) {
            if (visible) {
                terrainMaterial.EnableKeyword("GRID_ON");
            }
            else {
                terrainMaterial.DisableKeyword("GRID_ON");
            }
        }

        public void SetEditMode(bool toggle) {
            _editMode = toggle;
            hexGrid.ShowUI(!toggle);
        }
    }

    internal enum OptionalToggle
    {
        Ignore,
        Yes,
        No
    }
}