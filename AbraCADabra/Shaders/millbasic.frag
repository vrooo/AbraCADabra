#version 330 core
uniform sampler2D materialTex;
layout (location = 0) out vec4 oColor;

in VertexNormalData {
    vec3 W;
    vec3 V;
    vec3 N;
    vec2 UV;
} PSIn;

void main()
{
    vec4 color = texture(materialTex, PSIn.UV);
    // TODO: uniforms
    vec3 lightPos = vec3(20, 30, 20);
    vec3 ambientCol = vec3(0.5);
    vec3 lightCol = vec3(1);
    float kd = 0.7, ks = 0.3, m = 20;

    vec3 col = color.rgb * ambientCol;

    vec3 L = normalize(lightPos - PSIn.W);
    col += color.rgb * lightCol * kd * clamp(dot(PSIn.N, L), 0, 1);

    vec3 H = normalize(PSIn.V + L);
    float nh = clamp(pow(dot(PSIn.N, H), m), 0, 1);
    col += lightCol * ks * nh;

    oColor = vec4(col, 1);
}