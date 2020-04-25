using System;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using Xceed.Wpf.Toolkit;
using System.Windows.Controls;
using System.Linq;

namespace AbraCADabra
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const uint wireframeMax = 100;

        ShaderManager shader;
        string vertPath = "../../Shaders/basic.vert";
        string fragPath = "../../Shaders/basic.frag";
        string vertPathAdapt = "../../Shaders/bezier.vert";
        string fragPathAdapt = "../../Shaders/basic.frag";
        string geomPathAdapt = "../../Shaders/bezier.geom";

        Camera camera;
        PlaneXZ plane;
        Cursor cursor;
        CenterMarker centerMarker;
        ObservableCollection<TransformManager> objects = new ObservableCollection<TransformManager>();

        System.Drawing.Point prevLocation;
        float rotateSpeed = 0.02f;
        float translateSpeed = 0.04f;
        float scrollSpeed = 0.02f;
        float scaleSpeed = 0.001f;
        float shiftModifier = 10.0f;

        bool renderFromScreenCoordsChange = false;
        bool renderFromWorldCoordsChange = false;

        PropertyWindow propertyWindow;

        public MainWindow()
        {
            InitializeComponent();

            ListObjects.DataContext = objects;
        }

        public IEnumerable<TransformManager> GetObjectsOfType(Type type)
        {
            return objects.Where(tm => tm.GetType() == type);
        }

        private void OnLoad(object sender, EventArgs e)
        {
            GL.ClearColor(0.05f, 0.05f, 0.15f, 1.0f);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.PointSmooth);

            camera = new Camera(0, 5.0f, -40.0f, 0.3f, 0, 0);
            plane = new PlaneXZ(200.0f, 200.0f, 200, 200);
            cursor = new Cursor();
            centerMarker = new CenterMarker();

            shader = new ShaderManager(vertPath, fragPath, vertPathAdapt, fragPathAdapt, geomPathAdapt, camera, GLMain);

            objects.Add(new TorusManager(cursor.Position, wireframeMax, wireframeMax));
        }

        private void OnRender(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            GL.Viewport(0, 0, GLMain.Width, GLMain.Height);
            GL.Clear(ClearBufferMask.ColorBufferBit |
                     ClearBufferMask.DepthBufferBit);
            shader.UseBasic();

            if (CheckBoxGrid.IsChecked.HasValue && CheckBoxGrid.IsChecked.Value)
            {
                plane.Render(shader);
            }

            foreach (var ob in objects)
            {
                ob.Render(shader);
            }

            UpdateCenterMarker();
            centerMarker.Render(shader);

            cursor.Render(shader);
            if (!renderFromScreenCoordsChange)
            {
                UpdateCursorScreenPos();
            }
            if (!renderFromWorldCoordsChange)
            {
                UpdateCursorWorldPos();
            }
            renderFromScreenCoordsChange = false;
            renderFromWorldCoordsChange = false;

            GLMain.SwapBuffers();
        }

        private void UpdateCursorScreenPos()
        {
            var coords = cursor.GetScreenSpaceCoords(camera, GLMain.Width, GLMain.Height);

            UpdateDecimalValue(DecimalScreenX, DecimalScreenValueChanged, coords.X);
            UpdateDecimalValue(DecimalScreenY, DecimalScreenValueChanged, coords.Y);
            UpdateDecimalValue(DecimalScreenZ, DecimalScreenValueChanged, coords.Z);
        }

        private void UpdateCursorWorldPos()
        {
            UpdateDecimalValue(DecimalWorldX, DecimalWorldValueChanged, cursor.Position.X);
            UpdateDecimalValue(DecimalWorldY, DecimalWorldValueChanged, cursor.Position.Y);
            UpdateDecimalValue(DecimalWorldZ, DecimalWorldValueChanged, cursor.Position.Z);
        }

        private void UpdateDecimalValue(DecimalUpDown dud, RoutedPropertyChangedEventHandler<object> handler, float value)
        {
            dud.ValueChanged -= handler;
            dud.Value = (decimal?)value;
            dud.ValueChanged += handler;
        }

        private void RefreshView()
        {
            GLMain.Invalidate();
        }

        private void OnMouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            float diffX = e.X - prevLocation.X;
            float diffY = e.Y - prevLocation.Y;

            float speedModifier = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)
                ? shiftModifier : 1.0f;

            bool controlHeld = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

            if (e.Button.HasFlag(System.Windows.Forms.MouseButtons.Left))
            {
                float speed = translateSpeed * speedModifier;
                if (controlHeld)
                {
                    Vector4 translation = camera.GetRotationMatrix() * new Vector4(diffX * speed, -diffY * speed, 0, 1);
                    foreach (var ob in ListObjects.SelectedItems)
                    {
                        (ob as TransformManager).Translate(translation.X, translation.Y, translation.Z);
                    }
                }
                else
                {
                    camera.Translate(diffX * speed, -diffY * speed, 0);
                }
                RefreshView();
            }

            if (e.Button.HasFlag(System.Windows.Forms.MouseButtons.Right))
            {
                float speed = rotateSpeed * speedModifier;
                if (controlHeld)
                {
                    Vector3 center = (CheckBoxRotateOrigin.IsChecked.HasValue && CheckBoxRotateOrigin.IsChecked.Value) ?
                                     cursor.Position : centerMarker.Position;
                    foreach (var ob in ListObjects.SelectedItems)
                    {
                        (ob as TransformManager).RotateAround(diffY * speed, diffX * speed, 0, center);
                    }
                }
                else
                {
                    camera.Rotate(diffY * speed, diffX * speed, 0);
                }
                RefreshView();
            }

            if (e.Button.HasFlag(System.Windows.Forms.MouseButtons.Middle))
            {
                float speed = translateSpeed * speedModifier;
                Vector4 translation = camera.GetRotationMatrix() * new Vector4(diffX * speed, -diffY * speed, 0, 1);
                cursor.Translate(translation.X, translation.Y, translation.Z);
                RefreshView();
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
                foreach (var ob in ListObjects.SelectedItems)
                {
                    (ob as TransformManager).ScaleUniform(e.Delta * speed);
                }
            }
            else
            {
                float speed = scrollSpeed * speedModifier;
                camera.Translate(0, 0, e.Delta * speed);
            }
            RefreshView();
        }

        private void OnMouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            TransformManager clo = null;
            float cloZ = camera.ZFar;
            foreach (var ob in objects)
            {
                if (ob.TestHit(camera, GLMain.Width, GLMain.Height, e.X, e.Y, out float z))
                {
                    if (z > 0 && z < cloZ)
                    {
                        clo = ob;
                        cloZ = z;
                    }
                }
            }
            if (clo != null)
            {
                if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    ListObjects.SelectedItems.Clear();
                }
                ListObjects.SelectedItems.Add(clo);
            }
        }

        private void OnDisposed(object sender, EventArgs e)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            objects[0].Transform.Dispose();
            plane.Dispose();
            shader.Dispose();
        }

        private void RoutedInvalidate(object sender, RoutedEventArgs e)
        {
            RefreshView();
        }

        private void ListBoxItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (propertyWindow == null)
            {
                propertyWindow = new PropertyWindow()
                {
                    Owner = this
                };
                propertyWindow.Closed += PropertyWindowClosed;
                propertyWindow.PropertyUpdated += PropertyWindowUpdate;
            }
            UpdatePropertyWindowContext();
            propertyWindow.Show();
        }

        private void UpdateCenterMarker()
        {
            if (centerMarker != null && centerMarker.Visible)
            {
                int count = 0;
                Vector3 center = new Vector3();
                foreach (var ob in ListObjects.SelectedItems)
                {
                    count++;
                    center += (ob as TransformManager).Transform.Position;
                }
                center /= count;
                centerMarker.Position = center;
            }
        }

        private void ListObjectsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListObjects.SelectedItems.Count > 0)
            {
                centerMarker.Visible = true;
            }
            else
            {
                centerMarker.Visible = false;
            }
            UpdatePropertyWindowContext();
            RefreshView();
        }

        private void UpdatePropertyWindowContext()
        {
            if (propertyWindow != null)
            {
                if (ListObjects.SelectedItems.Count != 1)
                {
                    propertyWindow.DataContext = null;
                }
                else
                {
                    propertyWindow.DataContext = ListObjects.SelectedItem;
                }
            }
        }

        private void PropertyWindowClosed(object sender, EventArgs e)
        {
            propertyWindow.Closed -= PropertyWindowClosed;
            propertyWindow.PropertyUpdated -= PropertyWindowUpdate;
            propertyWindow = null;
        }

        private void PropertyWindowUpdate(TransformManager context)
        {
            context.Update();
            RefreshView();
        }

        private void ButtonCreatePoint(object sender, RoutedEventArgs e)
        {
            var point = new PointManager(cursor.Position);
            objects.Add(point);

            foreach (var ob in ListObjects.SelectedItems)
            {
                if (ob is Bezier3Manager bm)
                {
                    bm.AddPoint(point);
                }
            }

            RefreshView();
        }

        private void ButtonCreateTorus(object sender, RoutedEventArgs e)
        {
            objects.Add(new TorusManager(cursor.Position, wireframeMax, wireframeMax));
            RefreshView();
        }

        private void ButtonCreateBezier3C0(object sender, RoutedEventArgs e)
        {
            objects.Add(new Bezier3C0Manager(GetSelectedPoints()));
            RefreshView();
        }

        private void ButtonCreateBezier3C2(object sender, RoutedEventArgs e)
        {
            objects.Add(new Bezier3C2Manager(GetSelectedPoints()));
            RefreshView();
        }

        private void ButtonCreateBezier3Inter(object sender, RoutedEventArgs e)
        {
            objects.Add(new Bezier3InterManager(GetSelectedPoints()));
            RefreshView();
        }

        private List<PointManager> GetSelectedPoints()
        {
            List<PointManager> points = new List<PointManager>();
            foreach (var ob in ListObjects.SelectedItems)
            {
                if (ob is PointManager)
                {
                    points.Add(ob as PointManager);
                }
            }
            return points;
        }

        private void DecimalWorldValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (cursor != null && DecimalWorldX.Value.HasValue && DecimalWorldY.Value.HasValue && DecimalWorldZ.Value.HasValue)
            {
                cursor.Position = new Vector3((float)DecimalWorldX.Value.Value,
                                              (float)DecimalWorldY.Value.Value,
                                              (float)DecimalWorldZ.Value.Value);
                renderFromWorldCoordsChange = true;
                RefreshView();
            }
        }

        private void DecimalScreenValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (cursor != null && DecimalScreenX.Value.HasValue && DecimalScreenY.Value.HasValue && DecimalScreenZ.Value.HasValue)
            {
                var coords = new Vector4((float)DecimalScreenX.Value.Value,
                                         (float)DecimalScreenY.Value.Value,
                                         (float)DecimalScreenZ.Value.Value, 1.0f);
                coords.X = 2.0f * coords.X / GLMain.Width - 1.0f;
                coords.Y = -(2.0f * coords.Y / GLMain.Height - 1.0f);
                coords.Z = coords.Z == 0.0f ? 0.0f : (camera.ZFar * (camera.ZNear - coords.Z)) / (coords.Z * (camera.ZNear - camera.ZFar));

                Matrix4 view = camera.GetViewMatrix(),
                        proj = camera.GetProjectionMatrix(GLMain.Width, GLMain.Height);
                Matrix4 inv = Matrix4.Invert(view * proj);
                coords *= inv;
                coords /= coords.W;

                cursor.Position = new Vector3(coords.X, coords.Y, coords.Z);

                renderFromScreenCoordsChange = true;
                RefreshView();
            }
        }

        private void ButtonDelete(object sender, RoutedEventArgs e)
        {
            var selected = ListObjects.SelectedItems;
            if (selected.Count > 0)
            {
                var res = System.Windows.MessageBox.Show("Are you sure you want to delete all selected items?",
                                                         "Delete", MessageBoxButton.OKCancel);
                if (res == MessageBoxResult.OK)
                {
                    for (int i = selected.Count - 1; i >= 0; i--)
                    {
                        var removing = selected[i] as TransformManager;
                        objects.Remove(removing);
                        removing.Dispose();
                    }
                    RefreshView();
                }
            }
        }

        private void ButtonMoveUp(object sender, RoutedEventArgs e)
        {
            int index = ListObjects.SelectedIndex;
            if (ListObjects.SelectedItems.Count == 1 && index > 0)
            {
                objects.Move(index, index - 1);
            }
        }

        private void ButtonMoveDown(object sender, RoutedEventArgs e)
        {
            int index = ListObjects.SelectedIndex;
            if (ListObjects.SelectedItems.Count == 1 && index < objects.Count - 1)
            {
                objects.Move(index, index + 1);
            }
        }

        private void Debug_FourPoints(object sender, RoutedEventArgs e)
        {
            objects.Clear();
            objects.Add(new PointManager(new Vector3(-10.0f, -10.0f, 1.0f)));
            objects.Add(new PointManager(new Vector3(-3.0f, -3.0f, 4.0f)));
            objects.Add(new PointManager(new Vector3(4.0f, -12.0f, 2.0f)));
            objects.Add(new PointManager(new Vector3(11.0f, -4.0f, 3.0f)));
            CheckBoxGrid.IsChecked = false;
        }
    }
}
