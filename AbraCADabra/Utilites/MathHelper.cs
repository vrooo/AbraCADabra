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
    }
}
