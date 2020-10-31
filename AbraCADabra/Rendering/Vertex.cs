using System.Runtime.InteropServices;
using OpenTK;

namespace AbraCADabra
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NormalVertex
    {
        public Vector3 Point;
        public Vector3 Normal;
        public NormalVertex(Vector3 point, Vector3 normal)
        {
            Point = point;
            Normal = normal;
        }

        public static readonly int Size = Marshal.SizeOf<NormalVertex>();
        public static readonly int OffsetPoint = (int)Marshal.OffsetOf<NormalVertex>(nameof(Point));
        public static readonly int OffsetNormal = (int)Marshal.OffsetOf<NormalVertex>(nameof(Normal));
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IndexedVertex
    {
        public Vector3 Point;
        public int IndexX, IndexZ;
        public IndexedVertex(Vector3 point, int indX, int indZ)
        {
            Point = point;
            IndexX = indX;
            IndexZ = indZ;
        }

        public static readonly int Size = Marshal.SizeOf<IndexedVertex>();
        public static readonly int OffsetPoint = (int)Marshal.OffsetOf<IndexedVertex>(nameof(Point));
        public static readonly int OffsetIndexX = (int)Marshal.OffsetOf<IndexedVertex>(nameof(IndexX));
        public static readonly int OffsetIndexZ = (int)Marshal.OffsetOf<IndexedVertex>(nameof(IndexZ));
    }

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
        public int IndexX, IndexZ;
        public PatchVertex(float u, float v, int indX, int indZ)
        {
            UV = new Vector2(u, v);
            IndexX = indX;
            IndexZ = indZ;
        }

        public static readonly int Size = Marshal.SizeOf<PatchVertex>();
        public static readonly int OffsetUV = (int)Marshal.OffsetOf<PatchVertex>(nameof(UV));
        public static readonly int OffsetIndexX = (int)Marshal.OffsetOf<PatchVertex>(nameof(IndexX));
        public static readonly int OffsetIndexZ = (int)Marshal.OffsetOf<PatchVertex>(nameof(IndexZ));
    }
}
