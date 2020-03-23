using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Linq;

namespace AbraCADabra
{
    class Bezier3 : Transform
    {
        private List<float> vertexList = new List<float>();
        protected override float[] vertices => vertexList.ToArray();
        private List<uint> indexList = new List<uint>();
        protected override uint[] indices => indexList.ToArray();

        public Bezier3(IEnumerable<Vector3> points, List<int> divs)
        {
            primitiveType = PrimitiveType.Lines;
            Color = new Vector4(0.9f, 0.9f, 0.9f, 1.0f);

            CalculateVertices(points, divs);
            Initialize();
        }

        public void Update(IEnumerable<Vector3> points, List<int> divs)
        {
            CalculateVertices(points, divs);
            CreateBuffers();
        }

        private Vector3 CalculateBezier(List<Vector3> segmentPoints, float t)
        {
            int n = segmentPoints.Count;
            Vector3[,] arr = new Vector3[n, n];
            for (int j = 0; j < n; j++)
            {
                arr[0, j] = segmentPoints[j];
            }
            for (int i = 1; i < n; i++)
            {
                for (int j = 0; j < n - i; j++)
                {
                    arr[i, j] = (1 - t) * arr[i - 1, j] + t * arr[i - 1, j + 1];
                }
            }
            return arr[n - 1, 0];
        }

        private void CalculateVerticesForSegment(List<Vector3> segmentPoints, int divs)
        {
            if (divs <= 0)
            {
                divs = 1;
            }
            uint index = (uint)vertexList.Count / 3;
            float t, step = 1.0f / divs;
            for (uint i = 0; i <= divs; i++)
            {
                t = i * step;
                Vector3 point = CalculateBezier(segmentPoints, t);
                vertexList.Add(point.X);
                vertexList.Add(point.Y);
                vertexList.Add(point.Z);
                if (i > 0)
                {
                    indexList.Add(index + i - 1);
                    indexList.Add(index + i);
                }
            }
        }

        private void CalculateVertices(IEnumerable<Vector3> points, List<int> divs)
        {
            vertexList.Clear();
            indexList.Clear();

            int i = 0;
            Vector3 position = new Vector3();
            List<Vector3> segment = new List<Vector3>();
            foreach (var point in points)
            {
                segment.Add(point);
                if (segment.Count == 4)
                {
                    CalculateVerticesForSegment(segment, divs[i++]);
                    segment.Clear();
                    segment.Add(point);
                }
                position += point;
            }
            if (segment.Count > 1)
            {
                CalculateVerticesForSegment(segment, divs[i]);
            }
            int n = points.Count();
            Position = position / (n > 0 ? n : 1);
        }

        public override Matrix4 GetModelMatrix()
        {
            return Matrix4.Identity;
        }
        public override void Translate(float x, float y, float z) { }
        public override void Rotate(float x, float y, float z) { }
        public override void ScaleUniform(float delta) { }
    }
}
