#version 330 core
layout (location = 0) in vec3 position;

uniform mat4 viewMatrix;
uniform mat4 projMatrix;
uniform mat4 modelMatrix;

void main()
{
    gl_Position = projMatrix * (viewMatrix * modelMatrix) * vec4(position.x, position.y, position.z, 1.0);
}