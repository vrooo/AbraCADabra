#version 330 core
layout (location = 0) in vec3 position;
layout (location = 1) in vec3 normal;

uniform mat4 projMatrix;
uniform mat4 viewMatrix;
uniform mat4 invViewMatrix;
uniform mat4 modelMatrix;

varying vec3 W;
varying vec3 V;
varying vec3 N;

void main()
{
    vec4 worldPos = modelMatrix * vec4(position, 1);
    W = worldPos.xyz;
    vec3 camPos = (invViewMatrix * vec4(0, 0, 0, 1)).xyz;
    V = normalize(camPos - worldPos.xyz);
    N = normalize((modelMatrix * vec4(normal, 0)).xyz);
    gl_Position = projMatrix * viewMatrix * worldPos;
}