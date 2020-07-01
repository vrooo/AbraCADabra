using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTK;

namespace AbraCADabra
{
    [TestClass]
    public class MathHelperTests
    {
        private float eps = 1e-6f;
        [TestMethod]
        public void SolveTriDiag_OnesAndTwos()
        {
            float[] a = { 1.0f, 1.0f, 1.0f };
            float[] b = { 2.0f, 2.0f, 2.0f, 2.0f };
            float[] c = { 1.0f, 1.0f, 1.0f };
            Vector3[] g = { Vector3.One, Vector3.One, Vector3.One, Vector3.One };

            var res = MathHelper.SolveTriDiag(a, b, c, g);
            Assert.AreEqual(4, res.Length);
            Assert.AreEqual(0.4f, res[0].X, eps);
            Assert.AreEqual(0.2f, res[1].X, eps);
            Assert.AreEqual(0.2f, res[2].X, eps);
            Assert.AreEqual(0.4f, res[3].X, eps);
        }

        [TestMethod]
        public void Solve_Simple()
        {
            var A = new Matrix4(1, 2, -1, 1,
                                -1, 1, 2, -1,
                                2, -1, 2, 2,
                                1, 1, -1, 2);
            var b = new Vector4(6, 3, 14, 8);
            var x = MathHelper.Solve4(A, b);
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(i + 1, x[i], eps);
            }
        }
    }
}
