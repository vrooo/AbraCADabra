#version 330 core
layout (location = 0) in vec3 position;
layout (location = 1) in int valid;

uniform mat4 viewMatrix;
uniform mat4 projMatrix;
uniform mat4 modelMatrix;

out VertexData {
    vec4 World;
    vec4 Position;
    int Valid;
} VSOut;

void main()
{
    VSOut.World = modelMatrix * vec4(position.x, position.y, position.z, 1.0);
    VSOut.Position = projMatrix * viewMatrix * VSOut.World;
    VSOut.Valid = valid;
}