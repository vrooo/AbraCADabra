using OpenTK;
using System;
using System.Collections.Generic;

namespace AbraCADabra
{
    public static class MathHelper
    {
        public const double TWO_PI = Math.PI * 2;

        public static Vector3 MakeVector3(double x, double y, double z)
        {
            return new Vector3((float)x, (float)y, (float)z);
        }

        public static Vector4 MakeVector4(double x, double y, double z, double w)
        {
            return new Vector4((float)x, (float)y, (float)z, (float)w);
        }

        public static Vector4 MakeVector4(Vector2 v1, Vector2 v2)
        {
            return new Vector4(v1.X, v1.Y, v2.X, v2.Y);
        }

        public static float Sum(this Vector3 v)
        {
            return v.X + v.Y + v.Z;
        }

        public static (double sin, double cos) SinCos(double val)
        {
            return (Math.Sin(val), Math.Cos(val));
        }

        public static Vector2 FindPointAtX(float ax, float ay, float bx, float by, double x, out double t)
        {
            t = (x - ax) / (bx - ax);
            return new Vector2((float)x, (float)(ay + t * (by - ay)));
        }
        public static Vector2 FindPointAtX(Vector2 a, Vector2 b, double x, out double t) => FindPointAtX(a.X, a.Y, b.X, b.Y, x, out t);

        public static Vector2 FindPointAtY(float ax, float ay, float bx, float by, double y, out double t)
        {
            t = (y - ay) / (by - ay);
            return new Vector2((float)(ax + t * (bx - ax)), (float)y);
        }
        public static Vector2 FindPointAtY(Vector2 a, Vector2 b, double y, out double t) => FindPointAtY(a.X, a.Y, b.X, b.Y, y, out t);

        public static Vector2 FindPointAtT(float ax, float ay, float bx, float by, double t)
        {
            return new Vector2((float)(ax + t * (bx - ax)), (float)(ay + t * (by - ay)));
        }
        public static Vector2 FindPointAtT(Vector2 a, Vector2 b, double t) => FindPointAtT(a.X, a.Y, b.X, b.Y, t);

        public static bool HasIntersection(Vector2 v1start, Vector2 v1end, Vector2 v2start, Vector2 v2end, bool checkCollinear = false)
        {
            // https://stackoverflow.com/questions/217578/how-can-i-determine-whether-a-2d-point-is-within-a-polygon
            float a1 = v1end.Y - v1start.Y;
            float b1 = v1start.X - v1end.X;
            float c1 = (v1end.X * v1start.Y) - (v1start.X * v1end.Y);

            float d1 = (a1 * v2start.X) + (b1 * v2start.Y) + c1;
            float d2 = (a1 * v2end.X) + (b1 * v2end.Y) + c1;

            if (d1 > 0 && d2 > 0) return false;
            if (d1 < 0 && d2 < 0) return false;

            float a2 = v2end.Y - v2start.Y;
            float b2 = v2start.X - v2end.X;
            float c2 = (v2end.X * v2start.Y) - (v2start.X * v2end.Y);

            d1 = (a2 * v1start.X) + (b2 * v1start.Y) + c2;
            d2 = (a2 * v1end.X) + (b2 * v1end.Y) + c2;

            if (d1 > 0 && d2 > 0) return false;
            if (d1 < 0 && d2 < 0) return false;

            if ((a1 * b2) - (a2 * b1) == 0.0f) // collinear
            {
                if (checkCollinear)
                {
                    return IsPointInRectangle(v1start, v2start, v2end) ||
                           IsPointInRectangle(v1end, v2start, v2end) ||
                           IsPointInRectangle(v2start, v1start, v1end) ||
                           IsPointInRectangle(v2end, v1start, v1end);
                }
                return false;
            }
            return true;
        }

        private static bool IsPointInRectangle(Vector2 point, Vector2 a, Vector2 b)
        {
            return Math.Min(a.X, b.X) <= point.X && point.X <= Math.Max(a.X, b.X) &&
                   Math.Min(a.Y, b.Y) <= point.Y && point.Y <= Math.Max(a.Y, b.Y);
        }

        public static Vector2 GetIntersection(Vector2 v1start, Vector2 v1end, Vector2 v2start, Vector2 v2end, out float s)
        {
            Vector2 ba = v1end - v1start, ca = v2start - v1start, dc = v2end - v2start;
            s = (ba.X * ca.Y - ba.Y * ca.X) / (dc.X * ba.Y - dc.Y * ba.X);
            return v2start + s * dc;
        }

        public static bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
        {
            Vector2 end = new Vector2(0.5f, -0.5f); // will always be outside
            bool isInside = false;
            for (int i = 0; i < polygon.Count; i++)
            {
                if (HasIntersection(point, end, polygon[i], polygon[(i + 1) % polygon.Count]))
                {
                    isInside = !isInside;
                }
            }
            return isInside;
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

        public static Vector4 Solve4(Matrix4 A, Vector4 b)
        {
            for (int step = 0; step < 3; step++)
            {
                int bestInd = step;
                for (int j = step + 1; j < 4; j++)
                {
                    if (Math.Abs(A[bestInd, step]) < Math.Abs(A[j, step]))
                    {
                        bestInd = j;
                    }
                }
                if (A[bestInd, step] == 0)
                {
                    throw new ArgumentException("No solution");
                }
                SwapRows(ref A, ref b, step, bestInd);
                for (int row = step + 1; row < 4; row++)
                {
                    float a = A[row, step] / A[step, step];
                    A[row, step] = 0;
                    for (int col = step + 1; col < 4; col++)
                    {
                        A[row, col] -= a * A[step, col];
                    }
                    b[row] -= a * b[step];
                }
            }

            for (int step = 3; step >= 0; step--)
            {
                b[step] /= A[step, step];
                A[step, step] = 1;
                for (int row = step - 1; row >= 0; row--)
                {
                    float a = A[row, step];
                    A[row, step] = 0;
                    b[row] -= a * b[step];
                }
            }

            return b;
        }

        private static void SwapRows(ref Matrix4 A, ref Vector4 b, int i, int j)
        {
            if (i == j)
            {
                return;
            }

            float tmp;
            for (int k = 0; k < 4; k++)
            {
                tmp = A[i, k];
                A[i, k] = A[j, k];
                A[j, k] = tmp;
            }
            tmp = b[i];
            b[i] = b[j];
            b[j] = tmp;
        }
    }
}
