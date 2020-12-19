using OpenTK;
using System;
using System.Collections.Generic;

namespace AbraCADabra.Milling
{
    public struct ToolData
    {
        public bool IsFlat;
        public int Diameter;
        public ToolData(bool flat, int diameter)
        {
            IsFlat = flat;
            Diameter = diameter;
        }
    }

    public class MillingPathData
    {
        public List<Vector3> Points { get; }
        public ToolData ToolData { get; }
        public MillingPathData(List<Vector3> points, bool flat, int diameter)
        {
            Points = points;
            ToolData = new ToolData(flat, diameter);
        }
    }

    public class MillingException : Exception
    {
        public MillingException(string message) : base(message) { }
    }
}
