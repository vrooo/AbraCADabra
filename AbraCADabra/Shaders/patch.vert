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

out vec3 world;

vec4 getBezierPoint(float t, vec4 pts[n], int nn)
{
    vec4 arr[n][n];
    for (int j = 0; j < nn; j++)
    {
        arr[0][j] = pts[j];
    }
    for (int i = 1; i < nn; i++)
    {
        for (int j = 0; j < nn - i; j++)
        {
            arr[i][j] = (1 - t) * arr[i - 1][j] + t * arr[i - 1][j + 1];
        }
    }
    return arr[nn - 1][0];
}

vec4 getDeBoorPoint(float t, vec4 pts[n], int nn)
{
    float N[n], A[n - 1], B[n - 1];
    N[0] = 1.f;
    for (int i = 0; i < nn - 1; i++)
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
    for (int i = 0; i < nn; i++)
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
        q[i] = deboor ? getDeBoorPoint(uv.x, p, n) : getBezierPoint(uv.x, p, n);
    }
    return deboor ? getDeBoorPoint(uv.y, q, n) : getBezierPoint(uv.y, q, n);
}

vec4 getDu(vec2 uv, int sx, int sz, bool deboor, int mult)
{
    vec4 p[n], q[n]; // p is actually n-1
    for (int i = 0; i < n; i++)
    {
        for (int j = 0; j < n - 1; j++)
        {
            p[j] = mult * (texelFetch(texturePoint, ivec2(sx + j + 1, sz + i), 0) - texelFetch(texturePoint, ivec2(sx + j, sz + i), 0));
        }
        q[i] = deboor ? getDeBoorPoint(uv.x, p, n - 1) : getBezierPoint(uv.x, p, n - 1);
    }
    return deboor ? getDeBoorPoint(uv.y, q, n) : getBezierPoint(uv.y, q, n);
}

vec4 getDv(vec2 uv, int sx, int sz, bool deboor, int mult)
{
    vec4 p[n], q[n]; // p is actually n-1
    for (int i = 0; i < n; i++)
    {
        for (int j = 0; j < n - 1; j++)
        {
            p[j] = mult * (texelFetch(texturePoint, ivec2(sx + i, sz + j + 1), 0) - texelFetch(texturePoint, ivec2(sx + i, sz + j), 0));
        }
        q[i] = deboor ? getDeBoorPoint(uv.y, p, n - 1) : getBezierPoint(uv.y, p, n - 1);
    }
    return deboor ? getDeBoorPoint(uv.x, q, n) : getBezierPoint(uv.x, q, n);
}

void main()
{
    // assuming continuity is 0 or 2
    int mult = 3 - continuity;
    bool deboor = continuity == 2;
    vec4 pos = getPatchPoint(uv, mult * indexX, mult * indexZ, deboor);

    // begin temporary offset
//    vec3 du = getDu(uv, mult * indexX, mult * indexZ, deboor, mult).xyz;
//    vec3 dv = getDv(uv, mult * indexX, mult * indexZ, deboor, mult).xyz;
//    vec4 normal = vec4(normalize(cross(dv, du)), 0);
//    pos += normal * 0.4;
    // end temporary offset

    vec4 worldPos = modelMatrix * pos;
    world = worldPos.xyz;
    gl_Position = projMatrix * viewMatrix * worldPos;
}