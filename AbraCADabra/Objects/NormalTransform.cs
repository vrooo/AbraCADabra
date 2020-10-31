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

    public abstract class IndexedTransform : Transform<IndexedVertex>
    {
        public IndexedTransform() { }
        protected override void SetVertexAttribPointer()
        {
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, IndexedVertex.Size, IndexedVertex.OffsetPoint);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribIPointer(1, 1, VertexAttribIntegerType.Int, IndexedVertex.Size, (IntPtr)IndexedVertex.OffsetIndexX);
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribIPointer(2, 1, VertexAttribIntegerType.Int, IndexedVertex.Size, (IntPtr)IndexedVertex.OffsetIndexZ);
        }
    }
}
