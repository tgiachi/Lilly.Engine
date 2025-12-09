#shader fragment
#version 330 core

// Input from Vertex Shader
in vec2 vTexCoord;
in vec4 vColor;
in float vFogFactor;
in vec3 vNormal;
in vec3 vVertexLight;
in vec2 vTileBase;
in vec2 vTileSize;

// Output
out vec4 FragColor;

// Uniforms
uniform sampler2D uTexture;
uniform bool uFogEnabled;
uniform vec3 uFogColor;
uniform vec3 uAmbient;
uniform vec3 uLightDirection;
uniform float uFade;

void main()
{
vec2 tiledCoord = vec2(vTexCoord.x, 1.0 - vTexCoord.y);
vec2 atlasCoord = vTileBase + tiledCoord * vTileSize;
vec4 texResult = texture(uTexture, atlasCoord);

if (texResult.a < 0.001)
discard;

vec3 lightDir = normalize(-uLightDirection);
float diff = max(dot(vNormal, lightDir), 0.0);
vec3 diffuse = diff * vec3(1.0, 1.0, 1.0);

vec3 vertexLight = clamp(vVertexLight, 0.0, 1.0);
vertexLight = max(vertexLight, vec3(0.45)); // avoid fully black items
vec3 litColor = texResult.rgb * (uAmbient + diffuse) * vertexLight + texResult.rgb * 0.1;
vec3 fadedColor = litColor * uFade;
vec4 finalColor = vec4(fadedColor, texResult.a * vColor.a * uFade);

if (uFogEnabled)
{
finalColor.rgb = mix(uFogColor, finalColor.rgb, vFogFactor);
}

FragColor = finalColor;
}

#shader vertex
#version 330 core

// Vertex Shader Input
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec4 aColor;
layout(location = 2) in vec2 aTexCoord;
layout(location = 3) in vec2 aOffset;
layout(location = 4) in vec2 aTileBase;
layout(location = 5) in vec2 aTileSize;

// Uniforms
uniform float uTexMultiplier;
uniform vec3 uModel;
uniform mat4 uView;
uniform mat4 uProjection;
uniform bool uFogEnabled;
uniform float uFogStart;
uniform float uFogEnd;
uniform vec3 uCameraRight;
uniform vec3 uCameraUp;
uniform vec3 uCameraForward;

// Output to Fragment Shader
out vec2 vTexCoord;
out vec4 vColor;
out float vFogFactor;
out vec3 vNormal;
out vec3 vVertexLight;
out vec2 vTileBase;
out vec2 vTileSize;

void main()
{
vec3 worldCenter = aPosition + uModel;
vec3 billboardOffset = uCameraRight * aOffset.x + uCameraUp * aOffset.y;
vec3 worldPosition = worldCenter + billboardOffset;

vec4 worldPos4 = vec4(worldPosition, 1.0);
vec4 viewPosition = uView * worldPos4;
gl_Position = uProjection * viewPosition;

vTexCoord = aTexCoord * uTexMultiplier;
vColor = aColor;
vVertexLight = aColor.rgb; // already normalized
vTileBase = aTileBase;
vTileSize = aTileSize;

vNormal = normalize(-uCameraForward);

if (uFogEnabled)
{
float distance = length(viewPosition.xyz);
vFogFactor = clamp((uFogEnd - distance) / (uFogEnd - uFogStart), 0.0, 1.0);
}
else
{
vFogFactor = 1.0;
}
}
