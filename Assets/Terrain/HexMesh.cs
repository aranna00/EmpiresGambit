using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Terrain
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class HexMesh : MonoBehaviour
    {
        private Mesh _hexMesh;
        private static List<Vector3> _vertices = new List<Vector3>();
        private static List<int> _triangles = new List<int>();
        private MeshCollider _meshCollider;
        private static List<Color> _colors = new List<Color>();

        private void Awake() {
            GetComponent<MeshFilter>().mesh = _hexMesh = new Mesh();
            _meshCollider = gameObject.AddComponent<MeshCollider>();
            _hexMesh.name = "Hex Mesh";
        }


        public void Clear() {
            _hexMesh.Clear();
            _vertices.Clear();
            _triangles.Clear();
            _colors.Clear();
        }

        public void Apply() {
            {
                _hexMesh.SetVertices(_vertices);
                _hexMesh.SetColors(_colors);
                _hexMesh.SetTriangles(_triangles, 0);
                _hexMesh.RecalculateNormals();
                _meshCollider.sharedMesh = _hexMesh;
            }
        }


        public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3) {
            var vertexIndex = _vertices.Count;
            _vertices.Add(HexMetrics.Perturb(v1));
            _vertices.Add(HexMetrics.Perturb(v2));
            _vertices.Add(HexMetrics.Perturb(v3));
            _triangles.Add(vertexIndex);
            _triangles.Add(vertexIndex + 1);
            _triangles.Add(vertexIndex + 2);
        }


        public void AddTriangleColor(Color color) {
            _colors.Add(color);
            _colors.Add(color);
            _colors.Add(color);
        }

        public void AddTriangleColor(Color cellColor, Color neighborColor, Color nextNeighborColor) {
            _colors.Add(cellColor);
            _colors.Add(neighborColor);
            _colors.Add(nextNeighborColor);
        }

        public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) {
            var vertexIndex = _vertices.Count;
            _vertices.Add(HexMetrics.Perturb(v1));
            _vertices.Add(HexMetrics.Perturb(v2));
            _vertices.Add(HexMetrics.Perturb(v3));
            _vertices.Add(HexMetrics.Perturb(v4));
            _triangles.Add(vertexIndex);
            _triangles.Add(vertexIndex + 2);
            _triangles.Add(vertexIndex + 1);
            _triangles.Add(vertexIndex + 1);
            _triangles.Add(vertexIndex + 2);
            _triangles.Add(vertexIndex + 3);
        }

        public void AddQuadColor(Color c1, Color c2) {
            _colors.Add(c1);
            _colors.Add(c1);
            _colors.Add(c2);
            _colors.Add(c2);
        }

        public void AddQuadColor(Color c1, Color c2, Color c3, Color c4) {
            _colors.Add(c1);
            _colors.Add(c2);
            _colors.Add(c3);
            _colors.Add(c4);
        }

        public void AddQuadColor(Color cellColor) {
            _colors.Add(cellColor);
            _colors.Add(cellColor);
            _colors.Add(cellColor);
            _colors.Add(cellColor);
        }


        public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3) {
            var vertexIndex = _vertices.Count;
            _vertices.Add(v1);
            _vertices.Add(v2);
            _vertices.Add(v3);

            _triangles.Add(vertexIndex);
            _triangles.Add(vertexIndex + 1);
            _triangles.Add(vertexIndex + 2);
        }
    }
}