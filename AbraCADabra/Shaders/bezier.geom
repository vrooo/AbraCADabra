#version 430
layout (lines_adjacency) in;
layout (line_strip, max_vertices = 256) out;

const int divs = 20;
const int n = 4;

vec4 getBezierPoint(float t, vec4 pts[n]) {
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

void main() {
    vec4 pts[n];
    for (int i = 0; i < n; i++)
    {
        pts[i] = gl_in[i].gl_Position;
    }

    float t, st = 1.0f / divs;
    for (int i = 0; i <= divs; i++)
    {
        t = i * st;
        vec4 point = getBezierPoint(t, pts);
        gl_Position = point;
        EmitVertex();
    }

    EndPrimitive();
}