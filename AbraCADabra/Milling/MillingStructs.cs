using System;

namespace AbraCADabra.Milling
{
    public struct ToolData
    {
        public bool IsFlat;
        public float Diameter;
        public ToolData(bool flat, float diameter)
        {
            IsFlat = flat;
            Diameter = diameter;
        }
    }

    public class MillingException : Exception
    {
        public MillingException(string message) : base(message) { }
    }
}
