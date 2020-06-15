using System;
using System.Collections.Generic;

namespace AbraCADabra
{
    public struct PatchGraphEdge
    {
        public PointManager[] P;
        public PointManager[] Q;
        public PointManager From => P[0];
        public PointManager To => P[3];
        public PatchC0Manager Patch;
        public PatchGraphEdge(PatchC0Manager patch,
                              PointManager p1, PointManager p2, PointManager p3, PointManager p4,
                              PointManager q1, PointManager q2, PointManager q3, PointManager q4)
        {
            Patch = patch;
            P = new PointManager[] { p1, p2, p3, p4 };
            Q = new PointManager[] { q1, q2, q3, q4 };
        }

        public PatchGraphEdge GetReversed()
        {
            return new PatchGraphEdge(Patch, P[3], P[2], P[1], P[0], Q[3], Q[2], Q[1], Q[0]);
        }

        public bool IsBetween(PointManager p1, PointManager p2)
        {
            return (From == p1 && To == p2 || From == p2 && To == p1);
        }

        private bool ComparePoints(PointManager[] p, PointManager[] q)
        {
            bool same = true;
            for (int i = 0; i < 4; i++)
            {
                if (p[i] != q[i])
                {
                    same = false;
                    break;
                }
            }
            if (!same)
            {
                same = true;
                for (int i = 0; i < 4; i++)
                {
                    if (p[i] != q[3 - i])
                    {
                        same = false;
                        break;
                    }
                }
            }
            return same;
        }

        public override bool Equals(object obj)
        {
            if (obj is PatchGraphEdge pge)
            {
                return ComparePoints(P, pge.P) &&
                       ComparePoints(Q, pge.Q);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return 0; // TODO
        }
    }

    public class PatchGraphTriangle
    {
        public PatchGraphEdge[] Edges;
        public PatchGraphTriangle(PatchGraphEdge e1, PatchGraphEdge e2, PatchGraphEdge e3)
        {
            if (e1.To == e2.To)
            {
                e2 = e2.GetReversed();
            }
            else if (e1.To != e2.From)
            {
                e1 = e1.GetReversed();
                if (e1.To == e2.To)
                {
                    e2 = e2.GetReversed();
                }
            }
            if (e2.To == e3.To)
            {
                e3 = e3.GetReversed();
            }
            Edges = new PatchGraphEdge[] { e1, e2, e3 };
        }

        public override bool Equals(object obj)
        {
            if (obj is PatchGraphTriangle pgt)
            {
                return (Edges[0].Equals(pgt.Edges[0]) || Edges[0].Equals(pgt.Edges[1]) || Edges[0].Equals(pgt.Edges[2])) &&
                       (Edges[1].Equals(pgt.Edges[0]) || Edges[1].Equals(pgt.Edges[1]) || Edges[1].Equals(pgt.Edges[2])) &&
                       (Edges[2].Equals(pgt.Edges[0]) || Edges[2].Equals(pgt.Edges[1]) || Edges[2].Equals(pgt.Edges[2]));
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Edges[0].GetHashCode() ^ Edges[1].GetHashCode() ^ Edges[2].GetHashCode();
        }
    }

    public class PatchGraph
    {
        public Dictionary<PointManager, HashSet<PatchGraphEdge>> Vertices;
        public PatchGraph()
        {
            Vertices = new Dictionary<PointManager, HashSet<PatchGraphEdge>>();
        }

        public bool AddEdge(PatchGraphEdge edge)
        {
            AddPoint(edge.P[0]);
            AddPoint(edge.P[3]);
            if (!Vertices[edge.From].Contains(edge))
            {
                Vertices[edge.From].Add(edge);
                Vertices[edge.To].Add(edge);
                return true;
            }
            return false;
        }

        public List<PatchGraphEdge> GetEdgesBetween(PointManager p1, PointManager p2)
        {
            var list = new List<PatchGraphEdge>();
            foreach (var edge in Vertices[p1])
            {
                if (edge.IsBetween(p1, p2))
                {
                    list.Add(edge);
                }
            }
            return list;
        }

        private void AddPoint(PointManager pm)
        {
            if (!Vertices.ContainsKey(pm))
            {
                Vertices[pm] = new HashSet<PatchGraphEdge>();
            }
        }
    }
}
