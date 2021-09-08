using System;
using System.Collections.Generic;
using UnityEngine;

namespace Terrain
{
    public class HexGridChunk : MonoBehaviour
    {
        private static Color _color1 = new Color(1f, 0f, 0f);
        private static Color _color2 = new Color(0f, 1f, 0f);
        private static Color _color3 = new Color(0f, 0f, 1f);
        public HexMesh terrain, rivers, roads, water, waterShores, estuaries;
        public HexFeatureManager features;

        private HexCell[] _cells;
        private Canvas _gridCanvas;

        private void Awake() {
            _gridCanvas = GetComponentInChildren<Canvas>();

            _cells = new HexCell[HexMetrics.ChunkSizeX * HexMetrics.ChunkSizeZ];
        }

        private void LateUpdate() {
            Triangulate(_cells);
            enabled = false;
        }

        public void AddCell(int index, HexCell cell) {
            _cells[index] = cell;
            cell.chunk = this;
            cell.transform.SetParent(transform, false);
            cell.uiRect.SetParent(_gridCanvas.transform, false);
        }

        public void Refresh() {
            enabled = true;
        }

        public void ShowUI(bool visible) {
            _gridCanvas.gameObject.SetActive(visible);
        }

        public void Triangulate(IEnumerable<HexCell> cells) {
            terrain.Clear();
            rivers.Clear();
            roads.Clear();
            water.Clear();
            waterShores.Clear();
            estuaries.Clear();
            features.Clear();

            foreach (var cell in cells) {
                Triangulate(cell);
            }

            terrain.Apply();
            rivers.Apply();
            roads.Apply();
            water.Apply();
            waterShores.Apply();
            estuaries.Apply();
            features.Apply();
        }

        private void Triangulate(HexCell cell) {
            foreach (var direction in (HexDirection[])Enum.GetValues(typeof(HexDirection))) {
                Triangulate(direction, cell);
            }

            if (!cell.IsUnderwater) {
                if (!cell.HasRiver && !cell.HasRoads) {
                    features.AddFeature(cell, cell.Position);
                }

                if (cell.IsSpecial) {
                    features.AddSpecialFeature(cell, cell.Position);
                }
            }
        }

        private void Triangulate(HexDirection direction, HexCell cell) {
            var center = cell.Position;
            var e = new EdgeVertices(
                center + HexMetrics.GetFirstSolidCorner(direction),
                center + HexMetrics.GetSecondSolidCorner(direction)
            );

            if (cell.HasRiver) {
                if (cell.HasRiverThroughEdge(direction)) {
                    e.v3.y = cell.StreamBedY;
                    if (cell.HasRiverBeginOrEnd) {
                        TriangulateWithRiverBeginOrEnd(direction, cell, center, e);
                    }
                    else {
                        TriangulateWithRiver(direction, cell, center, e);
                    }
                }
                else {
                    TriangulateAdjectentToRiver(direction, cell, center, e);
                }
            }
            else {
                TriangulateWithoutRiver(direction, cell, center, e);

                if (!cell.IsUnderwater && !cell.HasRoadThroughEdge(direction)) {
                    features.AddFeature(cell, (center + e.v1 + e.v5) * (1f / 3f));
                }
            }

            if (direction <= HexDirection.SE) {
                TriangulateConnection(direction, cell, e);
            }

            if (cell.IsUnderwater) {
                TriangulateWater(direction, cell, center);
            }
        }

        private void TriangulateWithoutRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e) {
            TriangulateEdgeFan(center, e, cell.TerrainTypeIndex);

            if (cell.HasRoads) {
                var interpolators = GetRoadInterpolators(direction, cell);
                TriangulateRoad(
                    center,
                    Vector3.Lerp(center, e.v1, interpolators.x),
                    Vector3.Lerp(center, e.v5, interpolators.y),
                    e,
                    cell.HasRoadThroughEdge(direction)
                );
            }
        }

        private void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices e1) {
            var neighbor = cell.GetNeighbor(direction);
            if (neighbor == null) return;

            var bridge = HexMetrics.GetBridge(direction);
            bridge.y = neighbor.Position.y - cell.Position.y;
            var e2 = new EdgeVertices(e1.v1 + bridge, e1.v5 + bridge);

            var hasRiver = cell.HasRiverThroughEdge(direction);
            var hasRoad = cell.HasRoadThroughEdge(direction);

            if (hasRiver) {
                e2.v3.y = neighbor.StreamBedY;

                if (!cell.IsUnderwater) {
                    if (!neighbor.IsUnderwater) {
                        TriangulateRiverQuad(
                            e1.v2,
                            e1.v4,
                            e2.v2,
                            e2.v4,
                            cell.RiverSurfaceY,
                            neighbor.RiverSurfaceY,
                            .8f,
                            cell.HasIncomingRiver && cell.IncomingRiver == direction
                        );
                    }

                    else if (cell.Elevation > neighbor.WaterLevel) {
                        TriangulateWaterfallInWater(
                            e1.v2,
                            e1.v4,
                            e2.v4,
                            e2.v4,
                            cell.RiverSurfaceY,
                            neighbor.RiverSurfaceY,
                            neighbor.WaterSurfaceY
                        );
                    }
                }
                else if (!neighbor.IsUnderwater && neighbor.Elevation > cell.WaterLevel) {
                    TriangulateWaterfallInWater(
                        e2.v4,
                        e2.v2,
                        e1.v4,
                        e1.v2,
                        neighbor.RiverSurfaceY,
                        cell.RiverSurfaceY,
                        cell.WaterSurfaceY
                    );
                }
            }

            if (cell.GetEdgeType(direction) == HexEdgeType.Slope) {
                TriangulateEdgeTerraces(e1, cell, e2, neighbor, hasRoad);
            }
            else {
                TriangulateEdgeStrip(
                    e1,
                    _color1,
                    cell.TerrainTypeIndex,
                    e2,
                    _color2,
                    neighbor.TerrainTypeIndex,
                    hasRoad
                );
            }

            features.AddWall(e1, cell, e2, neighbor, hasRiver, hasRoad);

            var nextNeighbor = cell.GetNeighbor(direction.Next());
            if (direction <= HexDirection.E && nextNeighbor != null) {
                var v5 = e1.v5 + HexMetrics.GetBridge(direction.Next());
                v5.y = nextNeighbor.Position.y;

                if (cell.Elevation <= neighbor.Elevation) {
                    if (cell.Elevation <= nextNeighbor.Elevation) {
                        TriangulateCorner(e1.v5, cell, e2.v5, neighbor, v5, nextNeighbor);
                    }
                    else {
                        TriangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
                    }
                }
                else if (neighbor.Elevation <= nextNeighbor.Elevation) {
                    TriangulateCorner(e2.v5, neighbor, v5, nextNeighbor, e1.v5, cell);
                }
                else {
                    TriangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
                }
            }
        }

        private void TriangulateCorner(
            Vector3 bottom,
            HexCell bottomCell,
            Vector3 left,
            HexCell leftCell,
            Vector3 right,
            HexCell rightCell
        ) {
            var leftEdgeType = bottomCell.GetEdgeType(leftCell);
            var rightEdgeType = bottomCell.GetEdgeType(rightCell);

            if (leftEdgeType == HexEdgeType.Slope) {
                if (rightEdgeType == HexEdgeType.Slope) {
                    TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
                }
                else if (rightEdgeType == HexEdgeType.Flat) {
                    TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
                }
                else {
                    TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell);
                }
            }
            else if (rightEdgeType == HexEdgeType.Slope) {
                if (leftEdgeType == HexEdgeType.Flat) {
                    TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
                }
                else {
                    TriangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
                }
            }
            else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
                if (leftCell.Elevation < rightCell.Elevation) {
                    TriangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
                }
                else {
                    TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell);
                }
            }
            else {
                terrain.AddTriangle(bottom, left, right);
                terrain.AddTriangleColor(_color1, _color2, _color3);
                Vector3 types;
                types.x = bottomCell.TerrainTypeIndex;
                types.y = leftCell.TerrainTypeIndex;
                types.z = rightCell.TerrainTypeIndex;
                terrain.AddTriangleTerrainTypes(types);
            }

            features.AddWall(bottom, bottomCell, left, leftCell, right, rightCell);
        }

        private void TriangulateCornerTerracesCliff(
            Vector3 begin,
            HexCell beginCell,
            Vector3 left,
            HexCell leftCell,
            Vector3 right,
            HexCell rightCell
        ) {
            var b = Math.Abs(1f / (rightCell.Elevation - beginCell.Elevation));
            var boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);
            var boundaryColor = Color.Lerp(_color1, _color2, b);
            Vector3 types;
            types.x = beginCell.TerrainTypeIndex;
            types.y = leftCell.TerrainTypeIndex;
            types.z = rightCell.TerrainTypeIndex;

            TriangulateBoundaryTriangle(begin, _color1, left, _color2, boundary, boundaryColor, types);

            if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
                TriangulateBoundaryTriangle(left, _color2, right, _color3, boundary, boundaryColor, types);
            }
            else {
                terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
                terrain.AddTriangleColor(_color2, _color3, boundaryColor);
                terrain.AddTriangleTerrainTypes(types);
            }
        }

        private void TriangulateCornerCliffTerraces(
            Vector3 begin,
            HexCell beginCell,
            Vector3 left,
            HexCell leftCell,
            Vector3 right,
            HexCell rightCell
        ) {
            var b = Math.Abs(1f / (leftCell.Elevation - beginCell.Elevation));
            var boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);
            var boundaryColor = Color.Lerp(_color1, _color2, b);
            Vector3 types;
            types.x = beginCell.TerrainTypeIndex;
            types.y = leftCell.TerrainTypeIndex;
            types.z = rightCell.TerrainTypeIndex;

            TriangulateBoundaryTriangle(right, _color3, begin, _color1, boundary, boundaryColor, types);

            if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
                TriangulateBoundaryTriangle(left, _color2, right, _color3, boundary, boundaryColor, types);
            }
            else {
                terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
                terrain.AddTriangleColor(_color2, _color3, boundaryColor);
                terrain.AddTriangleTerrainTypes(types);
            }
        }

        private void TriangulateBoundaryTriangle(
            Vector3 begin,
            Color beginColor,
            Vector3 left,
            Color leftColor,
            Vector3 boundary,
            Color boundaryColor,
            Vector3 types
        ) {
            var v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
            var c2 = HexMetrics.TerraceLerp(beginColor, leftColor, 1);

            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
            terrain.AddTriangleColor(beginColor, c2, boundaryColor);
            terrain.AddTriangleTerrainTypes(types);


            for (var i = 2; i < HexMetrics.TerraceSteps; i++) {
                var v1 = v2;
                var c1 = c2;

                v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
                c2 = HexMetrics.TerraceLerp(beginColor, leftColor, i);

                terrain.AddTriangleUnperturbed(v1, v2, boundary);
                terrain.AddTriangleColor(c1, c2, boundaryColor);
                terrain.AddTriangleTerrainTypes(types);
            }

            terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
            terrain.AddTriangleColor(c2, leftColor, boundaryColor);
            terrain.AddTriangleTerrainTypes(types);
        }

        private void TriangulateCornerTerraces(
            Vector3 begin,
            HexCell beginCell,
            Vector3 left,
            HexCell leftCell,
            Vector3 right,
            HexCell rightCell
        ) {
            var v3 = HexMetrics.TerraceLerp(begin, left, 1);
            var v4 = HexMetrics.TerraceLerp(begin, right, 1);
            var c3 = HexMetrics.TerraceLerp(_color1, _color2, 1);
            var c4 = HexMetrics.TerraceLerp(_color1, _color3, 1);
            Vector3 types;
            types.x = beginCell.TerrainTypeIndex;
            types.y = leftCell.TerrainTypeIndex;
            types.z = rightCell.TerrainTypeIndex;

            terrain.AddTriangle(begin, v3, v4);
            terrain.AddTriangleColor(_color1, c3, c4);
            terrain.AddTriangleTerrainTypes(types);

            for (var i = 2; i < HexMetrics.TerraceSteps; i++) {
                var v1 = v3;
                var v2 = v4;
                var c1 = c3;
                var c2 = c4;

                v3 = HexMetrics.TerraceLerp(begin, left, i);
                v4 = HexMetrics.TerraceLerp(begin, right, i);
                c3 = HexMetrics.TerraceLerp(_color1, _color2, i);
                c4 = HexMetrics.TerraceLerp(_color1, _color3, i);

                terrain.AddQuad(v1, v2, v3, v4);
                terrain.AddQuadColor(c1, c2, c3, c4);
                terrain.AddQuadTerrainTypes(types);
            }

            terrain.AddQuad(v3, v4, left, right);
            terrain.AddQuadColor(c3, c4, _color2, _color3);
            terrain.AddQuadTerrainTypes(types);
        }

        private void TriangulateEdgeTerraces(
            EdgeVertices begin,
            HexCell beginCell,
            EdgeVertices end,
            HexCell endCell,
            bool hasRoad
        ) {
            var e2 = EdgeVertices.TerraceLerp(begin, end, 1);
            var c2 = HexMetrics.TerraceLerp(_color1, _color2, 1);
            var t1 = beginCell.TerrainTypeIndex;
            var t2 = endCell.TerrainTypeIndex;

            TriangulateEdgeStrip(begin, _color1, t1, e2, c2, t2, hasRoad);

            for (var i = 2; i < HexMetrics.TerraceSteps; i++) {
                var e1 = e2;
                var c1 = c2;

                e2 = EdgeVertices.TerraceLerp(begin, end, i);
                c2 = HexMetrics.TerraceLerp(_color1, _color2, i);

                TriangulateEdgeStrip(e1, c1, t1, e2, c2, t2, hasRoad);
            }

            TriangulateEdgeStrip(e2, c2, t1, end, _color2, t2, hasRoad);
        }


        private void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, float type) {
            terrain.AddTriangle(center, edge.v1, edge.v2);
            terrain.AddTriangle(center, edge.v2, edge.v3);
            terrain.AddTriangle(center, edge.v3, edge.v4);
            terrain.AddTriangle(center, edge.v4, edge.v5);
            terrain.AddTriangleColor(_color1);
            terrain.AddTriangleColor(_color1);
            terrain.AddTriangleColor(_color1);
            terrain.AddTriangleColor(_color1);

            Vector3 types;
            types.x = types.y = types.z = type;

            terrain.AddTriangleTerrainTypes(types);
            terrain.AddTriangleTerrainTypes(types);
            terrain.AddTriangleTerrainTypes(types);
            terrain.AddTriangleTerrainTypes(types);
        }

        private void TriangulateEdgeStrip(
            EdgeVertices e1,
            Color c1,
            float type1,
            EdgeVertices e2,
            Color c2,
            float type2,
            bool hasRoad = false
        ) {
            terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
            terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
            terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
            terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);

            terrain.AddQuadColor(c1, c2);
            terrain.AddQuadColor(c1, c2);
            terrain.AddQuadColor(c1, c2);
            terrain.AddQuadColor(c1, c2);

            Vector3 types;
            types.x = types.z = type1;
            types.y = type2;
            terrain.AddQuadTerrainTypes(types);
            terrain.AddQuadTerrainTypes(types);
            terrain.AddQuadTerrainTypes(types);
            terrain.AddQuadTerrainTypes(types);

            if (hasRoad) {
                TriangulateRoadSegment(e1.v2, e1.v3, e1.v4, e2.v2, e2.v3, e2.v4);
            }
        }

        private void TriangulateWithRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e) {
            Vector3 centerL, centerR;
            if (cell.HasRiverThroughEdge(direction.Opposite())) {
                centerL = center + HexMetrics.GetFirstSolidCorner(direction.Previous()) * .25f;
                centerR = center + HexMetrics.GetSecondSolidCorner(direction.Next()) * .25f;
            }
            else if (cell.HasRiverThroughEdge(direction.Next())) {
                centerL = center;
                centerR = Vector3.Lerp(center, e.v5, 2f / 3f);
            }
            else if (cell.HasRiverThroughEdge(direction.Previous())) {
                centerL = Vector3.Lerp(center, e.v1, 2f / 3f);
                centerR = center;
            }
            else if (cell.HasRiverThroughEdge(direction.Next2())) {
                centerL = center;
                centerR = center + HexMetrics.GetSolidEdgeMiddle(direction.Next()) * (.5f * HexMetrics.InnerToOuter);
            }
            else {
                centerL = center
                          + HexMetrics.GetSolidEdgeMiddle(direction.Previous()) * (.5f * HexMetrics.InnerToOuter);
                centerR = center;
            }

            center = Vector3.Lerp(centerL, centerR, .5f);

            var m = new EdgeVertices(Vector3.Lerp(centerL, e.v1, .5f), Vector3.Lerp(centerR, e.v5, .5f), 1f / 6f);
            m.v3.y = center.y = e.v3.y;


            TriangulateEdgeStrip(m, _color1, cell.TerrainTypeIndex, e, _color1, cell.TerrainTypeIndex);

            terrain.AddTriangle(centerL, m.v1, m.v2);
            terrain.AddQuad(centerL, center, m.v2, m.v3);
            terrain.AddQuad(center, centerR, m.v3, m.v4);
            terrain.AddTriangle(centerR, m.v4, m.v5);

            terrain.AddTriangleColor(_color1);
            terrain.AddQuadColor(_color1);
            terrain.AddQuadColor(_color1);
            terrain.AddTriangleColor(_color1);

            Vector3 types;
            types.x = types.y = types.z = cell.TerrainTypeIndex;
            terrain.AddTriangleTerrainTypes(types);
            terrain.AddQuadTerrainTypes(types);
            terrain.AddQuadTerrainTypes(types);
            terrain.AddTriangleTerrainTypes(types);

            if (cell.IsUnderwater) return;

            var reversed = cell.IncomingRiver == direction;

            TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, cell.RiverSurfaceY, .4f, reversed);
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, .6f, reversed);
        }

        private void TriangulateWithRiverBeginOrEnd(
            HexDirection direction,
            HexCell cell,
            Vector3 center,
            EdgeVertices e
        ) {
            var m = new EdgeVertices(Vector3.Lerp(center, e.v1, 0.5f), Vector3.Lerp(center, e.v5, 0.5f));
            m.v3.y = e.v3.y;

            TriangulateEdgeStrip(m, _color1, cell.TerrainTypeIndex, e, _color1, cell.TerrainTypeIndex);
            TriangulateEdgeFan(center, m, cell.TerrainTypeIndex);

            if (cell.IsUnderwater) return;
            var reversed = cell.HasIncomingRiver;
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed);
            center.y = m.v2.y = m.v4.y = cell.RiverSurfaceY;
            rivers.AddTriangle(center, m.v2, m.v4);
            if (reversed) {
                rivers.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(1f, 0.2f), new Vector2(0f, 0.2f));
            }
            else {
                rivers.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(0f, 0.6f), new Vector2(1f, 0.6f));
            }
        }

        private void TriangulateAdjectentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e) {
            if (cell.HasRoads) {
                TriangulateRoadAdjacentToRiver(direction, cell, center, e);
            }

            if (cell.HasRiverThroughEdge(direction.Next())) {
                if (cell.HasRiverThroughEdge(direction.Previous())) {
                    center += HexMetrics.GetSolidEdgeMiddle(direction) * (HexMetrics.InnerToOuter * .5f);
                }
                else if (cell.HasRiverThroughEdge(direction.Previous2())) {
                    center += HexMetrics.GetFirstSolidCorner(direction) * .25f;
                }
            }
            else if (cell.HasRiverThroughEdge(direction.Previous()) && cell.HasRiverThroughEdge(direction.Next2())) {
                center += HexMetrics.GetSecondSolidCorner(direction) * .25f;
            }

            var m = new EdgeVertices(Vector3.Lerp(center, e.v1, .5f), Vector3.Lerp(center, e.v5, .5f));

            TriangulateEdgeStrip(m, _color1, cell.TerrainTypeIndex, e, _color1, cell.TerrainTypeIndex);
            TriangulateEdgeFan(center, m, cell.TerrainTypeIndex);

            if (!cell.IsUnderwater && !cell.HasRiverThroughEdge(direction)) {
                features.AddFeature(cell, (center + e.v1 + e.v5) * (1f / 3f));
            }
        }

        private void TriangulateRiverQuad(
            Vector3 v1,
            Vector3 v2,
            Vector3 v3,
            Vector3 v4,
            float y1,
            float y2,
            float v,
            bool reversed
        ) {
            v1.y = v2.y = y1;
            v3.y = v4.y = y2;
            rivers.AddQuad(v1, v2, v3, v4);
            if (reversed) {
                rivers.AddQuadUV(1f, 0f, .8f - v, .6f - v);
            }
            else {
                rivers.AddQuadUV(0f, 1f, v, v + .2f);
            }
        }

        private void TriangulateRiverQuad(
            Vector3 v1,
            Vector3 v2,
            Vector3 v3,
            Vector3 v4,
            float y,
            float v,
            bool reversed
        ) {
            TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed);
        }

        private void TriangulateRoadSegment(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 v5, Vector3 v6) {
            roads.AddQuad(v1, v2, v4, v5);
            roads.AddQuad(v2, v3, v5, v6);

            roads.AddQuadUV(0f, 1f, 0f, 0f);
            roads.AddQuadUV(1f, 0f, 0f, 0f);
        }

        private void TriangulateRoad(
            Vector3 center,
            Vector3 mL,
            Vector3 mR,
            EdgeVertices e,
            bool hasRoadThroughCellEdge
        ) {
            if (hasRoadThroughCellEdge) {
                var mC = Vector3.Lerp(mL, mR, .5f);
                TriangulateRoadSegment(mL, mC, mR, e.v2, e.v3, e.v4);

                roads.AddTriangle(center, mL, mC);
                roads.AddTriangle(center, mC, mR);

                roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f));
                roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f));
            }
            else {
                TriangulateRoadEdge(center, mL, mR);
            }
        }

        private void TriangulateRoadEdge(Vector3 center, Vector3 mL, Vector3 mR) {
            roads.AddTriangle(center, mL, mR);
            roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        }

        private Vector2 GetRoadInterpolators(HexDirection direction, HexCell cell) {
            Vector2 interpolators;

            if (cell.HasRoadThroughEdge(direction)) {
                interpolators.x = interpolators.y = .5f;
            }
            else {
                interpolators.x = cell.HasRoadThroughEdge(direction.Previous()) ? .5f : .25f;
                interpolators.y = cell.HasRoadThroughEdge(direction.Next()) ? .5f : .25f;
            }

            return interpolators;
        }

        private void TriangulateRoadAdjacentToRiver(
            HexDirection direction,
            HexCell cell,
            Vector3 center,
            EdgeVertices e
        ) {
            var hasRoadThroughEdge = cell.HasRoadThroughEdge(direction);
            var previousHasRiver = cell.HasRiverThroughEdge(direction.Previous());
            var nextHasRiver = cell.HasRiverThroughEdge(direction.Next());
            var interpolators = GetRoadInterpolators(direction, cell);
            var roadCenter = center;

            if (cell.HasRiverBeginOrEnd) {
                roadCenter += HexMetrics.GetSolidEdgeMiddle(cell.RiverBeginOrEndDirection.Opposite()) * (1f / 3f);
            }
            else if (cell.IncomingRiver == cell.OutgoingRiver.Opposite()) {
                Vector3 corner;
                if (previousHasRiver) {
                    if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Next())) return;
                    corner = HexMetrics.GetSecondSolidCorner(direction);
                }
                else {
                    if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Previous())) return;
                    corner = HexMetrics.GetFirstSolidCorner(direction);
                }

                roadCenter += corner * .5f;
                if (cell.IncomingRiver == direction.Next() && cell.HasRoadThroughEdge(direction.Next2())
                    || cell.HasRoadThroughEdge(direction.Opposite())) {
                    features.AddBridge(roadCenter, center - corner * 0.5f);
                }

                center += corner * .25f;
            }
            else if (cell.IncomingRiver == cell.OutgoingRiver.Previous()) {
                roadCenter -= HexMetrics.GetSecondCorner(cell.IncomingRiver) * .2f;
            }
            else if (cell.IncomingRiver == cell.OutgoingRiver.Next()) {
                roadCenter -= HexMetrics.GetFirstCorner(cell.IncomingRiver) * .2f;
            }
            else if (previousHasRiver && nextHasRiver) {
                if (!hasRoadThroughEdge) {
                    return;
                }

                var offset = HexMetrics.GetSolidEdgeMiddle(direction) * HexMetrics.InnerToOuter;
                roadCenter += offset * .7f;
                center += offset * .5f;
            }
            else {
                HexDirection middle;
                if (previousHasRiver) {
                    middle = direction.Next();
                }
                else if (nextHasRiver) {
                    middle = direction.Previous();
                }
                else {
                    middle = direction;
                }

                if (!cell.HasRoadThroughEdge(middle)
                    && !cell.HasRoadThroughEdge(middle.Previous())
                    && !cell.HasRoadThroughEdge(middle.Next())) {
                    return;
                }

                var offset = HexMetrics.GetSolidEdgeMiddle(middle);
                roadCenter += offset * .25f;
                if (direction == middle && cell.HasRoadThroughEdge(direction.Opposite())) {
                    features.AddBridge(roadCenter, center - offset * (HexMetrics.InnerToOuter * 0.7f));
                }
            }

            var mL = Vector3.Lerp(roadCenter, e.v1, interpolators.x);
            var mR = Vector3.Lerp(roadCenter, e.v5, interpolators.y);
            TriangulateRoad(roadCenter, mL, mR, e, hasRoadThroughEdge);

            if (previousHasRiver) {
                TriangulateRoadEdge(roadCenter, center, mL);
            }

            if (nextHasRiver) {
                TriangulateRoadEdge(roadCenter, mR, center);
            }
        }

        private void TriangulateWater(HexDirection direction, HexCell cell, Vector3 center) {
            center.y = cell.WaterSurfaceY;

            var neighbor = cell.GetNeighbor(direction);
            if (neighbor != null && !neighbor.IsUnderwater) {
                TriangulateWaterShore(direction, cell, neighbor, center);
            }
            else {
                TriangulateOpenWater(direction, cell, neighbor, center);
            }
        }

        private void TriangulateWaterShore(HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center) {
            var e1 = new EdgeVertices(
                center + HexMetrics.GetFirstWaterCorner(direction),
                center + HexMetrics.GetSecondWaterCorner(direction)
            );
            water.AddTriangle(center, e1.v1, e1.v2);
            water.AddTriangle(center, e1.v2, e1.v3);
            water.AddTriangle(center, e1.v3, e1.v4);
            water.AddTriangle(center, e1.v4, e1.v5);

            var center2 = neighbor.Position;
            center2.y = center.y;
            var e2 = new EdgeVertices(
                center2 + HexMetrics.GetSecondSolidCorner(direction.Opposite()),
                center2 + HexMetrics.GetFirstSolidCorner(direction.Opposite())
            );

            if (cell.HasRiverThroughEdge(direction)) {
                TriangulateEstuary(e1, e2, cell.IncomingRiver == direction);
            }
            else {
                waterShores.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
                waterShores.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
                waterShores.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
                waterShores.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
                waterShores.AddQuadUV(0f, 0f, 0f, 1f);
                waterShores.AddQuadUV(0f, 0f, 0f, 1f);
                waterShores.AddQuadUV(0f, 0f, 0f, 1f);
                waterShores.AddQuadUV(0f, 0f, 0f, 1f);
            }

            var nextNeighbor = cell.GetNeighbor(direction.Next());
            if (nextNeighbor != null) {
                var v3 = nextNeighbor.Position
                         + (nextNeighbor.IsUnderwater
                             ? HexMetrics.GetFirstWaterCorner(direction.Previous())
                             : HexMetrics.GetFirstSolidCorner(direction.Previous()));
                v3.y = center.y;
                waterShores.AddTriangle(e1.v5, e2.v5, v3);
                waterShores.AddTriangleUV(
                    new Vector2(0f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, nextNeighbor.IsUnderwater ? 0f : 1f)
                );
            }
        }

        private void TriangulateEstuary(EdgeVertices e1, EdgeVertices e2, bool incomingRiver) {
            waterShores.AddTriangle(e2.v1, e1.v2, e1.v1);
            waterShores.AddTriangle(e2.v5, e1.v5, e1.v4);
            waterShores.AddTriangleUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            waterShores.AddTriangleUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));

            estuaries.AddQuad(e2.v1, e1.v2, e2.v2, e1.v3);
            estuaries.AddTriangle(e1.v3, e2.v2, e2.v4);
            estuaries.AddQuad(e1.v3, e1.v4, e2.v4, e2.v5);

            estuaries.AddQuadUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f));
            estuaries.AddTriangleUV(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 1f));
            estuaries.AddQuadUV(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));

            if (incomingRiver) {
                estuaries.AddQuadUV2(
                    new Vector2(1.5f, 1f),
                    new Vector2(0.7f, 1.15f),
                    new Vector2(1f, 0.8f),
                    new Vector2(0.5f, 1.1f)
                );
                estuaries.AddTriangleUV2(new Vector2(0.5f, 1.1f), new Vector2(1f, 0.8f), new Vector2(0f, 0.8f));
                estuaries.AddQuadUV2(
                    new Vector2(0.5f, 1.1f),
                    new Vector2(0.3f, 1.15f),
                    new Vector2(0f, 0.8f),
                    new Vector2(-0.5f, 1f)
                );
            }
            else {
                estuaries.AddQuadUV2(
                    new Vector2(-0.5f, -0.2f),
                    new Vector2(0.3f, -0.35f),
                    new Vector2(0f, 0f),
                    new Vector2(0.5f, -0.3f)
                );
                estuaries.AddTriangleUV2(new Vector2(0.5f, -0.3f), new Vector2(0f, 0f), new Vector2(1f, 0f));
                estuaries.AddQuadUV2(
                    new Vector2(0.5f, -0.3f),
                    new Vector2(0.7f, -0.35f),
                    new Vector2(1f, 0f),
                    new Vector2(1.5f, -0.2f)
                );
            }
        }

        private void TriangulateOpenWater(HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center) {
            var c1 = center + HexMetrics.GetFirstWaterCorner(direction);
            var c2 = center + HexMetrics.GetSecondWaterCorner(direction);

            water.AddTriangle(center, c1, c2);

            if (direction <= HexDirection.SE && neighbor != null) {
                var bridge = HexMetrics.GetWaterBridge(direction);
                var e1 = c1 + bridge;
                var e2 = c2 + bridge;

                water.AddQuad(c1, c2, e1, e2);

                if (direction <= HexDirection.E) {
                    var nextNeighbor = cell.GetNeighbor(direction.Next());
                    if (nextNeighbor == null || !nextNeighbor.IsUnderwater) {
                        return;
                    }

                    water.AddTriangle(c2, e2, c2 + HexMetrics.GetWaterBridge(direction.Next()));
                }
            }
        }

        private void TriangulateWaterfallInWater(
            Vector3 v1,
            Vector3 v2,
            Vector3 v3,
            Vector3 v4,
            float y1,
            float y2,
            float waterY
        ) {
            v1.y = v2.y = y1;
            v3.y = v4.y = y2;

            v1 = HexMetrics.Perturb(v1);
            v2 = HexMetrics.Perturb(v2);
            v3 = HexMetrics.Perturb(v3);
            v4 = HexMetrics.Perturb(v4);

            var t = (waterY - y2) / (y1 - y2);
            v3 = Vector3.Lerp(v3, v1, t);
            v4 = Vector3.Lerp(v4, v2, t);

            rivers.AddQuadUnperturbed(v1, v2, v3, v4);
            rivers.AddQuadUV(0f, 1f, 0.8f, 1f);
        }
    }
}