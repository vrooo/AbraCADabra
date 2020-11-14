#version 330 core
uniform vec4 color;
layout (location = 0) out vec4 oColor;

in vec3 world;

void main()
{
    oColor = vec4(world.y);
}