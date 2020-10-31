#version 430
layout (triangles) in;
layout (triangle_strip, max_vertices = 3) out;

in VertexData {
    vec3 W;
    vec3 V;
    vec2 UV;
} VSOut[3];

out VertexNormalData {
    vec3 W;
    vec3 V;
    vec3 N;
    vec2 UV;
} PSIn;

void main() {
    vec3 e1 = VSOut[1].W - VSOut[0].W;
    vec3 e2 = VSOut[2].W - VSOut[0].W;
    vec3 normal = normalize(cross(e1, e2));

    for (int i = 0; i < 3; i++)
    {
        PSIn.W = VSOut[i].W;
        PSIn.V = VSOut[i].V;
        PSIn.UV = VSOut[i].UV;
        PSIn.N = normal;
        gl_Position = gl_in[i].gl_Position;
        EmitVertex();
    }
    EndPrimitive();
}