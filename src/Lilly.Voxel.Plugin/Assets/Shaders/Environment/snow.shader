#shader fragment
#version 330 core

// Input from Vertex Shader
in vec2 vTexCoord;
in vec2 vCorner;
in float vAlpha;
in float vSize;
in vec4 vProjectedPos;
in float vNoiseVal;

// Output
out vec4 FragColor;

// Uniforms
uniform float uIntensity;
uniform float uTime;
uniform float uDepthThreshold;
uniform sampler2D uSnowflakeTexture;
uniform sampler2D uDepthTexture;

void main()
{
    float density = clamp(uIntensity, 0.0, 1.0);
    if (density <= 0.001)
        discard;

    // Screen-space position for depth sampling
    vec2 screenPos = vProjectedPos.xy / vProjectedPos.w * 0.5 + 0.5;

    // Sample depth for soft particle collision
    float depth = texture(uDepthTexture, screenPos).r;
    float projDepth = vProjectedPos.w / 1000.0;
    float depthDiff = clamp((depth - projDepth) / uDepthThreshold, 0.0, 1.0);

    // Sample snowflake texture
    vec4 snowTex = texture(uSnowflakeTexture, vTexCoord);

    // Additional soft particle mask (distance-based)
    vec2 uv = vCorner - 0.5;
    float dist = dot(uv, uv);
    float softMask = clamp(1.0 - dist * 4.0, 0.0, 1.0) * depthDiff;

    // Sparkle with variation based on noise
    float sparkle = 0.65 + 0.35 * sin(uTime * 4.0 + vNoiseVal * 10.0 + uv.x * 8.0 + uv.y * 6.0);

    // Final color with texture
    float alpha = clamp(vAlpha * density * softMask * snowTex.a, 0.0, 1.0);
    vec3 color = snowTex.rgb * vec3(0.92, 0.95, 1.0) * sparkle;

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
uniform vec3 uWindDirection;
uniform float uTime;
uniform float uParticleRotation;

// Output to Fragment Shader
out vec2 vTexCoord;
out vec2 vCorner;
out float vAlpha;
out float vSize;
out vec4 vProjectedPos;
out float vNoiseVal;

// Hash function for pseudo-random values
float hash(vec3 p)
{
    p = fract(p * 0.3183099 + 0.1);
    p *= 17.0;
    return fract(p.x * p.y * p.z * (p.x + p.y + p.z));
}

// Perlin-like noise for natural flutter
float noise(vec3 p)
{
    vec3 i = floor(p);
    vec3 f = fract(p);
    vec3 u = f * f * (3.0 - 2.0 * f);

    return mix(
        mix(mix(hash(i), hash(i + vec3(1, 0, 0)), u.x),
            mix(hash(i + vec3(0, 1, 0)), hash(i + vec3(1, 1, 0)), u.x), u.y),
        mix(mix(hash(i + vec3(0, 0, 1)), hash(i + vec3(1, 0, 1)), u.x),
            mix(hash(i + vec3(0, 1, 1)), hash(i + vec3(1, 1, 1)), u.x), u.y), u.z);
}

void main()
{
    vec3 basePos = (uWorld * vec4(aPosition, 1.0)).xyz;
    vec3 right = normalize(uCameraRight);
    vec3 upVec = normalize(uCameraUp);

    // Perlin noise-based flutter
    float noiseVal = noise(basePos * 0.5 + vec3(uTime * 0.8, 0, 0));
    float flutter = noiseVal * 0.8 - 0.4;

    // Wind simulation
    float windInfluence = sin(uTime * 0.6 + basePos.x * 0.1) * 0.3;
    vec3 windOffset = uWindDirection * windInfluence;
    vec3 flutterOffset = right * flutter * 0.4 + upVec * flutter * 0.2;

    // Billboard quad construction
    vec2 centered = aCorner - 0.5;

    // Rotation effect based on particle properties
    float rotAngle = uParticleRotation + uTime * (0.5 + noiseVal);
    float cosA = cos(rotAngle);
    float sinA = sin(rotAngle);
    vec2 rotCorner = vec2(
        centered.x * cosA - centered.y * sinA,
        centered.x * sinA + centered.y * cosA
    );

    vec3 worldPos = basePos + flutterOffset + windOffset +
                    right * rotCorner.x * aSize +
                    upVec * rotCorner.y * aSize;

    vec4 viewPos = uView * vec4(worldPos, 1.0);
    vec4 projPos = uProjection * viewPos;

    gl_Position = projPos;
    vProjectedPos = projPos;
    vTexCoord = aCorner;
    vCorner = aCorner;
    vAlpha = aAlpha;
    vSize = aSize;
    vNoiseVal = noiseVal;
}
