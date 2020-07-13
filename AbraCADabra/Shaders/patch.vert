#version 430

uniform sampler2D texturePoint;
uniform mat4 viewMatrix;
uniform mat4 projMatrix;
uniform mat4 modelMatrix;
uniform int continuity;

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

vec4 getDeBoorPoint(float t, vec4 pts[n])
{
    float N[n], A[n - 1], B[n - 1];
    N[0] = 1.f;
    for (int i = 0; i < n - 1; i++)
    {
        A[i] = i - t + 1.f;
        B[i] = i + t;
        float saved = 0.f;
        for (int j = 0; j <= i; j++)
        {
            float term = N[j] / (A[j] + B[i - j]);
            N[j] = saved + A[j] * term;
            saved = B[i - j] * term;
        }
		N[i + 1] = saved;
    }

    vec3 pos = vec3(0.f, 0.f, 0.f);
    for (int i = 0; i < n; i++)
    {
        pos += N[i] * pts[i].xyz;
    }
    return vec4(pos, 1.f);
}

vec4 getPatchPoint(vec2 uv, int sx, int sz, bool deboor)
{
    vec4 p[n], q[n];
    for (int i = 0; i < n; i++)
    {
        for (int j = 0; j < n; j++)
        {
            p[j] = texelFetch(texturePoint, ivec2(sx + j, sz + i), 0);
        }
        q[i] = deboor ? getDeBoorPoint(uv.x, p) : getBezierPoint(uv.x, p);
    }
    return deboor ? getDeBoorPoint(uv.y, q) : getBezierPoint(uv.y, q);
}

void main()
{
    // assuming continuity is 0 or 2
    int mult = 3 - continuity;
    bool deboor = continuity == 2;
    vec4 pos = getPatchPoint(uv, mult * indexX, mult * indexZ, deboor);
    gl_Position = projMatrix * viewMatrix * modelMatrix * pos;
}