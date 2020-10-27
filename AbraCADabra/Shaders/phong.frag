#version 330 core
uniform vec4 color;
layout (location = 0) out vec4 oColor;

varying vec3 W;
varying vec3 V;
varying vec3 N;

void main()
{
    // TODO: uniforms
    vec3 lightPos = vec3(20, 30, 20);
    vec3 ambientCol = vec3(0.5);
    vec3 lightCol = vec3(1);
    float kd = 0.5, ks = 0.5, m = 20;

    vec3 col = color.rgb * ambientCol;

    vec3 L = normalize(lightPos - W);
    col += color.rgb * lightCol * kd * clamp(dot(N, L), 0, 1);

    vec3 H = normalize(V + L);
    float nh = clamp(pow(dot(N, H), m), 0, 1);
    col += lightCol * ks * nh;

    oColor = vec4(col, 1);
}