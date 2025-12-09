#shader vertex
#version 330 core
layout (location = 0) in vec3 Position;
layout (location = 1) in vec3 Normal;
layout (location = 2) in vec2 TexCoords;

uniform mat4 World;
uniform mat4 View;
uniform mat4 Projection;

out vec2 vTexCoords;
out vec3 vNormal;

void main()
{
vec4 worldPos = World * vec4(Position, 1.0);
vTexCoords = TexCoords;
vNormal = mat3(World) * Normal;
gl_Position = Projection * View * worldPos;
}

#shader fragment
#version 330 core
in vec2 vTexCoords;
in vec3 vNormal;

out vec4 FragColor;

uniform sampler2D Texture;
uniform vec3 LightDir = vec3(-0.4, -1.0, -0.2);
uniform vec3 LightColor = vec3(1.0, 1.0, 1.0);
uniform vec3 Ambient = vec3(0.15, 0.15, 0.15);
uniform vec4 Tint = vec4(1.0, 1.0, 1.0, 1.0);

void main()
{
vec3 normal = normalize(vNormal);
vec3 lightDir = normalize(LightDir);

float ndl = max(dot(normal, -lightDir), 0.0);
vec3 light = Ambient + LightColor * ndl;

vec4 baseColor = texture(Texture, vTexCoords) * Tint;
FragColor = vec4(baseColor.rgb * light, baseColor.a);
}
