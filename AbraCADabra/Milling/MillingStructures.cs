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

    public struct MillingPathParams
    {
        public int StartIndex;
        public float ReductionEpsRough;
        public float ReductionEpsBase;
    }

    public class MillingException : Exception
    {
        public MillingException(string message) : base(message) { }
    }
}
