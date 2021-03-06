using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Terrain
{
    public static class HexMetrics
    {
        public const float OuterRadius = 10f;
        public const float InnerRadius = OuterRadius * 0.866025404f;
        public const float SolidFactor = 0.8f;
        public const float BlendFactor = 1f - SolidFactor;
        public const float ElevationsStep = 3f;
        public const int TerracesPerSlope = 2;
        public const int TerraceSteps = TerracesPerSlope * 2 + 1;
        public const float HorizontalTerraceStepSize = 1f / TerraceSteps;
        public const float VerticalTerraceStepSize = 1f / (TerracesPerSlope + 1);
        public const float CellPerturbStrength = 4f;
        public const float NoiseScale = 0.003f;
        public const float ElevationPerturbStrength = 1.5f;

        public static Texture2D NoiseSource;

        public static readonly Vector3[] Corners =
        {
            new Vector3(0f, 0f, OuterRadius),
            new Vector3(InnerRadius, 0f, .5f * OuterRadius),
            new Vector3(InnerRadius, 0f, -.5f * OuterRadius),
            new Vector3(0f, 0f, -OuterRadius),
            new Vector3(-InnerRadius, 0f, -.5f * OuterRadius),
            new Vector3(-InnerRadius, 0f, .5f * OuterRadius),
            new Vector3(0f, 0f, OuterRadius),
        };

        public static Vector3 GetFirstCorner(HexDirection direction)
        {
            return Corners[(int) direction];
        }

        public static Vector3 GetSecondCorner(HexDirection direction)
        {
            return Corners[(int) direction + 1];
        }

        public static Vector3 GetFirstSolidCorner(HexDirection direction)
        {
            return Corners[(int) direction] * SolidFactor;
        }

        public static Vector3 GetSecondSolidCorner(HexDirection direction)
        {
            return Corners[(int) direction + 1] * SolidFactor;
        }

        public static Vector3 GetBridge(HexDirection direction)
        {
            return (Corners[(int) direction] + Corners[(int) direction + 1]) * BlendFactor;
        }

        public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
        {
            var h = step * HorizontalTerraceStepSize;
            a.x += (b.x - a.x) * h;
            a.z += (b.z - a.z) * h;

            var v = ((step + 1) / 2) * VerticalTerraceStepSize;
            a.y += (b.y - a.y) * v;

            return a;
        }

        public static Color TerraceLerp(Color a, Color b, int step)
        {
            var h = step * HorizontalTerraceStepSize;
            return Color.Lerp(a, b, h);
        }

        public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
        {
            var delta = Math.Abs(elevation2 - elevation1);

            if (delta == 0)
            {
                return HexEdgeType.Flat;
            }

            return delta == 1 ? HexEdgeType.Slope : HexEdgeType.Cliff;
        }

        public static Vector4 SampleNoise(Vector3 position)
        {
            return NoiseSource.GetPixelBilinear(position.x * NoiseScale, position.z * NoiseScale);
        }
    }
}