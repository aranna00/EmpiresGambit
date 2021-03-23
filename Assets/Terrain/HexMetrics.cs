using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Terrain
{
    public static class HexMetrics
    {
        public const float OuterRadius = 10f;
        public const float InnerRadius = OuterRadius * OuterToInner;
        public const float SolidFactor = 0.8f;
        public const float BlendFactor = 1f - SolidFactor;
        public const float ElevationStep = 3f;
        public const int TerracesPerSlope = 2;
        public const int TerraceSteps = TerracesPerSlope * 2 + 1;
        public const float HorizontalTerraceStepSize = 1f / TerraceSteps;
        public const float VerticalTerraceStepSize = 1f / (TerracesPerSlope + 1);
        public const float CellPerturbStrength = 4f;
        public const float NoiseScale = 0.003f;
        public const float ElevationPerturbStrength = 1.5f;
        public const int ChunkSizeX = 5, ChunkSizeZ = 5;
        public const float StreamBedElevationOffset = -1.75f;
        public const float OuterToInner = .866025404f;
        public const float InnerToOuter = 1f / OuterToInner;
        public const float WaterElevationOffset = -.5f;
        public const int MaxSlopeHeight = 1;
        public const float WaterFactor = 0.6f;
        public const float waterBlendFactor = 1f - WaterFactor;
        public const int HashGridSize = 256;
        public const float HashGridScale = 0.25f;
        public const float WallHeight = 3f;
        public const float WallThickness = 0.75f;
        public const float WallElevationOffset = VerticalTerraceStepSize;
        public const float WallTowerThreshold = 0.5f;
        public const float WallYOffset = -1f;
        public const float BridgeDesignLength = 7f;

        public static Texture2D NoiseSource;

        private static HexHash[] _hashGrid;

        static float[][] _featureThresholds = {
            new float[] {0.0f, 0.0f, 0.4f}, new float[] {0.0f, 0.4f, 0.6f}, new float[] {0.4f, 0.6f, 0.8f}
        };

        public static readonly Vector3[] Corners = {
            new Vector3(0f, 0f, OuterRadius),
            new Vector3(InnerRadius, 0f, .5f * OuterRadius),
            new Vector3(InnerRadius, 0f, -.5f * OuterRadius),
            new Vector3(0f, 0f, -OuterRadius),
            new Vector3(-InnerRadius, 0f, -.5f * OuterRadius),
            new Vector3(-InnerRadius, 0f, .5f * OuterRadius),
            new Vector3(0f, 0f, OuterRadius),
        };


        public static Vector3 GetFirstCorner(HexDirection direction) {
            return Corners[(int) direction];
        }

        public static Vector3 GetSecondCorner(HexDirection direction) {
            return Corners[(int) direction + 1];
        }

        public static Vector3 GetFirstSolidCorner(HexDirection direction) {
            return Corners[(int) direction] * SolidFactor;
        }

        public static Vector3 GetSecondSolidCorner(HexDirection direction) {
            return Corners[(int) direction + 1] * SolidFactor;
        }

        public static Vector3 GetBridge(HexDirection direction) {
            return (Corners[(int) direction] + Corners[(int) direction + 1]) * BlendFactor;
        }

        public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step) {
            var h = step * HorizontalTerraceStepSize;
            a.x += (b.x - a.x) * h;
            a.z += (b.z - a.z) * h;

            var v = ((step + 1) / 2) * VerticalTerraceStepSize;
            a.y += (b.y - a.y) * v;

            return a;
        }

        public static Color TerraceLerp(Color a, Color b, int step) {
            var h = step * HorizontalTerraceStepSize;
            return Color.Lerp(a, b, h);
        }

        public static HexEdgeType GetEdgeType(int elevation1, int elevation2) {
            var delta = Math.Abs(elevation2 - elevation1);

            if (delta == 0) {
                return HexEdgeType.Flat;
            }

            return delta <= MaxSlopeHeight ? HexEdgeType.Slope : HexEdgeType.Cliff;
        }

        public static Vector4 SampleNoise(Vector3 position) {
            return NoiseSource.GetPixelBilinear(position.x * NoiseScale, position.z * NoiseScale);
        }

        public static Vector3 GetSolidEdgeMiddle(HexDirection direction) {
            return (Corners[(int) direction] + Corners[(int) direction + 1]) * (.5f * SolidFactor);
        }

        public static Vector3 Perturb(Vector3 position) {
            var sample = SampleNoise(position);

            position.x += (sample.x * 2f - 1f) * CellPerturbStrength;
            // position.y += (sample.y * 2f - 1f) * HexMetrics.CellPerturbStrength;
            position.z += (sample.z * 2f - 1f) * CellPerturbStrength;

            return position;
        }

        public static Vector3 GetFirstWaterCorner(HexDirection direction) {
            return Corners[(int) direction] * WaterFactor;
        }

        public static Vector3 GetSecondWaterCorner(HexDirection direction) {
            return Corners[(int) direction + 1] * WaterFactor;
        }

        public static Vector3 GetWaterBridge(HexDirection direction) {
            return (Corners[(int) direction] + Corners[(int) direction + 1]) * waterBlendFactor;
        }

        public static void InitializeHashGrid(int seed) {
            _hashGrid = new HexHash[HashGridSize * HashGridSize];
            var currentState = Random.state;
            Random.InitState(seed);
            for (var i = 0; i < _hashGrid.Length; i++) {
                _hashGrid[i] = HexHash.Create();
            }

            Random.state = currentState;
        }

        public static HexHash SampleHashGrid(Vector3 position) {
            var x = Math.Abs((int) (position.x * HashGridScale) % HashGridSize);
            var z = Math.Abs((int) (position.z * HashGridScale) % HashGridSize);
            return _hashGrid[x + z * HashGridSize];
        }

        public static float[] GetFeatureThresholds(int level) {
            return _featureThresholds[level];
        }

        public static Vector3 WallThicknessOffset(Vector3 near, Vector3 far) {
            Vector3 offset;

            offset.x = far.x - near.x;
            offset.y = 0;
            offset.z = far.z - near.z;

            return offset.normalized * (WallThickness * 0.5f);
        }

        public static Vector3 WallLerp(Vector3 near, Vector3 far) {
            near.x += (far.x - near.x) * 0.5f;
            near.z += (far.z - near.z) * 0.5f;

            var v = near.y < far.y ? WallElevationOffset : 1f - WallElevationOffset;
            near.y += (far.y - near.y) * v + WallYOffset;

            return near;
        }
    }
}