#version 430

uniform sampler2D texturePoint;
uniform mat4 viewMatrix;
uniform mat4 projMatrix;
uniform mat4 modelMatrix;
uniform int patchIndex;

const int n = 4;
const float eps = 1e-5;

layout (location = 0) in vec2 uv;
layout (location = 1) in int indexX; // unused
layout (location = 2) in int indexZ; // unused

vec4 getBezierPoint(float t, vec4 pts[n])
{
    vec4 arr[n][n];
    for (int j = 0; j < n; j++)
    {
        arr[0][j] = pts[j];
    }
    for (int i = 1; i < n; i++)
    {
        for (int j = 0; j < n - i; j++)
        {
            arr[i][j] = (1 - t) * arr[i - 1][j] + t * arr[i - 1][j + 1];
        }
    }
    return arr[n - 1][0];
}

vec4 getPatchPoint(vec2 uv, int sx)
{
    vec4 p[n], q[n], tmp;
    for (int i = 0; i < n; i++)
    {
        for (int j = 0; j < n; j++)
        {
            p[j] = texelFetch(texturePoint, ivec2(sx + i, j), 0);
            if (i > 0 && i < n - 1 && j > 0 && j < n - 1)
            {
                tmp = texelFetch(texturePoint, ivec2(sx + i + 3, j), 0);
                if (i == 1 && j == 1)
                {
                    p[j] = (uv.x * tmp + uv.y * p[j]) / (uv.x + uv.y + eps);
                }
                else if (i == 1 && j == 2)
                {
                    p[j] = ((1.f - uv.x) * tmp + uv.y * p[j]) / (1.f - uv.x + uv.y + eps);
                }
                else if (i == 2 && j == 1)
                {
                    p[j] = (uv.x * tmp + (1.f - uv.y) * p[j]) / (1.f + uv.x - uv.y + eps);
                }
                else if (i == 2 && j == 2)
                {
                    p[j] = ((1.f - uv.y) * tmp + (1.f - uv.y) * p[j]) / (2.f - uv.x - uv.y + eps);
                }
            }
        }
        q[i] = getBezierPoint(uv.x, p);
    }
    return getBezierPoint(uv.y, q);
}

void main()
{
    vec4 pos = getPatchPoint(uv, 6 * patchIndex);
    gl_Position = projMatrix * viewMatrix * modelMatrix * pos;
}