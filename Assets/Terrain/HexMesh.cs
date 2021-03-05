using System;
using System.Collections.Generic;
using UnityEngine;

namespace Terrain
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class HexMesh : MonoBehaviour
    {
        private Mesh _hexMesh;
        private List<Vector3> _vertices;
        private List<int> _triangles;
        private MeshCollider _meshCollider;
        private List<Color> _colors;

        private void Awake()
        {
            GetComponent<MeshFilter>().mesh = _hexMesh = new Mesh();
            _meshCollider = gameObject.AddComponent<MeshCollider>();
            _hexMesh.name = "Hex Mesh";
            _vertices = new List<Vector3>();
            _colors = new List<Color>();
            _triangles = new List<int>();
        }

        public void Triangulate(IEnumerable<HexCell> cells)
        {
            _hexMesh.Clear();
            _vertices.Clear();
            _triangles.Clear();
            _colors.Clear();

            foreach (var cell in cells)
            {
                Triangulate(cell);
            }
            
            Console.WriteLine("vertices:" + _vertices.Count);
            Console.WriteLine("colors:" + _colors.Count);
            Console.WriteLine("triangles:" + _triangles.Count);

            _hexMesh.vertices = _vertices.ToArray();
            _hexMesh.colors = _colors.ToArray();
            _hexMesh.triangles = _triangles.ToArray();
            _hexMesh.RecalculateNormals();
            _meshCollider.sharedMesh = _hexMesh;
        }

        private void Triangulate(HexCell cell)
        {
            foreach (var direction in (HexDirection[]) Enum.GetValues(typeof(HexDirection)))
            {
                Triangulate(direction, cell);
            }
        }

        private void Triangulate (HexDirection direction, HexCell cell) {
            var center = cell.transform.localPosition;
            var v1 = center + HexMetrics.GetFirstSolidCorner(direction);
            var v2 = center + HexMetrics.GetSecondSolidCorner(direction);

            AddTriangle(center, v1, v2);
            AddTriangleColor(cell.color);

            var v3 = center + HexMetrics.GetFirstCorner(direction);
            var v4 = center + HexMetrics.GetSecondCorner(direction);

            AddQuad(v1, v2, v3, v4);

            var prevNeighbor = cell.GetNeighbor(direction.Previous()) ?? cell;
            var neighbor = cell.GetNeighbor(direction) ?? cell;
            var nextNeighbor = cell.GetNeighbor(direction.Next()) ?? cell;

            AddQuadColor(
                cell.color,
                cell.color,
                (cell.color + prevNeighbor.color + neighbor.color) / 3f,
                (cell.color + neighbor.color + nextNeighbor.color) / 3f
            );
        }

        private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            var vertexIndex = _vertices.Count;
            _vertices.Add(v1);
            _vertices.Add(v2);
            _vertices.Add(v3);
            _triangles.Add(vertexIndex);
            _triangles.Add(vertexIndex + 1);
            _triangles.Add(vertexIndex + 2);
        }


        private void AddTriangleColor(Color color)
        {
            _colors.Add(color);
        }

        private void AddQuad (Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) {
            var vertexIndex = _vertices.Count;
            _vertices.Add(v1);
            _vertices.Add(v2);
            _vertices.Add(v3);
            _vertices.Add(v4);
            _triangles.Add(vertexIndex);
            _triangles.Add(vertexIndex + 2);
            _triangles.Add(vertexIndex + 1);
            _triangles.Add(vertexIndex + 1);
            _triangles.Add(vertexIndex + 2);
            _triangles.Add(vertexIndex + 3);
        }

        private void AddQuadColor (Color c1, Color c2, Color c3, Color c4) {
            _colors.Add(c1);
            _colors.Add(c2);
            _colors.Add(c3);
            _colors.Add(c4);
        }
    }
}