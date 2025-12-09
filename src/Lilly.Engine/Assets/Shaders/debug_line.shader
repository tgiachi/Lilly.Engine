#shader vertex
#version 330 core
layout (location = 0) in vec3 Position;

uniform mat4 World;
uniform mat4 View;
uniform mat4 Projection;

void main()
{
gl_Position = Projection * View * World * vec4(Position, 1.0);
}

#shader fragment
#version 330 core
out vec4 FragColor;

uniform vec4 Color;

void main()
{
FragColor = Color;
}
