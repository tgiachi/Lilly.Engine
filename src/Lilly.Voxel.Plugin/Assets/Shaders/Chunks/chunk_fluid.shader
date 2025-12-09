#shader fragment
#version 330 core

// Input from Vertex Shader
in vec2 vTexCoord;
in vec3 vNormal;
in float vFogFactor;
in vec3 vVertexLight;

// Output
out vec4 FragColor;

// Uniforms
uniform sampler2D uTexture;
uniform bool uFogEnabled;
uniform vec3 uFogColor;
uniform vec3 uAmbient;
uniform vec3 uLightDirection;
uniform float uWaterTransparency;
uniform float uFade;

// Simple fluid shading inspired by Craft's fluid shader
void main()
{
vec4 texResult = texture(uTexture, vTexCoord);
if (texResult.a == 0.0)
discard;

vec3 lightDir = normalize(uLightDirection);
float diff = max(dot(vNormal, lightDir), 0.0);
vec3 diffuse = diff * vec3(1.0);

vec3 vertexLight = clamp(vVertexLight, 0.0, 1.0);
vec3 lighting = uAmbient + diffuse;
// Clamp total lighting to prevent over-exposure on bright surfaces
lighting = min(lighting, vec3(1.0));
vec3 color = texResult.rgb * lighting * vertexLight;
color *= uFade;

if (uFogEnabled)
{
color = mix(uFogColor, color, vFogFactor);
}

// Apply controllable transparency
float t = clamp(uWaterTransparency, 0.0, 1.0);
float alpha = texResult.a * (1.0 - t) * uFade;

FragColor = vec4(color, alpha);
}

#shader vertex
#version 330 core

// Vertex Shader Input
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec4 aColor;
layout(location = 2) in vec2 aTexCoord;
layout(location = 3) in vec2 aTileBase;
layout(location = 4) in vec2 aTileSize;
layout(location = 5) in float aDirection;
layout(location = 6) in float aTop;

// Uniforms
uniform float uTexMultiplier;
uniform vec3 uModel;
uniform mat4 uView;
uniform mat4 uProjection;
uniform float uTime;
uniform bool uFogEnabled;
uniform float uFogStart;
uniform float uFogEnd;

// Output to Fragment Shader
out vec2 vTexCoord;
out vec3 vNormal;
out float vFogFactor;
out vec3 vVertexLight;

const float PI = 3.1415926535;
const float UV_INSET = 0.001; // tighten UVs to avoid sampling padded transparent texels
const int FRAME_COUNT = 32;
const float ANIMATION_TIME = 5.0;
const int FRAME_COLUMNS = 16;

// Array of possible normals based on direction
const vec3 normals[7] = vec3[7](
vec3( 0, 0, 1), // 0
vec3( 0, 0, -1), // 1
vec3( 1, 0, 0), // 2
vec3(-1, 0, 0), // 3
vec3( 0, 1, 0), // 4
vec3( 0, -1, 0), // 5
vec3( 0, -1, 0) // 6
);

void main()
{
vec3 pos = aPosition;

// Apply wave animation for top surface
if (int(aTop) == 1)
{
pos.y -= 0.1;
float wave = (sin((pos.x + uModel.x) * PI / 2.0 + uTime) + sin((pos.z + uModel.z) * PI / 2.0 + uTime * 1.5)) * 0.05;
pos.y += wave;
}

vec4 worldPosition = vec4(pos + uModel, 1.0);
vec4 viewPosition = uView * worldPosition;
gl_Position = uProjection * viewPosition;

// Atlas coordinates with animated frame offsets (Craft-style water animation)
float anim = mod(uTime / ANIMATION_TIME, 1.0);
float frame = floor(anim * float(FRAME_COUNT));
float frameX = mod(frame, float(FRAME_COLUMNS));
float frameY = floor(frame / float(FRAME_COLUMNS));

vec2 tiled = aTexCoord * (1.0 - 2.0 * UV_INSET) + UV_INSET; // shrink UVs slightly to avoid bleeding
vec2 frameOffset = vec2(frameX, frameY);
vec2 atlasCoord = aTileBase + (tiled + frameOffset) * aTileSize;
vTexCoord = atlasCoord * uTexMultiplier;

int dir = clamp(int(round(aDirection)), 0, 6);
vNormal = normals[dir];

// Adjust normal for waving top surface to keep lighting coherent
if (dir == 4 && int(aTop) == 1)
{
float slopeX = cos((pos.x + uModel.x) * PI / 2.0 + uTime) * (PI / 2.0) * 0.05;
float slopeZ = cos((pos.z + uModel.z) * PI / 2.0 + uTime * 1.5) * (PI / 2.0) * 0.05;
vNormal = normalize(vec3(-slopeX, 1.0, -slopeZ));
}
vVertexLight = aColor.rgb; // aColor is already normalized by the vertex attribute

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
