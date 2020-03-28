using System.Runtime.InteropServices;
using OpenTK;

namespace AbraCADabra
{
    [StructLayout(LayoutKind.Sequential)]
    struct AdjacencyVertex
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
}
