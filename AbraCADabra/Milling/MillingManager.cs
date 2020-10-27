using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;

namespace AbraCADabra.Milling
{
    public class MillingManager : INotifyPropertyChanged
    {
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
                AdjustTool();
            }
        }
        public float PositionY
        {
            get { return material.Position.Y; }
            set
            {
                material.Position.Y = value;
                AdjustTool();
            }
        }
        public float PositionZ
        {
            get { return material.Position.Z; }
            set
            {
                material.Position.Z = value;
                AdjustTool();
            }
        }
        public float SizeX
        {
            get { return material.SizeX; }
            set
            {
                material.SizeX = value;
                Reset();
            }
        }
        public float SizeY
        {
            get { return material.SizeY; }
            set
            {
                material.SizeY = value;
                Reset();
            }
        }
        public float SizeZ
        {
            get { return material.SizeZ; }
            set
            {
                material.SizeZ = value;
                Reset();
            }
        }
        public uint DivX
        {
            get { return material.DivX; }
            set
            {
                material.DivX = value;
                Reset();
            }
        }
        public uint DivZ
        {
            get { return material.DivZ; }
            set
            {
                material.DivZ = value;
                Reset();
            }
        }
        public float BaseHeight { get; set; } = 2;
        #endregion
        private float[,] materialHeight;
        private List<Vector3> gridPoints;
        private int materialTexture, materialHeightMap;

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

        public int StepLength { get; set; } = 1;
        private int curLine = 0;
        private int curPixel = 0;

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

        public void Reset()
        {
            material.Update();
            AdjustTool();
            materialHeight = null;
            curLine = curPixel = 0;
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

        public void BeginMilling(bool jumpToEnd = false)
        {
            if (!IsIdle || path.Points.Count == 0) return;

            IsIdle = false;
            tool.Position = path.Points[0];

            if (materialHeight == null)
            {
                materialHeight = new float[DivX + 1, DivZ + 1];
                for (int i = 0; i <= DivX; i++)
                {
                    for (int j = 0; j <= DivZ; j++)
                    {
                        materialHeight[i, j] = SizeY;
                    }
                }
            }
            UpdateHeightMap();

            gridPoints = new List<Vector3>();
            foreach (var point in path.Points)
            {
                gridPoints.Add(WorldToGrid(point));
            }

            if (jumpToEnd)
            {
                while (Step(true));
            }
        }

        public bool Step(bool jumping = false)
        {
            if (IsIdle) return false;

            int startX = (int)Math.Round(gridPoints[curLine].X);
            int startZ = (int)Math.Round(gridPoints[curLine].Z);
            if (gridPoints.Count == 1)
            {
                //if (startX >= 0 && startX <= DivX && startZ >= 0 && startZ <= DivZ)
                //{
                //    materialHeight[startX, startZ] = GetMilledHeight(startX, startZ, path.Points[0].Y);
                //}
                MillRadius(startX, startZ, path.Points[0].Y);
                UpdateHeightMap();
                return false;
            }

            // Bresenham
            float startY = gridPoints[curLine].Y;
            float endY = gridPoints[curLine + 1].Y;
            int endX = (int)Math.Round(gridPoints[curLine + 1].X);
            int endZ = (int)Math.Round(gridPoints[curLine + 1].Z);
            int w = endX - startX, h = endZ - startZ;
            int dx1 = 0, dx2 = 0, dz1 = 0, dz2 = 0;
            if (w < 0) dx1 = dx2 = -1;
            else if (w > 0) dx1 = dx2 = 1;
            if (h < 0) dz1 = -1;
            else if (h > 0) dz1 = 1;

            int longest = Math.Abs(w), shortest = Math.Abs(h);
            if (longest <= shortest)
            {
                longest = Math.Abs(h);
                shortest = Math.Abs(w);
                if (h < 0) dz2 = -1;
                else if (h > 0) dz2 = 1;
                dx2 = 0;
            }

            float stepY = (endY - startY) / longest;
            int x = startX, z = startZ;
            int numerator = longest >> 1;
            int stepLen = jumping ? longest : curPixel + StepLength - 1;
            for (; curPixel <= stepLen; curPixel++)
            {
                float y = startY + curPixel * stepY;
                Vector3 world = GridToWorld(x, y, z);
                MillRadius(x, z, world.Y);
                if (x >= 0 && x <= DivX && z >= 0 && z <= DivZ)
                {
                    materialHeight[x, z] = GetMilledHeight(x, z, 0, 0, world.Y);
                }
                tool.Position = world;

                numerator += shortest;
                if (numerator >= longest)
                {
                    numerator -= longest;
                    x += dx1;
                    z += dz1;
                }
                else
                {
                    x += dx2;
                    z += dz2;
                }
            }

            if (curPixel > longest)
            {
                curLine++;
                curPixel = 0;
            }

            bool finished = curLine == gridPoints.Count - 1;
            if (!jumping || finished)
            {
                UpdateHeightMap();
            }
            return !finished;
        }

        private float GetMilledHeight(int x, int z, int centerX, int centerZ, float tipHeight)
        {
            return Math.Min(materialHeight[x, z], tipHeight);
        }

        private void MillRadius(int x, int z, float tipHeight)
        {
            int centerX = x, centerZ = z;
            float radius = tool.Diameter / 2; 
            float gridRadX = (radius / SizeX) * DivX;
            float gridRadZ = (radius / SizeZ) * DivZ;


            Vector3 world = GridToWorld(x, tipHeight, z);
        }

        private Vector3 WorldToGrid(Vector3 point)
        {
            float x1 = PositionX - SizeX / 2;
            float z1 = PositionZ - SizeZ / 2;

            var res = new Vector3
            {
                X = ((point.X - x1) / SizeX) * (DivX + 1), // TODO: without 1?
                Y = point.Y - PositionY,
                Z = ((point.Z - z1) / SizeZ) * (DivZ + 1)
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
                X = x * SizeX / (DivX + 1) + x1,
                Y = y + PositionY,
                Z = z * SizeZ / (DivZ + 1) + z1
            };
            return res;
        }

        private void UpdateHeightMap()
        {
            var textureData = new float[materialHeight.Length];
            int ind = 0;
            for (int i = 0; i < materialHeight.GetLength(1); i++)
            {
                for (int j = 0; j < materialHeight.GetLength(0); j++)
                {
                    textureData[ind++] = materialHeight[j, i];
                }
            }

            GL.ActiveTexture(TextureUnit.Texture12);
            GL.BindTexture(TextureTarget.Texture2D, materialHeightMap);
            GL.TexImage2D(TextureTarget.Texture2D,
                          0,
                          PixelInternalFormat.R32f,
                          materialHeight.GetLength(0), materialHeight.GetLength(1), 0,
                          PixelFormat.Red, PixelType.Float,
                          textureData);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        }

        public bool LoadFile(string filename)
        {
            ToolData toolData;
            (path, toolData) = MillingReader.ReadFile(filename);
            ToolDiameter = toolData.Diameter * MillingPath.SCALE;
            IsFlat = toolData.IsFlat;
            Reset();
            return path.Points.Count > 0;
        }

        public void Render(ShaderManager shader)
        {
            if (path != null)
            {
                GL.Enable(EnableCap.CullFace);
                if (materialHeight == null)
                {
                    shader.UsePhongtex();
                }
                else
                {
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
