#version 330 core
layout (location = 0) in vec3 position;
layout (location = 1) in vec3 normal;
layout (location = 2) in int indexX;
layout (location = 3) in int indexZ;

uniform mat4 projMatrix;
uniform mat4 viewMatrix;
uniform mat4 invViewMatrix;
uniform mat4 modelMatrix;

varying vec3 W;
varying vec3 V;
varying vec3 N;
varying vec2 uv;

void main()
{
    uv = vec2(position.x, position.z) / 10;
    vec4 worldPos = modelMatrix * vec4(position, 1);
    W = worldPos.xyz;
    vec3 camPos = (invViewMatrix * vec4(0, 0, 0, 1)).xyz;
    V = normalize(camPos - worldPos.xyz);
    N = normalize((modelMatrix * vec4(normal, 0)).xyz);
    gl_Position = projMatrix * viewMatrix * worldPos;
}