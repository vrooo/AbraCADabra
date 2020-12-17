using AbraCADabra.Serialization;
using OpenTK;
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
                  xmlPatch.Name)
        {
            DrawPolynet = xmlPatch.ShowControlPolygon;
        }  

        private PatchC0Manager((PointManager[,] points, int xDim, int zDim, PatchType type, int divX, int divZ) data, string name)
            : base(data.points, data.type,
                  (data.type == PatchType.Cylinder ? data.xDim : data.xDim - 1) / 3,
                  (data.zDim - 1) / 3,
                  0, data.divX, data.divZ, name) { }

        public PatchC0Manager(PointManager[,] points, PatchType patchType, int patchCountX, int patchCountZ)
            : base(points, patchType, patchCountX, patchCountZ, 0) { }

        protected override Vector3 CalcPoint(float t, IList<Vector3> pts)
        {
            // de Casteljau
            int n = pts.Count;
            var arr = new Vector3[n, n];
            for (int j = 0; j < n; j++)
            {
                arr[0, j] = pts[j];
            }
            for (int i = 1; i < n; i++)
            {
                for (int j = 0; j < n - i; j++)
                {
                    arr[i, j] = (1 - t) * arr[i - 1, j] + t * arr[i - 1, j + 1];
                }
            }
            return arr[n - 1, 0];
        }

        public void AddEdgesFrom(PatchGraph graph, PointManager pm)
        {
            int last1 = 3 * patchCountX, last2 = 3 * patchCountZ;
            int xlen = points.GetLength(0);
            int last1mod = last1 % xlen;

            if (pm == points[0,0])
            {
                graph.AddEdge(new PatchGraphEdge(this, points[0, 0], points[0, 1], points[0, 2], points[0, 3],
                                                       points[1, 0], points[1, 1], points[1, 2], points[1, 3]));
                graph.AddEdge(new PatchGraphEdge(this, points[0, 0], points[1, 0], points[2, 0], points[3 % xlen, 0],
                                                       points[0, 1], points[1, 1], points[2, 1], points[3 % xlen, 1]));
            }
            if (pm == points[0, last2])
            {
                graph.AddEdge(new PatchGraphEdge(this, points[0, last2 - 3], points[0, last2 - 2], points[0, last2 - 1], points[0, last2],
                                                       points[1, last2 - 3], points[1, last2 - 2], points[1, last2 - 1], points[1, last2]));
                graph.AddEdge(new PatchGraphEdge(this, points[0, last2], points[1, last2], points[2, last2], points[3 % xlen, last2],
                                                       points[0, last2 - 1], points[1, last2 - 1], points[2, last2 - 1], points[3 % xlen, last2 - 1]));
            }
            if (pm == points[last1mod, 0])
            {
                graph.AddEdge(new PatchGraphEdge(this, points[last1mod, 0], points[last1mod, 1], points[last1mod, 2], points[last1mod, 3],
                                                       points[last1 - 1, 0], points[last1 - 1, 1], points[last1 - 1, 2], points[last1 - 1, 3]));
                graph.AddEdge(new PatchGraphEdge(this, points[last1 - 3, 0], points[last1 - 2, 0], points[last1 - 1, 0], points[last1mod, 0],
                                                       points[last1 - 3, 1], points[last1 - 2, 1], points[last1 - 1, 1], points[last1mod, 1]));
            }
            if (pm == points[last1mod, last2])
            {
                graph.AddEdge(new PatchGraphEdge(this, points[last1mod, last2 - 3], points[last1mod, last2 - 2], points[last1mod, last2 - 1], points[last1mod, last2],
                                                       points[last1 - 1, last2 - 3], points[last1 - 1, last2 - 2], points[last1 - 1, last2 - 1], points[last1 - 1, last2]));
                graph.AddEdge(new PatchGraphEdge(this, points[last1 - 3, last2], points[last1 - 2, last2], points[last1 - 1, last2], points[last1mod, last2],
                                                       points[last1 - 3, last2 - 1], points[last1 - 2, last2 - 1], points[last1 - 1, last2 - 1], points[last1mod, last2 - 1]));
            }

            for (int i = 1; i < patchCountX; i++)
            {
                int ii = 3 * i;
                if (pm == points[ii, 0])
                {
                    graph.AddEdge(new PatchGraphEdge(this, points[ii, 0], points[ii + 1, 0], points[ii + 2, 0], points[(ii + 3) % xlen, 0],
                                                           points[ii, 1], points[ii + 1, 1], points[ii + 2, 1], points[(ii + 3) % xlen, 1]));
                    graph.AddEdge(new PatchGraphEdge(this, points[ii - 3, 0], points[ii - 2, 0], points[ii - 1, 0], points[ii, 0],
                                                           points[ii - 3, 1], points[ii - 2, 1], points[ii - 1, 1], points[ii, 1]));
                }
                if (pm == points[ii, last2])
                {
                    graph.AddEdge(new PatchGraphEdge(this, points[ii, last2], points[ii + 1, last2], points[ii + 2, last2], points[(ii + 3) % xlen, last2],
                                                           points[ii, last2 - 1], points[ii + 1, last2 - 1], points[ii + 2, last2 - 1], points[(ii + 3) % xlen, last2 - 1]));
                    graph.AddEdge(new PatchGraphEdge(this, points[ii - 3, last2], points[ii - 2, last2], points[ii - 1, last2], points[ii, last2],
                                                           points[ii - 3, last2 - 1], points[ii - 2, last2 - 1], points[ii - 1, last2 - 1], points[ii, last2 - 1]));
                }
            }

            for (int i = 1; i < patchCountZ; i++)
            {
                int ii = 3 * i;
                if (pm == points[0, ii])
                {
                    graph.AddEdge(new PatchGraphEdge(this, points[0, ii], points[0, ii + 1], points[0, ii + 2], points[0, ii + 3],
                                                           points[1, ii], points[1, ii + 1], points[1, ii + 2], points[1, ii + 3]));
                    graph.AddEdge(new PatchGraphEdge(this, points[0, ii - 3], points[0, ii - 2], points[0, ii - 1], points[0, ii],
                                                           points[1, ii - 3], points[1, ii - 2], points[1, ii - 1], points[1, ii]));
                }
                if (pm == points[last1mod, ii])
                {
                    graph.AddEdge(new PatchGraphEdge(this, points[last1mod, ii], points[last1mod, ii + 1], points[last1mod, ii + 2], points[last1mod, ii + 3],
                                                           points[last1 - 1, ii], points[last1 - 1, ii + 1], points[last1 - 1, ii + 2], points[last1 - 1, ii + 3]));
                    graph.AddEdge(new PatchGraphEdge(this, points[last1mod, ii - 3], points[last1mod, ii - 2], points[last1mod, ii - 1], points[last1mod, ii],
                                                           points[last1 - 1, ii - 3], points[last1 - 1, ii - 2], points[last1 - 1, ii - 1], points[last1 - 1, ii]));
                }
            }
        }

        public void AddEdgesIncluding(PatchGraph graph, PointManager pm)
        {
            AddEdgesFrom(graph, pm); // WONTDO: other options
        }

        public void AddAllEdges(PatchGraph graph)
        {
            int last1 = 3 * patchCountX, last2 = 3 * patchCountZ;
            int xlen = points.GetLength(0);
            int last1mod = last1 % xlen;

            for (int i = 0; i < patchCountX; i++)
            {
                int ii = 3 * i;
                graph.AddEdge(new PatchGraphEdge(this, points[ii, 0], points[ii + 1, 0], points[ii + 2, 0], points[(ii + 3) % xlen, 0],
                                                       points[ii, 1], points[ii + 1, 1], points[ii + 2, 1], points[(ii + 3) % xlen, 1]));
                graph.AddEdge(new PatchGraphEdge(this, points[ii, last2], points[ii + 1, last2], points[ii + 2, last2], points[(ii + 3) % xlen, last2],
                                                       points[ii, last2 - 1], points[ii + 1, last2 - 1], points[ii + 2, last2 - 1], points[(ii + 3) % xlen, last2 - 1]));
            }

            for (int i = 0; i < patchCountZ; i++)
            {
                int ii = 3 * i;
                graph.AddEdge(new PatchGraphEdge(this, points[0, ii], points[0, ii + 1], points[0, ii + 2], points[0, ii + 3],
                                                       points[1, ii], points[1, ii + 1], points[1, ii + 2], points[1, ii + 3]));
                graph.AddEdge(new PatchGraphEdge(this, points[last1mod, ii], points[last1mod, ii + 1], points[last1mod, ii + 2], points[last1mod, ii + 3],
                                                       points[last1 - 1, ii], points[last1 - 1, ii + 1], points[last1 - 1, ii + 2], points[last1 - 1, ii + 3]));
            }
        }

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
