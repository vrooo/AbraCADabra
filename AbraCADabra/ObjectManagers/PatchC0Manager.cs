using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.ComponentModel;

namespace AbraCADabra
{
    public class PatchC0Manager : TransformManager<PatchVertex>
    {
        public override string DefaultName => "Patch C0";
        private static int counter = 0;
        protected override int instanceCounter => counter++;

        private PointManager[,] points;

        public bool DrawPolynet { get; set; }

        private const int divDefault = 4;
        public int DivX { get; set; } = divDefault;
        public int DivZ { get; set; } = divDefault;

        private Vector3 center;

        private PatchType patchType;
        private Patch patch;
        private PolyGrid polyGrid;

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

        public PatchC0Manager(PointManager[,] points, PatchType patchType, int patchCountX, int patchCountZ)
            : this(new Patch(patchCountX, patchCountZ, divDefault, divDefault),
                  new PolyGrid(GetPointPositions(points, patchType).points, new Vector4(0.7f, 0.7f, 0.0f, 1.0f)))
        {
            this.points = points;
            this.patchType = patchType;

            foreach (var point in points)
            {
                point.PropertyChanged += PointChanged;
            }

            UpdatePointTexture(GetPointPositions(points, patchType).points);
        }

        private PatchC0Manager(Patch patch, PolyGrid polyGrid) : base(patch)
        {
            this.patch = patch;
            this.polyGrid = polyGrid;
        }

        private static (Vector3[,] points, Vector3 center) GetPointPositions(PointManager[,] points, PatchType patchType)
        {
            int width = points.GetLength(0), height = points.GetLength(1);
            var res = new Vector3[patchType == PatchType.Cylinder ? width + 1 : width, height];

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
                for (int j = 0; j < height; j++)
                {
                    res[width, j] = points[0, j].Transform.Position;
                }
            }
            return (res, pos);
        }

        public override void Update()
        {
            var (pts, ctr) = GetPointPositions(points, patchType);
            center = ctr;
            patch.Update(DivX, DivZ);
            polyGrid.Update(pts);
            UpdatePointTexture(pts);
        }

        public override void Render(ShaderManager shader)
        {
            if (DrawPolynet)
            {
                polyGrid.Render(shader);
            }
            shader.UsePatch();
            GL.ActiveTexture(TextureUnit.Texture10);
            GL.BindTexture(TextureTarget.Texture2D, pointTexture);
            shader.SetupInt(10, "texturePoint");
            base.Render(shader);
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

        public override void Dispose()
        {
            foreach (var point in points)
            {
                point.IsSurface = false;
            }
            base.Dispose();
        }
    }
}
