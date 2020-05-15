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
        private const string vertPath = "../../Shaders/basic.vert";
        private const string fragPath = "../../Shaders/basic.frag";
        private const string vertPathBezier = "../../Shaders/bezier.vert";
        private const string geomPathBezier = "../../Shaders/bezier.geom";
        private const string vertPathPatch = "../../Shaders/patch.vert";
        private const string vertPathMultitex = "../../Shaders/multitex.vert";
        private const string fragPathMultitex = "../../Shaders/multitex.frag";

        private Camera camera;
        private GLControl glControl;

        private Shader shaderBasic;
        private Shader shaderBezier;
        private Shader shaderPatch;
        private Shader shaderMultitex;

        private Shader shaderCurrent;

        private AnaglyphMode anaglyphMode = AnaglyphMode.None;
        private float eyeDistance = 0.0f;
        private float planeDistance = 0.0f;

        public ShaderManager(Camera camera, GLControl glControl)
        {
            shaderBasic = new Shader(vertPath, fragPath);
            shaderBezier = new Shader(vertPathBezier, fragPath, geomPathBezier);
            shaderPatch = new Shader(vertPathPatch, fragPath);
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

        public void UsePatch()
        {
            Use(shaderPatch);
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

        public void SetupInt(int id, string name)
        {
            BindInt(id, name);
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
            shaderPatch.Dispose();
            shaderMultitex.Dispose();
        }
        #endregion
    }
}
