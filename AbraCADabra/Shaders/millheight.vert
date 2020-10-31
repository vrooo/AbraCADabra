#version 330 core
layout (location = 0) in vec3 position;
layout (location = 1) in int indexX;
layout (location = 2) in int indexZ;

uniform mat4 projMatrix;
uniform mat4 viewMatrix;
uniform mat4 invViewMatrix;
uniform mat4 modelMatrix;
uniform sampler2D materialHeightMap;

out VertexData {
    vec3 W;
    vec3 V;
    vec2 UV;
} VSOut;

void main()
{
    vec3 pos = position;
    if (indexX != -1 && indexZ != -1)
    {
        pos.y = texelFetch(materialHeightMap, ivec2(indexX, indexZ), 0).r;
    }
    VSOut.UV = vec2(pos.x, pos.z) / 10;
    vec4 worldPos = modelMatrix * vec4(pos, 1);
    VSOut.W = worldPos.xyz;
    vec3 camPos = (invViewMatrix * vec4(0, 0, 0, 1)).xyz;
    VSOut.V = normalize(camPos - worldPos.xyz);
    gl_Position = projMatrix * viewMatrix * worldPos;
}