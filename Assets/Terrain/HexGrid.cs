using System.Collections;
using System.IO;
using Lib;
using UnityEngine;
using UnityEngine.UI;

namespace Terrain
{
    public class HexGrid : MonoBehaviour
    {
        public int cellCountX = 40, cellCountZ = 30;
        public HexCell cellPrefab;
        public Text cellLabelPrefab;
        public HexGridChunk chunkPrefab;
        public Texture2D noiseSource;
        public int seed;

        private HexCell[] _cells;


        private int _chunkCountX;
        private int _chunkCountZ;
        private HexGridChunk[] _chunks;
        private HexCellPriorityQueue _searchFrontier;

        private void Awake() {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);

            CreateMap(cellCountX, cellCountZ);
        }

        private void OnEnable() {
            if (HexMetrics.noiseSource) return;
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
        }

        public bool CreateMap(int x, int z) {
            if (x <= 0 || x % HexMetrics.ChunkSizeX != 0 || z <= 0 || z % HexMetrics.ChunkSizeZ != 0) {
                Debug.LogError("Unsupported map size.");
                return false;
            }

            cellCountX = x;
            cellCountZ = z;
            if (_chunks != null) {
                foreach (var chunk in _chunks) {
                    Destroy(chunk.gameObject);
                }
            }

            _chunkCountX = cellCountX / HexMetrics.ChunkSizeX;
            _chunkCountZ = cellCountZ / HexMetrics.ChunkSizeZ;

            CreateChunks();
            CreateCells();
            return true;
        }

        private void CreateCell(int x, int z, int i) {
            Vector3 position;
            position.x = (x + z * .5f - z / 2) * (HexMetrics.InnerRadius * 2f);
            position.y = 0f;
            position.z = z * (HexMetrics.OuterRadius * 1.5f);

            var cell = _cells[i] = Instantiate(cellPrefab);
            cell.transform.localPosition = position;
            cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);

            if (x > 0) {
                cell.SetNeighbor(HexDirection.W, _cells[i - 1]);
            }

            if (z > 0) {
                if ((z & 1) == 0) {
                    cell.SetNeighbor(HexDirection.SE, _cells[i - cellCountX]);
                    if (x > 0) {
                        cell.SetNeighbor(HexDirection.SW, _cells[i - cellCountX - 1]);
                    }
                }
                else {
                    cell.SetNeighbor(HexDirection.SW, _cells[i - cellCountX]);
                    if (x < cellCountX - 1) {
                        cell.SetNeighbor(HexDirection.SE, _cells[i - cellCountX + 1]);
                    }
                }
            }

            var label = Instantiate(cellLabelPrefab);
            label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);

            cell.uiRect = label.rectTransform;
            cell.Elevation = 0;


            AddCellToChunk(x, z, cell);
        }


        public HexCell GetCell(Vector3 position) {
            position = transform.InverseTransformPoint(position);
            var coordinates = HexCoordinates.FromPosition(position);
            var index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
            return _cells[index];
        }

        private void CreateChunks() {
            _chunks = new HexGridChunk[_chunkCountX * _chunkCountZ];

            for (int z = 0, i = 0; z < _chunkCountZ; z++) {
                for (var x = 0; x < _chunkCountX; x++) {
                    var chunk = _chunks[i++] = Instantiate(chunkPrefab);
                    chunk.transform.SetParent(transform);
                }
            }
        }

        private void CreateCells() {
            _cells = new HexCell[cellCountZ * cellCountX];

            for (int z = 0, i = 0; z < cellCountZ; z++) {
                for (var x = 0; x < cellCountX; x++) {
                    CreateCell(x, z, i++);
                }
            }
        }

        private void AddCellToChunk(int x, int z, HexCell cell) {
            var chunkX = x / HexMetrics.ChunkSizeX;
            var chunkZ = z / HexMetrics.ChunkSizeZ;
            var chunk = _chunks[chunkX + chunkZ * _chunkCountX];

            var localX = x - chunkX * HexMetrics.ChunkSizeX;
            var localZ = z - chunkZ * HexMetrics.ChunkSizeZ;
            var pos = localX + localZ * HexMetrics.ChunkSizeX;
            chunk.AddCell(pos, cell);
        }

        public HexCell GetCell(HexCoordinates coordinates) {
            var z = coordinates.Z;
            if (z < 0 || z >= cellCountZ) {
                return null;
            }

            var x = coordinates.X + z / 2;
            if (x < 0 || x >= cellCountX) {
                return null;
            }

            return _cells[x + z * cellCountX];
        }

        public void ShowUI(bool visible) {
            foreach (var chunk in _chunks) {
                chunk.ShowUI(visible);
            }
        }

        public void Save(BinaryWriter writer) {
            writer.Write(cellCountX);
            writer.Write(cellCountZ);

            foreach (var cell in _cells) {
                cell.Save(writer);
            }
        }

        public void Load(BinaryReader reader, int header) {
            StopAllCoroutines();
            int x = 40, z = 30;
            if (header >= 1) {
                x = reader.ReadInt32();
                z = reader.ReadInt32();
            }

            if (x != cellCountX || z != cellCountZ) {
                if (!CreateMap(x, z)) return;
            }

            foreach (var cell in _cells) {
                cell.Load(reader);
            }

            foreach (var gridChunk in _chunks) {
                gridChunk.Refresh();
            }
        }

        public void FindPath(HexCell fromCell, HexCell toCell) {
            StopAllCoroutines();
            StartCoroutine(Search(fromCell, toCell));
        }

        private IEnumerator Search(HexCell fromCell, HexCell toCell) {
            if (_searchFrontier == null) {
                _searchFrontier = new HexCellPriorityQueue();
            }
            else {
                _searchFrontier.Clear();
            }

            foreach (var hexCell in _cells) {
                hexCell.Distance = int.MaxValue;
                hexCell.DisableHighlight();
            }

            fromCell.EnableHighlight(Color.blue);
            toCell.EnableHighlight(Color.red);

            var delay = new WaitForSeconds(1 / 120f);

            fromCell.Distance = 0;
            _searchFrontier.Enqueue(fromCell);
            while (_searchFrontier.Count > 0) {
                yield return delay;
                var current = _searchFrontier.Dequeue();

                if (current == toCell) {
                    current = current.PathFrom;
                    while (current != fromCell) {
                        current.EnableHighlight(Color.white);
                        current = current.PathFrom;
                    }

                    break;
                }

                for (var d = HexDirection.NE; d <= HexDirection.NW; d++) {
                    var neighbor = current.GetNeighbor(d);
                    if (neighbor == null) {
                        continue;
                    }

                    var edgeType = current.GetEdgeType(neighbor);

                    if (neighbor.IsUnderwater) {
                        continue;
                    }

                    if (edgeType == HexEdgeType.Cliff) {
                        continue;
                    }

                    var distance = current.Distance;
                    if (current.HasRoadThroughEdge(d)) {
                        distance += 1;
                    }
                    else if (current.Walled != neighbor.Walled) {
                        continue;
                    }
                    else {
                        distance += edgeType == HexEdgeType.Flat ? 5 : 10;
                        distance += neighbor.UrbanLevel + neighbor.FarmLevel + neighbor.PlantLevel;
                    }

                    if (neighbor.Distance == int.MaxValue) {
                        neighbor.Distance = distance;
                        neighbor.PathFrom = current;
                        neighbor.SearchHeuristic = neighbor.coordinates.DistanceTo(toCell.coordinates);
                        _searchFrontier.Enqueue(neighbor);
                    }
                    else if (distance < neighbor.Distance) {
                        var oldPriority = neighbor.SearchPriority;
                        neighbor.Distance = distance;
                        neighbor.PathFrom = current;
                        _searchFrontier.Change(neighbor, oldPriority);
                    }
                }
            }
        }
    }
}