using System;
using System.Linq;
using UnityEngine;

namespace Terrain
{
    public class HexCell : MonoBehaviour
    {
        public HexCoordinates coordinates;
        public RectTransform uiRect;
        public HexGridChunk chunk;
        public Vector3 Position => transform.localPosition;
        public bool HasIncomingRiver => _hasIncomingRiver;
        public bool HasOutgoingRiver => _hasOutgoingRiver;
        public HexDirection IncomingRiver => _incomingRiver;
        public HexDirection OutGoingRiver => _outGoingRiver;
        public bool HasRiver => _hasIncomingRiver || _hasOutgoingRiver;
        public bool HasRiverBeginOrEnd => _hasIncomingRiver != _hasOutgoingRiver;

        public float StreamBedY => (_elevation + HexMetrics.StreamBedElevationOffset) * HexMetrics.ElevationStep;

        public HexDirection RiverBeginOrEndDirection => _hasIncomingRiver ? _incomingRiver : _outGoingRiver;

        public Color Color {
            get => _color;
            set {
                if (_color == value) return;
                _color = value;
                Refresh();
            }
        }

        public int Elevation {
            get => _elevation;
            set {
                if (_elevation == value) return;

                _elevation = value;
                var position = transform.localPosition;
                position.y = value * HexMetrics.ElevationStep;
                position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.ElevationPerturbStrength;
                transform.localPosition = position;

                var uiPosition = uiRect.localPosition;
                uiPosition.z = -position.y;
                uiRect.localPosition = uiPosition;

                if (_hasOutgoingRiver && _elevation < GetNeighbor(_outGoingRiver)._elevation) {
                    RemoveOutGoingRiver();
                }

                if (_hasIncomingRiver && _elevation > GetNeighbor(_incomingRiver)._elevation) {
                    RemoveIncomingRiver();
                }

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
                Refresh();
            }
        }

        public float WaterSurfaceY => (_waterLevel + HexMetrics.WaterElevationOffset) * HexMetrics.ElevationStep;
        public bool IsUnderwater => _waterLevel > _elevation;

        [SerializeField] private HexCell[] neighbors;
        private int _elevation = int.MinValue;
        private Color _color;
        private bool _hasIncomingRiver;
        private bool _hasOutgoingRiver;
        private HexDirection _incomingRiver;
        private HexDirection _outGoingRiver;
        [SerializeField] private bool[] roads;
        private int _waterLevel;

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

        public bool HasRiverThroughEdge(HexDirection direction) {
            return _hasIncomingRiver && _incomingRiver == direction ||
                   HasOutgoingRiver && _outGoingRiver == direction;
        }

        public void RemoveOutGoingRiver() {
            if (!_hasOutgoingRiver) return;

            _hasOutgoingRiver = false;
            RefreshSelfOnly();

            var neighbor = GetNeighbor(_outGoingRiver);
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
            RemoveOutGoingRiver();
        }

        private void RefreshSelfOnly() {
            chunk.Refresh();
        }

        public void SetOutgoingRiver(HexDirection direction) {
            if (_hasOutgoingRiver && _outGoingRiver == direction) return;

            var neighbor = GetNeighbor(direction);
            if (!neighbor || _elevation < neighbor._elevation) return;

            RemoveOutGoingRiver();
            if (_hasIncomingRiver && _incomingRiver == direction) {
                RemoveIncomingRiver();
            }

            _hasOutgoingRiver = true;
            _outGoingRiver = direction;


            neighbor.RemoveIncomingRiver();
            neighbor._hasIncomingRiver = true;
            neighbor._incomingRiver = direction.Opposite();


            SetRoad((int) direction, false);
        }

        public void AddRoad(HexDirection direction) {
            if (!roads[(int) direction] && !HasRiverThroughEdge(direction) &&
                GetElevationDifference(direction) <= HexMetrics.MaxSlopeHeight) {
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
    }
}