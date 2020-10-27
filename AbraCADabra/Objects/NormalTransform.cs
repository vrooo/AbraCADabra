using OpenTK.Graphics.OpenGL;
using System;

namespace AbraCADabra
{
    public abstract class NormalTransform : Transform<NormalVertex>
    {
        public NormalTransform() { }
        protected override void SetVertexAttribPointer()
        {
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, NormalVertex.Size, NormalVertex.OffsetPoint);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, NormalVertex.Size, NormalVertex.OffsetNormal);
        }
    }

    public abstract class NormalIndexedTransform : Transform<NormalIndexedVertex>
    {
        public NormalIndexedTransform() { }
        protected override void SetVertexAttribPointer()
        {
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, NormalIndexedVertex.Size, NormalIndexedVertex.OffsetPoint);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, NormalIndexedVertex.Size, NormalIndexedVertex.OffsetNormal);
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribIPointer(2, 1, VertexAttribIntegerType.Int, NormalIndexedVertex.Size, (IntPtr)NormalIndexedVertex.OffsetIndexX);
            GL.EnableVertexAttribArray(3);
            GL.VertexAttribIPointer(3, 1, VertexAttribIntegerType.Int, NormalIndexedVertex.Size, (IntPtr)NormalIndexedVertex.OffsetIndexZ);
        }
    }
}
