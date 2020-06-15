using AbraCADabra.Serialization;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AbraCADabra
{
    public class GregoryPatchManager : TransformManager<PatchVertex>
    {
        public override string DefaultName => "Gregory Patch";
        private static int counter = 0;
        protected override int instanceCounter => counter++;

        public bool DrawVectors { get; set; }

        private const int divDefault = 4;
        private bool divChanged;
        protected int divx, divz;
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

        private PatchGraphTriangle triangle;
        private Patch[] patches;
        private LineSet vectors;

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

        private static Vector4 color = new Vector4(1.0f, 0.25f, 1.0f, 1.0f);
        public GregoryPatchManager(PatchGraphTriangle triangle, int divX = divDefault, int divZ = divDefault, string name = null)
            : this(new Patch(1, 1, divX, divZ, color),
                   new Patch(1, 1, divX, divZ, color),
                   new Patch(1, 1, divX, divZ, color),
                   new LineSet(GetPointPositions(triangle).vectors, new Vector4(1.0f, 1.0f, 0.0f, 1.0f)),
                   name)
        {
            this.triangle = triangle;
            this.divx = divX;
            this.divz = divZ;

            foreach (var edge in triangle.Edges)
            {
                edge.Patch.ManagerDisposing += HandlePatchDisposing;
                foreach (var point in edge.P)
                {
                    point.PropertyChanged += PointChanged;
                    point.PointReplaced += HandlePointReplaced;
                }

                foreach (var point in edge.Q)
                {
                    point.PropertyChanged += PointChanged;
                    point.PointReplaced += HandlePointReplaced;
                }
            }

            var (pts, _, ctr) = GetPointPositions(triangle);
            center = ctr;
            UpdatePointTexture(pts);
        }

        private GregoryPatchManager(Patch patch1, Patch patch2, Patch patch3, LineSet lineSet, string name = null) : base(patch1, name)
        {
            patches = new Patch[] { patch1, patch2, patch3 };
            vectors = lineSet;
        }

        private static Vector3[] SplitBezier(Vector3[] points, float t = 0.5f)
        {
            Vector3[] res = new Vector3[7];
            Vector3[,] arr = new Vector3[4, 4];
            for (int j = 0; j < 4; j++)
            {
                arr[0, j] = points[j];
            }
            res[0] = arr[0, 0];
            res[6] = arr[0, 3];
            for (int i = 1; i < 4; i++)
            {
                for (int j = 0; j < 4 - i; j++)
                {
                    arr[i, j] = (1 - t) * arr[i - 1, j] + t * arr[i - 1, j + 1];
                }
                res[i] = arr[i, 0];
                res[6 - i] = arr[i, 3 - i];
            }
            return res;
        }
        
        private static (Vector3[,] points, List<Vector3> vectors, Vector3 center) GetPointPositions(PatchGraphTriangle triangle)
        {
            var divP = new Vector3[19];
            var divQ = new Vector3[19];
            for (int i = 0; i < 3; i++)
            {
                var Parr = new Vector3[4];
                var Qarr = new Vector3[4];
                for (int j = 0; j < 4; j++)
                {
                    Parr[j] = triangle.Edges[i].P[j].Transform.Position;
                    Qarr[j] = triangle.Edges[i].Q[j].Transform.Position;
                }
                var divPTemp = SplitBezier(Parr);
                var divQTemp = SplitBezier(Qarr);
                for (int j = 0; j < 6; j++)
                {
                    int ind = (6 * i + 3 + j) % 18;
                    divP[ind] = divPTemp[j];
                    divQ[ind] = divQTemp[j];
                }
            }
            // 0, 6, 12 - P3i in divP
            // repeat first point to simplify calculations
            divP[18] = divP[0];
            divQ[18] = divQ[0];

            var P2 = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                P2[i] = 2 * divP[6 * i] - divQ[6 * i];
            }
            var Q = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                Q[i] = (3 * P2[i] - divP[6 * i]) / 2.0f;
            }
            var P = (Q[0] + Q[1] + Q[2]) / 3.0f;
            var P1 = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                P1[i] = (2 * Q[i] + P) / 3.0f;
            }

            var res = new Vector3[18, 4];
            for (int i = 0; i < 3; i++)
            {
                int start = 6 * i;
                int end = start + 3;
                // outer borders
                for (int j = 0; j < 4; j++)
                {
                    res[start + j, 0] = divP[start + j];
                }
                for (int j = 1; j < 4; j++)
                {
                    res[start + 3, j] = divP[start + 3 + j];
                }
                // C1 on outer borders - 1, 2, 4, 5
                res[start + 1, 1] = res[end + 1, 1] = 2 * divP[start + 1] - divQ[start + 1];
                res[start + 2, 1]                   = 2 * divP[start + 2] - divQ[start + 2];
                res[end + 2, 1]                     = 2 * divP[start + 4] - divQ[start + 4];
                res[start + 2, 2] = res[end + 2, 2] = 2 * divP[start + 5] - divQ[start + 5];

                // inner borders
                res[start, 1] = P2[i];
                res[start, 2] = P1[i];
                res[start, 3] = P;
                res[start + 1, 3] = P1[(i + 1) % 3];
                res[start + 2, 3] = P2[(i + 1) % 3];
            }

            var vectors = new List<Vector3>();
            for (int i = 0; i < 3; i++)
            {
                int start = 6 * i;
                int end = start + 3;
                int prev = 6 * ((i + 2) % 3), next = 6 * ((i + 1) % 3);
                // C1 on inner borders
                var g0 = (res[start + 1, 1] - res[prev + 2, 2]) * 0.5f;
                var g2 = (res[start + 1, 3] - res[prev, 2]) * 0.5f;
                var g1 = (g0 + g2) * 0.5f;
                res[end + 1, 2] = res[start, 2] + g1;

                g0 = (res[start + 2, 2] - res[next + 1, 1]) * 0.5f;
                g2 = (res[start, 2] - res[next + 1, 3]) * 0.5f;
                g1 = (g0 + g2) * 0.5f;
                res[start + 1, 2] = res[start + 1, 3] + g1;

                vectors.Add(res[start + 1, 0]);
                vectors.Add(res[start + 1, 1]);
                vectors.Add(res[start + 2, 0]);
                vectors.Add(res[start + 2, 1]);

                vectors.Add(res[start, 1]);
                vectors.Add(res[start + 1, 1]);
                vectors.Add(res[start, 2]);
                vectors.Add(res[end + 1, 2]);

                vectors.Add(res[start + 1, 3]);
                vectors.Add(res[start + 1, 2]);
                vectors.Add(res[start + 2, 3]);
                vectors.Add(res[start + 2, 2]);

                vectors.Add(res[start + 3, 1]);
                vectors.Add(res[end + 2, 1]);
                vectors.Add(res[start + 3, 2]);
                vectors.Add(res[start + 2, 2]);
            }

            return (res, vectors, P);
        }

        public override void Update()
        {
            shouldUpdate = true;
        }

        private void ActualUpdate()
        {
            var (pts, vecs, ctr) = GetPointPositions(triangle);
            center = ctr;
            if (divChanged)
            {
                divChanged = false;
                foreach (var patch in patches)
                {
                    patch.Update(DivX, DivZ);
                }
            }
            vectors.Update(vecs);
            UpdatePointTexture(pts);
        }

        public override void Render(ShaderManager shader)
        {
            if (shouldUpdate)
            {
                shouldUpdate = false;
                ActualUpdate();
            }
            if (DrawVectors)
            {
                vectors.Render(shader);
            }
            shader.UseGregory();
            GL.ActiveTexture(TextureUnit.Texture10);
            GL.BindTexture(TextureTarget.Texture2D, pointTexture);
            shader.SetupInt(10, "texturePoint");
            for (int i = 0; i < 3; i++)
            {
                shader.SetupInt(i, "patchIndex");
                patches[i].Render(shader);
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

        public override void Translate(float x, float y, float z) { }
        public override void Rotate(float x, float y, float z) { }
        public override void RotateAround(float xAngle, float yAngle, float zAngle, Vector3 center) { }
        public override void ScaleUniform(float delta) { }

        private void HandlePointReplaced(PointManager oldPoint, PointManager newPoint)
        {
            Dispose();
        }

        private void HandlePatchDisposing(TransformManager sender)
        {
            Dispose();
        }

        public override XmlNamedType GetSerializable()
        {
            return null;
        }

        public override void Dispose()
        {
            foreach (var edge in triangle.Edges)
            {
                edge.Patch.ManagerDisposing -= HandlePatchDisposing;
                foreach (var point in edge.P)
                {
                    point.PropertyChanged -= PointChanged;
                    point.PointReplaced -= HandlePointReplaced;
                }

                foreach (var point in edge.Q)
                {
                    point.PropertyChanged -= PointChanged;
                    point.PointReplaced -= HandlePointReplaced;
                }
            }

            base.Dispose();
            patches[1].Dispose();
            patches[2].Dispose();
        }
    }
}
