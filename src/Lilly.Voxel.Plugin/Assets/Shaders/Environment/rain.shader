#shader fragment
#version 330 core

// Input from Vertex Shader
in vec2 vCorner;
in float vAlpha;
in float vLength;

// Output
out vec4 FragColor;

// Uniforms
uniform float uIntensity;
uniform float uTime;

void main()
{
float density = clamp(uIntensity, 0.0, 1.0);
if (density <= 0.001)
discard;

float headFade = clamp(1.0 - vCorner.y, 0.0, 1.0);
float streakFade = clamp(0.35 + headFade * 0.65, 0.0, 1.0);
float shimmer = 0.5 + 0.5 * sin((vLength * 0.6) + uTime * 12.0 + vCorner.y * 9.42);

float alpha = clamp(vAlpha * density * streakFade, 0.0, 1.0);
float brightness = clamp(0.6 + shimmer * 0.3, 0.0, 1.0);

vec3 color = vec3(0.55, 0.68, 0.9) * brightness;

FragColor = vec4(color, alpha);
}

#shader vertex
#version 330 core

// Vertex Shader Input
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aCorner;
layout(location = 2) in float aLength;
layout(location = 3) in float aAlpha;

// Uniforms
uniform mat4 uWorld;
uniform mat4 uView;
uniform mat4 uProjection;
uniform vec3 uCameraRight;
uniform vec3 uRainDirection;
uniform float uDropWidth;

// Output to Fragment Shader
out vec2 vCorner;
out float vAlpha;
out float vLength;

void main()
{
vec3 basePos = (uWorld * vec4(aPosition, 1.0)).xyz;
vec3 right = normalize(uCameraRight);
vec3 direction = normalize(uRainDirection);

float widthOffset = (aCorner.x - 0.5) * uDropWidth;
float lengthOffset = aCorner.y * aLength;
vec3 worldPos = basePos + right * widthOffset + direction * lengthOffset;

vec4 viewPos = uView * vec4(worldPos, 1.0);
gl_Position = uProjection * viewPos;

vCorner = aCorner;
vAlpha = aAlpha;
vLength = aLength;
}
