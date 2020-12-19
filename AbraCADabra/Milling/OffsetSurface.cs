using OpenTK;
using System;
using System.Collections.Generic;

namespace AbraCADabra
{
    public class OffsetSurface : ISurface
    {
        private ISurface surface;
        private float offset;

        public OffsetSurface(ISurface surface, float offset)
        {
            this.surface = surface;
            this.offset = offset;
        }

        public Vector3 GetUVPoint(float u, float v)
        {
            var pt = surface.GetUVPoint(u, v);
            var normal = GetNormal(u, v);
            return pt + offset * normal;
        }

        public Vector3 GetUVPointSurface(float u, float v) => surface.GetUVPoint(u, v);

        public Vector3 GetNormal(float u, float v)
        {
            Vector3 du = surface.GetDu(u, v), dv = surface.GetDv(u, v);
            Vector3 normal = Vector3.Cross(dv, du);
            normal.Normalize();
            return normal;
        }

        public int UScale => surface.UScale;

        public int VScale => surface.VScale;

        public void AddIntersectionCurve(IntersectionCurveManager icm)
        {
            surface.AddIntersectionCurve(icm);
        }

        public List<IntersectionCurveManager> GetIntersectionCurves()
        {
            return surface.GetIntersectionCurves();
        }

        public Vector2 ClampScaledUV(float u, float v)
        {
            return surface.ClampScaledUV(u, v);
        }

        public Vector2 ClampUV(float u, float v)
        {
            return surface.ClampUV(u, v);
        }

        public Vector2 GetClosestValidUV(float u, float v, float uPrev, float vPrev, out double t)
        {
            return surface.GetClosestValidUV(u, v, uPrev, vPrev, out t);
        }

        public Vector3 GetDu(float u, float v)
        {
            var du = surface.GetDu(u, v);
            var dv = surface.GetDv(u, v);
            var dudu = surface.GetDuDu(u, v);
            var dudv = surface.GetDuDv(u, v);
            return offset * (Vector3.Cross(dv, dudu) + Vector3.Cross(dudv, du)) / (float)Math.Sqrt(Vector3.Dot(Vector3.Cross(dv, du), Vector3.Cross(dv, du))) 
                - offset * Vector3.Cross(dv, du) * (Vector3.Dot(Vector3.Cross(dv, du), Vector3.Cross(dv, dudu) + Vector3.Cross(dudv, du)) + Vector3.Dot(Vector3.Cross(dv, dudu) + Vector3.Cross(dudv, du), Vector3.Cross(dv, du))) / (2 * (float)Math.Pow(Vector3.Dot(Vector3.Cross(dv, du), Vector3.Cross(dv, du)), 1.5)) + du;
        }

        public Vector3 GetDuDu(float u, float v)
        {
            return surface.GetDuDu(u, v);
        }

        public Vector3 GetDuDv(float u, float v)
        {
            return surface.GetDuDv(u, v);
        }

        public Vector3 GetDv(float u, float v)
        {
            var du = surface.GetDu(u, v);
            var dv = surface.GetDv(u, v);
            var dudv = surface.GetDuDv(u, v);
            var dvdv = surface.GetDvDv(u, v);
            return offset * (Vector3.Cross(dv, dudv) + Vector3.Cross(dvdv, du)) / (float)Math.Sqrt(Vector3.Dot(Vector3.Cross(dv, du), Vector3.Cross(dv, du))) 
                - offset * Vector3.Cross(dv, du) * (Vector3.Dot(Vector3.Cross(dv, du), Vector3.Cross(dv, dudv) + Vector3.Cross(dvdv, du)) + Vector3.Dot(Vector3.Cross(dv, dudv) + Vector3.Cross(dvdv, du), Vector3.Cross(dv, du))) / (2 * (float)Math.Pow(Vector3.Dot(Vector3.Cross(dv, du), Vector3.Cross(dv, du)), 1.5)) + dv;
        }

        public Vector3 GetDvDv(float u, float v)
        {
            return surface.GetDvDv(u, v);
        }

        public bool IsEdgeAllowed(EdgeType start, EdgeType end)
        {
            return surface.IsEdgeAllowed(start, end);
        }

        public bool IsUVValid(float u, float v)
        {
            return surface.IsUVValid(u, v);
        }

        public void UpdateMesh()
        {
            surface.UpdateMesh();
        }
    }
}
