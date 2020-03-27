#version 330 core
layout (lines) in;
layout (line_strip, max_vertices = 256) out;

uniform vec4 bezierPoints[];

void main() {
    gl_Position = gl_in[0].gl_Position; 
    EmitVertex();
    gl_Position = gl_in[1].gl_Position;
    EmitVertex();
    gl_Position.x += 1;
    gl_Position.y += 1;
    EmitVertex();
    EndPrimitive();
}