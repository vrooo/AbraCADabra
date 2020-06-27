using OpenTK;
using System;
using System.Collections.Generic;

namespace AbraCADabra
{
    public static class IntersectionFinder
    {
        private static float Sum(this Vector3 v)
        {
            return v.X + v.Y + v.Z;
        }

        private static float GetDistSq(ISurface P, ISurface Q, float u, float v, float s, float t)
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
        private static float GetDistDu(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistD(P, Q, u, v, s, t, true);
        private static float GetDistDv(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistD(P, Q, u, v, s, t, false);
        private static float GetDistDs(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistD(Q, P, s, t, u, v, true);
        private static float GetDistDt(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistD(Q, P, s, t, u, v, false);
        private static Vector4 GetDistGradient(ISurface P, ISurface Q, Vector4 x)
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
        private static float GetDistDUDU(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistDPDP(P, Q, u, v, s, t, true, true);
        private static float GetDistDUDV(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistDPDP(P, Q, u, v, s, t, true, false);
        private static float GetDistDVDV(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistDPDP(P, Q, u, v, s, t, false, false);
        private static float GetDistDSDS(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistDPDP(Q, P, s, t, u, v, true, true);
        private static float GetDistDSDT(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistDPDP(Q, P, s, t, u, v, true, false);
        private static float GetDistDTDT(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistDPDP(Q, P, s, t, u, v, false, false);

        private static float GetDistDPDQ(ISurface P, ISurface Q, float u, float v, float s, float t, bool firstU, bool secondS)
        {
            var PuvD = firstU ? P.GetDu(u, v) : P.GetDv(u, v);
            var QstD = secondS ? Q.GetDu(s, t) : Q.GetDv(s, t);
            var res = PuvD * QstD;
            return -2.0f * res.Sum();
        }
        private static float GetDistDUDS(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistDPDQ(P, Q, u, v, s, t, true, true);
        private static float GetDistDUDT(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistDPDQ(P, Q, u, v, s, t, true, false);
        private static float GetDistDVDS(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistDPDQ(P, Q, u, v, s, t, false, true);
        private static float GetDistDVDT(ISurface P, ISurface Q, float u, float v, float s, float t)
            => GetDistDPDQ(P, Q, u, v, s, t, false, false);
        private static Matrix4 GetDistHessian(ISurface P, ISurface Q, Vector4 x)
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

        public static Vector4 FindIntersection(ISurface P, ISurface Q, Vector4 start, int iter = 100)
        {
            Vector4 x = start, grad = GetDistGradient(P, Q, x), r = -grad, p = r, rnew;
            float a, b;
            int counter = 0;
            while (counter < iter) // TODO: if we're getting close
            {
                counter++;
                a = Vector4.Dot(grad, p) / Vector4.Dot(p * GetDistHessian(P, Q, x), p);
                x += a * p;
                grad = GetDistGradient(P, Q, x);

                rnew = -grad;
                b = Math.Max(0.0f, Vector4.Dot(rnew, rnew - r) / r.LengthSquared);
                r = rnew;
                p = r + b * p;
            }

            float u = x.X, v = x.Y, s = x.Z, t = x.W;
            while (u > 1) u -= 1;
            while (u < 0) u += 1;
            while (v > 1) v -= 1;
            while (v < 0) v += 1;
            while (s > 1) s -= 1;
            while (s < 0) s += 1;
            while (t > 1) t -= 1;
            while (t < 0) t += 1;
            var test1 = P.GetUVPoint(u, v);
            var test2 = Q.GetUVPoint(s, t);

            return x;
        }
    }
}
