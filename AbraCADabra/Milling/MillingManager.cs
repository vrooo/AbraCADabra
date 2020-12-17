using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;

namespace AbraCADabra.Milling
{
    public class MillingManager : INotifyPropertyChanged
    {
        private enum FillingMode
        {
            Full, X, Z
        }
        private const float Y_EPS = 0.0001f;
        private const float TOOL_DIST = 5;
        private const float PATH_BASE_DIST = 0.5f;
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
        public bool DisplayMaterial { get; set; } = true;
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

            var (pixels, imgWidth, imgHeight) = ImageIO.LoadImageBytes(TEX_PATH);
            materialTexture = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture11);
            GL.BindTexture(TextureTarget.Texture2D, materialTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                          imgWidth, imgHeight, 0,
                          PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
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
            (path, toolData) = MillingIO.ReadFile(filename);
            ToolDiameter = toolData.Diameter * MillingPath.SCALE;
            IsFlat = toolData.IsFlat;
            AdjustTool();
            return path.Points.Count > 0;
        }

        //public void WritePaths(List<PatchManager> patches, float[,] modelHeightMap, string location, MillingPathParams pathParams)
        //{
        //    WriteRoughPath(modelHeightMap, location, pathParams.StartIndex, pathParams.ReductionEpsRough);
        //    WriteBasePaths(patches, pathParams.ReductionEpsBase);
        //}

        public void WriteRoughPath(float[,] modelHeightMap, float reductionEps, string location, int startIndex)
        {
            const int diamMil = 16;
            var toolData = new ToolData(false, diamMil);
            int mapDimX = modelHeightMap.GetLength(0), mapDimZ = modelHeightMap.GetLength(1);
            float diamEps = 2 * MillingPath.SCALE, stripWidth = 7 * MillingPath.SCALE; // TODO: should be params
            float radEps = diamEps / 2;
            float diamExt = (diamMil * MillingPath.SCALE) + diamEps, radExt = diamExt / 2, radExtSq = radExt * radExt;

            int gridRadX = (int)Math.Ceiling(radExt / SizeX * mapDimX);
            int gridRadZ = (int)Math.Ceiling(radExt / SizeZ * mapDimZ);
            float offsetStepX = radExt / gridRadX, offsetStepZ = radExt / gridRadZ;
            var toolOffsets = new float[2 * gridRadX + 1, 2 * gridRadZ + 1];
            for (int i = 0; i < toolOffsets.GetLength(0); i++)
            {
                Vector2 pt = new Vector2((i - gridRadX) * offsetStepX, 0);
                for (int j = 0; j < toolOffsets.GetLength(1); j++)
                {
                    pt.Y = (j - gridRadZ) * offsetStepZ;
                    float lenSq = pt.LengthSquared;
                    if (lenSq < radExtSq)
                    {
                        float h = (float)Math.Sqrt(Math.Max(0, radExtSq - pt.LengthSquared));
                        toolOffsets[i, j] = radExt - h;
                    }
                    else
                    {
                        toolOffsets[i, j] = -1;
                    }
                }
            }

            // TODO: add border?
            var minLegalHeight = new float[mapDimX, mapDimZ];
            for (int i = 0; i < mapDimX; i++)
            {
                for (int j = 0; j < mapDimZ; j++)
                {
                    float height = 0;
                    for (int ii = Math.Max(0, gridRadX - i); ii < toolOffsets.GetLength(0) && i + ii < mapDimX; ii++)
                    {
                        for (int jj = Math.Max(0, gridRadZ - j); jj < toolOffsets.GetLength(1) && j + jj < mapDimZ; jj++)
                        {
                            if (toolOffsets[ii, jj] >= 0)
                            {
                                height = Math.Max(height, modelHeightMap[i + ii - gridRadX, j + jj - gridRadZ] - toolOffsets[ii, jj] + radEps);
                            }
                        }
                    }
                    minLegalHeight[i, j] = height;
                }
            }

            int WorldToHeightX(float x)
            {
                float x1 = -SizeX / 2;
                return (int)Math.Round(((x - x1) / SizeX) * (mapDimX - 1));
            }
            int WorldToHeightZ(float z)
            {
                float z1 = -SizeZ / 2;
                return (int)Math.Round(((z - z1) / SizeZ) * (mapDimZ - 1));
            }

            //float HeightToWorldX(int x)
            //{
            //    float x1 = -SizeX / 2;
            //    return (x * SizeX) / (mapDimX - 1) + x1;
            //}
            float HeightToWorldZ(int z)
            {
                float z1 = -SizeZ / 2;
                return (z * SizeZ) / (mapDimZ - 1) + z1;
            }

            int stripCount = (int)Math.Floor(SizeX / stripWidth);
            float edgeMultX = stripCount / 2.0f + 1;
            float edgeZ = SizeZ / 2 + stripWidth;
            float finalY = BaseHeight + PATH_BASE_DIST + diamEps / 2, y = (SizeY + finalY) / 2;
            int directionX = 1, directionZ = 1;

            var pts = new List<Vector3>
            {
                new Vector3(0, SizeY + TOOL_DIST, 0),
                new Vector3(-directionX * edgeMultX * stripWidth, SizeY + TOOL_DIST, -directionZ * edgeZ)
            };
            for (int i = 0; i < 2; i++)
            {
                for (float ix = -directionX * edgeMultX; ix * directionX <= edgeMultX; ix += directionX)
                {
                    float x = ix * stripWidth, z = directionZ * edgeZ;
                    int gridX = WorldToHeightX(x);
                    int gridStartZ = WorldToHeightZ(-z);
                    int gridEndZ = WorldToHeightZ(z);
                    bool prevSkipped = false;
                    pts.Add(new Vector3(x, y, -z));

                    if (gridX >= 0 && gridX < minLegalHeight.GetLength(0))
                    {
                        for (int zz = gridStartZ; zz != gridEndZ; zz += directionZ)
                        {
                            if (zz >= 0 && zz < minLegalHeight.GetLength(1))
                            {
                                if (minLegalHeight[gridX, zz] > y)
                                {
                                    if (prevSkipped)
                                    {
                                        pts.Add(new Vector3(x, y, HeightToWorldZ(zz - directionZ)));
                                    }
                                    pts.Add(new Vector3(x, minLegalHeight[gridX, zz], HeightToWorldZ(zz)));
                                    prevSkipped = false;
                                }
                                else
                                {
                                    prevSkipped = true;
                                }
                            }
                        }
                    }

                    pts.Add(new Vector3(x, y, z));
                    directionZ = -directionZ;
                }
                y = finalY;
                directionX = -1;
            }

            pts = DouglasPeucker(pts, reductionEps); // TODO: per every line?
            pts.Add(new Vector3(directionX * edgeMultX * stripWidth, SizeY + TOOL_DIST, -directionZ * edgeZ));
            pts.Add(new Vector3(0, SizeY + TOOL_DIST, 0));
            MillingIO.SaveFile(pts, toolData, location, "1", startIndex);
        }

        private static class FishPart
        {
            public const int Head           = 0;
            public const int Collar         = 1;
            public const int Body           = 2;
            public const int MedLowerFin    = 3;
            public const int MedUpperFin    = 4;
            public const int LongFin        = 5;
            public const int SmallInnerFin  = 6;
            public const int SmallOuterFin  = 7;
            public const int Count          = 8;
        }

        // TODO: void
        public GraphVisualizer WriteBasePaths(List<PatchManager> patches, float reductionEps, string location, int startIndex)
        {
            if (patches.Count != FishPart.Count) // prevent an exception when the dumdum that is me tries to find paths before loading the model
            {
                //return;
                return null;
            }

            ResetGraphStructures();
            float y = BaseHeight + PATH_BASE_DIST;
            var (basePatch, _) = CerealFactory.CreatePatchC0(new Vector3(0, y, 0), PatchType.Simple, SizeX, SizeZ, 1, 1);
            var finderParams = new IntersectionFinderParams();
            int divs = 4;
            var segments = new List<ContourSegment>();

            bool success = true;
            // starting with inner body because it's needed for both sides
            success = success && AddIntersection(segments, reductionEps, finderParams, basePatch, patches[FishPart.Body], divs); // inner
            success = success && AddIntersection(segments, reductionEps, finderParams, basePatch, patches[FishPart.Body], divs, new Vector3(6.42f, y, -4.7f)); // outer

            success = success && AddIntersection(segments, reductionEps, finderParams, basePatch, patches[FishPart.Head], divs, new Vector3(-2.5f, y, 0)); // inner
            success = success && AddIntersection(segments, reductionEps, finderParams, basePatch, patches[FishPart.Head], divs); // outer

            success = success && AddIntersection(segments, reductionEps, finderParams, basePatch, patches[FishPart.Collar], divs, new Vector3(-0.5f, y, -3)); // inner
            success = success && AddIntersection(segments, reductionEps, finderParams, basePatch, patches[FishPart.Collar], divs); // outer

            finderParams.CurveEps = 1e-5f;
            success = success && AddIntersection(segments, reductionEps, finderParams, basePatch, patches[FishPart.MedLowerFin], divs, new Vector3(3, y, 2));
            success = success && AddIntersection(segments, reductionEps, finderParams, basePatch, patches[FishPart.MedUpperFin], divs, new Vector3(3, y, -3));

            finderParams.CurveEps = 1e-6f;
            success = success && AddIntersection(segments, reductionEps, finderParams, basePatch, patches[FishPart.LongFin], divs, new Vector3(6.42f, y, -4.7f));
            success = success && AddIntersection(segments, reductionEps, finderParams, basePatch, patches[FishPart.SmallInnerFin], divs, new Vector3(-1.5f, y, 0));
            success = success && AddIntersection(segments, reductionEps, finderParams, basePatch, patches[FishPart.SmallOuterFin], divs);

            if (!success) // something has gone terribly wrong
            {
                //return;
                return null;
            }

            var (graph, graphCount) = GetContourGraph(segments, 4);

            Vector3 prevPoint = new Vector3(-SizeX / 2, y, -SizeZ / 2); // top-left corner
            int curVertex = 0;
            float bestDistSq = float.MaxValue;
            for (int i = 0; i < graphCount; i++)
            {
                if (!graph.ContainsKey(i)) continue;
                float distSq = (prevPoint - graph[i].Point).LengthSquared;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    curVertex = i;
                }
            }

            var contour = GetPathFromGraph(graph, graphCount, curVertex, prevPoint);

            // building base path
            const int baseDiamMil = 12, contourDiamMil = 10;
            const float baseDiam = baseDiamMil * MillingPath.SCALE, contourDiam = contourDiamMil * MillingPath.SCALE;
            const float baseEps = baseDiam / 20.0f;

            var (offsetBasePoints, _) = OffsetContour(contour, baseDiam / 2.0f, true, 12);
            //MillingIO.SaveFile(offsetBasePoints, new ToolData(true, baseDiamMil), location, "3", startIndex); // TODO: separate offset
            //ToolDiameter = baseDiam * MillingPath.SCALE;
            //IsFlat = true;
            //path = new MillingPath(offsetBasePoints); return; // TODO
            
            float middleOffset = 1e-4f;
            float stripWidth = baseDiam - baseEps;
            int stripCount = (int)Math.Floor(SizeX / stripWidth);
            float edgeMultX = stripCount / 2.0f + 1;
            float edgeZ = SizeZ / 2 + stripWidth;
            var zigZagPoints = new List<Vector3>();
            //for (int x = -8; x <= 8; x += 2) // TODO: proper range!
            //{
            float ix;
            for (ix = -edgeMultX; ix <= edgeMultX; ix += 2)
            {
                float x = ix * stripWidth, xPlus = (ix + 1) * stripWidth;
                zigZagPoints.AddMany(
                    new Vector3(x, y, middleOffset),
                    new Vector3(x, y, edgeZ),
                    new Vector3(xPlus, y, edgeZ)
                    );
                //if (x < 8)
                if (ix + 2 <= edgeMultX)
                {
                    zigZagPoints.Add(new Vector3(xPlus, y, middleOffset));
                }
            }

            //for (int x = 8; x >= -8; x -= 2) // TODO: proper range!
            //{
            bool first = true;
            for (ix -= 2; ix >= -edgeMultX; ix -= 2)
            {
                float x = ix * stripWidth, xPlus = (ix + 1) * stripWidth;
                //if (x < 8)
                if (!first)
                {
                    zigZagPoints.Add(new Vector3(xPlus, y, -middleOffset));
                }
                first = false;
                zigZagPoints.AddMany(
                    new Vector3(xPlus, y, -edgeZ),
                    new Vector3(x, y, -edgeZ),
                    new Vector3(x, y, -middleOffset)
                    );
            }
            var fakeSegment1 = new List<Vector3> { new Vector3(-9, y, -middleOffset), new Vector3(-8, y, -middleOffset) };
            var fakeSegment2 = new List<Vector3> { new Vector3(10, y, middleOffset), new Vector3(9, y, middleOffset) };

            ResetGraphStructures();
            int halfCount = offsetBasePoints.Count / 2;
            var baseSegments = new List<ContourSegment> { new ContourSegment(zigZagPoints),
                new ContourSegment(fakeSegment1), new ContourSegment(fakeSegment2),
                new ContourSegment(offsetBasePoints.Take(halfCount).ToList()),
                new ContourSegment(offsetBasePoints.Skip(halfCount).ToList())};
            var (baseGraph, baseGraphCount) = GetContourGraph(baseSegments, 0);
            var basePath = GetPathFromGraph(baseGraph, baseGraphCount, 0, new Vector3(-8, y, -2));

            var basePathPoints = new List<Vector3>();
            foreach (var edge in basePath)
            {
                basePathPoints.AddRange(edge.Points);
            }

            MillingIO.SaveFile(basePathPoints, new ToolData(true, baseDiamMil), location, "2", startIndex);
            ToolDiameter = baseDiamMil * MillingPath.SCALE;
            IsFlat = true;
            path = new MillingPath(basePathPoints);

            var (contourBasePoints, visualizer) = OffsetContour(contour, contourDiam / 2.0f, true, 6);
            MillingIO.SaveFile(contourBasePoints, new ToolData(true, contourDiamMil), location, "3", startIndex);
            return visualizer;
        }

        public void WriteDetailPath(List<PatchManager> patches, float reductionEps, string location, int startIndex)
        {
            const int detailDiamMil = 8;
            const float detailRad = detailDiamMil * MillingPath.SCALE / 2;
            const float detailBaseEps = 0.01f;

            var offsetPatches = new List<OffsetSurface>();
            foreach (var patch in patches)
            {
                offsetPatches.Add(new OffsetSurface(patch, detailRad));
            }

            float yMax = BaseHeight + PATH_BASE_DIST + detailBaseEps;
            // only body for testing purposes
            var bodyPatch = offsetPatches[2];
            var lines = new List<List<Vector3>>();
            float uStep = 0.02f, vStep = 0.1f;
            for (float u = 0; u < bodyPatch.UScale; u += uStep)
            {
                var line = new List<Vector3>();
                int firstBelow = -1;
                for (float v = 0; v < bodyPatch.VScale; v += vStep)
                {
                    var pt = bodyPatch.GetUVPoint(u, v);
                    pt.Y -= detailRad;
                    if (pt.Y > yMax)
                    {
                        line.Add(pt);
                    }
                    else if(firstBelow == -1)
                    {
                        firstBelow = line.Count;
                    }
                }
                if (firstBelow != -1)
                {
                    var lineTmp = new List<Vector3>();
                    lineTmp.AddRange(line.Skip(firstBelow));
                    lineTmp.AddRange(line.Take(firstBelow));
                    line = lineTmp;
                }
                lines.Add(line);
            }

            var tmp = new List<Vector3>();
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.Count < 1) continue;
                if (i % 2 == 0) line.Reverse();
                tmp.Add(new Vector3(line[0].X, yMax + TOOL_DIST, line[0].Z));
                tmp.AddRange(line);
                tmp.Add(new Vector3(line[line.Count - 1].X, yMax + TOOL_DIST, line[line.Count - 1].Z));
            }

            ToolDiameter = detailDiamMil * MillingPath.SCALE;
            IsFlat = false;
            path = new MillingPath(tmp);
        }

        private void ResetGraphStructures()
        {
            ContourSegment.ResetCounter();
            ContourIntersection.ResetCounter();
        }

        // TODO: remove graph return
        private (List<Vector3>, GraphVisualizer) OffsetContour(List<ContourEdge> contour, float offset, bool ccw, int pathVertexInd)
        {
            var offsetSegments = new List<ContourSegment>();
            var vec = Vector3.UnitY;
            if (!ccw)
            {
                vec = -vec;
            };

            ResetGraphStructures();
            foreach (var edge in contour)
            {
                var points = edge.Points;
                var offsetPoints = new List<Vector3>();
                for (int i = 0; i < points.Count - 1; i++)
                {
                    Vector3 p1 = points[i], p2 = points[i + 1];
                    Vector3 diff = p2 - p1;
                    if (diff.LengthSquared == 0)
                    {
                        continue;
                    }
                    Vector3 normal = Vector3.Cross(diff, vec);
                    normal.Normalize();
                    offsetPoints.AddMany(p1 + normal * offset, p2 + normal * offset);
                }
                offsetSegments.Add(new ContourSegment(offsetPoints));
            }

            // add fake segments connecting actual segments
            // TODO: join with circular segments instead!
            int actualSegCount = offsetSegments.Count;
            for (int i = 0; i < actualSegCount; i++)
            {
                var seg1 = offsetSegments[i];
                var seg2 = offsetSegments[(i + 1) % actualSegCount];
                var points1 = seg1.Points;
                var points2 = seg2.Points;
                if (IntersectSegments(seg1, seg2).Count == 0)
                {
                    Vector3 p1 = points1[points1.Count - 1], p2 = points2[0];
                    List<Vector3> fakeSegment = new List<Vector3> { p1, p2 };
                    offsetSegments.Add(new ContourSegment(fakeSegment));
                }
            }

            ResetGraphStructures();
            var (graph, graphCount) = GetContourGraph(offsetSegments, 0);

            // TODO: REMOVE ME
            Console.WriteLine("\nAAA\n");
            for (int i = 0; i < graphCount; i++)
            {
                var tmpPath = GetPathFromGraph(graph, graphCount, i, new Vector3(-SizeX / 2, BaseHeight + PATH_BASE_DIST, -SizeZ / 2), false);
                var tmpPoints = new List<Vector3>();
                foreach (var edge in tmpPath)
                {
                    tmpPoints.AddRange(edge.Points);
                }
                Console.WriteLine($"{i}, {tmpPoints.Count}");
            }

            var fixedPath = GetPathFromGraph(graph, graphCount, pathVertexInd, new Vector3(-SizeX / 2, BaseHeight + PATH_BASE_DIST, -SizeZ / 2), false);
            var fixedPoints = new List<Vector3>();
            foreach (var edge in fixedPath)
            {
                fixedPoints.AddRange(edge.Points);
            }
            return (fixedPoints, new GraphVisualizer(graph));
        }

        private List<ContourEdge> GetPathFromGraph(
            Dictionary<int, ContourVertex> graph, int graphCount, int startVertex, Vector3 prevPoint, bool reverseCheck = false)
        {
            int prevVertex = -1;
            bool[] isVertUsed = new bool[graphCount];
            isVertUsed[startVertex] = true;
            var contour = new List<ContourEdge>();
            Vector3 curPoint = graph[startVertex].Point;

            Console.WriteLine("\nBBB\n");
            while (true)
            {
                int bestInd = -1;
                double bestVal = -4;
                Vector2 curDir = new Vector2(curPoint.X - prevPoint.X, curPoint.Z - prevPoint.Z);
                curDir.Normalize();
                for (int i = 0; i < graph[startVertex].OutEdges.Count; i++)
                {
                    var edge = graph[startVertex].OutEdges[i];
                    if (edge.To != prevVertex)
                    {
                        var potentialNext = edge.Points[1];
                        Vector2 potentialDir = new Vector2(potentialNext.X - prevPoint.X, potentialNext.Z - prevPoint.Z);
                        double val = 0;
                        if (potentialDir.Length == 0 && edge.Points.Count > 2)
                        {
                            // one more try
                            potentialNext = edge.Points[2];
                            potentialDir = new Vector2(potentialNext.X - prevPoint.X, potentialNext.Z - prevPoint.Z);
                        }
                        if (potentialDir.Length > 0)
                        {
                            potentialDir.Normalize();
                            float brodkaCross = curDir.X * potentialDir.Y - curDir.Y * potentialDir.X;
                            val = Math.Acos(Math.Max(-1, Math.Min(1, Vector2.Dot(curDir, potentialDir)))) * Math.Sign(brodkaCross);
                        }
                        if (val > bestVal)
                        {
                            bestInd = i;
                            bestVal = val;
                        }
                        Console.WriteLine($"{i} to {edge.To}: {val}");
                    }
                }
                if (bestInd == -1)
                {
                    break;
                }
                var bestEdge = graph[startVertex].OutEdges[bestInd];
                int bestVertex = bestEdge.To;
                contour.Add(bestEdge);
                if (isVertUsed[bestVertex])
                {
                    break;
                }
                isVertUsed[bestVertex] = true;
                prevPoint = bestEdge.Points[bestEdge.Points.Count - 2];
                curPoint = bestEdge.Points[bestEdge.Points.Count - 1];
                prevVertex = startVertex;
                startVertex = bestVertex;
            }

            //var contourPoints = new List<Vector3>();
            //foreach (var edge in contour)
            //{
            //    contourPoints.AddRange(edge.Points);
            //}
            return contour;
        }

        private (Dictionary<int, ContourVertex> graph, int graphCount) GetContourGraph(
            List<ContourSegment> segments, int minEdgeCount)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                for (int j = i + 1; j < segments.Count; j++)
                {
                    var intersections = IntersectSegments(segments[i], segments[j]);
                    foreach (var inter in intersections)
                    {
                        segments[i].Intersections.Add(inter);
                        segments[j].Intersections.Add(inter);
                    }
                }
            }

            int maxInd = -1;
            var graph = new Dictionary<int, ContourVertex>();
            foreach (var segment in segments)
            {
                segment.Intersections.Sort(
                    (i1, i2) =>
                    {
                        int ind1 = segment.Id == i1.SegmentFrom ? i1.IndexFrom : i1.IndexTo;
                        int ind2 = segment.Id == i2.SegmentFrom ? i2.IndexFrom : i2.IndexTo;
                        if (ind1 == ind2)
                        {
                            var lastPoint = segment.Points[ind1];
                            var l1 = (i1.Point - lastPoint).LengthSquared;
                            var l2 = (i2.Point - lastPoint).LengthSquared;
                            return l2.CompareTo(l1);
                        }
                        return ind2.CompareTo(ind1); // descending
                    });
                var edges = new List<ContourEdge>();
                var interIndices = new List<int>();
                bool first = true;
                foreach (var inter in segment.Intersections)
                {
                    int ind = (segment.Id == inter.SegmentFrom ? inter.IndexFrom : inter.IndexTo) + 1;
                    interIndices.Add(inter.Id);
                    maxInd = Math.Max(maxInd, inter.Id);
                    if (!first)
                    {
                        var newPoints = new List<Vector3> { inter.Point };
                        newPoints.AddRange(segment.Points.Skip(ind));
                        edges.Add(new ContourEdge(newPoints));
                    }
                    first = false;

                    segment.Points.RemoveRange(ind, segment.Points.Count - ind);
                    segment.Points.Add(inter.Point);
                }
                //if (!removeDangles)
                //{
                //    var finPoints = new List<Vector3>();
                //    finPoints.AddRange(segment.Points);
                //    edges.Add(new ContourEdge(finPoints));
                //}

                for (int i = 0; i < edges.Count; i++)
                {
                    int i1 = interIndices[i + 1], i2 = interIndices[i];
                    if (i1 == i2)
                    {
                        continue;
                    }

                    var edge1 = edges[i];
                    if (edge1.Points.Count < minEdgeCount) continue;
                    edge1.From = i1;
                    edge1.To = i2;
                    if (!graph.ContainsKey(i1))
                    {
                        graph.Add(i1, new ContourVertex(i1, edge1.Points[0]));
                    }
                    graph[i1].OutEdges.Add(edge1);

                    var revPoints = new List<Vector3>(edge1.Points);
                    revPoints.Reverse();
                    var edge2 = new ContourEdge(revPoints);
                    edge2.From = i2;
                    edge2.To = i1;
                    if (!graph.ContainsKey(i2))
                    {
                        graph.Add(i2, new ContourVertex(i2, edge2.Points[0]));
                    }
                    graph[i2].OutEdges.Add(edge2);
                }
            }

            return (graph, maxInd + 1);
        }

        private bool AddIntersection(List<ContourSegment> segments,
            float eps, IntersectionFinderParams finderParams, ISurface P, ISurface Q, int divs)
        {
            var (iRes, interPoints, _) = IntersectionFinder.FindIntersectionDataWithoutStartPoint(finderParams, P, Q, divs);
            if (iRes == IntersectionResult.OK)
            {
                segments.Add(new ContourSegment(DouglasPeucker(interPoints, eps)));
                return true;
            }
            return false;
        }

        private bool AddIntersection(List<ContourSegment> segments,
            float eps, IntersectionFinderParams finderParams, ISurface P, ISurface Q, int divs, Vector3 start)
        {
            var (iRes, interPoints, _) = IntersectionFinder.FindIntersectionDataWithStartPoint(finderParams, P, Q, divs, start);
            if (iRes == IntersectionResult.OK)
            {
                segments.Add(new ContourSegment(DouglasPeucker(interPoints, eps)));
                return true;
            }
            return false;
        }

        private List<ContourIntersection> IntersectSegments(ContourSegment seg1, ContourSegment seg2)
        {
            bool self = seg1 == seg2;
            float pointDistEps = 1e-2f;
            if (self)
            {
                pointDistEps = 0;
            }
            float pdeSq = pointDistEps * pointDistEps;
            // we can assume that all Y are equal to BaseHeight + PATH_BASE_DIST
            float y = BaseHeight + PATH_BASE_DIST;
            var intersections = new List<ContourIntersection>();
            for (int i = 0; i < seg1.Points.Count - 1; i++)
            {
                Vector2 a1 = new Vector2(seg1.Points[i].X, seg1.Points[i].Z);
                Vector2 b1 = new Vector2(seg1.Points[i + 1].X, seg1.Points[i + 1].Z);
                for (int j = 0; j < seg2.Points.Count - 1; j++)
                {
                    if (self && Math.Abs(i - j) < 2) continue;
                    Vector2 a2 = new Vector2(seg2.Points[j].X, seg2.Points[j].Z);
                    Vector2 b2 = new Vector2(seg2.Points[j + 1].X, seg2.Points[j + 1].Z);
                    if ((a1 - a2).LengthSquared < pdeSq ||
                        (a1 - b2).LengthSquared < pdeSq ||
                        (b1 - a2).LengthSquared < pdeSq ||
                        (b1 - b2).LengthSquared < pdeSq ||
                        MathHelper.HasIntersection(a1, b1, a2, b2))
                    {
                        var inter2d = MathHelper.GetIntersection(a1, b1, a2, b2, out _);
                        var inter = new Vector3(inter2d.X, y, inter2d.Y);
                        intersections.Add(new ContourIntersection(seg1.Id, seg2.Id, i, j, inter));
                    }
                }
            }
            return intersections;
        }

        private List<Vector3> DouglasPeucker(List<Vector3> pts, float eps)
        {
            float PointDist(Vector3 p, Vector3 a, Vector3 b)
            {
                Vector3 dir = b - a;
                float dirLength = dir.Length;
                return dirLength > 0 ? Vector3.Cross(dir, a - p).Length / dirLength : 0;
            }

            int n = pts.Count;
            if (n < 3)
            {
                return pts;
            }

            float dMax = 0;
            int iMax = 0;
            for (int i = 1; i < n - 1; i++)
            {
                float d = PointDist(pts[i], pts[0], pts[n - 1]);
                if (d > dMax)
                {
                    dMax = d;
                    iMax = i;
                }
            }

            var res = new List<Vector3>();
            if (dMax > eps)
            {
                var first = DouglasPeucker(pts.Take(iMax + 1).ToList(), eps);
                var second = DouglasPeucker(pts.Skip(iMax).ToList(), eps);
                res.AddRange(first.Take(first.Count - 1));
                res.AddRange(second);
            }
            else
            {
                res.AddMany(pts[0], pts[n - 1]);
            }
            return res;
        }

        public void Render(ShaderManager shader)
        {
            if (path != null)
            {
                GL.Enable(EnableCap.CullFace);
                if (DisplayMaterial)
                {
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
                }

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
            var args = new PropertyChangedEventArgs(name);
            if (Application.Current != null)
            {
                foreach (Delegate d in PropertyChanged.GetInvocationList())
                {
                    Application.Current.Dispatcher.Invoke(d, this, args);
                }
            }
        }

        // TODO: private all these
        public class ContourSegment
        {
            private static int counter = 0;
            public int Id { get; }
            public List<ContourIntersection> Intersections { get; } = new List<ContourIntersection>();
            public List<Vector3> Points { get; set; }

            public ContourSegment(List<Vector3> points)
            {
                Id = counter++;
                Points = points;
            }

            public static void ResetCounter()
            {
                counter = 0;
            }
        }

        public class ContourIntersection
        {
            private static int counter = 0;
            public int Id { get; }
            public int SegmentFrom { get; set; }
            public int SegmentTo { get; set; }
            public int IndexFrom { get; set; }
            public int IndexTo { get; set; }
            public Vector3 Point { get; set; }

            public ContourIntersection(int from, int to, int indFrom, int indTo, Vector3 point)
            {
                Id = counter++;
                SegmentFrom = from;
                SegmentTo = to;
                IndexFrom = indFrom;
                IndexTo = indTo;
                Point = point;
            }

            public static void ResetCounter()
            {
                counter = 0;
            }
        }

        public class ContourVertex
        {
            public int Id { get; }
            public Vector3 Point { get; }
            public List<ContourEdge> OutEdges { get; } = new List<ContourEdge>();

            public ContourVertex(int id, Vector3 point)
            {
                Id = id;
                Point = point;
            }
        }

        public class ContourEdge
        {
            public int From { get; set; }
            public int To { get; set; }
            public List<Vector3> Points { get; } = new List<Vector3>();
            public ContourEdge(List<Vector3> points)
            {
                Points = points;
            }
        }

        public class GraphVisualizer
        {
            private Random colorRand = new Random();
            private List<PolyLine> lines = new List<PolyLine>();
            private List<CustomLine> vertexMarkers = new List<CustomLine>();

            public GraphVisualizer(Dictionary<int, ContourVertex> graph)
            {
                foreach (var vertex in graph)
                {
                    float r = (float)colorRand.NextDouble();
                    float g = (float)colorRand.NextDouble();
                    float b = (float)colorRand.NextDouble();
                    var p1 = vertex.Value.Point;
                    var p2 = p1 + Vector3.UnitY * vertex.Value.OutEdges.Count;
                    vertexMarkers.Add(new CustomLine(p1, p2, new Vector4(r, g, b, 1)));
                    foreach (var edge in vertex.Value.OutEdges)
                    {
                        r = (float)colorRand.NextDouble();
                        g = (float)colorRand.NextDouble();
                        b = (float)colorRand.NextDouble();
                        if (edge.From < edge.To)
                            lines.Add(new PolyLine(edge.Points, new Vector4(r, g, b, 1)));
                    }
                }
            }

            public GraphVisualizer(List<ContourSegment> segments)
            {
                foreach (var segment in segments)
                {
                    float r = (float)colorRand.NextDouble();
                    float g = (float)colorRand.NextDouble();
                    float b = (float)colorRand.NextDouble();
                    lines.Add(new PolyLine(segment.Points, new Vector4(r, g, b, 1)));
                }
            }

            public void Render(ShaderManager shader)
            {
                foreach (var line in lines)
                {
                    line.Render(shader);
                }
                foreach (var marker in vertexMarkers)
                {
                    marker.Render(shader);
                }
            }
        }
    }
}
