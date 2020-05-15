#version 330 core
uniform sampler2D texturePoint;

layout (location = 0) in vec2 uv;
layout (location = 1) in int index;

uniform mat4 viewMatrix;
uniform mat4 projMatrix;
uniform mat4 modelMatrix;

void main()
{
    vec4 tmp = texture(texturePoint, uv);
    //tmp.w = 1.0f;
    gl_Position = projMatrix * viewMatrix * modelMatrix * tmp;
}