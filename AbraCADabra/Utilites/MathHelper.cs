using OpenTK;
using System;
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

        public static Vector4 MakeVector4(Vector2 v1, Vector2 v2)
        {
            return new Vector4(v1.X, v1.Y, v2.X, v2.Y);
        }

        public static float Sum(this Vector3 v)
        {
            return v.X + v.Y + v.Z;
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
