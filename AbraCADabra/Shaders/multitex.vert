﻿#version 330 core
layout (location = 0) in vec3 position;

out vec2 uv;

void main()
{
    gl_Position = vec4(position, 1.0f);
    uv = position.xy / 2 + vec2(0.5f, 0.5f);
}