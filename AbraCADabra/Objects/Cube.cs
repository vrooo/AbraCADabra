namespace AbraCADabra
{
    class Cube : Transform
    {
        private float[] _vertices =
        {
            // front
            -1.0f, -1.0f, -1.0f,
            +1.0f, -1.0f, -1.0f,
            +1.0f, +1.0f, -1.0f,
            -1.0f, +1.0f, -1.0f, 
            // top
            -1.0f, +1.0f, -1.0f,
            +1.0f, +1.0f, -1.0f,
            +1.0f, +1.0f, +1.0f,
            -1.0f, +1.0f, +1.0f, 
            // right
            +1.0f, -1.0f, -1.0f,
            +1.0f, -1.0f, +1.0f,
            +1.0f, +1.0f, +1.0f,
            +1.0f, +1.0f, -1.0f, 
            // back
            +1.0f, -1.0f, +1.0f,
            -1.0f, -1.0f, +1.0f,
            -1.0f, +1.0f, +1.0f,
            +1.0f, +1.0f, +1.0f, 
            // left
            -1.0f, -1.0f, +1.0f,
            -1.0f, -1.0f, -1.0f,
            -1.0f, +1.0f, -1.0f,
            -1.0f, +1.0f, +1.0f, 
            // bottom
            -1.0f, -1.0f, +1.0f,
            +1.0f, -1.0f, +1.0f,
            +1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f, -1.0f,
        };
        protected override float[] vertices => _vertices;

        private uint[] _indices = {
             0,  2,  1,      0,  3,  2,
             4,  6,  5,      4,  7,  6,
             8, 10,  9,      8, 11, 10,
            12, 14, 13,     12, 15, 14,
            16, 18, 17,     16, 19, 18,
            20, 22, 21,     20, 23, 22
        };
        protected override uint[] indices => _indices;

        public Cube()
        {
            Color = new OpenTK.Vector4(0.31f, 0.9f, 0.73f, 1.0f);
            Initialize();
        }
    }
}
