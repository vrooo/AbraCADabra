#version 430
layout (lines_adjacency) in;
layout (line_strip, max_vertices = 256) out;

const int minDivs = 10;
const int maxDivs = 255;
const int n = 4;
const float lenMult = 20.0f;

vec4 getBezierPoint(float t, vec4 pts[n], int len) {
    vec4 arr[n][n];
    for (int j = 0; j < len; j++)
    {
        arr[0][j] = pts[j];
    }
    for (int i = 1; i < len; i++)
    {
        for (int j = 0; j < len - i; j++)
        {
            arr[i][j] = (1 - t) * arr[i - 1][j] + t * arr[i - 1][j + 1];
        }
    }
    return arr[len - 1][0];
}

void main() {
    vec4 pts[n];
    vec4 ptsNorm[n];
    int cnt = 0;
    for (int i = 0; i < n; i++)
    {
        if (gl_in[i].gl_PointSize > 0)
        {
            pts[cnt] = gl_in[cnt].gl_Position;
            ptsNorm[cnt] = pts[cnt] / pts[cnt].w;
            cnt++;
        }
    }

    float total = 0.0f;
    for (int i = 0; i < cnt - 1; i++)
    {
        total += length((ptsNorm[i] - ptsNorm[i + 1]).xy);
    }
    int divs = int(clamp(lenMult * total, minDivs, maxDivs)); // TODO: different step sizes for curve segments?

    float t, st = 1.0f / divs;
    for (int i = 0; i <= divs; i++)
    {
        t = i * st;
        vec4 point = getBezierPoint(t, pts, cnt);
        gl_Position = point;
        EmitVertex();
        // debug!
//        if (t < 0.33f)      { gl_Position.y += 1; }
//        else if (t < 0.67f) { gl_Position.y -= 3; }
//        else if (t < 2.00f) { gl_Position.y += 1; }
//        else                { gl_Position.y -= 3; }
//        EmitVertex();
//        gl_Position = point;
//        EmitVertex();
    }

    EndPrimitive();
}