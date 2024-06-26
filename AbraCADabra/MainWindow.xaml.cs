﻿using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using Xceed.Wpf.Toolkit;
using System.Windows.Controls;
using System.Linq;
using System.IO;
using Microsoft.Win32;
using AbraCADabra.Serialization;
using System.Xml.Serialization;
using AbraCADabra.Milling;
using System.Windows.Threading;
using OpenTK.Graphics;

namespace AbraCADabra
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private enum MergeMode
        {
            ToFirst, ToLast, ToCursor, ToCenter
        }

        const uint WIREFRAME_MAX = 100;
        const int SPHERICAL_INDEX = 0, FLAT_INDEX = 1;

        ShaderManager shader;

        int anaglyphFrameBufferLeft, anaglyphFrameBufferRight;
        int anaglyphRenderBufferLeft, anaglyphRenderBufferRight;
        int anaglyphTexLeft, anaglyphTexRight;
        Quad anaglyphQuad;

        Camera camera;
        Vector3 cameraDefPosition;
        Vector3 cameraDefRotation;

        Color4 defBackgroundColor;

        PlaneXZ plane;
        CrossCursor cursor;
        CenterMarker centerMarker;
        ObservableCollection<TransformManager> objects = new ObservableCollection<TransformManager>();

        System.Drawing.Point prevLocation;
        float rotateSpeed = 0.01f;
        float translateSpeed = 0.04f;
        float scrollCamSpeed = 0.02f;
        float scrollMoveSpeed = 0.005f;
        float scrollScaleSpeed = 0.001f;
        float shiftModifier = 10.0f;

        System.Drawing.Point boxSelectStart;
        bool isBoxSelecting = false;

        bool renderFromScreenCoordsChange = false;
        bool renderFromWorldCoordsChange = false;

        PropertyWindow propertyWindow;
        string currentDirectory = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../../../"));

        public double MillingStepDelay { get; set; } = 0.001;
        MillingManager millingManager;
        DispatcherTimer millingTimer = new DispatcherTimer();
        MillingManager.GraphVisualizer graphVis;

        int pathHeightFrameBuffer;
        int pathHeightRenderBuffer;
        int pathHeightTex;

        public MainWindow()
        {
            InitializeComponent();

            ListObjects.DataContext = objects;
            ContextMergeToFirst.Click += (sender, e) => MergePoints(MergeMode.ToFirst);
            ContextMergeToLast.Click += (sender, e) => MergePoints(MergeMode.ToLast);
            ContextMergeToCursor.Click += (sender, e) => MergePoints(MergeMode.ToCursor);
            ContextMergeToCenter.Click += (sender, e) => MergePoints(MergeMode.ToCenter);
        }

        public IEnumerable<TransformManager> GetObjectsOfType(Type type) => objects.Where(tm => tm.GetType() == type);

        private void OnLoad(object sender, EventArgs e)
        {
            defBackgroundColor = new Color4(0.05f, 0.05f, 0.15f, 1.0f);
            GL.ClearColor(defBackgroundColor);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.PointSmooth);

            anaglyphFrameBufferLeft = GL.GenFramebuffer();
            anaglyphFrameBufferRight = GL.GenFramebuffer();
            anaglyphRenderBufferLeft = GL.GenRenderbuffer();
            anaglyphRenderBufferRight = GL.GenRenderbuffer();
            anaglyphTexLeft = GL.GenTexture();
            anaglyphTexRight = GL.GenTexture();
            anaglyphQuad = new Quad();

            cameraDefPosition = new Vector3(0.0f, 5.0f, -40.0f);
            cameraDefRotation = new Vector3(0.3f, 0.0f, 0.0f);
            camera = new Camera(cameraDefPosition, cameraDefRotation);

            plane = new PlaneXZ(200.0f, 200.0f, 200, 200);
            cursor = new CrossCursor();
            centerMarker = new CenterMarker();

            shader = new ShaderManager(camera, GLMain);

            millingManager = new MillingManager();
            MenuMilling.DataContext = millingManager;
            ExpanderMilling.DataContext = millingManager;
            millingManager.PropertyChanged += MillingPropertyChanged;
            millingTimer.Tick += OnMillingTimer;

            pathHeightFrameBuffer = GL.GenFramebuffer();
            pathHeightRenderBuffer = GL.GenRenderbuffer();
            pathHeightTex = GL.GenTexture();
        }

        private void OnRender(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            //shader.EnableHeightMode(15, 5, 15); // debug
            GL.Viewport(0, 0, GLMain.Width, GLMain.Height);
            GL.Clear(ClearBufferMask.ColorBufferBit |
                     ClearBufferMask.DepthBufferBit);

            if (CheckBoxAnaglyph.IsChecked == true)
            {
                float eye = (float)SliderEyeDistance.Value, plane = (float)SliderProjDistance.Value;

                GL.ActiveTexture(TextureUnit.Texture0);
                SetRenderTexture(anaglyphTexLeft, anaglyphFrameBufferLeft, anaglyphRenderBufferLeft,
                    GLMain.Width, GLMain.Height, PixelInternalFormat.Rgb, OpenTK.Graphics.OpenGL.PixelFormat.Rgb, PixelType.UnsignedByte);
                shader.SetAnaglyphMode(AnaglyphMode.Left, eye, plane, false);
                shader.UseBasic();
                RenderScene();

                GL.ActiveTexture(TextureUnit.Texture1);
                SetRenderTexture(anaglyphTexRight, anaglyphFrameBufferRight, anaglyphRenderBufferRight,
                    GLMain.Width, GLMain.Height, PixelInternalFormat.Rgb, OpenTK.Graphics.OpenGL.PixelFormat.Rgb, PixelType.UnsignedByte);
                shader.SetAnaglyphMode(AnaglyphMode.Right, eye, plane);
                RenderScene();

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                GL.Clear(ClearBufferMask.ColorBufferBit |
                         ClearBufferMask.DepthBufferBit);
                shader.SetAnaglyphMode(AnaglyphMode.None, eye, plane, false);
                shader.UseMultitex();

                var lCol = ColorPickerLeft.SelectedColor.Value;
                var rCol = ColorPickerRight.SelectedColor.Value;
                shader.SetupAnaglyphColors(new Vector4(lCol.R / 255.0f, lCol.G / 255.0f, lCol.B / 255.0f, 1.0f),
                                           new Vector4(rCol.R / 255.0f, rCol.G / 255.0f, rCol.B / 255.0f, 1.0f));
                anaglyphQuad.Render(shader);
                shader.UseBasic();
            }
            else
            {
                shader.UseBasic();
                RenderScene();
            }

            renderFromScreenCoordsChange = false;
            renderFromWorldCoordsChange = false;

            GLMain.SwapBuffers();
        }

        private void SetupTexture(int tex, int width, int height,
            PixelInternalFormat internalFormat, OpenTK.Graphics.OpenGL.PixelFormat pixelFormat, PixelType pixelType)
        {
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.TexImage2D(TextureTarget.Texture2D,
                          0,
                          internalFormat,
                          width, height, 0,
                          pixelFormat, pixelType,
                          IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
        }

        private void SetRenderTexture(int tex, int fbo, int dbo, int width, int height,
            PixelInternalFormat internalFormat, OpenTK.Graphics.OpenGL.PixelFormat pixelFormat, PixelType pixelType)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

            GL.BindTexture(TextureTarget.Texture2D, tex);
            SetupTexture(tex, width, height, internalFormat, pixelFormat, pixelType);

            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, dbo);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer,
                                   RenderbufferStorage.DepthComponent,
                                   width, height);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                                       FramebufferAttachment.DepthAttachment,
                                       RenderbufferTarget.Renderbuffer,
                                       dbo);

            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, tex, 0);
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

            GL.Clear(ClearBufferMask.ColorBufferBit |
                     ClearBufferMask.DepthBufferBit);
        }

        private void RenderScene()
        {
            if (CheckBoxGrid.IsChecked == true)
            {
                plane.Render(shader);
            }

            if (CheckBoxObjects.IsChecked == true)
            {
                foreach (var ob in objects)
                {
                    if (!(ob is PointManager) || CheckBoxPoints.IsChecked == true)
                        ob.Render(shader);
                }
            }

            if (graphVis != null)
            {
                graphVis.Render(shader);
            }

            millingManager.Render(shader);

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
        }

        private float[,] SetupObjectHeightMap(IEnumerable<PatchManager> patches)
        {
            int texWidth = 500, texHeight = 500; // TODO: values from params window?
            GL.ActiveTexture(TextureUnit.Texture2);
            SetRenderTexture(pathHeightTex, pathHeightFrameBuffer, pathHeightRenderBuffer, texWidth, texHeight,
                             PixelInternalFormat.R32f, OpenTK.Graphics.OpenGL.PixelFormat.Red, PixelType.Float);

            GL.Viewport(0, 0, texWidth, texHeight);
            GL.ClearColor(0, 0, 0, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit |
                     ClearBufferMask.DepthBufferBit);
            shader.EnableHeightMode(millingManager.SizeX, millingManager.SizeY, millingManager.SizeZ);

            foreach (var pm in patches)
            {
                pm.RenderFilled(shader);
            }

            GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
            var tmpPixels = new float[texWidth * texHeight];
            GL.ReadPixels(0, 0, texWidth, texHeight, OpenTK.Graphics.OpenGL.PixelFormat.Red, PixelType.Float, tmpPixels);
            int ind = 0;
            var pixels = new float[texWidth, texHeight];
            // order of loops is important!
            for (int i = texHeight - 1; i >= 0; i--)
            {
                for (int j = 0; j < texWidth; j++)
                {
                    pixels[j, i] = tmpPixels[ind++];
                }
            }

            shader.DisableHeightMode();
            GL.ClearColor(defBackgroundColor);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            shader.UseBasic();

            return pixels;
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

        private void RefreshView() => GLMain.Invalidate();

        private void OnMouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (isBoxSelecting)
            {
                prevLocation = e.Location;
                return;
            }
            float diffX = e.X - prevLocation.X;
            float diffY = e.Y - prevLocation.Y;

            float speedModifier = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)
                ? shiftModifier : 1.0f;

            bool controlHeld = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            bool xHeld = Keyboard.IsKeyDown(Key.X);
            bool yHeld = Keyboard.IsKeyDown(Key.Y);
            bool zHeld = Keyboard.IsKeyDown(Key.Z);

            if (e.Button.HasFlag(System.Windows.Forms.MouseButtons.Left))
            {
                float speed = translateSpeed * speedModifier;
                if (controlHeld)
                {
                    Vector4 translation = camera.GetRotationMatrix() * new Vector4(diffX * speed, -diffY * speed, 0, 1);
                    if (xHeld) translation.Y = translation.Z = 0.0f;
                    else if (yHeld) translation.X = translation.Z = 0.0f;
                    else if (zHeld) translation.X = translation.Y = 0.0f;
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
                    Vector3 center = (CheckBoxRotateOrigin.IsChecked == true) ?
                                     cursor.Position : centerMarker.Position;
                    foreach (var ob in ListObjects.SelectedItems)
                    {
                        float xrot = diffY * speed, yrot = diffX * speed, zrot = 0.0f;
                        if (xHeld) yrot = 0.0f;
                        else if (yHeld) xrot = 0.0f;
                        else if (zHeld)
                        {
                            xrot = yrot = 0.0f;
                            zrot = diffY * speed;
                        }
                        (ob as TransformManager).RotateAround(xrot, yrot, zrot, center);
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
            bool xHeld = Keyboard.IsKeyDown(Key.X);
            bool yHeld = Keyboard.IsKeyDown(Key.Y);
            bool zHeld = Keyboard.IsKeyDown(Key.Z);

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (ListObjects.SelectedItems.Count == 1)
                {
                    float speed = scrollScaleSpeed * speedModifier;
                    (ListObjects.SelectedItem as TransformManager).ScaleUniform(e.Delta * speed);
                }
                else if (ListObjects.SelectedItems.Count > 1)
                {
                    float speed = scrollMoveSpeed * speedModifier;
                    Vector3 center = (CheckBoxRotateOrigin.IsChecked == true) ?
                                     cursor.Position : centerMarker.Position;
                    foreach (var ob in ListObjects.SelectedItems)
                    {
                        var tm = ob as TransformManager;
                        Vector3 translation = new Vector3(tm.PositionX, tm.PositionY, tm.PositionZ) - center;
                        if (translation.Length < 120 * scrollMoveSpeed && e.Delta < 0)
                        {
                            tm.Translate(-translation.X, -translation.Y, -translation.Z);
                        }
                        else if (translation.Length > scrollMoveSpeed)
                        {
                            translation.Normalize();
                            translation *= e.Delta * speed;
                            tm.Translate(translation.X, translation.Y, translation.Z);
                        }
                    }
                }
            }
            else
            {
                float speed = scrollCamSpeed * speedModifier;
                camera.Translate(0, 0, e.Delta * speed);
            }
            RefreshView();
        }

        private void OnMouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Q)) // box selecting
            {
                isBoxSelecting = true;
                boxSelectStart = e.Location;
            }
            else
            {
                TransformManager clo = null;
                float cloZ = camera.ZFar;
                foreach (var ob in objects)
                {
                    if (ob.TestHit(camera, GLMain.Width, GLMain.Height, e.X, e.Y, out float z))
                    {
                        if (z > camera.ZNear && z < cloZ)
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
        }

        private void OnMouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (isBoxSelecting)
            {
                isBoxSelecting = false;
                if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    ListObjects.SelectedItems.Clear();
                }

                int left = Math.Min(e.X, boxSelectStart.X);
                int right = Math.Max(e.X, boxSelectStart.X);
                int top = Math.Min(e.Y, boxSelectStart.Y);
                int bottom = Math.Max(e.Y, boxSelectStart.Y);
                foreach (var ob in objects)
                {
                    if (ob is PointManager)
                    {
                        var coords = ob.GetScreenSpaceCoords(camera, GLMain.Width, GLMain.Height);
                        if (coords.X > left && coords.X < right &&
                            coords.Y > top && coords.Y < bottom &&
                            coords.Z > camera.ZNear)
                        {
                            ListObjects.SelectedItems.Add(ob);
                        }
                    }
                }
            }
        }

        private void OnDisposed(object sender, EventArgs e)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            for (int i = objects.Count - 1; i >= 0; i--)
            {
                var ob = objects[i];
                if (ob is GregoryPatchManager)
                {
                    ob.ManagerDisposing -= HandleManagerDisposing;
                }
                objects[i].Dispose();
            }
            anaglyphQuad.Dispose();
            centerMarker.Dispose();
            cursor.Dispose();
            plane.Dispose();
            shader.Dispose();
        }

        private void RoutedInvalidate(object sender, RoutedEventArgs e) => RefreshView();

        private void UpDownInvalidate(object sender, RoutedPropertyChangedEventArgs<object> e) => RefreshView();

        private void ValueChangedInvalidate(object sender, RoutedPropertyChangedEventArgs<double> e) => RefreshView();

        private void ColorChangedInvalidate(object sender, RoutedPropertyChangedEventArgs<Color?> e) => RefreshView();

        private void DecimalBaseHeightChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            decimal sizeY = DecimalSizeY.Value.Value, baseHeight = DecimalBaseHeight.Value.Value;
            if (baseHeight > sizeY)
            {
                DecimalSizeY.Value = baseHeight;
            }
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
                    var tm = ob as TransformManager;
                    center += new Vector3(tm.PositionX, tm.PositionY, tm.PositionZ);
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

        public void AddManager(TransformManager tm)
        {
            objects.Add(tm);
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
            objects.Add(new TorusManager(cursor.Position, WIREFRAME_MAX, WIREFRAME_MAX));
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

        private void ButtonCreatePatchC0(object sender, RoutedEventArgs e)
        {
            PatchWindow pw = new PatchWindow();
            bool? res = pw.ShowDialog();
            if (res == true)
            {
                var (patch, points) =
                    CerealFactory.CreatePatchC0(cursor.Position, pw.PatchType,
                                                pw.DimX, pw.DimZ,
                                                pw.PatchCountX, pw.PatchCountZ);
                for (int j = 0; j < points.GetLength(1); j++)
                {
                    for (int i = 0; i < points.GetLength(0); i++)
                    {
                        objects.Add(points[i, j]);
                    }
                }
                objects.Add(patch);
                RefreshView();
            }
        }

        private void ButtonCreatePatchC2(object sender, RoutedEventArgs e)
        {
            PatchWindow pw = new PatchWindow();
            bool? res = pw.ShowDialog();
            if (res == true)
            {
                var (patch, points) =
                    CerealFactory.CreatePatchC2(cursor.Position, pw.PatchType,
                                                pw.DimX, pw.DimZ,
                                                pw.PatchCountX, pw.PatchCountZ);
                for (int j = 0; j < points.GetLength(1); j++)
                {
                    for (int i = 0; i < points.GetLength(0); i++)
                    {
                        objects.Add(points[i, j]);
                    }
                }
                objects.Add(patch);
                RefreshView();
            }
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
                        if (removing.Deletable)
                        {
                            if (removing is GregoryPatchManager)
                            {
                                removing.ManagerDisposing -= HandleManagerDisposing;
                            }
                            objects.Remove(removing);
                            removing.Dispose();
                        }
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

        private void ResetCamera()
        {
            camera.Position = cameraDefPosition;
            camera.Rotation = cameraDefRotation;
            RefreshView();
        }

        private void MenuNewClick(object sender, RoutedEventArgs e)
        {
            var res = System.Windows.MessageBox.Show("Are you sure you want to create a new scene?",
                                                     "New Scene", MessageBoxButton.OKCancel);
            if (res == MessageBoxResult.OK)
            {
                objects.Clear();
                RefreshView();
            }
        }

        private void MenuResetClick(object sender, RoutedEventArgs e) => ResetCamera();

        private void MenuOpenClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = currentDirectory;
            ofd.Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*";
            if (ofd.ShowDialog() == true)
            {
                currentDirectory = Path.GetDirectoryName(ofd.FileName);
                LoadSceneFromFile(ofd.FileName);
            }
        }

        private void LoadSceneFromFile(string path)
        {
            List<TransformManager> objectBuffer = new List<TransformManager>();
            try
            {
                var ser = new XmlSerializer(typeof(XmlScene));
                var sr = new StreamReader(path);
                var scene = ser.Deserialize(sr) as XmlScene;

                var pointDict = new Dictionary<string, PointManager>();
                foreach (var ob in scene.Items)
                {
                    if (ob is XmlPoint)
                    {
                        pointDict.Add(ob.Name, ob.GetTransformManager(null) as PointManager);
                    }
                }
                foreach (var ob in scene.Items)
                {
                    objectBuffer.Add(ob.GetTransformManager(pointDict));
                }

                objects.Clear();
                foreach (var ob in objectBuffer)
                {
                    objects.Add(ob);
                }
                RefreshView();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Scene file could not be processed.\n{ex.Message}",
                                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.M)
            {
                ContextMain.IsOpen = !ContextMain.IsOpen;
            }
            else if (e.Key == Key.P)
            {
                LoadSceneFromFile("../../../../paths/mine/mm_model_aligned.xml");
            }
        }

        private void MenuSaveClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = currentDirectory;
            sfd.Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*";
            if (sfd.ShowDialog() == true)
            {
                currentDirectory = Path.GetDirectoryName(sfd.FileName);
                var sceneObjects = new List<XmlNamedType>();
                foreach (var ob in objects)
                {
                    var serOb = ob.GetSerializable();
                    if (serOb != null)
                    {
                        sceneObjects.Add(serOb);
                    }
                }
                var scene = new XmlScene { Items = sceneObjects.ToArray() };

                try
                {
                    var ser = new XmlSerializer(typeof(XmlScene));
                    var sw = new StreamWriter(sfd.FileName);
                    ser.Serialize(sw, scene);
                    sw.Close();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Scene file could not be processed.\n{ex.Message}",
                                                   "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MenuMillingLoadClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = currentDirectory;
            ofd.Filter = "Path Files (*.kXX, *.fXX)|*.k??;*.f??";
            if (ofd.ShowDialog() == true)
            {
                currentDirectory = Path.GetDirectoryName(ofd.FileName);
                try
                {
                    if (!millingManager.LoadFile(ofd.FileName))
                    {
                        System.Windows.MessageBox.Show("Warning: loaded path is empty.",
                                                       "Warning", MessageBoxButton.OK);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Scene file could not be processed.\n{ex.Message}",
                                                   "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private List<PatchManager> GetPatches()
        {
            var patches = new List<PatchManager>();
            foreach (var ob in objects)
            {
                if (ob is PatchManager pm)
                {
                    patches.Add(pm);
                }
            }
            return patches;
        }

        // TODO: window with generation params, maybe one for all?
        //private void MenuMillingGenerateRoughClick(object sender, RoutedEventArgs e)
        //{
        //    var pixels = SetupObjectHeightMap(GetPatches());
        //    millingManager.WriteRoughPath(pixels, 0.01f, "../../../../paths/mine/", 1);
        //}

        //private void MenuMillingGenerateBaseClick(object sender, RoutedEventArgs e)
        //{
        //    graphVis = millingManager.WriteBasePaths(GetPatches(), 0.0001f, "../../../../paths/mine/", 1);
        //    RefreshView(); // useful for debugging
        //}

        //private void MenuMillingGenerateDetailClick(object sender, RoutedEventArgs e)
        //{
        //    millingManager.WriteDetailPath(GetPatches(), 0.0001f, "../../../../paths/mine/", 1);
        //}

        private void MenuMillingGenerateClick(object sender, RoutedEventArgs e)
        {
            int startMove = 3;
            string location = "../../../../paths/mine/";
            var patches = GetPatches();
            var pixels = SetupObjectHeightMap(patches);

            var roughPath = millingManager.GetRoughPath(pixels, 0.01f);
            var (basePath, contourPath, innerPath) = millingManager.GetBasePaths(patches, 0.0001f);
            var detailPath = millingManager.GetDetailPath(patches);
            // join innerPath and detailPath - same ToolData
            innerPath.Points.RemoveAt(innerPath.Points.Count - 1);
            innerPath.Points.AddRange(detailPath.Points.Skip(1));

            MillingIO.SaveFile(roughPath, location, "1", startMove);
            MillingIO.SaveFile(basePath, location, "2", startMove);
            MillingIO.SaveFile(contourPath, location, "3", startMove);
            MillingIO.SaveFile(innerPath, location, "4", startMove);
        }

        private void MenuMillingScaleClick(object sender, RoutedEventArgs e)
        {
            ScaleWindow sw = new ScaleWindow();
            if (sw.ShowDialog() == true)
            {
                foreach (var ob in objects)
                {
                    if (ob is PointManager pm)
                    {
                        pm.PositionX = (float)(pm.PositionX * sw.ScaleFactor);
                        pm.PositionY = (float)(pm.PositionY * sw.ScaleFactor);
                        pm.PositionZ = (float)(pm.PositionZ * sw.ScaleFactor);
                    }
                }
                RefreshView();
            }
        }

        private void MenuMillingAlignClick(object sender, RoutedEventArgs e)
        {
            // assuming that model is centered and with y = 0 as base plane
            if (millingManager != null)
            {
                foreach (var ob in objects)
                {
                    if (ob is PointManager pm)
                    {
                        pm.Translate(millingManager.PositionX,
                                     millingManager.PositionY + millingManager.BaseHeight,
                                     millingManager.PositionZ);
                    }
                }
                RefreshView();
            }
        }

        private void FillTriangles(object sender, RoutedEventArgs e)
        {
            PatchGraph graph = new PatchGraph();

            foreach (var sob in ListObjects.SelectedItems)
            {
                if (sob is PointManager pm)
                {
                    foreach (var ob in objects)
                    {
                        if (ob is PatchC0Manager pcm)
                        {
                            pcm.AddEdgesIncluding(graph, pm);
                        }
                    }
                }
                else if (sob is PatchC0Manager pcm)
                {
                    pcm.AddAllEdges(graph);
                }
            }

            var triangles = new HashSet<PatchGraphTriangle>();
            foreach (var vert in graph.Vertices.Values)
            {
                foreach (var edge in vert)
                {
                    foreach (var u in graph.Vertices.Keys)
                    {
                        foreach (var e1 in graph.GetEdgesBetween(u, edge.From))
                        {
                            if (!edge.Equals(e1))
                            {
                                foreach (var e2 in graph.GetEdgesBetween(u, edge.To))
                                {
                                    if (!edge.Equals(e2) && !e1.Equals(e2))
                                    {
                                        triangles.Add(new PatchGraphTriangle(edge, e1, e2));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (triangles.Count > 0)
            {
                foreach (var triangle in triangles)
                {
                    var gpm = new GregoryPatchManager(triangle);
                    gpm.ManagerDisposing += HandleManagerDisposing;
                    objects.Add(gpm);
                }
                RefreshView();
            }
            else
            {
                System.Windows.MessageBox.Show("No gaps selected.", "Fill gaps", MessageBoxButton.OK);
            }
        }

        private void FindIntersections(object sender, RoutedEventArgs e)
        {
            var surfaces = objects.OfType<ISurface>().ToList();
            if (surfaces.Count == 0)
            {
                System.Windows.MessageBox.Show("No possible surfaces to intersect.", "Find intersections", MessageBoxButton.OK);
            }
            else
            {
                var selected = ListObjects.SelectedItems.OfType<ISurface>().ToList();
                IntersectionFinderWindow ifw = new IntersectionFinderWindow(surfaces, selected);
                bool? res = ifw.ShowDialog();
                if (res == true)
                {
                    int divs = IntersectionFinderWindow.FinderParams.StartDims;
                    float fdivs = divs;
                    bool noCurve = false;
                    if (IntersectionFinderWindow.UseCursorPosition)
                    {
                        var pointSurface = new PointSurface(cursor.Position);
                        bool foundStart = false;
                        var pointsSecond = new (bool? valid, Vector4 start)[divs, divs];
                        for (int x = 0; x < divs; x++)
                        {
                            for (int y = 0; y < divs; y++)
                            {
                                Vector4 preStart1 = divs > 1 ? new Vector4(x / fdivs, y / fdivs, 0.0f, 0.0f)
                                                             : new Vector4(0.5f, 0.5f, 0.0f, 0.0f);
                                var (boolIntRes1, start1) = IntersectionFinder.FindIntersectionPoint(IntersectionFinderWindow.FinderParams,
                                                                                                     IntersectionFinderWindow.SelectedFirst,
                                                                                                     pointSurface,
                                                                                                     preStart1);
                                if (!boolIntRes1) continue;

                                for (int z = 0; z < divs; z++)
                                {
                                    for (int w = 0; w < divs; w++)
                                    {
                                        if (IntersectionFinderWindow.IsSingleSurface &&
                                            Vector2.DistanceSquared(new Vector2(x / fdivs, y / fdivs), new Vector2(z / fdivs, w / fdivs)) < IntersectionFinderWindow.FinderParams.StartSelfDiffSquared)
                                            continue;

                                        Vector4 preStart2 = divs > 1 ? new Vector4(z / fdivs, w / fdivs, 0.0f, 0.0f)
                                                                 : new Vector4(0.5f, 0.5f, 0.0f, 0.0f);
                                        if (pointsSecond[z, w].valid == null)
                                        {
                                            pointsSecond[z, w] = IntersectionFinder.FindIntersectionPoint(IntersectionFinderWindow.FinderParams,
                                                                                                          IntersectionFinderWindow.SelectedSecond,
                                                                                                          pointSurface,
                                                                                                          preStart2);
                                        }
                                        var (boolIntRes2, start2) = pointsSecond[z, w];
                                        if (boolIntRes2 == false) continue;
                                        if (!IntersectionFinderWindow.IsSingleSurface &&
                                            Vector2.DistanceSquared(new Vector2(x / fdivs, y / fdivs), new Vector2(z / fdivs, w / fdivs)) < IntersectionFinderWindow.FinderParams.StartSelfDiffSquared)
                                            continue;

                                        foundStart = true;
                                        Vector4 start = new Vector4(start1.X, start1.Y, start2.X, start2.Y);
                                        var (intRes, icm) = IntersectionFinder.FindIntersection(IntersectionFinderWindow.FinderParams,
                                                                                                IntersectionFinderWindow.SelectedFirst,
                                                                                                IntersectionFinderWindow.SelectedSecond,
                                                                                                start, false);
                                        if (intRes == IntersectionResult.OK)
                                        {
                                            objects.Add(icm);
                                            RefreshView();
                                            return;
                                        }
                                        else if (intRes == IntersectionResult.NoCurve)
                                        {
                                            noCurve = true;
                                        }
                                    }
                                }
                            }
                        }
                        if (!foundStart)
                        {
                            System.Windows.MessageBox.Show("Failed to find starting point.", "Find intersections", MessageBoxButton.OK);
                            return;
                        }
                    }
                    else
                    {
                        for (int x = 0; x < divs; x++)
                        {
                            for (int y = 0; y < divs; y++)
                            {
                                for (int z = 0; z < divs; z++)
                                {
                                    for (int w = 0; w < divs; w++)
                                    {
                                        if (IntersectionFinderWindow.IsSingleSurface &&
                                            Vector2.DistanceSquared(new Vector2(x / fdivs, y / fdivs), new Vector2(z / fdivs, w / fdivs)) < IntersectionFinderWindow.FinderParams.StartSelfDiffSquared)
                                            continue;

                                        Vector4 start = divs > 1 ? new Vector4(x / fdivs, y / fdivs, z / fdivs, w / fdivs)
                                                                     : new Vector4(0.5f);
                                        var (intRes, icm) = IntersectionFinder.FindIntersection(IntersectionFinderWindow.FinderParams,
                                                                                                IntersectionFinderWindow.SelectedFirst,
                                                                                                IntersectionFinderWindow.SelectedSecond,
                                                                                                start);
                                        if (intRes == IntersectionResult.OK)
                                        {
                                            objects.Add(icm);
                                            RefreshView();
                                            return;
                                        }
                                        else if (intRes == IntersectionResult.NoCurve)
                                        {
                                            noCurve = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (noCurve)
                    {
                        System.Windows.MessageBox.Show("Failed to find intersection curve.", "Find intersections", MessageBoxButton.OK);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("No intersections found.", "Find intersections", MessageBoxButton.OK);
                    }
                }
            }
        }

        private void HandleManagerDisposing(TransformManager sender)
        {
            sender.ManagerDisposing -= HandleManagerDisposing;
            objects.Remove(sender);
        }

        private void ComboToolSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (millingManager != null)
            {
                millingManager.IsFlat = ComboTool.SelectedIndex == FLAT_INDEX;
                RefreshView();
            }
        }

        private void StartMillingTimer()
        {
            millingTimer.Start();
            StackPanelMillingStep.IsEnabled = false;
        }

        private void StopMillingTimer()
        {
            millingTimer.Stop();
            StackPanelMillingStep.IsEnabled = true;
        }

        private void ButtonBeginMilling(object sender, RoutedEventArgs e)
        {
            millingManager.BeginMilling();
            millingTimer.Interval = new TimeSpan((long)(1e7 * MillingStepDelay));
            StartMillingTimer();
            RefreshView();
        }

        private void ButtonPauseMilling(object sender, RoutedEventArgs e)
        {
            StopMillingTimer();
            RefreshView();
        }

        private void ButtonResetMilling(object sender, RoutedEventArgs e)
        {
            StopMillingTimer();
            millingManager.Reset();
            RefreshView();
        }

        private void ButtonMillingJumpToEnd(object sender, RoutedEventArgs e)
        {
            StopMillingTimer();
            //try
            //{
            //    millingManager.BeginMilling(true);
            //}
            //catch (MillingException ex)
            //{
            //    RefreshView();
            //    System.Windows.MessageBox.Show(ex.Message, "Milling error", MessageBoxButton.OK, MessageBoxImage.Error);
            //    return;
            //}
            StackPanelMillingButtons.IsEnabled = false;
            millingManager.MillAsync(EndMillingJumpToEnd, UpdateMillingProgressBar);
            RefreshView();
        }

        private void UpdateMillingProgressBar(int step, int total)
        {
            ProgressBarMain.Value = (100.0 * step) / total;
        }

        private void EndMillingJumpToEnd(Exception ex)
        {
            StackPanelMillingButtons.IsEnabled = true;
            ProgressBarMain.Value = 0;
            RefreshView();
            if (ex != null)
            {
                System.Windows.MessageBox.Show(ex.Message, "Milling error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MillingPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsFlat")
            {
                ComboTool.SelectedIndex = millingManager.IsFlat ? FLAT_INDEX : SPHERICAL_INDEX;
            }
        }

        private void OnMillingTimer(object sender, EventArgs e)
        {
            try
            {
                if (!millingManager.Step())
                {
                    StopMillingTimer();
                }
            }
            catch (MillingException ex)
            {
                RefreshView();
                System.Windows.MessageBox.Show(ex.Message, "Milling error", MessageBoxButton.OK, MessageBoxImage.Error);
                StopMillingTimer();
                return;
            }
            RefreshView();
        }

        private void MergePoints(MergeMode mode)
        {
            var selectedPoints = new List<PointManager>();
            foreach (var ob in ListObjects.SelectedItems)
            {
                if (ob is PointManager pm)
                {
                    selectedPoints.Add(pm);
                }
            }
            if (selectedPoints.Count == 0)
            {
                return;
            }

            Vector3 pos;
            switch (mode)
            {
                case MergeMode.ToFirst:
                    pos = selectedPoints[0].Transform.Position;
                    break;
                case MergeMode.ToLast:
                    pos = selectedPoints[selectedPoints.Count - 1].Transform.Position;
                    break;
                case MergeMode.ToCursor:
                    pos = cursor.Position;
                    break;
                case MergeMode.ToCenter:
                    pos = centerMarker.Position;
                    break;
                default:
                    return;
            }

            var newPoint = new PointManager(pos);
            foreach (var point in selectedPoints)
            {
                point.Replace(newPoint);
                objects.Remove(point);
                point.Dispose();
            }
            objects.Add(newPoint);
            RefreshView();
        }

        private void Debug_Empty(object sender, RoutedEventArgs e)
        {
            objects.Clear();
            CheckBoxGrid.IsChecked = false;
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
