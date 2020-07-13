using OpenTK;

namespace AbraCADabra
{
    public interface ISurface
    {
        Vector3 GetUVPoint(float u, float v);

        // first derivative
        Vector3 GetDu(float u, float v);
        Vector3 GetDv(float u, float v);

        // second derivative
        Vector3 GetDuDu(float u, float v);
        Vector3 GetDuDv(float u, float v);
        Vector3 GetDvDv(float u, float v);

        // for scaling from [0; 1] to actual
        int UScale { get; }
        int VScale { get; }
        Vector2 ClampUV(float u, float v); // for input in [0; U/VScale]
        Vector2 ClampScaledUV(float u, float v); // for input in [0; 1]
        bool IsUVValid(float u, float v); // for input in [0; U/VScale]
        Vector2 GetClosestValidUV(float u, float v, float uPrev, float vPrev, out double t); // prev is valid

        void AddIntersectionCurve(IntersectionCurveManager icm);
        void UpdateMesh();

        bool IsEdgeAllowed(EdgeType start, EdgeType end);
    }
}
