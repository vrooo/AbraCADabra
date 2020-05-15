#version 330 core
//uniform vec4 color;
uniform vec4 colorLeft;
uniform vec4 colorRight;
uniform sampler2D textureLeft;
uniform sampler2D textureRight;

in vec2 uv;
out vec4 oColor;

float grayscale(vec4 v)
{
    return (v.x + v.y + v.z) / 3.0f;
    //return 0.299f * v.x + 0.587f * v.y + 0.114f * v.z;
}

void main()
{
    vec4 texLeft = texture(textureLeft, uv);
    float valLeft = grayscale(texLeft);
    vec4 texRight = texture(textureRight, uv);
    float valRight = grayscale(texRight);

    float r = (valLeft * colorLeft.x + valRight * colorRight.x);
    float g = (valLeft * colorLeft.y + valRight * colorRight.y);
    float b = (valLeft * colorLeft.z + valRight * colorRight.z);
    oColor = vec4(r, g, b, 1.0f);
}