using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Terrain
{
    public class HexCell : MonoBehaviour
    {
        public HexCoordinates coordinates;
        public RectTransform uiRect;
        public HexGridChunk chunk;

        [SerializeField] private HexCell[] neighbors;
        [SerializeField] private bool[] roads;
        private int _elevation = int.MinValue;
        private bool _hasIncomingRiver;
        private bool _hasOutgoingRiver;
        private HexDirection _incomingRiver;
        private HexDirection _outgoingRiver;
        private int _specialIndex;
        private int _terrainTypeIndex;
        private int _urbanLevel, _farmLevel, _plantLevel;
        private bool _walled;
        private int _waterLevel;
        public Vector3 Position => transform.localPosition;
        public bool HasIncomingRiver => _hasIncomingRiver;
        public bool HasOutgoingRiver => _hasOutgoingRiver;
        public HexDirection IncomingRiver => _incomingRiver;
        public HexDirection OutgoingRiver => _outgoingRiver;
        public bool HasRiver => _hasIncomingRiver || _hasOutgoingRiver;
        public bool HasRiverBeginOrEnd => _hasIncomingRiver != _hasOutgoingRiver;

        public float StreamBedY => (_elevation + HexMetrics.StreamBedElevationOffset) * HexMetrics.ElevationStep;

        public HexDirection RiverBeginOrEndDirection => _hasIncomingRiver ? _incomingRiver : _outgoingRiver;

        public Color Color => HexMetrics.colors[_terrainTypeIndex];

        public int TerrainTypeIndex {
            get => _terrainTypeIndex;
            set {
                if (_terrainTypeIndex != value) {
                    _terrainTypeIndex = value;
                    Refresh();
                }
            }
        }

        public int Elevation {
            get => _elevation;
            set {
                if (_elevation == value) return;
                _elevation = value;

                RefreshPosition();

                ValidateRivers();

                for (var i = 0; i < roads.Length; i++) {
                    if (roads[i] && GetElevationDifference((HexDirection) i) > HexMetrics.MaxSlopeHeight) {
                        SetRoad(i, false);
                    }
                }

                Refresh();
            }
        }

        public float RiverSurfaceY => (_elevation + HexMetrics.WaterElevationOffset) * HexMetrics.ElevationStep;
        public bool HasRoads => roads.Any(road => road);

        public int WaterLevel {
            get => _waterLevel;
            set {
                if (_waterLevel == value) {
                    return;
                }

                _waterLevel = value;
                ValidateRivers();
                Refresh();
            }
        }

        public float WaterSurfaceY => (_waterLevel + HexMetrics.WaterElevationOffset) * HexMetrics.ElevationStep;
        public bool IsUnderwater => _waterLevel > _elevation;

        public int UrbanLevel {
            get => _urbanLevel;
            set {
                if (_urbanLevel == value) return;
                _urbanLevel = value;
                RefreshSelfOnly();
            }
        }

        public int FarmLevel {
            get => _farmLevel;
            set {
                if (_farmLevel == value) return;
                _farmLevel = value;
                RefreshSelfOnly();
            }
        }

        public int PlantLevel {
            get => _plantLevel;
            set {
                if (_plantLevel == value) return;
                _plantLevel = value;
                RefreshSelfOnly();
            }
        }

        public bool Walled {
            get => _walled;
            set {
                if (_walled != value) {
                    _walled = value;
                    Refresh();
                }
            }
        }

        public int SpecialIndex {
            get => _specialIndex;
            set {
                if (_specialIndex != value && !HasRiver) {
                    _specialIndex = value;
                    RemoveRoads();
                    RefreshSelfOnly();
                }
            }
        }

        public bool IsSpecial => _specialIndex > 0;

        public void SetNeighbor(HexDirection direction, HexCell cell) {
            neighbors[(int) direction] = cell;
            cell.neighbors[(int) direction.Opposite()] = this;
        }

        public HexCell GetNeighbor(HexDirection direction) {
            return neighbors[(int) direction];
        }

        public HexEdgeType GetEdgeType(HexDirection direction) {
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

        public bool HasRiverThroughEdge(HexDirection direction) {
            return _hasIncomingRiver && _incomingRiver == direction || HasOutgoingRiver && _outgoingRiver == direction;
        }

        public void RemoveOutgoingRiver() {
            if (!_hasOutgoingRiver) return;

            _hasOutgoingRiver = false;
            RefreshSelfOnly();

            var neighbor = GetNeighbor(_outgoingRiver);
            neighbor._hasIncomingRiver = false;
            neighbor.RefreshSelfOnly();
        }

        public void RemoveIncomingRiver() {
            if (!_hasIncomingRiver) return;

            _hasIncomingRiver = false;
            RefreshSelfOnly();

            var neighbor = GetNeighbor(IncomingRiver);
            neighbor._hasOutgoingRiver = false;
            neighbor.RefreshSelfOnly();
        }

        public void RemoveRiver() {
            RemoveIncomingRiver();
            RemoveOutgoingRiver();
        }

        private void RefreshSelfOnly() {
            chunk.Refresh();
        }

        public void SetOutgoingRiver(HexDirection direction) {
            if (_hasOutgoingRiver && _outgoingRiver == direction) return;

            var neighbor = GetNeighbor(direction);
            if (!IsValidRiverDestination(neighbor)) return;

            RemoveOutgoingRiver();
            if (_hasIncomingRiver && _incomingRiver == direction) {
                RemoveIncomingRiver();
            }

            _hasOutgoingRiver = true;
            _outgoingRiver = direction;
            _specialIndex = 0;


            neighbor.RemoveIncomingRiver();
            neighbor._hasIncomingRiver = true;
            neighbor._incomingRiver = direction.Opposite();
            neighbor.SpecialIndex = 0;


            SetRoad((int) direction, false);
        }

        public void AddRoad(HexDirection direction) {
            if (!roads[(int) direction]
                && !HasRiverThroughEdge(direction)
                && !IsSpecial
                && !GetNeighbor(direction).IsSpecial
                && GetElevationDifference(direction) <= HexMetrics.MaxSlopeHeight) {
                SetRoad((int) direction, true);
            }
        }

        public void RemoveRoads() {
            for (var i = 0; i < neighbors.Length; i++) {
                if (roads[i]) {
                    SetRoad(i, false);
                }
            }
        }

        public void SetRoad(int index, bool state) {
            roads[index] = state;
            neighbors[index].roads[(int) ((HexDirection) index).Opposite()] = state;
            neighbors[index].RefreshSelfOnly();
            RefreshSelfOnly();
        }

        public int GetElevationDifference(HexDirection direction) {
            var difference = _elevation - GetNeighbor(direction)._elevation;
            return Math.Abs(difference);
        }

        public bool HasRoadThroughEdge(HexDirection direction) {
            return roads[(int) direction];
        }

        private bool IsValidRiverDestination(HexCell neighbor) {
            return neighbor && (_elevation >= neighbor._elevation || _waterLevel == neighbor._elevation);
        }

        private void ValidateRivers() {
            if (_hasOutgoingRiver && !IsValidRiverDestination(GetNeighbor(_outgoingRiver))) {
                RemoveOutgoingRiver();
            }

            if (_hasIncomingRiver && !GetNeighbor(_incomingRiver).IsValidRiverDestination(this)) {
                RemoveIncomingRiver();
            }
        }

        public void Save(BinaryWriter writer) {
            writer.Write((byte) _terrainTypeIndex);
            writer.Write((byte) _elevation);
            writer.Write((byte) _waterLevel);
            writer.Write((byte) _urbanLevel);
            writer.Write((byte) _farmLevel);
            writer.Write((byte) _plantLevel);
            writer.Write((byte) _specialIndex);
            writer.Write(_walled);

            writer.Write((byte) (_hasIncomingRiver ? _incomingRiver + 128 : 0));
            writer.Write((byte) (_hasOutgoingRiver ? _outgoingRiver + 128 : 0));

            var roadFlags = 0;
            for (var i = 0; i < roads.Length; i++) {
                var road = roads[i];
                if (road) {
                    roadFlags |= 1 << i;
                }
            }

            writer.Write((byte) roadFlags);
        }

        public void Load(BinaryReader reader) {
            _terrainTypeIndex = reader.ReadByte();
            _elevation = reader.ReadByte();
            RefreshPosition();
            _waterLevel = reader.ReadByte();
            _urbanLevel = reader.ReadByte();
            _farmLevel = reader.ReadByte();
            _plantLevel = reader.ReadByte();
            _specialIndex = reader.ReadByte();
            _walled = reader.ReadBoolean();

            var riverData = reader.ReadByte();
            if (riverData >= 128) {
                _hasIncomingRiver = true;
                _incomingRiver = (HexDirection) (riverData - 128);
            }
            else {
                _hasIncomingRiver = false;
            }

            riverData = reader.ReadByte();
            if (riverData >= 128) {
                _hasOutgoingRiver = true;
                _outgoingRiver = (HexDirection) (riverData - 128);
            }
            else {
                _hasOutgoingRiver = false;
            }

            var roadFlags = reader.ReadByte();
            for (var i = 0; i < roads.Length; i++) {
                roads[i] = (roadFlags & (1 << i)) != 0;
            }
        }

        private void RefreshPosition() {
            var position = transform.localPosition;
            position.y = _elevation * HexMetrics.ElevationStep;
            position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.ElevationPerturbStrength;
            transform.localPosition = position;

            var uiPosition = uiRect.localPosition;
            uiPosition.z = -position.y;
            uiRect.localPosition = uiPosition;
        }
    }
}