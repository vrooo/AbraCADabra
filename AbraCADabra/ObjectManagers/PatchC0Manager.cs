using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.ComponentModel;
using System.Windows.Documents;

namespace AbraCADabra
{
    // TODO: need to calculate position here
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

        private PatchType patchType;
        private Patch patch;
        private PolyGrid polyGrid;

        private int pointTexture = GL.GenTexture();

        public PatchC0Manager(PointManager[,] points, PatchType patchType, int patchCountX, int patchCountZ)
            : this(new Patch(patchCountX, patchCountZ, divDefault, divDefault),
                  new PolyGrid(GetPointPositions(points, patchType), new Vector4(0.7f, 0.7f, 0.0f, 1.0f)))
        {
            this.points = points;
            this.patchType = patchType;
            foreach (var point in points)
            {
                point.PropertyChanged += PointChanged;
            }
        }

        private PatchC0Manager(Patch patch, PolyGrid polyGrid) : base(patch)
        {
            this.patch = patch;
            this.polyGrid = polyGrid;
        }

        private static Vector3[,] GetPointPositions(PointManager[,] points, PatchType patchType)
        {
            int width = points.GetLength(0), height = points.GetLength(1);
            var res = new Vector3[patchType == PatchType.Cylinder ? width + 1 : width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    res[i, j] = points[i, j].Transform.Position;
                }
            }
            if (patchType == PatchType.Cylinder)
            {
                for (int j = 0; j < height; j++)
                {
                    res[width, j] = points[0, j].Transform.Position;
                }
            }
            return res;
        }

        public override void Update()
        {
            var pts = GetPointPositions(points, patchType);
            patch.Update(DivX, DivZ);
            polyGrid.Update(pts);
        }

        public override void Render(ShaderManager shader)
        {
            if (DrawPolynet)
            {
                polyGrid.Render(shader);
            }
            shader.UsePatch();
            SetPointTexture(shader);
            base.Render(shader);
            shader.UseBasic();
        }

        private void PointChanged(object sender, PropertyChangedEventArgs e)
        {
            Update();
        }

        private void SetPointTexture(ShaderManager shader)
        {
            //if (first)
            {
                var pts = GetPointPositions(points, patchType);
                var tmp = new System.Collections.Generic.List<float>();
                for (int i = 0; i < pts.GetLength(0); i++)
                {
                    for (int j = 0; j < pts.GetLength(1); j++)
                    {
                        var p = pts[i, j];
                        tmp.AddMany(p.X, p.Y, p.Z);
                    }
                }
                GL.ActiveTexture(TextureUnit.Texture10);
                GL.BindTexture(TextureTarget.Texture2D, pointTexture);
                GL.TexImage2D(TextureTarget.Texture2D,
                              0,
                              PixelInternalFormat.Rgb32f,
                              pts.GetLength(0), pts.GetLength(1), 0,
                              PixelFormat.Rgb, PixelType.Float,
                              tmp.ToArray());
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                //first = false;
            }
            shader.SetupInt(10, "texturePoint");
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
