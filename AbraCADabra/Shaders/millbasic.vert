#version 330 core
layout (location = 0) in vec3 position;
layout (location = 1) in int indexX;
layout (location = 2) in int indexZ;

uniform mat4 projMatrix;
uniform mat4 viewMatrix;
uniform mat4 invViewMatrix;
uniform mat4 modelMatrix;

out VertexData {
    vec3 W;
    vec3 V;
    vec2 UV;
} VSOut;

void main()
{
    VSOut.UV = vec2(position.x, position.z) / 10;
    vec4 worldPos = modelMatrix * vec4(position, 1);
    VSOut.W = worldPos.xyz;
    vec3 camPos = (invViewMatrix * vec4(0, 0, 0, 1)).xyz;
    VSOut.V = normalize(camPos - worldPos.xyz);
    gl_Position = projMatrix * viewMatrix * worldPos;
}