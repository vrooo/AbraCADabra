using OpenTK;
using System.Collections.Generic;

namespace AbraCADabra
{
    public static class MathHelper
    {
        public static Vector3 MakeVector3(double x, double y, double z)
        {
            return new Vector3((float)x, (float)y, (float)z);
        }

        public static Vector4 MakeVector4(double x, double y, double z, double w)
        {
            return new Vector4((float)x, (float)y, (float)z, (float)w);
        }

        public static Vector3[] SolveTriDiag(IList<float> a, IList<float> b, IList<float> c, IList<Vector3> g)
        {
            int n = b.Count;

            // row: a[i - 1] b[i] c[i] | g[i]
            for (int i = 0; i < n - 1; i++)
            {
                c[i] /= b[i];
                g[i] /= b[i];

                b[i + 1] -= a[i] * c[i];
                g[i + 1] -= a[i] * g[i];
            }

            g[n - 1] /= b[n - 1];
            // entire a is 0, entire b is 1
            Vector3[] res = new Vector3[n];
            res[n - 1] = g[n - 1];
            for (int i = n - 2; i >= 0; i--)
            {
                res[i] = g[i] - c[i] * res[i + 1];
            }
            return res;
        }

        public static float Sum(this Vector3 v)
        {
            return v.X + v.Y + v.Z;
        }

        public static float GetDistSq(ISurface P, ISurface Q, float u, float v, float s, float t)
        {
            var Puv = P.GetUVPoint(u, v);
            var Qst = Q.GetUVPoint(s, t);
            return (Puv - Qst).LengthSquared;
        }

        private static float GetDistD(ISurface P, ISurface Q, float u, float v, float s, float t, bool useU)
        {
            var Puv = P.GetUVPoint(u, v);
            var PuvD = useU ? P.GetDu(u, v) : P.GetDv(u, v);
            var Qst = Q.GetUVPoint(s, t);
            var PQ = Puv - Qst;
            var res = PQ * PuvD;
            return 2.0f * res.Sum();
        }
        public static float GetDistDu(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistD(P, Q, u, v, s, t, true);
        public static float GetDistDv(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistD(P, Q, u, v, s, t, false);
        public static float GetDistDs(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistD(Q, P, s, t, u, v, true);
        public static float GetDistDt(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistD(Q, P, s, t, u, v, false);
        public static Vector4 GetDistGradient(ISurface P, ISurface Q, Vector4 x)
        {
            float u = x.X, v = x.Y, s = x.Z, t = x.W;
            return new Vector4(GetDistDu(P, Q, u, v, s, t),
                               GetDistDv(P, Q, u, v, s, t),
                               GetDistDs(P, Q, u, v, s, t),
                               GetDistDt(P, Q, u, v, s, t));
        }

        private static float GetDistDPDP(ISurface P, ISurface Q, float u, float v, float s, float t, bool firstU, bool secondU)
        {
            var Puv = P.GetUVPoint(u, v);
            var PuvD1 = firstU ? P.GetDu(u, v) : P.GetDv(u, v);
            var PuvD2 = secondU ? P.GetDu(u, v) : P.GetDv(u, v);
            Vector3 PuvDD;
            if (firstU && secondU)
                PuvDD = P.GetDuDu(u, v);
            else if (firstU || secondU)
                PuvDD = P.GetDuDv(u, v);
            else
                PuvDD = P.GetDvDv(u, v);
            var Qst = Q.GetUVPoint(s, t);
            var PQ = Puv - Qst;
            var res = PuvDD * PQ + PuvD1 * PuvD2;
            return 2.0f * res.Sum();
        }
        public static float GetDistDUDU(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistDPDP(P, Q, u, v, s, t, true, true);
        public static float GetDistDUDV(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistDPDP(P, Q, u, v, s, t, true, false);
        public static float GetDistDVDV(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistDPDP(P, Q, u, v, s, t, false, false);
        public static float GetDistDSDS(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistDPDP(Q, P, s, t, u, v, true, true);
        public static float GetDistDSDT(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistDPDP(Q, P, s, t, u, v, true, false);
        public static float GetDistDTDT(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistDPDP(Q, P, s, t, u, v, false, false);

        private static float GetDistDPDQ(ISurface P, ISurface Q, float u, float v, float s, float t, bool firstU, bool secondS)
        {
            var PuvD = firstU ? P.GetDu(u, v) : P.GetDv(u, v);
            var QstD = secondS ? Q.GetDu(s, t) : Q.GetDv(s, t);
            var res = PuvD * QstD;
            return -2.0f * res.Sum();
        }
        public static float GetDistDUDS(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistDPDQ(P, Q, u, v, s, t, true, true);
        public static float GetDistDUDT(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistDPDQ(P, Q, u, v, s, t, true, false);
        public static float GetDistDVDS(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistDPDQ(P, Q, u, v, s, t, false, true);
        public static float GetDistDVDT(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistDPDQ(P, Q, u, v, s, t, false, false);
        public static Matrix4 GetDistHessian(ISurface P, ISurface Q, Vector4 x)
        {
            float u = x.X, v = x.Y, s = x.Z, t = x.W;
            float dudu = GetDistDUDU(P, Q, u, v, s, t),
                  dudv = GetDistDUDV(P, Q, u, v, s, t),
                  duds = GetDistDUDS(P, Q, u, v, s, t),
                  dudt = GetDistDUDT(P, Q, u, v, s, t),

                  dvdv = GetDistDVDV(P, Q, u, v, s, t),
                  dvds = GetDistDVDS(P, Q, u, v, s, t),
                  dvdt = GetDistDVDT(P, Q, u, v, s, t),

                  dsds = GetDistDSDS(P, Q, u, v, s, t),
                  dsdt = GetDistDSDT(P, Q, u, v, s, t),

                  dtdt = GetDistDTDT(P, Q, u, v, s, t);

            return new Matrix4(dudu, dudv, duds, dudt,
                               dudv, dvdv, dvds, dudt,
                               duds, dvds, dsds, dsdt,
                               dudt, dvdt, dsdt, dtdt);
        }
    }
}
