using OpenTK;
using System;
using System.Collections.Generic;

namespace AbraCADabra
{
    public static class IntersectionFinder
    {
        public static IntersectionCurveManager FindIntersection(ISurface P, ISurface Q, Vector4 start, int iter, float uvEps, float pointEps)
        {
            bool IsAnyNaN(Vector4 v)
            {
                for (int i = 0; i < 4; i++)
                    if (float.IsNaN(v[i]) || float.IsInfinity(v[i]))
                        return true;
                return false;
            }

            Vector4 x = start, xprev, grad = MathHelper.GetDistGradient(P, Q, start), r = -grad, p = r, rnew;
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

                a = -Vector4.Dot(grad, p) / Vector4.Dot(p, MathHelper.GetDistHessian(P, Q, x) * p);
                x += a * p;
                grad = MathHelper.GetDistGradient(P, Q, x);

                rnew = -grad;
                b = Math.Max(0.0f, Vector4.Dot(rnew, rnew - r) / r.LengthSquared);
                r = rnew;
                p = r + b * p;
            } while ((xprev - x).LengthSquared > eps);

            if (!converged || IsAnyNaN(x))
            {
                return null;
            }

            for (int i = 0; i < 4; i++)
            {
                if (x[i] < 0 || x[i] > 1)
                {
                    x[i] -= (float)Math.Floor(x[i]);
                }
            }
            var testP = P.GetUVPoint(x.X, x.Y);
            var testQ = Q.GetUVPoint(x.Z, x.W);
            if ((testP - testQ).LengthSquared > pointEps * pointEps)
            {
                return null;
            }

            List<Vector3> points = new List<Vector3> { testP, testQ };
            return new IntersectionCurveManager(points);
        }
    }
}
