using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace AbraCADabra.Milling
{
    public class MillingManager : INotifyPropertyChanged
    {
        private enum FillingMode
        {
            Full, X, Z
        }
        private const float Y_EPS = 0.0001f;
        private const float TOOL_DIST = 1;
        private const string TEX_PATH = "../../Images/bamboo.png";

        private Cuboid material;
        #region Material properties
        public float PositionX
        {
            get { return material.Position.X; }
            set
            {
                material.Position.X = value;
            }
        }
        public float PositionY
        {
            get { return material.Position.Y; }
            set
            {
                material.Position.Y = value;
            }
        }
        public float PositionZ
        {
            get { return material.Position.Z; }
            set
            {
                material.Position.Z = value;
            }
        }
        public float SizeX
        {
            get { return material.SizeX; }
            set
            {
                material.SizeX = value;
                Reset(true);
            }
        }
        public float SizeY
        {
            get { return material.SizeY; }
            set
            {
                material.SizeY = value;
                Reset(true);
            }
        }
        public float SizeZ
        {
            get { return material.SizeZ; }
            set
            {
                material.SizeZ = value;
                Reset(true);
            }
        }
        public uint DivX
        {
            get { return material.DivX; }
            set
            {
                material.DivX = value;
                Reset(true);
            }
        }
        public uint DivZ
        {
            get { return material.DivZ; }
            set
            {
                material.DivZ = value;
                Reset(true);
            }
        }
        public float BaseHeight { get; set; } = 1.5f;
        #endregion
        private float[,] materialHeight;
        private List<Vector3> gridPoints;
        private int materialTexture, materialHeightMap;
        private int texFirstX, texFirstZ, texLastX, texLastZ;
        private bool updateHeightMap, recreateHeightMap;
        private bool isHeightMapInvalid;

        private MillingPath path;

        public bool DisplayPath { get; set; } = true;
        public bool ShowPathOnTop { get; set; } = false;

        private Tool tool;
        public float ToolDiameter
        {
            get { return tool.Diameter; }
            set
            {
                tool.Diameter = value;
                tool.Update();
                OnPropertyChanged();
            }
        }
        public float ToolHeight
        {
            get { return tool.Height; }
            set
            {
                tool.Height = value;
                tool.Update();
                OnPropertyChanged();
            }
        }
        public bool IsFlat
        {
            get { return tool.IsFlat; }
            set
            {
                tool.IsFlat = value;
                tool.Update();
                OnPropertyChanged();
            }
        }

        private bool isIdle = true;
        public bool IsIdle
        {
            get { return isIdle; }
            set
            {
                isIdle = value;
                OnPropertyChanged();
            }
        }

        public int StepLength { get; set; } = 10;

        public event PropertyChangedEventHandler PropertyChanged;

        public MillingManager()
        {
            material = new Cuboid();
            tool = new Tool(false, 1, 10);

            Image<Rgba32> img = Image.Load<Rgba32>(TEX_PATH);
            img.TryGetSinglePixelSpan(out System.Span<Rgba32> span);
            var tempPixels = span.ToArray();

            var pixels = new List<byte>();
            foreach (Rgba32 p in tempPixels)
            {
                pixels.Add(p.R);
                pixels.Add(p.G);
                pixels.Add(p.B);
                pixels.Add(p.A);
            }
            materialTexture = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture11);
            GL.BindTexture(TextureTarget.Texture2D, materialTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                          img.Width, img.Height, 0,
                          PixelFormat.Rgba, PixelType.UnsignedByte, pixels.ToArray());
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

            materialHeightMap = GL.GenTexture();
        }

        public void Reset(bool updateMaterial = false)
        {
            if (updateMaterial)
            {
                material.Update();
            }
            AdjustTool();
            materialHeight = null;
            stepCurLine = stepCurPixel = 0;
            IsIdle = true;
        }

        private void AdjustTool()
        {
            if (path != null && path.Points.Count > 0)
            {
                tool.Position = path.Points[0];
            }
            else
            {
                tool.Position = new Vector3(PositionX, PositionY + SizeY + TOOL_DIST, PositionZ);
            }
        }

        public async void MillAsync(Action<Exception> endAction, Action<int, int> stepAction)
        {
            var millTask = new Task(() => BeginMilling(true, stepAction));
            MillingException caughtEx = null;
            millTask.Start();
            try
            {
                await millTask;
            }
            catch (MillingException ex)
            {
                caughtEx = ex;
            }
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(endAction, caughtEx);
            }
        }

        public void BeginMilling(bool jumpToEnd = false, Action<int, int> stepAction = null)
        {
            if (path == null || path.Points.Count == 0) return;
            if (!IsIdle)
            {
                if (jumpToEnd)
                {
                    while (Step(true))
                    {
                        if (stepAction != null && Application.Current != null)
                        {
                            Application.Current.Dispatcher.Invoke(stepAction, stepCurLine, path.Points.Count - 1);
                        }
                    }
                }
                return;
            }

            IsIdle = false;
            isNewLine = true;
            stepCurLine = 0;
            AdjustTool();

            texFirstX = texFirstZ = int.MaxValue;
            texLastX = texLastZ = -int.MaxValue;
            if (materialHeight == null)
            {
                isHeightMapInvalid = true;
                materialHeight = new float[DivX + 1, DivZ + 1];
                for (int i = 0; i <= DivX; i++)
                {
                    for (int j = 0; j <= DivZ; j++)
                    {
                        materialHeight[i, j] = SizeY;
                    }
                }
                isHeightMapInvalid = false;
            }
            updateHeightMap = true;
            recreateHeightMap = true;

            gridPoints = new List<Vector3>();
            foreach (var point in path.Points)
            {
                gridPoints.Add(WorldToGrid(point));
            }

            if (jumpToEnd)
            {
                while (Step(true))
                {
                    if (stepAction != null && Application.Current != null)
                    {
                        Application.Current.Dispatcher.Invoke(stepAction, stepCurLine, path.Points.Count - 1);
                    }
                }
            }
        }

        private int stepCurX, stepCurZ, stepdx1, stepdx2, stepdz1, stepdz2, stepLongest, stepShortest, stepDistCounter, stepLen, stepCurLine, stepCurPixel;
        private float stepStartY, stepDeltaY, stepDiffY;
        private FillingMode stepInnerMode;
        private bool isNewLine = false;
        public bool Step(bool jumping = false)
        {
            if (IsIdle) return false;

            if (gridPoints.Count == 1)
            {
                stepDiffY = 0;
                MillRadius((int)Math.Round(gridPoints[0].X), gridPoints[0].Y, (int)Math.Round(gridPoints[0].Z));
                updateHeightMap = true;
                return false;
            }

            if (isNewLine)
            {
                isNewLine = false;
                stepCurPixel = 0;
                int startX = (int)Math.Round(gridPoints[stepCurLine].X);
                int startZ = (int)Math.Round(gridPoints[stepCurLine].Z);

                // Bresenham
                stepStartY = gridPoints[stepCurLine].Y;
                float endY = gridPoints[stepCurLine + 1].Y;
                int endX = (int)Math.Round(gridPoints[stepCurLine + 1].X);
                int endZ = (int)Math.Round(gridPoints[stepCurLine + 1].Z);
                int w = endX - startX, h = endZ - startZ;

                stepdx1 = 0; stepdx2 = 0; stepdz1 = 0; stepdz2 = 0;
                if (w < 0) stepdx1 = stepdx2 = -1;
                else if (w > 0) stepdx1 = stepdx2 = 1;
                if (h < 0) stepdz1 = -1;
                else if (h > 0) stepdz1 = 1;

                stepLongest = Math.Abs(w);
                stepShortest = Math.Abs(h);
                stepInnerMode = FillingMode.Z;
                if (stepLongest <= stepShortest)
                {
                    stepInnerMode = FillingMode.X;
                    stepLongest = Math.Abs(h);
                    stepShortest = Math.Abs(w);
                    if (h < 0) stepdz2 = -1;
                    else if (h > 0) stepdz2 = 1;
                    stepdx2 = 0;
                }

                stepDiffY = endY - stepStartY;
                stepDeltaY = stepDiffY / (stepLongest > 0 ? stepLongest : 1);
                stepCurX = startX;
                stepCurZ = startZ;
                stepDistCounter = stepLongest >> 1;
                stepLen = jumping ? stepLongest : StepLength - 1;
            }

            int stepStart = stepCurPixel, stepEnd = Math.Min(stepLongest, stepStart + stepLen);
            for (; stepCurPixel <= stepEnd; stepCurPixel++)
            {
                float curY = stepStartY + stepCurPixel * stepDeltaY;
                if (stepLongest == 0) // vertical line
                {
                    curY = stepStartY + stepDiffY;
                }
                if (stepCurPixel == stepStart || stepCurPixel == stepLen)
                {
                    MillRadius(stepCurX, curY, stepCurZ);
                }
                else
                {
                    MillRadius(stepCurX, curY, stepCurZ, stepInnerMode);
                }
                if (!jumping)
                {
                    tool.Position = GridToWorld(stepCurX, curY, stepCurZ);
                }

                stepDistCounter += stepShortest;
                if (stepDistCounter >= stepLongest)
                {
                    stepDistCounter -= stepLongest;
                    stepCurX += stepdx1;
                    stepCurZ += stepdz1;
                }
                else
                {
                    stepCurX += stepdx2;
                    stepCurZ += stepdz2;
                }
            }

            if (stepCurPixel > stepLongest)
            {
                stepCurLine++;
                isNewLine = true;
            }

            bool finished = stepCurLine == gridPoints.Count - 1;
            if (!jumping || finished)
            {
                updateHeightMap = true;
            }
            IsIdle = finished;
            if (finished)
            {
                tool.Position = path.Points[path.Points.Count - 1];
            }
            return !finished;
        }

        private void ThrowMillingError(string message, Vector3 toolPos)
        {
            IsIdle = true;
            updateHeightMap = true;
            tool.Position = toolPos;
            throw new MillingException(message);
        }

        private float GetMilledHeight(int x, float tipHeight, int z, Vector3 world, Vector3 centerWorld)
        {
            float height;
            if (tool.IsFlat)
            {
                height = tipHeight;
            }
            else
            {
                float radius = tool.Diameter / 2, radSq = radius * radius;
                Vector2 fromCenter = new Vector2(world.X - centerWorld.X, world.Z - centerWorld.Z);
                float h = (float)Math.Sqrt(Math.Max(0, radSq - fromCenter.LengthSquared));
                height = tipHeight + radius - h;
            }
            return Math.Min(materialHeight[x, z], height);
        }

        private void MillRadius(int x, float tipHeight, int z, FillingMode mode = FillingMode.Full)
        {
            int centerX = x, centerZ = z;
            float radius = tool.Diameter / 2; 
            int gridRadX = mode == FillingMode.Z ? 0 : (int)Math.Ceiling(radius / SizeX * DivX);
            int gridRadZ = mode == FillingMode.X ? 0 : (int)Math.Ceiling(radius / SizeZ * DivZ);
            Vector3 centerWorld = GridToWorld(x, tipHeight, z);

            for (int xx = Math.Max(centerX - gridRadX, 0); xx <= Math.Min(centerX + gridRadX, DivX); xx++)
            {
                for (int zz = Math.Max(centerZ - gridRadZ, 0); zz <= Math.Min(centerZ + gridRadZ, DivZ); zz++)
                {
                    Vector3 world = GridToWorld(xx, tipHeight, zz);
                    if ((centerWorld - world).LengthSquared <= radius * radius)
                    {
                        float height = GetMilledHeight(xx, tipHeight, zz, world, centerWorld);
                        if (height <= BaseHeight)
                        {
                            ThrowMillingError("Cannot mill below base level.", centerWorld);
                        }
                        if (IsFlat && height < materialHeight[xx, zz] && stepDiffY < -Y_EPS)
                        {
                            ThrowMillingError("Cannot mill using the flat side of a flat tool.", centerWorld);
                        }

                        materialHeight[xx, zz] = height;
                        texFirstX = Math.Min(texFirstX, xx);
                        texFirstZ = Math.Min(texFirstZ, zz);
                        texLastX = Math.Max(texLastX, xx);
                        texLastZ = Math.Max(texLastZ, zz);
                    }
                }
            }
        }

        private Vector3 WorldToGrid(Vector3 point)
        {
            float x1 = PositionX - SizeX / 2;
            float z1 = PositionZ - SizeZ / 2;

            var res = new Vector3
            {
                X = ((point.X - x1) / SizeX) * DivX,
                Y = point.Y - PositionY,
                Z = ((point.Z - z1) / SizeZ) * DivZ
            };
            return res;
        }

        private Vector3 GridToWorld(Vector3 point)
        {
            return GridToWorld(point.X, point.Y, point.Z);
        }

        private Vector3 GridToWorld(float x, float y, float z)
        {
            float x1 = PositionX - SizeX / 2;
            float z1 = PositionZ - SizeZ / 2;
            var res = new Vector3
            {
                X = x * SizeX / DivX + x1,
                Y = y + PositionY,
                Z = z * SizeZ / DivZ + z1
            };
            return res;
        }

        private void ActualUpdateHeightMap()
        {
            int texDimX = materialHeight.GetLength(0), texDimZ = materialHeight.GetLength(1);
            int startX = 0, startZ = 0, endX = texDimX, endZ = texDimZ;
            if (!recreateHeightMap && texFirstX < texLastX)
            {
                startX = texFirstX;
                endX = texLastX + 1;
            }
            if (!recreateHeightMap && texFirstZ < texLastZ)
            {
                startZ = texFirstZ;
                endZ = texLastZ + 1;
            }

            var textureData = new float[materialHeight.Length];
            int ind = 0;
            for (int i = startZ; i < endZ; i++)
            {
                for (int j = startX; j < endX; j++)
                {
                    textureData[ind++] = materialHeight[j, i];
                }
            }

            GL.ActiveTexture(TextureUnit.Texture12);
            GL.BindTexture(TextureTarget.Texture2D, materialHeightMap);
            if (recreateHeightMap)
            {
                GL.TexImage2D(TextureTarget.Texture2D,
                              0,
                              PixelInternalFormat.R32f,
                              texDimX, texDimZ, 0,
                              PixelFormat.Red, PixelType.Float,
                              textureData);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            }
            else
            {
                GL.TexSubImage2D(TextureTarget.Texture2D, 0,
                                 startX, startZ, endX - startX, endZ - startZ,
                                 PixelFormat.Red, PixelType.Float,
                                 textureData);
            }
        }

        public bool LoadFile(string filename)
        {
            ToolData toolData;
            (path, toolData) = MillingReader.ReadFile(filename);
            ToolDiameter = toolData.Diameter * MillingPath.SCALE;
            IsFlat = toolData.IsFlat;
            AdjustTool();
            return path.Points.Count > 0;
        }

        public void Render(ShaderManager shader)
        {
            if (path != null)
            {
                GL.Enable(EnableCap.CullFace);
                if (materialHeight == null || isHeightMapInvalid)
                {
                    shader.UseMillBasic();
                }
                else
                {
                    if (updateHeightMap)
                    {
                        ActualUpdateHeightMap();
                        updateHeightMap = recreateHeightMap = false;
                    }
                    shader.UseMillHeight();
                    GL.ActiveTexture(TextureUnit.Texture12);
                    GL.BindTexture(TextureTarget.Texture2D, materialHeightMap);
                    shader.SetupInt(12, "materialHeightMap");
                }
                GL.ActiveTexture(TextureUnit.Texture11);
                GL.BindTexture(TextureTarget.Texture2D, materialTexture);
                shader.SetupInt(11, "materialTex");
                material.Render(shader);
                shader.UseBasic();

                if (DisplayPath)
                {
                    if (ShowPathOnTop)
                    {
                        GL.Disable(EnableCap.DepthTest);
                    }
                    path.Render(shader);
                    if (ShowPathOnTop)
                    {
                        GL.Enable(EnableCap.DepthTest);
                    }
                }

                shader.UsePhong();
                tool.Render(shader);
                shader.UseBasic();

                GL.Disable(EnableCap.CullFace);
            }
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            var args = new PropertyChangedEventArgs(name);
            //foreach (Delegate d in PropertyChanged.GetInvocationList())
            //{
            //    ISynchronizeInvoke syncer = d.Target as ISynchronizeInvoke;
            //    if (syncer == null)
            //    {
            //        d.DynamicInvoke(this, args);
            //    }
            //    else
            //    {
            //        syncer.BeginInvoke(d, new object[] { this, args });
            //    }
            //}
            if (Application.Current != null)
            {
                foreach (Delegate d in PropertyChanged.GetInvocationList())
                {
                    Application.Current.Dispatcher.Invoke(d, this, args);
                }
            }
        }
    }
}
