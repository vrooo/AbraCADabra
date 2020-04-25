using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System;

namespace AbraCADabra
{
    public class CrossCursor : FloatTransform
    {
        private float scaleModifier = 0.05f;
        private float thickness = 2.0f;
        private float[] _vertices =
        {
            0.0f, 0.0f, 0.0f
        };
        protected override float[] vertices => _vertices;

        private uint[] _indices =
        {
            0
        };
        protected override uint[] indices => _indices;

        private List<Arrow> arrows;

        public CrossCursor()
        {
            primitiveType = PrimitiveType.Lines;
            Color = Vector4.One;

            var arrowx = new Arrow(new Vector4(0.9f, 0.0f, 0.0f, 1.0f));
            arrowx.Rotation.Z = -(float)(Math.PI / 2);
            var arrowy = new Arrow(new Vector4(0.0f, 0.9f, 0.0f, 1.0f));
            var arrowz = new Arrow(new Vector4(0.0f, 0.0f, 0.9f, 1.0f));
            arrowz.Rotation.X = (float)(Math.PI / 2);
            arrows = new List<Arrow> { arrowx, arrowy, arrowz };

            Initialize();
        }

        public override void Render(ShaderManager shader)
        {
            var scale = new Vector3(shader.GetCameraDistance(Position) * scaleModifier);

            GL.Disable(EnableCap.DepthTest);
            GL.LineWidth(thickness);

            foreach (var arrow in arrows)
            {
                arrow.Position = Position;
                arrow.Scale = scale;
                arrow.Render(shader);
            }

            GL.LineWidth(1.0f);
            GL.Enable(EnableCap.DepthTest);
        }

        public override void Rotate(float x, float y, float z) { }
        public override void ScaleUniform(float delta) { }
    }
}
