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
        Torus torus;
        PlaneXZ plane;

        System.Drawing.Point prevLocation;
        float cameraRotateSpeed = 0.02f;
        float cameraTranslateSpeed = 0.02f;
        float cameraScrollSpeed = 0.01f;
        float shiftModifier = 10.0f;

        public MainWindow()
        {
            InitializeComponent();

            SliderMajorR.ValueChanged += SliderTorusUpdate;
            SliderMinorR.ValueChanged += SliderTorusUpdate;
            SliderVerticalSlices.ValueChanged += SliderTorusUpdate;
            SliderHorizontalSlices.ValueChanged += SliderTorusUpdate;
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

            bool moveTorus = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

            if (e.Button.HasFlag(MouseButtons.Left))
            {
                float speed = cameraTranslateSpeed * speedModifier;
                if (moveTorus)
                {
                    // TODO: relative to camera
                    torus.Translate(diffX * speed, -diffY * speed, 0);
                }
                else
                {
                    camera.Translate(diffX * speed, -diffY * speed, 0);
                }
                GLMain.Invalidate();
            }

            if (e.Button.HasFlag(MouseButtons.Right))
            {
                float speed = cameraRotateSpeed * speedModifier;
                if (moveTorus)
                {
                    torus.Rotate(diffY * speed, diffX * speed, 0);
                }
                else
                {
                    camera.Rotate(diffY * speed, diffX * speed, 0);
                }
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
            GL.ClearColor(0.05f, 0.05f, 0.15f, 1.0f);
            GL.Enable(EnableCap.DepthTest);

            camera = new Camera(0, 0, -20.0f, 0.5f, 0, 0);
            torus = new Torus((float)SliderMajorR.Value, (float)SliderMinorR.Value,
                              (uint)SliderVerticalSlices.Value, (uint)SliderHorizontalSlices.Value,
                              (uint)SliderVerticalSlices.Maximum, (uint)SliderHorizontalSlices.Maximum);
            plane = new PlaneXZ(200.0f, 200.0f, 200, 200);
        }

        private void OnRender(object sender, PaintEventArgs e)
        {
            GL.Viewport(0, 0, GLMain.Width, GLMain.Height);
            GL.Clear(ClearBufferMask.ColorBufferBit | 
                     ClearBufferMask.DepthBufferBit);

            shader.Use(torus, camera, GLMain.Width, GLMain.Height);
            torus.Render();
            if (CheckBoxGrid.IsChecked.HasValue && CheckBoxGrid.IsChecked.Value)
            {
                shader.Use(plane, camera, GLMain.Width, GLMain.Height);
                plane.Render();
            }

            GLMain.SwapBuffers();
        }

        private void OnDisposed(object sender, EventArgs e)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            torus.Dispose();
            plane.Dispose();
            shader.Dispose();
        }

        private void RoutedInvalidate(object sender, RoutedEventArgs e)
        {
            GLMain.Invalidate();
        }

        private void SliderTorusUpdate(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            torus?.Update((float)SliderMajorR.Value, (float)SliderMinorR.Value, 
                          (uint)SliderVerticalSlices.Value, (uint)SliderHorizontalSlices.Value);
            GLMain.Invalidate();
        }
    }
}
