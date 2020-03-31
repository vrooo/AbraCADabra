//using System.Collections.Generic;
//using OpenTK.Graphics.OpenGL;
//using OpenTK;

//namespace AbraCADabra
//{
//    class Bezier3C2 : Bezier3
//    {
//        const int n = 3;
//        public Bezier3C2(IEnumerable<Vector3> points) : base(points) { }

//        public override void Render(ShaderManager shader)
//        {
//            shader.SetupTransform(Color, GetModelMatrix());
//            GL.BindVertexArray(vao);
//            for (int start = 1; start < indices.Length; start += 3)
//            {
//                GL.DrawArrays(primitiveType, start, 4);
//            }
//            GL.BindVertexArray(0);
//        }

//        protected override void CalculateVertices(IEnumerable<Vector3> points)
//        {
//            vertexList.Clear();
//            indexList.Clear();

//            var bPoints = GetBernsteinPoints(points);
//            uint i = 0;
//            Vector3 position = new Vector3();
//            Vector3 last = new Vector3();
//            foreach (var point in bPoints)
//            {
//                if (i == 0)
//                {
//                    AddVertex(point, false);
//                }
//                AddVertex(point);
//                indexList.Add(i);
//                i++;
//                position += point;
//                last = point;
//            }
//            if (i > 0)
//            {
//                AddVertex(last, false);
//                AddVertex(last, false);
//            }
//            Position = position / (i > 0 ? i : 1);
//        }

//        private List<Vector3> GetBernsteinPoints(IEnumerable<Vector3> points)
//        {
//            var pointsList = new List<Vector3>(points);
//            var bPoints = new List<Vector3>();

//            int pc = pointsList.Count;
//            if (pc > 0)
//            {
//                bPoints.Add(pointsList[0]);
//                for (int i = 1; i < pc; i++)
//                {
//                    Vector3 third = (pointsList[i] - pointsList[i - 1]) / 3;
//                    Vector3 v1 = pointsList[i - 1] + third, v2 = pointsList[i - 1] + 2 * third;
//                    if (i > 1)
//                    {
//                        bPoints.Add((v1 + bPoints[3 * i - 4]) / 2);
//                    }
//                    bPoints.Add(v1);
//                    bPoints.Add(v2);
//                }
//                bPoints.Add(pointsList[pc - 1]);
//            }

//            return bPoints;
//        }
//    }
//}
