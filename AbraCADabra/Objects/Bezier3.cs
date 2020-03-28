using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace AbraCADabra
{
    class Bezier3 : Transform<AdjacencyVertex>
    {
        private List<AdjacencyVertex> vertexList = new List<AdjacencyVertex>();
        protected override AdjacencyVertex[] vertices => vertexList.ToArray();
        private List<uint> indexList = new List<uint>();
        protected override uint[] indices => indexList.ToArray();

        public Bezier3(IEnumerable<Vector3> points)
        {
            primitiveType = PrimitiveType.LineStripAdjacencyExt;
            Color = new Vector4(0.9f, 0.9f, 0.9f, 1.0f);

            CalculateVertices(points);
            Initialize();
        }
        protected override void SetVertexAttribPointer()
        {
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, AdjacencyVertex.Size, AdjacencyVertex.OffsetPoint);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 1, VertexAttribPointerType.Byte, false, AdjacencyVertex.Size, AdjacencyVertex.OffsetValid);
        }

        public override void Render(ShaderManager shader)
        {
            shader.SetupTransform(Color, GetModelMatrix());
            GL.BindVertexArray(vao);
            for (int start = 1; start < indices.Length; start += 3)
            {
                //GL.DrawElements(primitiveType, indices.Length, DrawElementsType.UnsignedInt, 0);
                //GL.DrawElements(primitiveType, 4, DrawElementsType.UnsignedInt, (start + 1) * sizeof(uint));
                GL.DrawArrays(primitiveType, start, 4);
                //GL.DrawRangeElements(primitiveType, start, indices.Length, 4, DrawElementsType.UnsignedInt, indices);
            }
            GL.BindVertexArray(0);
        }

        public void Update(IEnumerable<Vector3> points)
        {
            CalculateVertices(points);
            CreateBuffers();
        }

        //private Vector3 CalculateBezier(List<Vector3> segmentPoints, float t)
        //{
        //    int n = segmentPoints.Count;
        //    Vector3[,] arr = new Vector3[n, n];
        //    for (int j = 0; j < n; j++)
        //    {
        //        arr[0, j] = segmentPoints[j];
        //    }
        //    for (int i = 1; i < n; i++)
        //    {
        //        for (int j = 0; j < n - i; j++)
        //        {
        //            arr[i, j] = (1 - t) * arr[i - 1, j] + t * arr[i - 1, j + 1];
        //        }
        //    }
        //    return arr[n - 1, 0];
        //}

        //private void CalculateVerticesForSegment(List<Vector3> segmentPoints, int divs)
        //{
        //    if (divs <= 0)
        //    {
        //        divs = 1;
        //    }
        //    uint index = (uint)vertexList.Count / 3;
        //    float t, step = 1.0f / divs;
        //    for (uint i = 0; i <= divs; i++)
        //    {
        //        t = i * step;
        //        Vector3 point = CalculateBezier(segmentPoints, t);
        //        vertexList.Add(point.X);
        //        vertexList.Add(point.Y);
        //        vertexList.Add(point.Z);
        //        if (i > 0)
        //        {
        //            indexList.Add(index + i - 1);
        //            indexList.Add(index + i);
        //        }
        //    }
        //}

        private void CalculateVertices(IEnumerable<Vector3> points)
        {
            vertexList.Clear();
            indexList.Clear();

            //int i = 0;
            //Vector3 position = new Vector3();
            //List<Vector3> segment = new List<Vector3>();
            //foreach (var point in points)
            //{
            //    segment.Add(point);
            //    if (segment.Count == 4)
            //    {
            //        CalculateVerticesForSegment(segment, divs[i++]);
            //        segment.Clear();
            //        segment.Add(point);
            //    }
            //    position += point;
            //}
            //if (segment.Count > 1)
            //{
            //    CalculateVerticesForSegment(segment, divs[i]);
            //}
            //int n = points.Count();
            //Position = position / (n > 0 ? n : 1);

            uint i = 0;
            Vector3 position = new Vector3();
            Vector3 last = new Vector3();
            foreach (var point in points)
            {
                if (i == 0)
                {
                    AddVertex(point);
                }
                AddVertex(point);
                indexList.Add(i);
                i++;
                position += point;
                last = point;
            }
            if (i > 0)
            {
                AddVertex(last);
                AddVertex(last);
            }
            Position = position / (i > 0 ? i : 1);
        }

        private void AddVertex(Vector3 point, bool valid = true)
        {
            vertexList.Add(new AdjacencyVertex(point, valid));
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
