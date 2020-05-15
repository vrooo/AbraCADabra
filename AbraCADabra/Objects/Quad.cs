namespace AbraCADabra
{
    public class Quad : FloatTransform
    {
        private float[] _vertices =
        {
            -1.0f, -1.0f, 0.0f,
            +1.0f, -1.0f, 0.0f,
            +1.0f, +1.0f, 0.0f,
            -1.0f, +1.0f, 0.0f,
        };
        protected override float[] vertices => _vertices;

        private uint[] _indices = {
             0,  2,  1,      0,  3,  2
        };
        protected override uint[] indices => _indices;

        public Quad()
        {
            Color = OpenTK.Vector4.One;
            Initialize();
        }
    }
}
