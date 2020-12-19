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
        private const float TOOL_DIST = 2;
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
        public bool DisplayTool { get; set; } = true;
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

        public MillingPathData GetRoughPath(float[,] modelHeightMap, float reductionEps)
        {
            const int diamMil = 16;
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

            pts = DouglasPeucker(pts, reductionEps);
            pts.Add(new Vector3(directionX * edgeMultX * stripWidth, SizeY + TOOL_DIST, -directionZ * edgeZ));
            pts.Add(new Vector3(0, SizeY + TOOL_DIST, 0));
            //MillingIO.SaveFile(pts, toolData, location, "1", startIndex);
            return new MillingPathData(pts, false, diamMil);
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

        public (MillingPathData basePath, MillingPathData contourPath, MillingPathData innerPath) GetBasePaths(
            List<PatchManager> patches, float reductionEps)
        {
            if (patches.Count != FishPart.Count) // prevent an exception when the dumdum that is me tries to find paths before loading the model
            {
                return (null, null, null);
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
                return (null, null, null);
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

            var (offsetBasePoints, _) = OffsetContour(contour, baseDiam / 2.0f, true, 0);
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

            float maxX = edgeMultX * stripWidth;
            var fakeSegment1 = new List<Vector3> { new Vector3(-maxX - 1, y, -middleOffset), new Vector3(-maxX + 0.1f, y, -middleOffset) };
            var fakeSegment2 = new List<Vector3> { new Vector3(-maxX - 1, y, middleOffset), new Vector3(-maxX + 0.1f, y, middleOffset) };
            var fakeSegment3 = new List<Vector3> { new Vector3(maxX + 1, y, -middleOffset), new Vector3(maxX - 0.1f, y, -middleOffset) };
            var fakeSegment4 = new List<Vector3> { new Vector3(maxX + 1, y, middleOffset), new Vector3(maxX - 0.1f, y, middleOffset) };

            ResetGraphStructures();
            int halfCount = offsetBasePoints.Count / 2;
            var baseSegments = new List<ContourSegment> { new ContourSegment(zigZagPoints),
                new ContourSegment(fakeSegment1), new ContourSegment(fakeSegment2),
                new ContourSegment(fakeSegment3), new ContourSegment(fakeSegment4),
                new ContourSegment(offsetBasePoints.Take(halfCount).ToList()),
                new ContourSegment(offsetBasePoints.Skip(halfCount).ToList())};
            var (baseGraph, baseGraphCount) = GetContourGraph(baseSegments, 0);

            var basePath = GetPathFromGraph(baseGraph, baseGraphCount, 0, new Vector3(-maxX - 2, y, 0));

            var firstBasePathPoint = basePath[0].Points[0];
            var basePathPoints = new List<Vector3>
            {
                new Vector3(0, SizeY + TOOL_DIST, 0),
                new Vector3(firstBasePathPoint.X - baseDiam, SizeY + TOOL_DIST, firstBasePathPoint.Z),
                new Vector3(firstBasePathPoint.X - baseDiam, firstBasePathPoint.Y, firstBasePathPoint.Z)
            };

            foreach (var edge in basePath)
            {
                basePathPoints.AddRange(edge.Points);
            }
            basePathPoints.RemoveAt(basePathPoints.Count - 1);

            var bottomZigZagPoints = new List<Vector3>();
            float bottomEdgeX = (ix + 2) * stripWidth, topEdgeX = -5;
            first = true;
            float z1 = -edgeZ;
            for (; z1 <= edgeZ; z1 += 2 * stripWidth)
            {
                if (!first)
                {
                    bottomZigZagPoints.Add(new Vector3(topEdgeX, y, z1));
                }
                first = false;
                bottomZigZagPoints.AddMany(
                    new Vector3(bottomEdgeX, y, z1),
                    new Vector3(bottomEdgeX, y, z1 + stripWidth),
                    new Vector3(topEdgeX, y, z1 + stripWidth)
                );
            }
            z1 -= stripWidth;

            ResetGraphStructures();
            var bottomFakeSegment1 = new List<Vector3> { new Vector3(bottomEdgeX - 1, y, -edgeZ), new Vector3(bottomEdgeX, y, -edgeZ) };
            var bottomFakeSegment2 = new List<Vector3> { new Vector3(bottomEdgeX - 1, y, z1), new Vector3(bottomEdgeX, y, z1) };
            var bottomBaseSegments = new List<ContourSegment> { new ContourSegment(bottomZigZagPoints),
                new ContourSegment(bottomFakeSegment1), new ContourSegment(bottomFakeSegment2),
                new ContourSegment(offsetBasePoints.Take(halfCount).ToList()),
                new ContourSegment(offsetBasePoints.Skip(halfCount).ToList())};
            var (bottomBaseGraph, bottomBaseGraphCount) = GetContourGraph(bottomBaseSegments, 0);

            var bottomBasePath = GetPathFromGraph(bottomBaseGraph, bottomBaseGraphCount, 0, new Vector3(bottomEdgeX - 1, y, -edgeZ - 1));
            foreach (var edge in bottomBasePath)
            {
                basePathPoints.AddRange(edge.Points);
            }
            var lastBasePathPoint = basePathPoints[basePathPoints.Count - 1];
            basePathPoints.AddMany(
                new Vector3(lastBasePathPoint.X, SizeY + TOOL_DIST, lastBasePathPoint.Z),
                new Vector3(0, SizeY + TOOL_DIST, 0));

            //MillingIO.SaveFile(basePathPoints, new ToolData(true, baseDiamMil), location, "2", startIndex);
            //ToolDiameter = baseDiamMil * MillingPath.SCALE;
            //IsFlat = true;
            //path = new MillingPath(basePathPoints);

            // outer contour path
            var (offsetContourPoints, _) = OffsetContour(contour, contourDiam / 2.0f, true, 6);
            var firstContourPathPoint = offsetContourPoints[0];
            var contourPathPoints = new List<Vector3>
            {
                new Vector3(0, SizeY + TOOL_DIST, 0),
                new Vector3(-SizeX / 2 - baseDiam - 1, SizeY + TOOL_DIST, firstContourPathPoint.Z),
                new Vector3(-SizeX / 2 - baseDiam - 1, firstContourPathPoint.Y, firstContourPathPoint.Z)
            };
            contourPathPoints.AddRange(offsetContourPoints);
            var lastContourPathPoint = contourPathPoints[contourPathPoints.Count - 1];
            contourPathPoints.AddMany(
                new Vector3(lastContourPathPoint.X, SizeY + TOOL_DIST, lastContourPathPoint.Z),
                new Vector3(0, SizeY + TOOL_DIST, 0));
            //MillingIO.SaveFile(contourPathPoints, new ToolData(true, contourDiamMil), location, "3", startIndex);

            // inner base
            prevPoint = new Vector3(0, y, 0); // center
            curVertex = 0;
            bestDistSq = float.MaxValue;
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

            var innerContour = GetPathFromGraph(graph, graphCount, curVertex, prevPoint);

            const int innerBaseDiamMil = 8;
            const float innerBaseDiam = innerBaseDiamMil * MillingPath.SCALE;
            const float innerBaseEps = 6 * MillingPath.SCALE;
            var (offsetInnerBasePoints, _) = OffsetContour(innerContour, innerBaseDiam / 2.0f, true, 0);

            // stripes along Z
            ResetGraphStructures();
            int innerHalfCount = offsetInnerBasePoints.Count / 2;
            var innerBaseSegmentsX = new List<ContourSegment>
            {
                new ContourSegment(offsetInnerBasePoints.Take(innerHalfCount + 1).ToList()),
                new ContourSegment(offsetInnerBasePoints.Skip(innerHalfCount).ToList())
            };

            float innerStripWidth = innerBaseDiam - innerBaseEps;
            float innerEdgeZ = SizeZ / 2 + innerStripWidth;
            float startX = -1.24f, endX = 4f; // a wizard gave me these. that's why they're called magic numbers.
            for (float xx = startX; xx <= endX; xx += innerStripWidth)
            {
                var stripe = new List<Vector3> {
                    new Vector3(xx, y, -innerEdgeZ),
                    new Vector3(xx, y, innerEdgeZ)
                };
                innerBaseSegmentsX.Add(new ContourSegment(stripe));
            }
            var (innerBaseGraphX, innerBaseGraphCountX) = GetContourGraph(innerBaseSegmentsX, 0);
            var innerBasePath1 = GetPathFromGraph(innerBaseGraphX, innerBaseGraphCountX, 61, Vector3.Zero, false, true);

            // stripes along X
            ResetGraphStructures();
            var innerBaseSegmentsZ = new List<ContourSegment>
            {
                new ContourSegment(offsetInnerBasePoints.Take(innerHalfCount + 1).ToList()),
                new ContourSegment(offsetInnerBasePoints.Skip(innerHalfCount).ToList())
            };

            float innerEdgeX = SizeX / 2 + innerStripWidth;
            float startZ = -4f, endZ = 4f; // this time the wizard was less precise, but it was enough
            for (float zz = startZ; zz <= endZ; zz += innerStripWidth)
            {
                var stripe = new List<Vector3> {
                    new Vector3(-innerEdgeX, y, zz),
                    new Vector3(innerEdgeX, y, zz)
                };
                innerBaseSegmentsZ.Add(new ContourSegment(stripe));
            }
            var (innerBaseGraphZ, innerBaseGraphCountZ) = GetContourGraph(innerBaseSegmentsZ, 0);
            //Console.WriteLine("\nAAA\n");
            //foreach (var i in innerBaseGraphZ.Keys)
            //{
            //    var tmpPath = GetPathFromGraph(innerBaseGraphZ, innerBaseGraphCountZ, i, Vector3.Zero, false, true);
            //    //var tmpPointsA = new List<Vector3>();
            //    //foreach (var edge in tmpPath)
            //    //{
            //    //    tmpPointsA.AddRange(edge.Points);
            //    //}
            //    Console.WriteLine($"{i}, {tmpPath.Count}");
            //}
            var innerBasePath2 = GetPathFromGraph(innerBaseGraphZ, innerBaseGraphCountZ, 37, Vector3.Zero, false, true);

            var firstInnerPathPoint1 = innerBasePath1[0].Points[0];
            var innerPathPoints = new List<Vector3>
            {
                new Vector3(0, SizeY + TOOL_DIST, 0),
                new Vector3(firstInnerPathPoint1.X, SizeY + TOOL_DIST, firstInnerPathPoint1.Z),
            };
            foreach (var edge in innerBasePath1)
            {
                innerPathPoints.AddRange(edge.Points);
            }
            var lastInnerPathPoint1 = innerPathPoints[innerPathPoints.Count - 1];
            lastInnerPathPoint1.Y = SizeY + TOOL_DIST;
            var firstInnerPathPoint2 = innerBasePath2[0].Points[0];
            firstInnerPathPoint2.Y = SizeY + TOOL_DIST;
            innerPathPoints.AddMany(lastInnerPathPoint1, firstInnerPathPoint2);
            foreach (var edge in innerBasePath2)
            {
                innerPathPoints.AddRange(edge.Points);
            }

            var lastInnerPathPoint2 = innerPathPoints[innerPathPoints.Count - 1];
            float closestDistSq = float.MaxValue;
            int bestIndex = 0;
            for (int i = 0; i < offsetInnerBasePoints.Count; i++)
            {
                float distSq = (lastInnerPathPoint2 - offsetInnerBasePoints[i]).LengthSquared;
                if (distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    bestIndex = i;
                }
            }
            innerPathPoints.AddRange(offsetInnerBasePoints.Skip(bestIndex));
            innerPathPoints.AddRange(offsetInnerBasePoints.Take(bestIndex));
            var lastInnerPathPoint = innerPathPoints[innerPathPoints.Count - 1];
            innerPathPoints.AddMany(
                new Vector3(lastInnerPathPoint.X, SizeY + TOOL_DIST, lastInnerPathPoint.Z),
                new Vector3(0, SizeY + TOOL_DIST, 0)
                );

            //MillingIO.SaveFile(innerPathPoints, new ToolData(false, innerBaseDiamMil), location, "4", startIndex);
            var basePathData = new MillingPathData(basePathPoints, true, baseDiamMil);
            var contourPathData = new MillingPathData(contourPathPoints, true, contourDiamMil);
            var innerPathData = new MillingPathData(innerPathPoints, false, innerBaseDiamMil);
            return (basePathData, contourPathData, innerPathData);
        }

        public MillingPathData GetDetailPath(List<PatchManager> patches)
        {
            if (patches.Count != FishPart.Count) // dumdumproof this too
            {
                return null;
            }

            const int detailDiamMil = 8;
            const float detailRad = detailDiamMil * MillingPath.SCALE / 2;
            const float detailBaseEps = 0.01f;

            var offsetPatches = new List<OffsetSurface>();
            foreach (var patch in patches)
            {
                offsetPatches.Add(new OffsetSurface(patch, detailRad + 0.01f));
            }

            // create all intersection objects
            var icms = new List<IntersectionCurveManager>();
            var finderParams = new IntersectionFinderParams();
            int divs = 4;

            var icm = GetIntersectionCurve(finderParams, offsetPatches[FishPart.Collar], offsetPatches[FishPart.Body], divs);
            if (icm == null) return null;
            icm.TrimModeP = TrimMode.SideA;
            icm.TrimModeQ = TrimMode.SideB;
            icms.Add(icm);

            icm = GetIntersectionCurve(finderParams, offsetPatches[FishPart.Body], offsetPatches[FishPart.MedUpperFin], divs);
            if (icm == null) return null;
            icm.TrimModeP = TrimMode.SideA;
            icm.TrimModeQ = TrimMode.SideB;
            icms.Add(icm);

            icm = GetIntersectionCurve(finderParams, offsetPatches[FishPart.Head], offsetPatches[FishPart.Body], divs);
            if (icm == null) return null;
            icm.TrimModeP = TrimMode.SideA;
            icm.TrimModeQ = TrimMode.SideA; // only exception!
            icms.Add(icm);

            icm = GetIntersectionCurve(finderParams, offsetPatches[FishPart.Head], offsetPatches[FishPart.SmallInnerFin], divs);
            if (icm == null) return null;
            icm.TrimModeP = TrimMode.SideA;
            icm.TrimModeQ = TrimMode.SideB;
            icms.Add(icm);

            icm = GetIntersectionCurve(finderParams, offsetPatches[FishPart.Head], offsetPatches[FishPart.SmallOuterFin], divs);
            if (icm == null) return null;
            icm.TrimModeP = TrimMode.SideA;
            icm.TrimModeQ = TrimMode.SideB;
            icms.Add(icm);

            icm = GetIntersectionCurve(finderParams, offsetPatches[FishPart.Body], offsetPatches[FishPart.MedLowerFin], divs);
            if (icm == null) return null;
            icm.TrimModeP = TrimMode.SideA;
            icm.TrimModeQ = TrimMode.SideB;
            icms.Add(icm);

            finderParams.StartMaxIterations = finderParams.CurveMaxIterations = 100;
            icm = GetIntersectionCurve(finderParams, offsetPatches[FishPart.Body], offsetPatches[FishPart.MedLowerFin], divs, new Vector3(0.1f, 2, 3.1f));
            if (icm == null) return null;
            icm.TrimModeP = TrimMode.SideA;
            icm.TrimModeQ = TrimMode.SideB;
            icms.Add(icm);
            finderParams.Reset();

            finderParams.StartMaxIterations = finderParams.CurveMaxIterations = 100;
            icm = GetIntersectionCurve(finderParams, offsetPatches[FishPart.Head], offsetPatches[FishPart.Collar], divs);
            if (icm == null) return null;
            icm.TrimModeP = TrimMode.SideA;
            icm.TrimModeQ = TrimMode.SideB;
            icms.Add(icm);
            finderParams.Reset();

            finderParams.CurveEps = 1e-5f;
            icm = GetIntersectionCurve(finderParams, offsetPatches[FishPart.Body], offsetPatches[FishPart.LongFin], divs);
            if (icm == null) return null;
            icm.TrimModeP = TrimMode.SideA;
            icm.TrimModeQ = TrimMode.SideB;
            icms.Add(icm);

            var partParams = new (float uStep, float vStep, float yBelowAllowed, bool trimCurveSum, bool doDistTest)[FishPart.Count];
            partParams[FishPart.Head]           = (0.04f, 0.02f, 0.00f, false, false);
            partParams[FishPart.Collar]         = (0.04f, 0.02f, 0.00f, false, true);
            partParams[FishPart.Body]           = (0.04f, 0.01f, 0.00f, false, false);
            partParams[FishPart.MedLowerFin]    = (0.04f, 0.10f, 0.10f, true, false);
            partParams[FishPart.MedUpperFin]    = (0.10f, 0.10f, 0.10f, false, false);
            partParams[FishPart.LongFin]        = (0.10f, 0.05f, 0.00f, false, false);
            partParams[FishPart.SmallInnerFin]  = (0.10f, 0.10f, 0.10f, false, false);
            partParams[FishPart.SmallOuterFin]  = (0.10f, 0.10f, 0.00f, false, false);

            float yMax = BaseHeight + PATH_BASE_DIST + detailBaseEps;
            var lines = new List<List<Vector3>>();
            for (int i = 0; i < FishPart.Count; i++)
            {
                var curPatch = offsetPatches[i];
                var (uStep, vStep, yBelowAllowed, trimCurveSum, doDistTest) = partParams[i];
                for (float u = 0; u <= curPatch.UScale; u += uStep)
                {
                    var line = new List<Vector3>();
                    bool skipping = false;
                    for (float v = 0; v <= curPatch.VScale; v += vStep)
                    {
                        var pt = curPatch.GetUVPoint(u, v);
                        pt.Y -= detailRad;
                        bool remove = false;
                        if (pt.Y >= yMax - yBelowAllowed)
                        {
                            pt.Y = Math.Max(pt.Y, yMax);
                            if (trimCurveSum)
                            {
                                remove = true;
                                foreach (var curve in curPatch.GetIntersectionCurves())
                                {
                                    if (!curve.IsPointInside(curPatch, u, v))
                                    {
                                        remove = false;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                foreach (var curve in curPatch.GetIntersectionCurves())
                                {
                                    if (curve.IsPointInside(curPatch, u, v))
                                    {
                                        remove = true;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            remove = true;
                        }

                        bool prevSkipped = skipping;
                        if (remove && !skipping) // start skipping
                        {
                            skipping = true;
                            if (line.Count > 0)
                            {
                                var ptUp = line[line.Count - 1];
                                ptUp.Y = SizeY + TOOL_DIST;
                                line.Add(ptUp);
                            }
                        }
                        else if (!remove && skipping) // end skipping
                        {
                            skipping = false;
                            var ptUp = pt;
                            ptUp.Y = SizeY + TOOL_DIST;
                            line.Add(ptUp);
                        }

                        if (!skipping)
                        {
                            if (doDistTest && v > 0 && !prevSkipped)
                            {
                                var prevPt = curPatch.GetUVPoint(u, v - vStep);
                                prevPt.Y -= detailRad;
                                if ((pt - prevPt).LengthSquared > 0.25f)
                                {
                                    Vector3 normal = (curPatch.GetNormal(u, v) + curPatch.GetNormal(u, v - vStep)) / 2;
                                    normal.Normalize();
                                    Vector3 prevOffset = prevPt + detailRad * normal, curOffset = pt + detailRad * normal;
                                    prevOffset.Y = Math.Max(prevOffset.Y, yMax);
                                    curOffset.Y = Math.Max(curOffset.Y, yMax);
                                    line.AddMany(prevOffset, curOffset);
                                }
                            }
                            line.Add(pt);
                        }
                    }
                    lines.Add(line);
                }
            }

            foreach (var curve in icms)
            {
                var curveSegments = new List<List<Vector3>>();
                curveSegments.Add(new List<Vector3>());
                foreach (var point in curve.Points)
                {
                    var pt = point;
                    pt.Y -= detailRad;
                    if (pt.Y < yMax)
                    {
                        if (curveSegments[curveSegments.Count - 1].Count > 0)
                        {
                            curveSegments.Add(new List<Vector3>()); // close previous segment
                        }
                    }
                    else
                    {
                        curveSegments[curveSegments.Count - 1].Add(pt);
                    }
                }
                lines.AddRange(curveSegments);
            }

            var detailPoints = new List<Vector3> { new Vector3(0, SizeY + TOOL_DIST, 0) };
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.Count < 1) continue;
                if (i % 2 == 0) line.Reverse();

                if (line[0].Y < SizeY + TOOL_DIST)
                {
                    detailPoints.Add(new Vector3(line[0].X, SizeY + TOOL_DIST, line[0].Z));
                }
                detailPoints.AddRange(line);
                if (line[line.Count - 1].Y < SizeY + TOOL_DIST)
                {
                    detailPoints.Add(new Vector3(line[line.Count - 1].X, SizeY + TOOL_DIST, line[line.Count - 1].Z));
                }
            }
            detailPoints.Add(new Vector3(0, SizeY + TOOL_DIST, 0));

            //ToolDiameter = detailDiamMil * MillingPath.SCALE;
            //IsFlat = false;
            //path = new MillingPath(detailPoints);
            //MillingIO.SaveFile(detailPoints, new ToolData(false, detailDiamMil), location, "5", startIndex);
            return new MillingPathData(detailPoints, false, detailDiamMil);
        }

        private IntersectionCurveManager GetIntersectionCurve(IntersectionFinderParams finderParams, ISurface P, ISurface Q, int divs)
        {
            var (iRes, points, xs, loop) = IntersectionFinder.FindIntersectionDataWithoutStartPoint(
                finderParams, P, Q, divs);
            if (iRes == IntersectionResult.OK)
            {
                return new IntersectionCurveManager(P, Q, points, xs, loop);
            }
            return null;
        }

        private IntersectionCurveManager GetIntersectionCurve(IntersectionFinderParams finderParams, ISurface P, ISurface Q, int divs, Vector3 startPoint)
        {
            var (iRes, points, xs, loop) = IntersectionFinder.FindIntersectionDataWithStartPoint(
                finderParams, P, Q, divs, startPoint);
            if (iRes == IntersectionResult.OK)
            {
                return new IntersectionCurveManager(P, Q, points, xs, loop);
            }
            return null;
        }

        private void ResetGraphStructures()
        {
            ContourSegment.ResetCounter();
            ContourIntersection.ResetCounter();
        }

        // returning a visualizer for debug purposes
        private (List<Vector3>, GraphVisualizer) OffsetContour(List<ContourEdge> contour, float offset, bool ccw, int pathVertexInd)
        {
            var offsetSegments = new List<ContourSegment>();
            var firstOrigPoints = new List<Vector3>();
            var lastOrigPoints = new List<Vector3>();
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
                    Vector3 newP1 = p1 + normal * offset, newP2 = p2 + normal * offset;
                    if (offsetPoints.Count > 0)
                    {
                        Vector3 prevP1 = offsetPoints[offsetPoints.Count - 1];
                        Vector3 p1Diff = newP1 - prevP1;
                        if (p1Diff.LengthSquared > 0.1)
                        {
                            offsetPoints.AddRange(GetCircularSegment(prevP1, newP1, p1, 20));
                        }
                    }
                    offsetPoints.AddMany(newP1, newP2);
                }
                firstOrigPoints.Add(points[0]);
                lastOrigPoints.Add(points[points.Count - 1]);
                offsetSegments.Add(new ContourSegment(offsetPoints));
            }

            // add circular segments connecting actual segments where needed
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
                    Vector3 center = (lastOrigPoints[i] + firstOrigPoints[(i + 1) % actualSegCount]) / 2;
                    var fakeSegment = new List<Vector3> { p1 };
                    fakeSegment.AddRange(GetCircularSegment(p1, p2, center, 20));
                    fakeSegment.Add(p2);
                    offsetSegments.Add(new ContourSegment(fakeSegment));
                }
            }
            ResetGraphStructures();
            var (graph, graphCount) = GetContourGraph(offsetSegments, 0);

            //Console.WriteLine("\nAAA\n");
            //foreach (var i in graph.Keys)
            //{
            //    var tmpPath = GetPathFromGraph(graph, graphCount, i, new Vector3(-SizeX / 2, BaseHeight + PATH_BASE_DIST, -SizeZ / 2), false);
            //    var tmpPoints = new List<Vector3>();
            //    foreach (var edge in tmpPath)
            //    {
            //        tmpPoints.AddRange(edge.Points);
            //    }
            //    Console.WriteLine($"{i}, {tmpPoints.Count}");
            //}

            var fixedPath = GetPathFromGraph(graph, graphCount, pathVertexInd, new Vector3(-SizeX / 2, BaseHeight + PATH_BASE_DIST, -SizeZ / 2), false);
            var fixedPoints = new List<Vector3>();
            foreach (var edge in fixedPath)
            {
                fixedPoints.AddRange(edge.Points);
            }
            return (fixedPoints, new GraphVisualizer(graph));
        }

        private List<Vector3> GetCircularSegment(Vector3 start, Vector3 end, Vector3 center, int divs)
        {
            var segment = new List<Vector3>();
            Vector2 startXZ = new Vector2(start.X - center.X, start.Z - center.Z), endXZ = new Vector2(end.X - center.X, end.Z - center.Z);
            if (startXZ.LengthSquared == 0 || endXZ.LengthSquared == 0)
            {
                return segment;
            }

            //if (BrodkaCrossSign(startXZ, endXZ) > 0)
            //{
            //    Vector2 tmp = startXZ;
            //    startXZ = endXZ;
            //    endXZ = tmp;
            //}
            double angle = Math.Acos(Math.Max(-1, Math.Min(1, Vector2.Dot(startXZ.Normalized(), endXZ.Normalized()))));

            double angleStep = angle / divs;
            double sinTotal = Math.Sin(angle);
            // we're excluding start/end (start = p_0 and end = p_divs) since sometimes we already have them added
            for (int i = 1; i < divs; i++)
            {
                double sin1 = Math.Sin((divs - i) * angleStep), sin2 = Math.Sin(i * angleStep);
                Vector2 pointXZ = (float)(sin1 / sinTotal) * startXZ + (float)(sin2 / sinTotal) * endXZ;
                segment.Add(center + new Vector3(pointXZ.X, 0, pointXZ.Y));
            }
            return segment;
        }

        private List<ContourEdge> GetPathFromGraph(
            Dictionary<int, ContourVertex> graph, int graphCount, int startVertex, Vector3 prevPoint,
            bool reverseCheck = false, bool alternating = false, int alternatingSteps = 2, int alternatingStepsOffset = 0)
        {
            int prevVertex = -1;
            bool[] isVertUsed = new bool[graphCount];
            isVertUsed[startVertex] = true;
            var contour = new List<ContourEdge>();
            Vector3 curPoint = graph[startVertex].Point;

            //Console.WriteLine("\nBBB\n");
            int mult = reverseCheck ? -1 : 1;
            int steps = 0;
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
                            val = Math.Acos(Math.Max(-1, Math.Min(1, Vector2.Dot(curDir, potentialDir)))) * BrodkaCrossSign(curDir, potentialDir);
                        }
                        if (mult * val > bestVal)
                        {
                            bestInd = i;
                            bestVal = mult * val;
                        }
                        //Console.WriteLine($"{i} to {edge.To}: {val}");
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

                if (graph[startVertex].OutEdges.Count > 2)
                {
                    steps++;
                }
                isVertUsed[bestVertex] = true;
                prevPoint = bestEdge.Points[bestEdge.Points.Count - 2];
                curPoint = bestEdge.Points[bestEdge.Points.Count - 1];
                prevVertex = startVertex;
                startVertex = bestVertex;

                if (alternating && ((steps + alternatingStepsOffset) % alternatingSteps) == 0)
                {
                    mult = -mult;
                }
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

            float sameVertexDist = 1e-2f, svdSq = sameVertexDist * sameVertexDist;
            // merge vertices
            for (int i = 0; i <= maxInd; i++)
            {
                if (!graph.ContainsKey(i)) continue;
                for (int j = i + 1; j <= maxInd; j++)
                {
                    if (!graph.ContainsKey(j)) continue;
                    if ((graph[i].Point - graph[j].Point).LengthSquared < svdSq)
                    {
                        foreach (var vertex in graph)
                        {
                            if (vertex.Key == j) continue;
                            for (int e = vertex.Value.OutEdges.Count - 1; e >= 0; e--)
                            {
                                var edge = vertex.Value.OutEdges[e];
                                if (edge.To == j)
                                {
                                    if (vertex.Key == i)
                                    {
                                        vertex.Value.OutEdges.RemoveAt(e);
                                    }
                                    else
                                    {
                                        edge.To = i;
                                    }
                                }
                            }
                        }
                        foreach (var edge in graph[j].OutEdges)
                        {
                            if (edge.To != i)
                            {
                                edge.From = i;
                                graph[i].OutEdges.Add(edge);
                            }
                        }
                        graph.Remove(j);
                    }
                }
            }

            return (graph, maxInd + 1);
        }

        private int BrodkaCrossSign(Vector2 a, Vector2 b)
        {
            return Math.Sign(a.X * b.Y - a.Y * b.X);
        }

        private bool AddIntersection(List<ContourSegment> segments,
            float eps, IntersectionFinderParams finderParams, ISurface P, ISurface Q, int divs)
        {
            var (iRes, interPoints, _, _) = IntersectionFinder.FindIntersectionDataWithoutStartPoint(finderParams, P, Q, divs);
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
            var (iRes, interPoints, _, _) = IntersectionFinder.FindIntersectionDataWithStartPoint(finderParams, P, Q, divs, start);
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
                        if (!float.IsNaN(inter2d.X) && !float.IsNaN(inter2d.Y))
                        {
                            var inter = new Vector3(inter2d.X, y, inter2d.Y);
                            intersections.Add(new ContourIntersection(seg1.Id, seg2.Id, i, j, inter));
                        }
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

                if (DisplayTool)
                {
                    shader.UsePhong();
                    tool.Render(shader);
                    shader.UseBasic();
                }

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

            public GraphVisualizer(List<ContourEdge> segments)
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
