#version 330 core
layout (location = 0) in vec3 position;

uniform mat4 viewMatrix;
uniform mat4 projMatrix;
uniform mat4 modelMatrix;

out vec3 world;

void main()
{
    vec4 worldPos = modelMatrix * vec4(position.x, position.y, position.z, 1.0);
    world = worldPos.xyz;
    gl_Position = projMatrix * viewMatrix * worldPos;
}