using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace AbraCADabra
{
    abstract class Mesh
    {
        protected int vao, vbo, ebo;
        protected abstract float[] vertices { get; }
        protected abstract uint[] indices { get; }

        protected PrimitiveType primitiveType = PrimitiveType.Triangles;
        public Vector4 Color { get; set; } = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);

        public void Initialize()
        {
            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer,
                          vertices.Length * sizeof(float), vertices,
                          BufferUsageHint.DynamicDraw);

            ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                          indices.Length * sizeof(uint), indices,
                          BufferUsageHint.DynamicDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.BindVertexArray(0);
        }

        public void Render()
        {
            GL.BindVertexArray(vao);
            GL.DrawElements(primitiveType, indices.Length, DrawElementsType.UnsignedInt, 0);
        }

        public Matrix4 GetModelMatrix()
        {
            // TODO
            return Matrix4.Identity;
        }

        public void Dispose()
        {
            ebo = DeleteBuffer(ebo);
            vbo = DeleteBuffer(vbo);
            vao = DeleteBuffer(vao);
        }

        private int DeleteBuffer(int buffer)
        {
            if (buffer != 0)
            {
                GL.DeleteBuffer(buffer);
            }
            return 0;
        }
    }
}
