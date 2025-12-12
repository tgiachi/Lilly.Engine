#shader vertex
#version 330 core
layout (location = 0) in vec3 Position;
layout (location = 1) in vec3 Normal;
layout (location = 2) in vec2 TexCoords;

uniform mat4 World;
uniform mat4 View;
uniform mat4 Projection;
uniform mat4 uLightView;
uniform mat4 uLightProjection;

out vec2 vTexCoords;
out vec3 vNormal;
out vec4 vFragPosLightSpace;

void main()
{
vec4 worldPos = World * vec4(Position, 1.0);
vTexCoords = TexCoords;
vNormal = mat3(World) * Normal;
vFragPosLightSpace = uLightProjection * uLightView * worldPos;
gl_Position = Projection * View * worldPos;
}

#shader fragment
#version 330 core
in vec2 vTexCoords;
in vec3 vNormal;
in vec4 vFragPosLightSpace;

out vec4 FragColor;

uniform sampler2D Texture;
uniform sampler2D uShadowMap;
uniform vec3 LightDir = vec3(-0.4, -1.0, -0.2);
uniform vec3 LightColor = vec3(1.0, 1.0, 1.0);
uniform vec3 Ambient = vec3(0.15, 0.15, 0.15);
uniform vec4 Tint = vec4(1.0, 1.0, 1.0, 1.0);
uniform bool uEnableShadows = true;
uniform float uShadowBias = 0.002;

float CalculateShadow(vec4 fragPosLightSpace, vec3 normal, vec3 lightDir)
{
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    projCoords = projCoords * 0.5 + 0.5;

    if (projCoords.z > 1.0)
        return 0.0;

    float currentDepth = projCoords.z;
    float bias = max(0.05 * (1.0 - dot(normal, lightDir)), uShadowBias);

    float shadow = 0.0;
    vec2 texelSize = 1.0 / textureSize(uShadowMap, 0);

    for(int x = -1; x <= 1; ++x)
    {
        for(int y = -1; y <= 1; ++y)
        {
            float pcfDepth = texture(uShadowMap, projCoords.xy + vec2(x, y) * texelSize).r;
            shadow += currentDepth - bias > pcfDepth ? 1.0 : 0.0;
        }
    }

    return shadow / 9.0;
}

void main()
{
vec3 normal = normalize(vNormal);
vec3 lightDir = normalize(-LightDir);

float ndl = max(dot(normal, lightDir), 0.0);
float shadow = uEnableShadows ? CalculateShadow(vFragPosLightSpace, normal, lightDir) : 0.0;

vec3 directLight = LightColor * ndl * (1.0 - shadow);
vec3 light = Ambient + directLight;

vec4 baseColor = texture(Texture, vTexCoords) * Tint;
FragColor = vec4(baseColor.rgb * light, baseColor.a);
}
