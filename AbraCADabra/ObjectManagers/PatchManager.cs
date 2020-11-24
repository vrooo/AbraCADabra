using AbraCADabra.Serialization;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AbraCADabra
{
    public abstract class PatchManager : TransformManager<PatchVertex>, ISurface
    {
        protected int continuity;

        protected PointManager[,] points;

        public bool DrawPolynet { get; set; }

        private const int divDefault = 4;
        private bool divChanged;
        protected int divx, divz;
        protected int patchCountX, patchCountZ;
        public int DivX
        {
            get { return divx; }
            set { divChanged = true; divx = value; }
        }
        public int DivZ
        {
            get { return divz; }
            set { divChanged = true; divz = value; }
        }

        private Vector3 center;

        protected PatchType patchType;
        private Patch patch;
        private QuadPatch quadPatch;
        private PolyGrid polyGrid;

        private bool shouldUpdate = false;
        private int pointTexture = GL.GenTexture();

        public override float PositionX
        {
            get { return center.X; }
            set { }
        }

        public override float PositionY
        {
            get { return center.Y; }
            set { }
        }

        public override float PositionZ
        {
            get { return center.Z; }
            set { }
        }

        private List<IntersectionCurveManager> curves = new List<IntersectionCurveManager>();

        protected PatchManager(PointManager[,] points, PatchType patchType, int patchCountX, int patchCountZ, int continuity,
            int divX = divDefault, int divZ = divDefault, string name = null)
            : this(new Patch(patchCountX, patchCountZ, divX, divZ),
                   new QuadPatch(patchCountX, patchCountZ, divX, divZ),
                   new PolyGrid(GetPointPositions(points, patchType, continuity).points, new Vector4(0.7f, 0.7f, 0.0f, 1.0f)), name)
        {
            this.points = points;
            this.patchType = patchType;
            this.continuity = continuity;
            this.patchCountX = patchCountX;
            this.patchCountZ = patchCountZ;
            this.divx = divX;
            this.divz = divZ;

            foreach (var point in points)
            {
                point.PropertyChanged += PointChanged;
                point.PointReplaced += ReplacePoint;
            }

            var (pts, ctr) = GetPointPositions(points, patchType, continuity);
            center = ctr;
            UpdatePointTexture(pts);
        }

        private PatchManager(Patch patch, QuadPatch quadPatch, PolyGrid polyGrid, string name = null) : base(patch, name)
        {
            this.patch = patch;
            this.quadPatch = quadPatch;
            quadPatch.Color = patch.Color;
            this.polyGrid = polyGrid;
        }

        private static (Vector3[,] points, Vector3 center) GetPointPositions(PointManager[,] points, PatchType patchType, int continuity)
        {
            int width = points.GetLength(0), height = points.GetLength(1);
            var res = new Vector3[patchType == PatchType.Cylinder ? width + continuity + 1 : width, height];

            Vector3 pos = new Vector3();
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    var p = points[i, j];
                    pos += p.Transform.Position;
                    res[i, j] = p.Transform.Position;
                }
            }
            pos /= width * height;

            if (patchType == PatchType.Cylinder)
            {
                for (int i = 0; i <= continuity; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        res[width + i, j] = points[i % width, j].Transform.Position;
                    }
                }
            }
            return (res, pos);
        }

        // TODO: base type for XmlPatch pls
        protected static (PointManager[,] pointManagers, int xDim, int zDim, PatchType type, int divX, int divZ) GetPointsFromDictionary(XmlPatchPointRef[] pointRefs,
            WrapType wrapType, int rowSlices, int columnSlices, Dictionary<string, PointManager> points)
        {
            // normally: point[i, j]: Column = i, Row = j, rowSlices = divX, columnSlices = divZ
            // if wrapType is Row, all of this needs to be swapped
            bool swap = wrapType == WrapType.Row;
            int maxRow = 0, maxCol = 0;
            foreach (var pr in pointRefs)
            {
                maxRow = Math.Max(maxRow, pr.Row);
                maxCol = Math.Max(maxCol, pr.Column);
            }

            int xDim = (swap ? maxRow : maxCol) + 1;
            int zDim = (swap ? maxCol : maxRow) + 1;
            var pms = new PointManager[xDim, zDim];

            foreach (var pr in pointRefs)
            {
                int i = swap ? pr.Row : pr.Column;
                int j = swap ? pr.Column : pr.Row;
                if (!points.TryGetValue(pr.Name, out pms[i, j]))
                {
                    throw new KeyNotFoundException("Required point was not found in dictionary");
                }
            }
            for (int i = 0; i < xDim; i++)
            {
                for (int j = 0; j < zDim; j++)
                {
                    if (pms[i, j] == null)
                    {
                        throw new KeyNotFoundException("At least one point from patch is missing");
                    }
                    else
                    {
                        pms[i, j].SurfaceCount++;
                    }
                }
            }

            int divX = swap ? columnSlices : rowSlices;
            int divZ = swap ? rowSlices : columnSlices;
            return (pms, xDim, zDim, wrapType == WrapType.None ? PatchType.Simple : PatchType.Cylinder, divX, divZ);
        }

        protected abstract Vector3 CalcPoint(float t, IList<Vector3> pts);

        public int UScale => patchCountX;
        public int VScale => patchCountZ;

        public Vector2 ClampUV(float u, float v)
        {
            return ClampUV(u, v, false);
        }

        public Vector2 ClampScaledUV(float u, float v)
        {
            return ClampUV(u, v, true);
        }

        private Vector2 ClampUV(float u, float v, bool scaled)
        {
            u = Math.Max(0, Math.Min(scaled ? 1 : patchCountX, WrapU(u, scaled)));
            v = Math.Max(0, Math.Min(scaled ? 1 : patchCountZ, v));
            return new Vector2(u, v);
        }

        private float WrapU(float u, bool scaled = false)
        {
            int uMax = scaled ? 1 : patchCountX;
            if (patchType == PatchType.Cylinder && (u < 0 || u > uMax))
            {
                u = u % uMax;
                if (u < 0)
                {
                    u += uMax;
                }
            }
            return u;
        }

        public bool IsUVValid(float u, float v)
        {
            if (v >= 0 && v <= patchCountZ)
            {
                return patchType == PatchType.Cylinder || (u >= 0 && u <= patchCountX);
            }
            return false;
        }

        public Vector2 GetClosestValidUV(float u, float v, float uPrev, float vPrev, out double t)
        {
            // line between (uPrev, vPrev) and (u, v) crosses the border
            Vector2 point = new Vector2(u, v);
            t = 1;
            if (v < 0)
            {
                point = MathHelper.FindPointAtY(uPrev, vPrev, point.X, point.Y, 0, out t);
            }
            else if (v > patchCountZ)
            {
                point = MathHelper.FindPointAtY(uPrev, vPrev, point.X, point.Y, patchCountZ, out t);
            }

            if (patchType == PatchType.Simple)
            {
                if (u < 0)
                {
                    point = MathHelper.FindPointAtX(uPrev, vPrev, point.X, point.Y, 0, out double tt);
                    t *= tt;
                }
                else if (u > patchCountX)
                {
                    point = MathHelper.FindPointAtX(uPrev, vPrev, point.X, point.Y, patchCountX, out double tt);
                    t *= tt;
                }
            }
            return point;
        }

        private (float u, float v, int sx, int sz) TranslateUV(float u, float v)
        {
            u = WrapU(u); // TODO: check with clamping?
            //var tmp = ClampUV(u, v);
            //u = tmp.X;
            //v = tmp.Y;
            int indexX = (int)Math.Floor(u), indexZ = (int)Math.Floor(v);
            indexX = Math.Max(0, Math.Min(patchCountX - 1, indexX));
            indexZ = Math.Max(0, Math.Min(patchCountZ - 1, indexZ));
            int mult = 3 - continuity;
            return (u - indexX, v - indexZ, mult * indexX, mult * indexZ);
        }

        public Vector3 GetNormal(float u, float v)
        {
            Vector3 du = GetDu(u, v), dv = GetDv(u, v);
            Vector3 normal = Vector3.Cross(du, dv);
            normal.Normalize();
            return normal;
        }

        public Vector3 GetUVPoint(float u, float v)
        {
            var (pts, _) = GetPointPositions(points, patchType, continuity);
            int sx, sz;
            (u, v, sx, sz) = TranslateUV(u, v);
            Vector3[] p = new Vector3[4], q = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    p[j] = pts[sx + j, sz + i];
                }
                q[i] = CalcPoint(u, p);
            }
            return CalcPoint(v, q);
            // TODO: begin temp offset
            var pt = CalcPoint(v, q);
            Vector3 du = GetDu(u, v), dv = GetDv(u, v);
            Vector3 normal = Vector3.Cross(dv, du);
            normal.Normalize();
            return pt + 0.4f * normal;
            // TODO: end temp offset
        }

        public Vector3 GetDu(float u, float v)
        {
            var (pts, _) = GetPointPositions(points, patchType, continuity);
            int sx, sz;
            (u, v, sx, sz) = TranslateUV(u, v);
            float coef = 3 - continuity;
            Vector3[] p = new Vector3[3], q = new Vector3[4];
            for (int i = 0; i < q.Length; i++)
            {
                for (int j = 0; j < p.Length; j++)
                {
                    p[j] = coef * (pts[sx + j + 1, sz + i] - pts[sx + j, sz + i]);
                }
                q[i] = CalcPoint(u, p);
            }
            return CalcPoint(v, q);
        }

        public Vector3 GetDv(float u, float v)
        {
            var (pts, _) = GetPointPositions(points, patchType, continuity);
            int sx, sz;
            (u, v, sx, sz) = TranslateUV(u, v);
            float coef = 3 - continuity;
            Vector3[] p = new Vector3[3], q = new Vector3[4];
            for (int i = 0; i < q.Length; i++)
            {
                for (int j = 0; j < p.Length; j++)
                {
                    p[j] = coef * (pts[sx + i, sz + j + 1] - pts[sx + i, sz + j]);
                }
                q[i] = CalcPoint(v, p);
            }
            return CalcPoint(u, q);
        }

        public Vector3 GetDuDu(float u, float v)
        {
            var (pts, _) = GetPointPositions(points, patchType, continuity);
            int sx, sz;
            (u, v, sx, sz) = TranslateUV(u, v);
            float coef = (3 - continuity) * (3 - continuity);
            Vector3[] p = new Vector3[2], q = new Vector3[4];
            for (int i = 0; i < q.Length; i++)
            {
                for (int j = 0; j < p.Length; j++)
                {
                    p[j] = coef * (pts[sx + j + 2, sz + i] - 2 * pts[sx + j + 1, sz + i] + pts[sx + j, sz + i]);
                }
                q[i] = CalcPoint(u, p);
            }
            return CalcPoint(v, q);
        }

        public Vector3 GetDvDv(float u, float v)
        {
            var (pts, _) = GetPointPositions(points, patchType, continuity);
            int sx, sz;
            (u, v, sx, sz) = TranslateUV(u, v);
            float coef = (3 - continuity) * (3 - continuity);
            Vector3[] p = new Vector3[2], q = new Vector3[4];
            for (int i = 0; i < q.Length; i++)
            {
                for (int j = 0; j < p.Length; j++)
                {
                    p[j] = coef * (pts[sx + i, sz + j + 2] - 2 * pts[sx + i, sz + j + 1] + pts[sx + i, sz + j]);
                }
                q[i] = CalcPoint(v, p);
            }
            return CalcPoint(u, q);
        }

        public Vector3 GetDuDv(float u, float v)
        {
            var (pts, _) = GetPointPositions(points, patchType, continuity);
            int sx, sz;
            (u, v, sx, sz) = TranslateUV(u, v);
            float coef = (3 - continuity) * (3 - continuity);
            Vector3[] p = new Vector3[3], q = new Vector3[3];
            for (int i = 0; i < q.Length; i++)
            {
                for (int j = 0; j < p.Length; j++)
                {
                    p[j] = coef * (pts[sx + j + 1, sz + i + 1] 
                                 - pts[sx + j + 1, sz + i]
                                 - pts[sx + j, sz + i + 1]
                                 + pts[sx + j, sz + i]);
                }
                q[i] = CalcPoint(u, p);
            }
            return CalcPoint(v, q);
        }

        public void AddIntersectionCurve(IntersectionCurveManager icm)
        {
            icm.ManagerDisposing += CurveDisposing;
            curves.Add(icm);
        }

        private void CurveDisposing(TransformManager sender)
        {
            sender.ManagerDisposing -= CurveDisposing;
            curves.Remove(sender as IntersectionCurveManager);
            divChanged = true;
            Update();
        }

        public bool IsEdgeAllowed(EdgeType start, EdgeType end)
        {
            if (patchType == PatchType.Simple)
            {
                return true;
            }
            if ((start == EdgeType.Top && end == EdgeType.Bottom) || (start == EdgeType.Bottom && end == EdgeType.Top))
            {
                return false;
            }
            return true;
        }

        public void UpdateMesh()
        {
            shouldUpdate = true;
            divChanged = true;
        }

        public override void Update()
        {
            shouldUpdate = true;
        }

        private void ActualUpdate()
        {
            var (pts, ctr) = GetPointPositions(points, patchType, continuity);
            center = ctr;
            if (divChanged)
            {
                divChanged = false;
                patch.Update(DivX, DivZ, this, curves);
            }
            polyGrid.Update(pts);
            UpdatePointTexture(pts);
        }

        public void RenderFilled(ShaderManager shader)
        {
            ActualRender(shader, true);
        }

        public override void Render(ShaderManager shader)
        {
            ActualRender(shader, false);
        }

        private void ActualRender(ShaderManager shader, bool fillPatch)
        {
            if (shouldUpdate)
            {
                shouldUpdate = false;
                ActualUpdate();
            }
            if (DrawPolynet && !fillPatch)
            {
                polyGrid.Render(shader);
            }
            shader.UsePatch();
            GL.ActiveTexture(TextureUnit.Texture10);
            GL.BindTexture(TextureTarget.Texture2D, pointTexture);
            shader.SetupInt(10, "texturePoint");
            shader.SetupInt(continuity, "continuity");
            if (fillPatch)
            {
                quadPatch.Update(DivX, DivZ); // we're using quadPatch rarely so this is better
                quadPatch.Render(shader);
            }
            else
            {
                patch.Render(shader);
            }
            shader.UseBasic();
        }

        private void PointChanged(object sender, PropertyChangedEventArgs e)
        {
            Update();
        }

        private void UpdatePointTexture(Vector3[,] pts)
        {
            var textureData = new Vector3[pts.Length];
            int ind = 0;
            // order of loops is important!
            for (int i = 0; i < pts.GetLength(1); i++)
            {
                for (int j = 0; j < pts.GetLength(0); j++)
                {
                    textureData[ind++] = pts[j, i];
                }
            }

            GL.ActiveTexture(TextureUnit.Texture10);
            GL.BindTexture(TextureTarget.Texture2D, pointTexture);
            GL.TexImage2D(TextureTarget.Texture2D,
                          0,
                          PixelInternalFormat.Rgb32f,
                          pts.GetLength(0), pts.GetLength(1), 0,
                          PixelFormat.Rgb, PixelType.Float,
                          textureData);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        }

        private void ReplacePoint(PointManager oldPoint, PointManager newPoint)
        {
            int ii = -1, jj = -1;
            for (int i = 0; i < points.GetLength(0); i++)
            {
                for (int j = 0; j < points.GetLength(1); j++)
                {
                    if (points[i, j] == oldPoint)
                    {
                        ii = i;
                        jj = j;
                        break;
                    }
                }
                if (ii != -1) break;
            }

            oldPoint.PropertyChanged -= PointChanged;
            oldPoint.PointReplaced -= ReplacePoint;
            oldPoint.SurfaceCount--;
            points[ii, jj] = newPoint;
            newPoint.PropertyChanged += PointChanged;
            newPoint.PointReplaced += ReplacePoint;
            newPoint.SurfaceCount++;
            Update();
        }

        public override void Translate(float x, float y, float z)
        {
            foreach (var pm in points)
            {
                pm.PropertyChanged -= PointChanged;
                pm.Translate(x, y, z);
                pm.PropertyChanged += PointChanged;
            }
            base.Translate(x, y, z);
            Update();
        }

        public override void RotateAround(float xAngle, float yAngle, float zAngle, Vector3 center)
        {
            foreach (var pm in points)
            {
                pm.PropertyChanged -= PointChanged;
                pm.RotateAround(xAngle, yAngle, zAngle, center);
                pm.PropertyChanged += PointChanged;
            }
            base.RotateAround(xAngle, yAngle, zAngle, center);
            Update();
        }

        protected XmlPatchPointRef[] GetSerializablePoints()
        {
            var pts = new XmlPatchPointRef[points.Length];
            int ind = 0;
            for (int j = 0; j < points.GetLength(1); j++)
            {
                for (int i = 0; i < points.GetLength(0); i++)
                {
                    pts[ind++] = new XmlPatchPointRef { Name = points[i, j].Name, Column = i, Row = j };
                }
            }
            return pts;
        }

        public override void Dispose()
        {
            foreach (var point in points)
            {
                point.SurfaceCount--;
                point.PropertyChanged -= PointChanged;
                point.PointReplaced -= ReplacePoint;
            }
            base.Dispose();
        }
    }
}
