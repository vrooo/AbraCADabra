#version 330 core
layout (location = 0) in vec3 position;
layout (location = 1) in vec3 normal;
layout (location = 2) in int indexX;
layout (location = 3) in int indexZ;

uniform mat4 projMatrix;
uniform mat4 viewMatrix;
uniform mat4 invViewMatrix;
uniform mat4 modelMatrix;
uniform sampler2D materialHeightMap;

varying vec3 W;
varying vec3 V;
varying vec3 N;
varying vec2 uv;

void main()
{
    vec3 pos = position;
    if (indexX != -1 && indexZ != -1)
    {
        pos.y = texelFetch(materialHeightMap, ivec2(indexX, indexZ), 0).r;
    }
    uv = vec2(pos.x, pos.z) / 10;
    vec4 worldPos = modelMatrix * vec4(pos, 1);
    W = worldPos.xyz;
    vec3 camPos = (invViewMatrix * vec4(0, 0, 0, 1)).xyz;
    V = normalize(camPos - worldPos.xyz);
    if (indexX != -1 && indexZ != -1)
    {
        float left   = texelFetch(materialHeightMap, ivec2(indexX - 1, indexZ), 0).r;
        float right  = texelFetch(materialHeightMap, ivec2(indexX + 1, indexZ), 0).r;
        float top    = texelFetch(materialHeightMap, ivec2(indexX, indexZ - 1), 0).r;
        float bottom = texelFetch(materialHeightMap, ivec2(indexX, indexZ + 1), 0).r;
        N = normalize(vec3(left - right, 2, top - bottom));
    }
    else
    {
        N = normalize((modelMatrix * vec4(normal, 0)).xyz);
    }
    gl_Position = projMatrix * viewMatrix * worldPos;
}