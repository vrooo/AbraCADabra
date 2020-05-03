using System;
using System.IO;
using System.Text;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace AbraCADabra
{
    public class Shader
    {
        private int program;

        private bool disposed = false;

        public Shader(string vertPath, string fragPath, string geomPath = null)
        {
            program = GL.CreateProgram();
            bool doGeom = !string.IsNullOrEmpty(geomPath);

            int vert = CompileShader(vertPath, ShaderType.VertexShader);
            GL.AttachShader(program, vert);
            int frag = CompileShader(fragPath, ShaderType.FragmentShader);
            GL.AttachShader(program, frag);

            int geom = -1;
            if (doGeom)
            {
                geom = CompileShader(geomPath, ShaderType.GeometryShader);
                GL.AttachShader(program, geom);
            }

            GL.LinkProgram(program);
            GL.DetachShader(program, vert);
            GL.DeleteShader(vert);
            GL.DetachShader(program, frag);
            GL.DeleteShader(frag);

            if (doGeom)
            {
                GL.DetachShader(program, geom);
                GL.DeleteShader(geom);
            }
        }

        public void Use()
        {
            GL.UseProgram(program);
        }

        public void BindMatrix(Matrix4 matrix, string name)
        {
            int location = GL.GetUniformLocation(program, name);
            GL.UniformMatrix4(location, false, ref matrix);
        }

        public void BindVector4(Vector4 vector, string name)
        {
            int location = GL.GetUniformLocation(program, name);
            GL.Uniform4(location, vector);
        }

        public void BindInt(int obj, string name)
        {
            int location = GL.GetUniformLocation(program, name);
            GL.Uniform1(location, obj);
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
                GL.DeleteProgram(program);
                disposed = true;
            }
        }

        ~Shader()
        {
            GL.DeleteProgram(program);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
