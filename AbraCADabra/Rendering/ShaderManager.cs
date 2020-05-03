using System;
using OpenTK;

namespace AbraCADabra
{
    public enum AnaglyphMode
    {
        None, Left, Right
    }

    public class ShaderManager : IDisposable
    {
        private Camera camera;
        private GLControl glControl;

        private Shader shaderBasic;
        private Shader shaderBezier;
        private Shader shaderMultitex;

        private Shader shaderCurrent;

        private AnaglyphMode anaglyphMode = AnaglyphMode.None;
        private float eyeDistance = 0.0f;
        private float planeDistance = 0.0f;

        public ShaderManager(string vertPath, string fragPath,
                             string vertPathBezier, string geomPathBezier,
                             string vertPathMultitex, string fragPathMultitex,
                             Camera camera, GLControl glControl)
        {
            shaderBasic = new Shader(vertPath, fragPath);
            shaderBezier = new Shader(vertPathBezier, fragPath, geomPathBezier);
            shaderMultitex = new Shader(vertPathMultitex, fragPathMultitex);
            this.camera = camera;
            this.glControl = glControl;
        }

        public void UseBasic()
        {
            Use(shaderBasic);
        }

        public void UseBezier()
        {
            Use(shaderBezier);
        }

        public void UseMultitex()
        {
            Use(shaderMultitex);
        }

        private void Use(Shader shader)
        {
            shader.Use();
            shaderCurrent = shader;
            SetupCamera();
        }

        public void SetAnaglyphMode(AnaglyphMode mode, float eyeDist, float planeDist, bool resetCam = true)
        {
            anaglyphMode = mode;
            eyeDistance = eyeDist;
            planeDistance = planeDist;
            if (resetCam)
            {
                SetupCamera();
            }
        }

        public void SetupColor(Vector4 color)
        {
            BindVector4(color, "color");
        }

        public void SetupAnaglyphColors(Vector4 colorLeft, Vector4 colorRight)
        {
            BindInt(0, "textureLeft");
            BindInt(1, "textureRight");
            BindVector4(colorLeft, "colorLeft");
            BindVector4(colorRight, "colorRight");
        }

        public void SetupTransform(Vector4 color, Matrix4 model)
        {
            BindVector4(color, "color");
            BindMatrix(model, "modelMatrix");
        }

        public void SetupCamera()
        {
            BindMatrix(camera.GetViewMatrix(), "viewMatrix");
            Matrix4 projMatrix;
            if (anaglyphMode == AnaglyphMode.None)
            {
                projMatrix = camera.GetProjectionMatrix(glControl.Width, glControl.Height);
            }
            else
            {
                var (left, right) = camera.GetStereoscopicMatrices(glControl.Width, glControl.Height, eyeDistance, planeDistance);
                projMatrix = anaglyphMode == AnaglyphMode.Left ? left : right;
            }
            BindMatrix(projMatrix, "projMatrix");
        }

        public float GetCameraDistance(Vector3 point)
        {
            var pos = camera.GetInvViewMatrix().ExtractTranslation();
            return (pos - point).Length;
        }

        private void BindMatrix(Matrix4 matrix, string name)
        {
            shaderCurrent.BindMatrix(matrix, name);
        }

        private void BindVector4(Vector4 vector, string name)
        {
            shaderCurrent.BindVector4(vector, name);
        }

        private void BindInt(int obj, string name)
        {
            shaderCurrent.BindInt(obj, name);
        }

        #region Disposing
        public void Dispose()
        {
            shaderBasic.Dispose();
            shaderBezier.Dispose();
        }
        #endregion
    }
}
