#version 330 core
layout (location = 0) in vec3 position;
layout (location = 1) in int valid;

uniform mat4 viewMatrix;
uniform mat4 projMatrix;
uniform mat4 modelMatrix;

void main()
{
    gl_Position = projMatrix * viewMatrix * modelMatrix * vec4(position.x, position.y, position.z, 1.0);
    gl_PointSize = valid;
}