using AbraCADabra.Serialization;
using System.Collections.Generic;

namespace AbraCADabra
{
    public class PatchC0Manager : PatchManager
    {
        public override string DefaultName => "Patch C0";
        private static int counter = 0;
        protected override int instanceCounter => counter++;

        public PatchC0Manager(XmlPatchC0 xmlPatch, Dictionary<string, PointManager> points)
            : this(GetPointsFromDictionary(xmlPatch.Points, xmlPatch.WrapDirection, xmlPatch.RowSlices, xmlPatch.ColumnSlices, points),
                  xmlPatch.Name) { }

        private PatchC0Manager((PointManager[,] points, int xDim, int zDim, PatchType type, int divX, int divZ) data, string name)
            : base(data.points, data.type,
                  (data.type == PatchType.Cylinder ? data.xDim : data.xDim - 1) / 3,
                  (data.zDim - 1) / 3,
                  0, data.divX, data.divZ, name) { }

        public PatchC0Manager(PointManager[,] points, PatchType patchType, int patchCountX, int patchCountZ)
            : base(points, patchType, patchCountX, patchCountZ, 0) { }

        public override XmlNamedType GetSerializable()
        {
            return new XmlPatchC0
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
