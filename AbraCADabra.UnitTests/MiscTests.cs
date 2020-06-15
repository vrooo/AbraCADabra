using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AbraCADabra
{
    [TestClass]
    public class MiscTests
    {
        private static float[] SplitBezier(float[] points, float t = 0.5f)
        {
            int n = points.Length;
            float[] res = new float[2 * n - 1];
            float[,] arr = new float[n, n];
            for (int j = 0; j < n; j++)
            {
                arr[0, j] = points[j];
            }
            res[0] = arr[0, 0];
            res[2 * n - 2] = arr[0, n - 1];
            for (int i = 1; i < n; i++)
            {
                for (int j = 0; j < n - i; j++)
                {
                    arr[i, j] = (1 - t) * arr[i - 1, j] + t * arr[i - 1, j + 1];
                }
                res[i] = arr[i, 0];
                res[2 * n - 2 - i] = arr[i, n - 1 - i];
            }
            return res;
        }

        [TestMethod]
        public void SplitBezier_CorrectResult()
        {
            float eps = 0.001f;
            float[] points = { 4, -2, 1 };

            var res = SplitBezier(points, 2.0f / 3.0f);
            Assert.AreEqual(4.0f, res[0], eps);
            Assert.AreEqual(0.0f, res[1], eps);
            Assert.AreEqual(0.0f, res[2], eps);
            Assert.AreEqual(0.0f, res[3], eps);
            Assert.AreEqual(1.0f, res[4], eps);
        }
    }
}
