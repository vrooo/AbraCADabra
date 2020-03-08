namespace AbraCADabra
{
    class Cube : Mesh
    {
        private float[] _vertices = {
            // front
             -0.5f, -0.5f, -0.5f,
            +0.5f, -0.5f, -0.5f,
            +0.5f, +0.5f, -0.5f,
            -0.5f, +0.5f, -0.5f, 
            // top
            -0.5f, +0.5f, -0.5f,
            +0.5f, +0.5f, -0.5f,
            +0.5f, +0.5f, +0.5f,
            -0.5f, +0.5f, +0.5f, 
            // right
            +0.5f, -0.5f, -0.5f,
            +0.5f, -0.5f, +0.5f,
            +0.5f, +0.5f, +0.5f,
            +0.5f, +0.5f, -0.5f, 
            // back
            +0.5f, -0.5f, +0.5f,
            -0.5f, -0.5f, +0.5f,
            -0.5f, +0.5f, +0.5f,
            +0.5f, +0.5f, +0.5f, 
            // left
            -0.5f, -0.5f, +0.5f,
            -0.5f, -0.5f, -0.5f,
            -0.5f, +0.5f, -0.5f,
            -0.5f, +0.5f, +0.5f, 
            // bottom
            -0.5f, -0.5f, +0.5f,
            +0.5f, -0.5f, +0.5f,
            +0.5f, -0.5f, -0.5f,
            -0.5f, -0.5f, -0.5f,
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
