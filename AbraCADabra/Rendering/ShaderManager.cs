using System;
using OpenTK;

namespace AbraCADabra
{
    public class ShaderManager : IDisposable
    {
        private Camera camera;
        private GLControl glControl;

        private Shader shaderBasic;
        private Shader shaderAdapt;

        private Shader shaderCurrent;

        public ShaderManager(string vertPath, string fragPath, string geomPath,
                             Camera camera, GLControl glControl)
        {
            shaderBasic = new Shader(vertPath, fragPath);
            shaderAdapt = new Shader(vertPath, fragPath, geomPath);
            this.camera = camera;
            this.glControl = glControl;
        }

        public void UseBasic()
        {
            Use(shaderBasic);
        }

        public void UseAdapt()
        {
            Use(shaderAdapt);
        }

        private void Use(Shader shader)
        {
            shader.Use();
            shaderCurrent = shader;
            SetupCamera();
        }

        public void SetupTransform(Vector4 color, Matrix4 model)
        {
            BindVector4(color, "color");
            BindMatrix(model, "modelMatrix");
        }

        public void SetupCamera()
        {
            BindMatrix(camera.GetViewMatrix(), "viewMatrix");
            BindMatrix(camera.GetProjectionMatrix(glControl.Width, glControl.Height), "projMatrix");
        }

        private void BindMatrix(Matrix4 matrix, string name)
        {
            shaderCurrent.BindMatrix(matrix, name);
        }

        private void BindVector4(Vector4 vector, string name)
        {
            shaderCurrent.BindVector4(vector, name);
        }

        #region Disposing
        public void Dispose()
        {
            shaderBasic.Dispose();
            shaderAdapt.Dispose();
        }
        #endregion
    }
}
