﻿using OpenTK;

namespace AbraCADabra
{
    public struct PointSurface : ISurface
    {
        public Vector3 Position { get; set; }
        public PointSurface(Vector3 pos)
        {
            Position = pos;
        }

        public Vector3 GetUVPoint(float u, float v) => Position;

        public Vector3 GetDu(float u, float v) => Vector3.Zero;
        public Vector3 GetDuDu(float u, float v) => Vector3.Zero;
        public Vector3 GetDuDv(float u, float v) => Vector3.Zero;
        public Vector3 GetDv(float u, float v) => Vector3.Zero;
        public Vector3 GetDvDv(float u, float v) => Vector3.Zero;

        public int UScale => 0;
        public int VScale => 0;
        public Vector2 ClampUV(float u, float v) => Vector2.Zero;
        public bool IsUVValid(float u, float v) => true;
    }
}
