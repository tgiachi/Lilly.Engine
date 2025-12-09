#shader vertex
#version 330 core
layout (location = 0) in vec3 Position;
layout (location = 1) in vec4 Color;
layout (location = 2) in vec2 TexCoords;

uniform mat4 World;
uniform mat4 View;
uniform mat4 Projection;

out vec4 vColor;
out vec2 vTexCoords;

void main()
{
gl_Position = Projection * View * World * vec4(Position, 1.0);
vColor = Color;
vTexCoords = TexCoords;
}

#shader fragment
#version 330 core
in vec4 vColor;
in vec2 vTexCoords;
out vec4 FragColor;

uniform sampler2D Texture;

void main()
{
FragColor = vColor * texture(Texture, vTexCoords);
}
