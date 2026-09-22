#version 330 core
layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec2 aUV;
layout (location = 2) in vec4 aColor;
out vec2 fragUV;
out vec4 vertexColor;
uniform mat4 transform;
uniform vec4 color;
uniform vec4 UVOffset;
void main() {
    gl_Position = transform * vec4(aPosition, 0.0, 1.0);
    fragUV = vec2(aUV.x*UVOffset.b+UVOffset.r, 1.0 - (aUV.y*UVOffset.a+UVOffset.g));
    vertexColor = aColor * color;
}