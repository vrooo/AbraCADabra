#version 430

uniform sampler2D texturePoint;
uniform mat4 viewMatrix;
uniform mat4 projMatrix;
uniform mat4 modelMatrix;

const int n = 4;

layout (location = 0) in vec2 uv;
layout (location = 1) in int indexX;
layout (location = 2) in int indexZ;

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

vec4 getPatchPoint(vec2 uv, int indx, int indz)
{
    int sx = 3 * indx, sz = 3 * indz;
    vec4 p[n], q[n];
    for (int i = 0; i < n; i++)
    {
        for (int j = 0; j < n; j++)
        {
            p[j] = texelFetch(texturePoint, ivec2(sx + i, sz + j), 0);
        }
        q[i] = getBezierPoint(uv.x, p);
    }
    return getBezierPoint(uv.y, q);
}

void main()
{
    vec4 pos = getPatchPoint(uv, indexX, indexZ);
    gl_Position = projMatrix * viewMatrix * modelMatrix * pos;
}