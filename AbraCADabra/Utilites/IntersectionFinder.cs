using OpenTK;
using System;
using System.Collections.Generic;

namespace AbraCADabra
{
    public static class IntersectionFinder
    {
        private static bool IsAnyNaN(Vector4 v)
        {
            for (int i = 0; i < 4; i++)
                if (float.IsNaN(v[i]) || float.IsInfinity(v[i]))
                    return true;
            return false;
        }

        private static Vector4 Reduce(Vector4 v) // TODO: move to ISurface
        {
            for (int i = 0; i < 4; i++)
            {
                if (v[i] < 0 || v[i] > 1)
                {
                    v[i] -= (float)Math.Floor(v[i]);
                }
            }
            return v;
        }

        public static (IntersectionResult intRes, IntersectionCurveManager icm) FindIntersection(ISurface P, ISurface Q,
            Vector4 x0, int iter, float d, float uvEps, float pointEps)
        {
            Vector4 x = x0, xprev, grad = GetDistGradient(P, Q, x0), r = -grad, p = r, rnew;
            float a, b;

            float eps = uvEps * uvEps;
            int counter = 0;
            bool converged = true;
            do
            {
                if (counter >= iter || IsAnyNaN(x))
                {
                    converged = false;
                    break;
                }
                counter++;
                xprev = x;

                a = -Vector4.Dot(grad, p) / Vector4.Dot(p, GetDistHessian(P, Q, x) * p);
                x = Reduce(x + a * p);
                grad = GetDistGradient(P, Q, x);

                rnew = -grad;
                b = Math.Max(0.0f, Vector4.Dot(rnew, rnew - r) / r.LengthSquared);
                r = rnew;
                p = r + b * p;
            } while ((xprev - x).LengthSquared > eps);

            if (!converged || IsAnyNaN(x))
            {
                return (IntersectionResult.NoIntersection, null);
            }

            var startP = P.GetUVPoint(x.X, x.Y);
            var startQ = Q.GetUVPoint(x.Z, x.W);
            if ((startP - startQ).LengthSquared > pointEps * pointEps)
            {
                return (IntersectionResult.NoIntersection, null);
            }

            var points = new List<Vector3> { (startP + startQ) / 2.0f };
            for (int tmp = 0; tmp < 50; tmp++) // TODO: while something
            {
                var nP = Vector3.Cross(P.GetDu(x.X, x.Y), P.GetDv(x.X, x.Y));
                var nQ = Vector3.Cross(Q.GetDu(x.Z, x.W), Q.GetDv(x.Z, x.W));
                var tan = Vector3.Normalize(Vector3.Cross(nP, nQ));

                Vector3 pP = startP, pQ = startQ;
                for (int tmp2 = 0; tmp2 < 50; tmp2++) // TODO: while something
                {
                    var J = GetJacobian(P, Q, tan, x);
                    var F = new Vector4(pP - pQ, Vector3.Dot(pP - startP, tan) - d);
                    try
                    {
                        x += MathHelper.Solve4(J, -F);
                    }
                    catch (ArgumentException)
                    {
                        return (IntersectionResult.NoCurve, null);
                    }
                    pP = P.GetUVPoint(x.X, x.Y);
                    pQ = Q.GetUVPoint(x.Z, x.W);
                }
                startP = pP;
                startQ = pQ;
                points.Add((startP + startQ) / 2.0f);
            }

            return (IntersectionResult.OK, new IntersectionCurveManager(points));
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

        private static Matrix4 GetJacobian(ISurface P, ISurface Q, Vector3 tan, Vector4 x)
        {
            float u = x.X, v = x.Y, s = x.Z, t = x.W;
            var Pu = P.GetDu(u, v);
            var Pv = P.GetDv(u, v);
            var Qs = Q.GetDu(s, t);
            var Qt = Q.GetDv(s, t);

            float tanu = 0.0f, tanv = 0.0f;
            for (int i = 0; i < 3; i++)
            {
                tanu += tan[i] * Pu[i];
                tanv += tan[i] * Pv[i];
            }

            return new Matrix4(Pu.X, Pv.X, -Qs.X, -Qt.X,
                               Pu.Y, Pv.Y, -Qs.Y, -Qt.Y,
                               Pu.Z, Pv.Z, -Qs.Z, -Qt.Z,
                               tanu, tanv, 0.0f, 0.0f);
        }
    }

    public enum IntersectionResult
    {
        OK, NoIntersection, NoCurve 
    }
}
