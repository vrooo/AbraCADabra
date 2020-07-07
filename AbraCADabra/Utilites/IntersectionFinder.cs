using OpenTK;
using System;
using System.Collections.Generic;

using IFW = AbraCADabra.IntersectionFinderWindow;

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

        public static (bool res, Vector4 x) FindIntersectionPoint(ISurface P, ISurface Q, Vector4 x0, bool scale = true)
        {
            Vector2 x1, x2;
            if (scale)
            {
                x1 = P.ClampUV(x0.X * P.UScale, x0.Y * P.VScale);
                x2 = Q.ClampUV(x0.Z * Q.UScale, x0.W * Q.VScale);
            }
            else
            {
                x1 = P.ClampUV(x0.X, x0.Y);
                x2 = Q.ClampUV(x0.Z, x0.W);
            }
            x0 = MathHelper.MakeVector4(x1, x2);
            Vector4 x = x0, xprev, grad = GetDistGradient(P, Q, x0), r = -grad, p = r, rnew;
            float a, b;

            float eps = IFW.StartEps * IFW.StartEps;
            int startCounter = 0;
            do
            {
                if (startCounter >= IFW.StartMaxIterations || IsAnyNaN(x))
                {
                    return (false, x);
                }
                startCounter++;
                xprev = x;

                a = -Vector4.Dot(grad, p) / Vector4.Dot(p, GetDistHessian(P, Q, x) * p);
                x += a * p;
                x = MathHelper.MakeVector4(P.ClampUV(x.X, x.Y), Q.ClampUV(x.Z, x.W));
                grad = GetDistGradient(P, Q, x);

                rnew = -grad;
                b = Math.Max(0.0f, Vector4.Dot(rnew, rnew - r) / r.LengthSquared);
                r = rnew;
                p = r + b * p;
            } while ((xprev - x).LengthSquared > eps);

            if (IsAnyNaN(x))
            {
                return (false, x);
            }
            return (true, x);
        }

        public static (IntersectionResult intRes, IntersectionCurveManager icm) FindIntersection(ISurface P, ISurface Q, Vector4 x0, bool scale = true)
        {
            //x0 = new Vector4(x0.X * P.UScale, x0.Y * P.VScale, x0.Z * Q.UScale, x0.W * Q.VScale);
            //Vector4 x = x0, xprev, grad = GetDistGradient(P, Q, x0), r = -grad, p = r, rnew;
            //float a, b;

            //float eps = IFW.StartEps * IFW.StartEps;
            //int startCounter = 0;
            //do
            //{
            //    if (startCounter >= IFW.StartMaxIterations || IsAnyNaN(x))
            //    {
            //        return (IntersectionResult.NoIntersection, null);
            //    }
            //    startCounter++;
            //    xprev = x;

            //    a = -Vector4.Dot(grad, p) / Vector4.Dot(p, GetDistHessian(P, Q, x) * p);
            //    x += a * p;
            //    x = MathHelper.MakeVector4(P.ClampUV(x.X, x.Y), Q.ClampUV(x.Z, x.W));
            //    grad = GetDistGradient(P, Q, x);

            //    rnew = -grad;
            //    b = Math.Max(0.0f, Vector4.Dot(rnew, rnew - r) / r.LengthSquared);
            //    r = rnew;
            //    p = r + b * p;
            //} while ((xprev - x).LengthSquared > eps);

            //if (IsAnyNaN(x))
            //{
            //    return (IntersectionResult.NoIntersection, null);
            //}
            (bool res, Vector4 x) = FindIntersectionPoint(P, Q, x0, scale);
            if (!res)
            {
                return (IntersectionResult.NoIntersection, null);
            }

            var startP = P.GetUVPoint(x.X, x.Y);
            var startQ = Q.GetUVPoint(x.Z, x.W);
            if ((startP - startQ).LengthSquared > IFW.StartPointEps * IFW.StartPointEps)
            {
                return (IntersectionResult.NoIntersection, null);
            }

            var first = (startP + startQ) / 2.0f;
            List<Vector3> points = new List<Vector3> { first }, pointsEnd = null;
            List<Vector4> xs = new List<Vector4> { x }, xsEnd = null;
            float endDist = IFW.CurveEndDist * IFW.CurveEndDist;
            float eps = IFW.CurveEps * IFW.CurveEps;
            Vector4 xprev;
            bool loop = false, afterReverse = false;
            for (int pointCounter = 0; pointCounter < IFW.CurveMaxPoints; pointCounter++)
            {
                var nP = Vector3.Cross(P.GetDu(x.X, x.Y), P.GetDv(x.X, x.Y));
                var nQ = Vector3.Cross(Q.GetDu(x.Z, x.W), Q.GetDv(x.Z, x.W));
                var tan = Vector3.Normalize(Vector3.Cross(nP, nQ));
                if (afterReverse)
                {
                    tan = -tan;
                }

                Vector3 pP = startP, pQ = startQ;
                int iterCounter = 0;
                do
                {
                    if (iterCounter >= IFW.CurveMaxIterations || IsAnyNaN(x))
                    {
                        return (IntersectionResult.NoCurve, null);
                    }
                    iterCounter++;
                    xprev = x;

                    var J = GetJacobian(P, Q, tan, x);
                    var F = new Vector4(pP - pQ, Vector3.Dot(pP - startP, tan) - IFW.CurveStep);
                    try
                    {
                        x += MathHelper.Solve4(J, -F);
                    }
                    catch (ArgumentException)
                    {
                        return (IntersectionResult.NoCurve, null);
                    }
                    //x = MathHelper.MakeVector4(P.ClampUV(x.X, x.Y), Q.ClampUV(x.Z, x.W));
                    pP = P.GetUVPoint(x.X, x.Y);
                    pQ = Q.GetUVPoint(x.Z, x.W);
                } while ((xprev - x).LengthSquared > eps);
                
                if (IsAnyNaN(x))
                {
                    return (IntersectionResult.NoCurve, null);
                }
                if (!P.IsUVValid(x.X, x.Y) || !Q.IsUVValid(x.Z, x.W))
                {
                    if (afterReverse)
                    {
                        points.Reverse();
                        points.AddRange(pointsEnd);
                        xs.Reverse();
                        xs.AddRange(xsEnd);
                        break;
                    }
                    else
                    {
                        afterReverse = true;

                        pointsEnd = points;
                        points = new List<Vector3>();
                        xsEnd = xs;
                        xs = new List<Vector4>();

                        first = pointsEnd[pointsEnd.Count - 1];
                        x = xsEnd[0];
                        startP = P.GetUVPoint(x.X, x.Y);
                        startQ = Q.GetUVPoint(x.Z, x.W);
                        continue;
                    }
                }

                startP = pP;
                startQ = pQ;
                var point = (startP + startQ) / 2.0f;
                points.Add(point);
                xs.Add(x);
                if (points.Count > 2 && (point - first).LengthSquared < endDist)
                {
                    loop = true;
                    if (pointsEnd != null)
                    {
                        points.Reverse();
                        points.AddRange(pointsEnd);
                        xs.Reverse();
                        xs.AddRange(xsEnd);
                    }
                    break;
                }
            }

            return (IntersectionResult.OK, new IntersectionCurveManager(P, Q, points, xs, loop));
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

            float tanu = Vector3.Dot(tan, Pu), tanv = Vector3.Dot(tan, Pv);

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
