using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;

namespace AbraCADabra
{
    public abstract class FloatTransform : Transform<float>
    {
        public FloatTransform() { }

        public FloatTransform(Vector3 position) : base(position) { }

        protected override void SetVertexAttribPointer()
        {
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        }
    }

    public abstract class Transform<T> : Transform where T : struct
    {
        protected int vao, vbo, ebo;
        protected abstract T[] vertices { get; }
        protected abstract uint[] indices { get; }

        public Transform() { }

        public Transform(Vector3 position) : base(position) { }

        protected override void Initialize()
        {
            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            vbo = GL.GenBuffer();
            ebo = GL.GenBuffer();
            CreateBuffers();

            SetVertexAttribPointer();

            GL.BindVertexArray(0);
        }

        protected override void CreateBuffers()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer,
                          vertices.Length * Marshal.SizeOf<T>(), vertices,
                          BufferUsageHint.DynamicDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                          indices.Length * sizeof(uint), indices,
                          BufferUsageHint.DynamicDraw);
        }

        //protected override void UpdateBuffers()
        //{
        //    GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        //    GL.BufferSubData(BufferTarget.ArrayBuffer, (System.IntPtr)0,
        //                     vertices.Length * Marshal.SizeOf<T>(), vertices);
        //    GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
        //    GL.BufferSubData(BufferTarget.ElementArrayBuffer, (System.IntPtr)0,
        //                     indices.Length * sizeof(uint), indices);
        //}

        public override void Render(ShaderManager shader)
        {
            shader.SetupTransform(Color, GetModelMatrix());
            GL.BindVertexArray(vao);
            GL.DrawElements(primitiveType, indices.Length, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }
        public override void Dispose()
        {
            ebo = DeleteBuffer(ebo);
            vbo = DeleteBuffer(vbo);
            vao = DeleteVertexArray(vao);
        }
    }

    public abstract class Transform
    {
        protected PrimitiveType primitiveType = PrimitiveType.Triangles;
        public Vector4 Color { get; set; }

        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale = Vector3.One;

        public Transform() { }

        public Transform(Vector3 position)
        {
            Position = position;
        }

        protected abstract void Initialize();

        protected abstract void SetVertexAttribPointer();

        //protected abstract void CreateBuffers(int maxVertices = -1, int maxIndices = -1);
        protected abstract void CreateBuffers();

        //protected abstract void UpdateBuffers();

        public abstract void Render(ShaderManager shader);

        public virtual void Rotate(float x, float y, float z)
        {
            Rotation += new Vector3(x, y, z);
        }

        public virtual void RotateAround(float xAngle, float yAngle, float zAngle, Vector3 center)
        {
            var oldVect = new Vector4(Position - center, 1.0f);
            var newVect = oldVect * Matrix4.CreateFromAxisAngle(Vector3.UnitX, xAngle) * 
                                    Matrix4.CreateFromAxisAngle(Vector3.UnitY, yAngle) * 
                                    Matrix4.CreateFromAxisAngle(Vector3.UnitZ, zAngle);
            var translation = newVect - oldVect;
            Translate(translation.X, translation.Y, translation.Z);
            Rotate(xAngle, yAngle, zAngle);
        }

        public virtual void Translate(float x, float y, float z)
        {
            Position += new Vector3(x, y, z);
        }

        public virtual void ScaleUniform(float delta)
        {
            Scale += new Vector3(delta);
        }

        public virtual Matrix4 GetModelMatrix()
        {
            return Matrix4.CreateScale(Scale) *
                   Matrix4.CreateRotationX(Rotation.X) *
                   Matrix4.CreateRotationY(Rotation.Y) *
                   Matrix4.CreateRotationZ(Rotation.Z) *
                   Matrix4.CreateTranslation(Position);
        }

        public Vector3 GetScreenSpaceCoords(Camera camera, float width, float height)
        {
            Matrix4 model = GetModelMatrix(),
                    view = camera.GetViewMatrix(),
                    proj = camera.GetProjectionMatrix(width, height);
            var coords = Vector4.UnitW * model * view * proj;
            coords /= coords.W;
            coords.X = (coords.X + 1.0f) * (width / 2.0f);
            coords.Y = (-coords.Y + 1.0f) * (height / 2.0f);
            coords.Z = camera.ZFar * camera.ZNear / (camera.ZFar + coords.Z * (camera.ZNear - camera.ZFar));

            return new Vector3(coords);
        }

        public virtual bool TestHit(Camera camera, float width, float height, float x, float y, out float z)
        {
            z = 0.0f;
            return false;
        }

        public abstract void Dispose();

        protected int DeleteBuffer(int buffer)
        {
            if (buffer != 0)
            {
                GL.DeleteBuffer(buffer);
            }
            return 0;
        }
        protected int DeleteVertexArray(int array)
        {
            if (array != 0)
            {
                GL.DeleteVertexArray(array);
            }
            return 0;
        }
    }
}
