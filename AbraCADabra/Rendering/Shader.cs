using System;
using System.IO;
using System.Text;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace AbraCADabra
{
    class Shader : IDisposable
    {
        private int shaderProgram;

        private bool disposed = false;

        public Shader(string vertPath, string fragPath)
        {
            int vert = CompileShader(vertPath, ShaderType.VertexShader);
            int frag = CompileShader(fragPath, ShaderType.FragmentShader);

            shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vert);
            GL.AttachShader(shaderProgram, frag);
            GL.LinkProgram(shaderProgram);

            GL.DetachShader(shaderProgram, vert);
            GL.DetachShader(shaderProgram, frag);
            GL.DeleteShader(vert);
            GL.DeleteShader(frag);
        }

        public void Use(Mesh mesh, Camera cam, float width, float height)
        {
            GL.UseProgram(shaderProgram);
            BindVector4(mesh.Color, "color");
            BindMatrix(mesh.GetModelMatrix(), "modelMatrix");
            BindMatrix(cam.GetViewMatrix(), "viewMatrix");
            BindMatrix(cam.GetProjectionMatrix(width, height), "projMatrix");
        }

        private void BindMatrix(Matrix4 matrix, string name)
        {
            int location = GL.GetUniformLocation(shaderProgram, name);
            GL.UniformMatrix4(location, false, ref matrix);
        }

        private void BindVector4(Vector4 vector, string name)
        {
            int location = GL.GetUniformLocation(shaderProgram, name);
            GL.Uniform4(location, vector);
        }

        private int CompileShader(string path, ShaderType type)
        {
            // TODO: error checking
            string source;
            using (StreamReader sr = new StreamReader(path, Encoding.UTF8))
            {
                source = sr.ReadToEnd();
            }

            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);
            return shader;
        }

        #region Disposing
        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                GL.DeleteProgram(shaderProgram);
                disposed = true;
            }
        }

        ~Shader()
        {
            GL.DeleteProgram(shaderProgram);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
