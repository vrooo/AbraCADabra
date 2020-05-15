﻿using System.Runtime.InteropServices;
using OpenTK;

namespace AbraCADabra
{
    [StructLayout(LayoutKind.Sequential)]
    public struct AdjacencyVertex
    {
        public Vector3 Point;
        public byte Valid;
        public AdjacencyVertex(Vector3 point, bool valid = true)
        {
            Point = point;
            Valid = valid ? (byte)1 : (byte)0;
        }

        public static readonly int Size = Marshal.SizeOf<AdjacencyVertex>();
        public static readonly int OffsetPoint = (int)Marshal.OffsetOf<AdjacencyVertex>(nameof(Point));
        public static readonly int OffsetValid = (int)Marshal.OffsetOf<AdjacencyVertex>(nameof(Valid));
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PatchVertex
    {
        public Vector2 UV;
        public int Index;
        public PatchVertex(float u, float v, int ind)
        {
            UV = new Vector2(u, v);
            Index = ind;
        }

        public static readonly int Size = Marshal.SizeOf<PatchVertex>();
        public static readonly int OffsetUV = (int)Marshal.OffsetOf<PatchVertex>(nameof(UV));
        public static readonly int OffsetIndex = (int)Marshal.OffsetOf<PatchVertex>(nameof(Index));
    }
}
