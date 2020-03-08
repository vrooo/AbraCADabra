using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using System.Drawing;

using OpenTK.Graphics.OpenGL;

namespace AbraCADabra
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Shader shader;
        string vertPath = "../../Shaders/mvp.vert";
        string fragPath = "../../Shaders/oneColor.frag";

        Camera camera;
        Mesh mesh;
        PlaneXZ plane;

        System.Drawing.Point prevLocation;
        float cameraRotateSpeed = 0.02f;
        float cameraTranslateSpeed = 0.01f;
        float cameraScrollSpeed = 0.01f;
        float shiftModifier = 10.0f;

        public MainWindow()
        {
            InitializeComponent();
        }

        #region Old events
        private void RenderFrame(object sender, EventArgs e)
        {

        }

        private void SliderRedraw(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //redraw = true;
        }

        private void SizeRedraw(object sender, SizeChangedEventArgs e)
        {
            //redraw = true;
        }

        private void RadioRedraw(object sender, RoutedEventArgs e)
        {
            //redraw = true;
        }

        private void MouseWheelTransform(object sender, MouseWheelEventArgs e)
        {
            //translationZ += e.Delta * wheelMult;
            //redraw = true;
        }

        #endregion

        private void OnMouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            float diffX = e.X - prevLocation.X;
            float diffY = e.Y - prevLocation.Y;

            float speedModifier = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)
                ? shiftModifier : 1.0f;

            if (e.Button.HasFlag(MouseButtons.Left))
            {
                float speed = cameraTranslateSpeed * speedModifier;
                camera.Translate(diffX * speed, -diffY * speed, 0);
                GLMain.Invalidate();
            }

            if (e.Button.HasFlag(MouseButtons.Right))
            {
                float speed = cameraRotateSpeed * speedModifier;
                camera.Rotate(diffY * speed, diffX * speed, 0);
                GLMain.Invalidate();
            }

            prevLocation = e.Location;
        }

        private void OnMouseScroll(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            float speedModifier = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)
                ? shiftModifier : 1.0f;
            float speed = cameraScrollSpeed * speedModifier;
            camera.Translate(0, 0, e.Delta * speed);
            GLMain.Invalidate();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            shader = new Shader(vertPath, fragPath);
            GL.ClearColor(Color.Black);
            GL.Enable(EnableCap.DepthTest);

            camera = new Camera(0, 0, -7.0f, 0.5f, 0, 0);
            mesh = new Cube();
            plane = new PlaneXZ(100.0f, 100.0f, 100, 100);
        }

        private void OnRender(object sender, PaintEventArgs e)
        {
            GL.Viewport(0, 0, GLMain.Width, GLMain.Height);
            GL.Clear(ClearBufferMask.ColorBufferBit | 
                     ClearBufferMask.DepthBufferBit);

            shader.Use(mesh, camera, GLMain.Width, GLMain.Height);
            mesh.Render();
            shader.Use(plane, camera, GLMain.Width, GLMain.Height);
            plane.Render();

            GLMain.SwapBuffers();
        }

        private void OnDisposed(object sender, EventArgs e)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            mesh.Dispose();
            plane.Dispose();
            shader.Dispose();
        }
    }
}
