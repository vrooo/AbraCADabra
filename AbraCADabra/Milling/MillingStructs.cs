using System;

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

    public class MillingException : Exception
    {
        public MillingException(string message) : base(message) { }
    }
}
