#shader fragment
#version 330 core

// Input from Vertex Shader
in vec2 vCorner;
in float vAlpha;
in float vSize;

// Output
out vec4 FragColor;

// Uniforms
uniform float uIntensity;
uniform float uTime;
uniform sampler2D uSnowflakeTexture;

void main()
{
float density = clamp(uIntensity, 0.0, 1.0);
if (density <= 0.001)
discard;

vec4 texColor = texture(uSnowflakeTexture, vCorner);

vec2 uv = vCorner - 0.5;
float sparkle = 0.65 + 0.35 * sin(uTime * 4.0 + uv.x * 8.0 + uv.y * 6.0);
float alpha = clamp(vAlpha * density * texColor.a, 0.0, 1.0);

vec3 color = texColor.rgb * vec3(0.92, 0.95, 1.0) * sparkle;

FragColor = vec4(color, alpha);
}

#shader vertex
#version 330 core

// Vertex Shader Input
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aCorner;
layout(location = 2) in float aSize;
layout(location = 3) in float aAlpha;

// Uniforms
uniform mat4 uWorld;
uniform mat4 uView;
uniform mat4 uProjection;
uniform vec3 uCameraRight;
uniform vec3 uCameraUp;
uniform float uTime;

// Output to Fragment Shader
out vec2 vCorner;
out float vAlpha;
out float vSize;

void main()
{
vec3 basePos = (uWorld * vec4(aPosition, 1.0)).xyz;
vec3 right = normalize(uCameraRight);
vec3 upVec = normalize(uCameraUp);

float flutter = sin(uTime * 0.9 + basePos.x * 0.25 + basePos.z * 0.19) * 0.35;
vec3 jitter = right * flutter * 0.4 + upVec * flutter * 0.2;

vec2 centered = aCorner - 0.5;
vec3 worldPos = basePos + jitter + right * centered.x * aSize + upVec * centered.y * aSize;

vec4 viewPos = uView * vec4(worldPos, 1.0);
gl_Position = uProjection * viewPos;

vCorner = aCorner;
vAlpha = aAlpha;
vSize = aSize;
}
