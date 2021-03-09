using UnityEngine;
using UnityEngine.UI;

namespace Terrain
{
    public class HexGrid : MonoBehaviour
    {
        public int chunkCountX = 4, chunkCountZ = 3;
        public Color defaultColor = Color.white;
        public HexCell cellPrefab;
        public Text cellLabelPrefab;
        public HexGridChunk chunkPrefab;
        public Texture2D noiseSource;

        private HexCell[] _cells;
        private int _cellCountX;
        private int _cellCountZ;
        private HexGridChunk[] _chunks;

        private void Awake() {
            HexMetrics.NoiseSource = noiseSource;

            _cellCountX = chunkCountX * HexMetrics.ChunkSizeX;
            _cellCountZ = chunkCountZ * HexMetrics.ChunkSizeZ;

            CreateChunks();
            CreateCells();
        }

        private void OnEnable() {
            HexMetrics.NoiseSource = noiseSource;
        }

        private void CreateCell(int x, int z, int i) {
            Vector3 position;
            position.x = (x + z * .5f - z / 2) * (HexMetrics.InnerRadius * 2f);
            position.y = 0f;
            position.z = z * (HexMetrics.OuterRadius * 1.5f);

            var cell = _cells[i] = Instantiate(cellPrefab);
            cell.transform.localPosition = position;
            cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
            cell.Color = defaultColor;

            if (x > 0) {
                cell.SetNeighbor(HexDirection.W, _cells[i - 1]);
            }

            if (z > 0) {
                if ((z & 1) == 0) {
                    cell.SetNeighbor(HexDirection.SE, _cells[i - _cellCountX]);
                    if (x > 0) {
                        cell.SetNeighbor(HexDirection.SW, _cells[i - _cellCountX - 1]);
                    }
                }
                else {
                    cell.SetNeighbor(HexDirection.SW, _cells[i - _cellCountX]);
                    if (x < _cellCountX - 1) {
                        cell.SetNeighbor(HexDirection.SE, _cells[i - _cellCountX + 1]);
                    }
                }
            }

            var label = Instantiate(cellLabelPrefab);
            label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
            label.text = cell.coordinates.ToStringOnSeparateLines();

            cell.uiRect = label.rectTransform;
            cell.Elevation = 0;

            AddCellToChunk(x, z, cell);
        }


        public HexCell GetCell(Vector3 position) {
            position = transform.InverseTransformPoint(position);
            var coordinates = HexCoordinates.FromPosition(position);
            var index = coordinates.X + coordinates.Z * _cellCountX + coordinates.Z / 2;
            return _cells[index];
        }

        private void CreateChunks() {
            _chunks = new HexGridChunk[chunkCountX * chunkCountZ];

            for (int z = 0, i = 0; z < chunkCountZ; z++) {
                for (var x = 0; x < chunkCountX; x++) {
                    var chunk = _chunks[i++] = Instantiate(chunkPrefab);
                    chunk.transform.SetParent(transform);
                }
            }
        }

        private void CreateCells() {
            _cells = new HexCell[_cellCountZ * _cellCountX];

            for (int z = 0, i = 0; z < _cellCountZ; z++) {
                for (var x = 0; x < _cellCountX; x++) {
                    CreateCell(x, z, i++);
                }
            }
        }

        private void AddCellToChunk(int x, int z, HexCell cell) {
            var chunkX = x / HexMetrics.ChunkSizeX;
            var chunkZ = z / HexMetrics.ChunkSizeZ;
            var chunk = _chunks[chunkX + chunkZ * chunkCountX];

            var localX = x - chunkX * HexMetrics.ChunkSizeX;
            var localZ = z - chunkZ * HexMetrics.ChunkSizeZ;
            chunk.AddCell(localX + localZ * HexMetrics.ChunkSizeX, cell);
        }

        public HexCell GetCell(HexCoordinates coordinates) {
            var z = coordinates.Z;
            if (z < 0 || z >= _cellCountZ) {
                return null;
            }
            var x = coordinates.X + z / 2;
            if (x < 0 || x >= _cellCountX) {
                return null;
            }
            return _cells[x + z * _cellCountX];
        }

        public void ShowUI(bool visible) {
            foreach (var chunk in _chunks) {
                chunk.ShowUI(visible);
            }
        }
    }
}