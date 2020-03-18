using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace AbraCADabra
{
    public abstract class Mesh
    {
        protected int vao, vbo, ebo;
        protected abstract float[] vertices { get; }
        protected abstract uint[] indices { get; }

        protected PrimitiveType primitiveType = PrimitiveType.Triangles;
        public Vector4 Color { get; set; }

        public Vector3 Position { get; protected set; }
        public Vector3 Rotation { get; protected set; }
        public Vector3 Scale { get; protected set; } = Vector3.One;

        protected void Initialize(int maxVertices = -1, int maxIndices = -1)
        {
            if (maxVertices == -1)
            {
                maxVertices = vertices.Length;
            }
            if (maxIndices == -1)
            {
                maxIndices = indices.Length;
            }

            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer,
                          maxVertices * sizeof(float), vertices,
                          BufferUsageHint.DynamicDraw);

            ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                          maxIndices * sizeof(uint), indices,
                          BufferUsageHint.DynamicDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.BindVertexArray(0);
        }

        protected void UpdateBuffers()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, (System.IntPtr)0,
                             vertices.Length * sizeof(float), vertices);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferSubData(BufferTarget.ElementArrayBuffer, (System.IntPtr)0,
                             indices.Length * sizeof(float), indices);
        }

        public void Render()
        {
            GL.BindVertexArray(vao);
            GL.DrawElements(primitiveType, indices.Length, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        public void Rotate(float x, float y, float z)
        {
            Rotation += new Vector3(x, y, z);
            Rotation = Vector3.Clamp(Rotation, new Vector3(-89.0f), new Vector3(89.0f));
        }

        public void Translate(float x, float y, float z)
        {
            Position += new Vector3(x, y, z);
        }

        public void ScaleUniform(float delta)
        {
            Scale += new Vector3(delta);
            Scale = Vector3.Clamp(Scale, new Vector3(-0.01f), new Vector3(10.0f));
        }

        public Matrix4 GetModelMatrix()
        {
            return Matrix4.CreateScale(Scale) *
                   Matrix4.CreateRotationX(Rotation.X) *
                   Matrix4.CreateRotationY(Rotation.Y) *
                   Matrix4.CreateRotationZ(Rotation.Z) *
                   Matrix4.CreateTranslation(Position);
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
