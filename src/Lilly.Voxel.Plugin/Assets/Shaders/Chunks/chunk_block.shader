#shader fragment
#version 330 core

// Input from Vertex Shader
in vec3 vNormal;
in float vFogFactor;
in vec2 vTileCoord;
in vec2 vTileBase;
in vec2 vTileSize;
in vec3 vBlockCoord;
in vec3 vVertexLight;

// Output
out vec4 FragColor;

// Uniforms
uniform float uTexMultiplier;
uniform sampler2D uTexture;
uniform bool uFogEnabled;
uniform vec3 uFogColor;
uniform vec3 uAmbient;
uniform vec3 uLightDirection;
uniform float uFade;

void main()
{
vec2 tiledCoord = fract(vec2(vTileCoord.x, 1.0 - vTileCoord.y));
vec2 atlasCoord = vTileBase + tiledCoord * vTileSize;
vec4 texResult = texture(uTexture, atlasCoord * uTexMultiplier);

// Discard transparent pixels
if (texResult.a == 0.0)
discard;

// Treat fully white textures (clouds) as softer lit so they don't get harsh shading.
bool isCloud = all(greaterThanEqual(texResult.rgb, vec3(0.99)));

vec3 lightDir = normalize(uLightDirection);
float diff = max(dot(vNormal, lightDir), 0.0);
float diffTerm = isCloud ? (1.0 - diff * 0.2) : diff;
vec3 diffuse = diffTerm * vec3(1.0, 1.0, 1.0);

vec3 vertexLight = clamp(vVertexLight, 0.0, 1.0);
if (isCloud) {
vertexLight = max(vertexLight, vec3(0.75)); // keep clouds bright even with low light
}

vec3 color = texResult.rgb * (uAmbient + diffuse) * vertexLight * uFade;

// Apply fog
if (uFogEnabled)
{
color = mix(uFogColor, color, vFogFactor);
}

FragColor = vec4(color, texResult.a * uFade);
}

#shader vertex
#version 330 core

// Vertex Shader Input
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec4 aColor;
layout(location = 2) in vec2 aTileCoord;
layout(location = 3) in vec2 aTileBase;
layout(location = 4) in vec2 aTileSize;
layout(location = 5) in vec3 aBlockCoord;

// Uniforms
uniform vec3 uModel;
uniform mat4 uView;
uniform mat4 uProjection;
uniform bool uFogEnabled;
uniform float uFogStart;
uniform float uFogEnd;

// Output to Fragment Shader
out vec3 vNormal;
out float vFogFactor;
out vec2 vTileCoord;
out vec2 vTileBase;
out vec2 vTileSize;
out vec3 vBlockCoord;
out vec3 vVertexLight;

// Array of possible normals based on direction
const vec3 normals[7] = vec3[7](
vec3( 0, 0, 1), // 0 - South
vec3( 0, 0, -1), // 1 - North
vec3( 1, 0, 0), // 2 - East
vec3(-1, 0, 0), // 3 - West
vec3( 0, 1, 0), // 4 - Top
vec3( 0, -1, 0), // 5 - Bottom
vec3( 0, -1, 0) // 6 - Default
);

void main()
{
vec4 worldPosition = vec4(aPosition + uModel, 1.0);
vec4 viewPosition = uView * worldPosition;
gl_Position = uProjection * viewPosition;

vTileCoord = aTileCoord;
vTileBase = aTileBase;
vTileSize = aTileSize;
vBlockCoord = aBlockCoord;
vVertexLight = aColor.rgb;

// Extract direction from color.a and use it to get normal
int direction = int(round(aColor.a * 255.0));
vNormal = normals[clamp(direction, 0, 6)];

// Calculate fog
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
