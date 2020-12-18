using OpenTK;
using System;
using System.Collections.Generic;

namespace AbraCADabra
{
    public class IntersectionFinderParams
    {
        public int StartDims { get; set; } = 4;
        public int StartMaxIterations { get; set; } = 30;
        public float StartEps { get; set; } = 1e-6f;
        public float StartPointEps { get; set; } = 1e-2f;
        public float StartSelfDiff { get; set; } = 0.1f;
        public float StartSelfDiffSquared => StartSelfDiff * StartSelfDiff;

        public int CurveMaxPoints { get; set; } = 1000;
        public int CurveMaxIterations { get; set; } = 30;
        public float CurveStep { get; set; } = 0.05f;
        public float CurveEps { get; set; } = 1e-6f;
        public float CurveEndDist { get; set; } = 0.05f;

        public void Reset()
        {
            StartDims = 4;
            StartMaxIterations = 30;
            StartEps = 1e-6f;
            StartPointEps = 1e-2f;
            StartSelfDiff = 0.1f;

            CurveMaxPoints = 1000;
            CurveMaxIterations = 30;
            CurveStep = 0.05f;
            CurveEps = 1e-6f;
            CurveEndDist = 0.05f;
        }
    }

    public static class IntersectionFinder
    {
        private static bool IsAnyNaN(Vector4 v)
        {
            for (int i = 0; i < 4; i++)
                if (float.IsNaN(v[i]) || float.IsInfinity(v[i]))
                    return true;
            return false;
        }

        public static (bool res, Vector4 x) FindIntersectionPoint(
            IntersectionFinderParams finderParams, ISurface P, ISurface Q, Vector4 x0, bool scale = true)
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

            float eps = finderParams.StartEps * finderParams.StartEps;
            int startCounter = 0;
            do
            {
                if (startCounter >= finderParams.StartMaxIterations || IsAnyNaN(x))
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

        public static (IntersectionResult intRes, IntersectionCurveManager icm) FindIntersection(
            IntersectionFinderParams finderParams, ISurface P, ISurface Q, Vector4 x0, bool scale = true)
        {
            var (intRes, points, xs, loop) = FindIntersectionData(finderParams, P, Q, x0, scale);
            if (intRes == IntersectionResult.OK)
            {
                return (intRes, new IntersectionCurveManager(P, Q, points, xs, loop));
            }
            return (intRes, null);
        }

        public static (IntersectionResult iRes, List<Vector3> points, List<Vector4> xs, bool loop) FindIntersectionDataWithStartPoint(
            IntersectionFinderParams finderParams, ISurface P, ISurface Q, int divs, Vector3 startPoint)
        {
            float fdivs = divs;
            bool noCurve = false;
            var pointSurface = new PointSurface(startPoint);
            var pointsSecond = new (bool? valid, Vector4 start)[divs, divs];
            for (int x = 0; x < divs; x++)
            {
                for (int y = 0; y < divs; y++)
                {
                    Vector4 preStart1 = divs > 1 ? new Vector4(x / fdivs, y / fdivs, 0.0f, 0.0f)
                                                 : new Vector4(0.5f, 0.5f, 0.0f, 0.0f);
                    var (boolIntRes1, start1) = FindIntersectionPoint(finderParams, P, pointSurface, preStart1);
                    if (!boolIntRes1) continue;

                    for (int z = 0; z < divs; z++)
                    {
                        for (int w = 0; w < divs; w++)
                        {
                            Vector4 preStart2 = divs > 1 ? new Vector4(z / fdivs, w / fdivs, 0.0f, 0.0f)
                                                     : new Vector4(0.5f, 0.5f, 0.0f, 0.0f);
                            if (pointsSecond[z, w].valid == null)
                            {
                                pointsSecond[z, w] = FindIntersectionPoint(finderParams, Q, pointSurface, preStart2);
                            }
                            var (boolIntRes2, start2) = pointsSecond[z, w];
                            if (boolIntRes2 == false) continue;

                            Vector4 start = new Vector4(start1.X, start1.Y, start2.X, start2.Y);
                            var (intRes, points, xs, loop) = FindIntersectionData(finderParams, P, Q, start, false);
                            if (intRes == IntersectionResult.OK)
                            {
                                return (intRes, points, xs, loop);
                            }
                            else if (intRes == IntersectionResult.NoCurve)
                            {
                                noCurve = true;
                            }
                        }
                    }
                }
            }
            if (noCurve)
            {
                return (IntersectionResult.NoCurve, null, null, false);
            }
            return (IntersectionResult.NoIntersection, null, null, false);
        }

        public static (IntersectionResult iRes, List<Vector3> points, List<Vector4> xs, bool loop) FindIntersectionDataWithoutStartPoint(
            IntersectionFinderParams finderParams, ISurface P, ISurface Q, int divs)
        {
            float fdivs = divs;
            bool noCurve = false;
            for (int x = 0; x < divs; x++)
            {
                for (int y = 0; y < divs; y++)
                {
                    for (int z = 0; z < divs; z++)
                    {
                        for (int w = 0; w < divs; w++)
                        {
                            Vector4 start = divs > 1 ? new Vector4(x / fdivs, y / fdivs, z / fdivs, w / fdivs)
                                                         : new Vector4(0.5f);
                            var (intRes, points, xs, loop) = FindIntersectionData(finderParams, P, Q, start);
                            if (intRes == IntersectionResult.OK)
                            {
                                return (intRes, points, xs, loop);
                            }
                            else if (intRes == IntersectionResult.NoCurve)
                            {
                                noCurve = true;
                            }
                        }
                    }
                }
            }
            if (noCurve)
            {
                return (IntersectionResult.NoCurve, null, null, false);
            }
            return (IntersectionResult.NoIntersection, null, null, false);
        }

        public static (IntersectionResult intRes, List<Vector3> points, List<Vector4> xs, bool loop) FindIntersectionData(
            IntersectionFinderParams finderParams, ISurface P, ISurface Q, Vector4 x0, bool scale = true)
        {
            (bool res, Vector4 x) = FindIntersectionPoint(finderParams, P, Q, x0, scale);
            if (!res || (P == Q && Vector2.DistanceSquared(new Vector2(x.X, x.Y), new Vector2(x.Z, x.W)) < finderParams.StartSelfDiffSquared))
            {
                return (IntersectionResult.NoIntersection, null, null, false);
            }

            var startP = P.GetUVPoint(x.X, x.Y);
            var startQ = Q.GetUVPoint(x.Z, x.W);
            if ((startP - startQ).LengthSquared > finderParams.StartPointEps * finderParams.StartPointEps)
            {
                return (IntersectionResult.NoIntersection, null, null, false);
            }

            var first = (startP + startQ) / 2.0f;
            List<Vector3> points = new List<Vector3> { first }, pointsEnd = null;
            List<Vector4> xs = new List<Vector4> { x }, xsEnd = null;
            float endDist = finderParams.CurveEndDist * finderParams.CurveEndDist;
            float eps = finderParams.CurveEps * finderParams.CurveEps;
            Vector4 xprev, xprevValid;
            bool loop = false, afterReverse = false;
            for (int pointCounter = 0; pointCounter < finderParams.CurveMaxPoints; pointCounter++)
            {
                var nP = Vector3.Cross(P.GetDu(x.X, x.Y), P.GetDv(x.X, x.Y));
                var nQ = Vector3.Cross(Q.GetDu(x.Z, x.W), Q.GetDv(x.Z, x.W));
                var tan = Vector3.Normalize(Vector3.Cross(nP, nQ));
                if (afterReverse)
                {
                    tan = -tan;
                }

                xprevValid = x;
                Vector3 pP = startP, pQ = startQ;
                int iterCounter = 0;
                do
                {
                    if (iterCounter >= finderParams.CurveMaxIterations || IsAnyNaN(x))
                    {
                        return (IntersectionResult.NoCurve, null, null, false);
                    }
                    iterCounter++;
                    xprev = x;

                    var J = GetJacobian(P, Q, tan, x);
                    var F = new Vector4(pP - pQ, Vector3.Dot(pP - startP, tan) - finderParams.CurveStep);
                    try
                    {
                        x += MathHelper.Solve4(J, -F);
                    }
                    catch (ArgumentException)
                    {
                        return (IntersectionResult.NoCurve, null, null, false);
                    }
                    //x = MathHelper.MakeVector4(P.ClampUV(x.X, x.Y), Q.ClampUV(x.Z, x.W));
                    pP = P.GetUVPoint(x.X, x.Y);
                    pQ = Q.GetUVPoint(x.Z, x.W);
                } while ((xprev - x).LengthSquared > eps);
                
                if (IsAnyNaN(x))
                {
                    return (IntersectionResult.NoCurve, null, null, false);
                }
                bool valid = true;
                Vector2 closestP, closestQ;
                if (!P.IsUVValid(x.X, x.Y))
                {
                    valid = false;
                    closestP = P.GetClosestValidUV(x.X, x.Y, xprevValid.X, xprevValid.Y, out double t);
                    closestQ = MathHelper.FindPointAtT(xprevValid.Zw, x.Zw, t);
                    x = MathHelper.MakeVector4(closestP, closestQ);
                }
                if (!Q.IsUVValid(x.Z, x.W))
                {
                    valid = false;
                    closestQ = Q.GetClosestValidUV(x.Z, x.W, xprevValid.Z, xprevValid.W, out double t);
                    closestP = MathHelper.FindPointAtT(xprevValid.Xy, x.Xy, t);
                    x = MathHelper.MakeVector4(closestP, closestQ);
                }

                if (!valid)
                {
                    if (P.IsUVValid(x.X, x.Y) && Q.IsUVValid(x.Z, x.W))
                    {
                        pP = P.GetUVPoint(x.X, x.Y);
                        pQ = Q.GetUVPoint(x.Z, x.W);
                        points.Add((pP + pQ) / 2.0f);
                        xs.Add(x);
                    }
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

            return (IntersectionResult.OK, points, xs, loop);
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
