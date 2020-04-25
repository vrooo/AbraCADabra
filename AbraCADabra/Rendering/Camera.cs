using System;
using System.Drawing.Drawing2D;
using OpenTK;

namespace AbraCADabra
{
    public class Camera
    {
        public Vector3 Position { get; private set; }
        public Vector3 Rotation { get; private set; }
        public Vector3 Offset { get; set; } = new Vector3(0, 0, -30);

        public float ZNear { get; set; } = 0.1f;
        public float ZFar { get; set; } = 5000.0f;
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
            // TODO: cache and calc inv matrix
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

        public Matrix4 GetProjectionMatrix(float width, float height)
        {
            // TODO: cache
            return Matrix4.CreatePerspectiveFieldOfView(FOV, width/height, ZNear, ZFar);
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
