using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTK;

namespace AbraCADabra
{
    [TestClass]
    public class MathHelperTests
    {
        [TestMethod]
        public void SolveTriDiag_OnesAndTwos()
        {
            float eps = 0.001f;
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
    }
}
