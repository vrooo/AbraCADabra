﻿using OpenTK;

namespace AbraCADabra
{
    public interface ISurface
    {
        Vector3 GetUVPoint(float u, float v);

        // first derivative
        Vector3 GetDu(float u, float v);
        Vector3 GetDv(float u, float v);

        // second derivative
        Vector3 GetDuDu(float u, float v);
        Vector3 GetDuDv(float u, float v);
        Vector3 GetDvDv(float u, float v);

        // from [0; 1] to actual
        Vector2 ScaleUV(float u, float v);
        Vector2 ClampUV(float u, float v);
    }
}
