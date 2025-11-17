#shader fragment
#version 330 core

in vec2 vTexCoord;
in float vAlpha;
in float vLength;
in float vNoise;
in vec4 vProjectedPos;

out vec4 FragColor;

uniform float uIntensity;
uniform float uTime;
uniform float uDepthThreshold;
uniform float uRefractionStrength;
uniform sampler2D uRainTexture;
uniform sampler2D uNormalTexture;
uniform sampler2D uDepthTexture;

void main()
{
    float density = clamp(uIntensity, 0.0, 1.0);
    if (density <= 0.001)
        discard;

    vec2 screenPos = vProjectedPos.xy / vProjectedPos.w * 0.5 + 0.5;
    // Clamp screen coordinates to valid sampling range instead of discarding
    screenPos = clamp(screenPos, vec2(0.0), vec2(1.0));

    float depthFade = 1.0;
    // Only apply depth fade if the rain is in front of the camera
    if (vProjectedPos.w > 0.0)
    {
        float depth = texture(uDepthTexture, screenPos).r;
        float projDepth = vProjectedPos.w / 1000.0;
        depthFade = clamp((depth - projDepth) / max(uDepthThreshold, 0.0001), 0.0, 1.0);
    }

    vec2 streakUV = vec2(vTexCoord.x * 0.6, fract(vTexCoord.y * (0.5 + vLength * 0.05) + uTime * 0.4 + vNoise));
    float streak = texture(uRainTexture, streakUV).r;

    vec2 normalUV = streakUV + vec2(0.0, uTime * 0.3);
    vec3 normal = texture(uNormalTexture, normalUV).rgb * 2.0 - 1.0;

    float sideMask = smoothstep(0.0, 0.12, vTexCoord.x) * smoothstep(0.0, 0.12, 1.0 - vTexCoord.x);
    float headMask = smoothstep(0.0, 0.18, vTexCoord.y);
    float tailMask = 1.0 - smoothstep(0.65, 1.0, vTexCoord.y);
    float shapeMask = sideMask * headMask * tailMask;

    float shimmer = 0.7 + 0.3 * sin(uTime * 18.0 + vNoise * 30.0 + vTexCoord.y * 12.0);
    float alpha = vAlpha * density * depthFade * shapeMask * (0.4 + 0.6 * streak) * shimmer;

    if (alpha <= 0.01)
        discard;

    float refractionImpact = clamp(uRefractionStrength, 0.0, 1.5);
    vec3 color = vec3(0.5, 0.68, 0.92) * (0.55 + 0.45 * (normal.z * 0.5 + 0.5) * refractionImpact);
    FragColor = vec4(color, alpha);
}

#shader vertex
#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aCorner;
layout(location = 2) in float aLength;
layout(location = 3) in float aAlpha;

uniform mat4 uWorld;
uniform mat4 uView;
uniform mat4 uProjection;
uniform vec3 uCameraRight;
uniform vec3 uCameraUp;
uniform vec3 uRainDirection;
uniform vec3 uWindDirection;
uniform float uTime;
uniform float uDropWidth;

out vec2 vTexCoord;
out float vAlpha;
out float vLength;
out float vNoise;
out vec4 vProjectedPos;

float Hash(vec3 p)
{
    return fract(sin(dot(p, vec3(12.9898, 78.233, 37.719))) * 43758.5453);
}

void main()
{
    vec3 basePos = (uWorld * vec4(aPosition, 1.0)).xyz;
    vec3 right = normalize(uCameraRight);
    vec3 up = normalize(uCameraUp);
    vec3 rainDir = normalize(uRainDirection);

    float sway = sin(uTime * 1.4 + basePos.x * 0.21 + basePos.z * 0.17);
    vec3 windOffset = uWindDirection * sway * 0.12;

    vec2 centered = aCorner - 0.5;

    vec3 worldPos = basePos + windOffset +
                    right * centered.x * uDropWidth +
                    up * centered.y * aLength +
                    rainDir * centered.y * (aLength * 0.15);

    vec4 viewPos = uView * vec4(worldPos, 1.0);
    vec4 projPos = uProjection * viewPos;

    gl_Position = projPos;
    vProjectedPos = projPos;
    vTexCoord = aCorner;
    vAlpha = aAlpha;
    vLength = aLength;
    vNoise = Hash(basePos);
}
