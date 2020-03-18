using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using System.Collections.ObjectModel;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace AbraCADabra
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const uint wireframeMax = 100;

        Shader shader;
        string vertPath = "../../Shaders/mvp.vert";
        string fragPath = "../../Shaders/oneColor.frag";

        Camera camera;
        PlaneXZ plane;
        ObservableCollection<MeshManager> objects = new ObservableCollection<MeshManager>();

        System.Drawing.Point prevLocation;
        float rotateSpeed = 0.02f;
        float translateSpeed = 0.04f;
        float scrollSpeed = 0.02f;
        float scaleSpeed = 0.001f;
        float shiftModifier = 10.0f;

        PropertyWindow propertyWindow;

        public MainWindow()
        {
            InitializeComponent();

            ListObjects.DataContext = objects;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            shader = new Shader(vertPath, fragPath);
            GL.ClearColor(0.05f, 0.05f, 0.15f, 1.0f);
            GL.Enable(EnableCap.DepthTest);

            camera = new Camera(0, 5.0f, -40.0f, 0.3f, 0, 0);
            objects.Add(new TorusManager(wireframeMax, wireframeMax));
            //GroupTorus.DataContext = objects[0] as TorusManager;
            plane = new PlaneXZ(200.0f, 200.0f, 200, 200);
        }

        private void OnRender(object sender, PaintEventArgs e)
        {
            GL.Viewport(0, 0, GLMain.Width, GLMain.Height);
            GL.Clear(ClearBufferMask.ColorBufferBit |
                     ClearBufferMask.DepthBufferBit);

            foreach (var ob in objects)
            {
                shader.Use(ob.Mesh, camera, GLMain.Width, GLMain.Height);
                ob.Mesh.Render();
            }

            if (CheckBoxGrid.IsChecked.HasValue && CheckBoxGrid.IsChecked.Value)
            {
                shader.Use(plane, camera, GLMain.Width, GLMain.Height);
                plane.Render();
            }

            GLMain.SwapBuffers();
        }

        private void OnMouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            float diffX = e.X - prevLocation.X;
            float diffY = e.Y - prevLocation.Y;

            float speedModifier = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)
                ? shiftModifier : 1.0f;

            bool moveTorus = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

            if (e.Button.HasFlag(MouseButtons.Left))
            {
                float speed = translateSpeed * speedModifier;
                if (moveTorus)
                {
                    Vector4 translation = camera.GetRotationMatrix() * new Vector4(diffX * speed, -diffY * speed, 0, 1);
                    objects[0].Mesh.Translate(translation.X, translation.Y, translation.Z);
                }
                else
                {
                    camera.Translate(diffX * speed, -diffY * speed, 0);
                }
                GLMain.Invalidate();
            }

            if (e.Button.HasFlag(MouseButtons.Right))
            {
                float speed = rotateSpeed * speedModifier;
                if (moveTorus)
                {
                    objects[0].Mesh.Rotate(diffY * speed, diffX * speed, 0);
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

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                float speed = scaleSpeed * speedModifier;
                objects[0].Mesh.ScaleUniform(e.Delta * speed);
            }
            else
            {
                float speed = scrollSpeed * speedModifier;
                camera.Translate(0, 0, e.Delta * speed);
            }
            GLMain.Invalidate();
        }

        private void OnDisposed(object sender, EventArgs e)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            objects[0].Mesh.Dispose();
            plane.Dispose();
            shader.Dispose();
        }

        private void RoutedInvalidate(object sender, RoutedEventArgs e)
        {
            GLMain.Invalidate();
        }

        private void ListBoxItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (propertyWindow == null)
            {
                propertyWindow = new PropertyWindow()
                {
                    DataContext = (sender as FrameworkElement).DataContext,
                    Owner = this
                };
                propertyWindow.Closed += PropertyWindowClosed;
                propertyWindow.PropertyUpdated += PropertyWindowUpdate;
                propertyWindow.Show();
            }
        }

        private void PropertyWindowClosed(object sender, EventArgs e)
        {
            propertyWindow.Closed -= PropertyWindowClosed;
            propertyWindow.PropertyUpdated -= PropertyWindowUpdate;
            propertyWindow = null;
        }

        private void PropertyWindowUpdate(MeshManager context)
        {
            context.Update();
            GLMain.Invalidate();
        }

        private void ButtonCreatePoint(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonCreateTorus(object sender, RoutedEventArgs e)
        {

        }

        //private void ListBoxItemFocus(object sender, RoutedEventArgs e)
        //{
        //    // TODO: focus textbox after showing
        //    var dob = sender as DependencyObject;
        //    while (!(dob is StackPanel))
        //    {
        //        dob = VisualTreeHelper.GetChild(dob, 0);
        //    }
        //    var sp = dob as StackPanel;
        //    var tb = sp.Children.OfType<System.Windows.Controls.TextBox>().FirstOrDefault();
        //    if (tb.Visibility == Visibility.Visible)
        //    {
        //        tb?.Focus();
        //    }
        //}
    }
}
