using OpenTK;
using System.Collections.Generic;

namespace AbraCADabra.Milling
{
    public class MillingPath
    {
        public const float SCALE = 0.1f;

        private PolyLine polyLine;
        public List<Vector3> Points { get; }

        public MillingPath(List<Vector3> moves)
        {
            Points = moves;
            polyLine = new PolyLine(Points, new Vector4(0.8f, 0.6f, 0.2f, 1.0f), 2);
        }

        public void Render(ShaderManager shader)
        {
            polyLine.Render(shader);
        }
    }
}
