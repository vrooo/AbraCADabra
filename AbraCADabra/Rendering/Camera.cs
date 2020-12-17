using System;
using OpenTK;

namespace AbraCADabra
{
    public class Camera
    {
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Offset { get; set; } = new Vector3(0, 0, -30.0f);

        public float ZNear { get; set; } = 1;
        public float ZFar { get; set; } = 1000;
        public float FOV { get; set; } = (float)(Math.PI / 4.0);

        public Camera(Vector3 position, Vector3 rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public Camera(Vector3 position) : this(position, Vector3.Zero) { }

        public Camera(float x, float y, float z) : this(new Vector3(x, y, z)) { }

        public Camera(float x, float y, float z, float rx, float ry, float rz)
            : this(new Vector3(x, y, z), new Vector3(rx, ry, rz)) { }

        public Matrix4 GetRotationMatrix()
        {
            return Matrix4.CreateRotationZ(Rotation.Z) *
                   Matrix4.CreateRotationY(Rotation.Y) *
                   Matrix4.CreateRotationX(Rotation.X);
        }

        public Matrix4 GetViewMatrix()
        {
            // WONTDO: cache and calc inv matrix
            Vector3 rotateAround = Position - Offset;
            return Matrix4.CreateTranslation(rotateAround) *
                   GetRotationMatrix() *
                   Matrix4.CreateTranslation(-rotateAround) *
                   Matrix4.CreateTranslation(Position);
        }

        public Matrix4 GetInvViewMatrix()
        {
            var view = GetViewMatrix();
            view.Invert();
            return view;
        }

        public (Matrix4 view, Matrix4 invView) GetViewAndInvViewMatrix()
        {
            var view = GetViewMatrix();
            return (view, view.Inverted());
        }

        public Matrix4 GetProjectionMatrix(float width, float height)
        {
            // WONTDO: cache
            return Matrix4.CreatePerspectiveFieldOfView(FOV, width/height, ZNear, ZFar);
        }

        public Matrix4 GetOrthographicMatrix(float width, float height)
        {
            return Matrix4.CreateOrthographic(width, height, ZNear, ZFar);
        }

        public (Matrix4 left, Matrix4 right) GetStereoscopicMatrices(float width, float height, float eyeDist, float planeDist)
        {
            double tan = Math.Tan(FOV / 2.0f), asp = width/height;
            double lD = ZNear * (asp * tan - eyeDist / (2 * planeDist));
            float r = (float)(2 * ZNear * asp * tan - lD);
            float l = (float)lD;
            float ud = (float)(ZNear * tan);

            float eh = eyeDist / 2.0f;
            var left  = Matrix4.CreateTranslation(eh, 0.0f, 0.0f) *
                        Matrix4.CreatePerspectiveOffCenter(-l, r, -ud, ud, ZNear, ZFar);
            var right = Matrix4.CreateTranslation(-eh, 0.0f, 0.0f) * 
                        Matrix4.CreatePerspectiveOffCenter(-r, l, -ud, ud, ZNear, ZFar);
            return (left, right);
        }

        public void Rotate(float x, float y, float z)
        {
            Rotation += new Vector3(x, y, z);
        }

        public void Translate(float x, float y, float z)
        {
            Vector4 change = GetRotationMatrix() * new Vector4(x, y, z, 1);
            Position += new Vector3(change.X, change.Y, change.Z);
        }
    }
}
