using AbraCADabra.Serialization;
using OpenTK;
using System.Collections.Generic;

namespace AbraCADabra
{
    public class PatchC2Manager : PatchManager
    {
        public override string DefaultName => "Patch C2";
        private static int counter = 0;
        protected override int instanceCounter => counter++;

        public PatchC2Manager(XmlPatchC2 xmlPatch, Dictionary<string, PointManager> points)
            : this(GetPointsFromDictionary(xmlPatch.Points, xmlPatch.WrapDirection, xmlPatch.RowSlices, xmlPatch.ColumnSlices, points),
                  xmlPatch.Name)
        {
            DrawPolynet = xmlPatch.ShowControlPolygon;
        }

        private PatchC2Manager((PointManager[,] points, int xDim, int zDim, PatchType type, int divX, int divZ) data, string name)
            : base(data.points, data.type,
                  data.type == PatchType.Cylinder ? data.xDim : data.xDim - 3,
                  data.zDim - 3,
                  2, data.divX, data.divZ, name) { }

        public PatchC2Manager(PointManager[,] points, PatchType patchType, int patchCountX, int patchCountZ)
            : base(points, patchType, patchCountX, patchCountZ, 2) { }

        protected override Vector3 CalcPoint(float t, IList<Vector3> pts)
        {
            // de Boor
            int n = pts.Count;
            float[] N = new float[n], A = new float[n - 1], B = new float[n - 1];
            N[0] = 1.0f;
            for (int i = 0; i < n - 1; i++)
            {
                A[i] = i - t + 1.0f;
                B[i] = i + t;
                float saved = 0.0f;
                for (int j = 0; j <= i; j++)
                {
                    float term = N[j] / (A[j] + B[i - j]);
                    N[j] = saved + A[j] * term;
                    saved = B[i - j] * term;
                }
                N[i + 1] = saved;
            }

            Vector3 pos = Vector3.Zero;
            for (int i = 0; i < n; i++)
            {
                pos += N[i] * pts[i];
            }
            return pos;
        }

        public override XmlNamedType GetSerializable()
        {
            return new XmlPatchC2
            {
                Name = Name,
                WrapDirection = patchType == PatchType.Cylinder ? WrapType.Column : WrapType.None,
                RowSlices = divx,
                ColumnSlices = divz,
                ShowControlPolygon = DrawPolynet,
                Points = GetSerializablePoints()
            };
        }
    }
}
