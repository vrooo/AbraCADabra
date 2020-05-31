using AbraCADabra.Serialization;
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
