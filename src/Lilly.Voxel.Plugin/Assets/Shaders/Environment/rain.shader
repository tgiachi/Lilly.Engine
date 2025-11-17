#shader fragment
#version 330 core

// Input from Vertex Shader
in vec2 vTexCoord;
in vec2 vCorner;
in float vAlpha;
in float vLength;
in vec4 vProjectedPos;

// Output
out vec4 FragColor;

// Uniforms
uniform float uIntensity;
uniform float uTime;
uniform float uDepthThreshold;
uniform float uRefractionStrength;
uniform sampler2D uRainTexture;
uniform sampler2D uDepthTexture;
uniform sampler2D uNormalTexture;

void main()
{
    float density = clamp(uIntensity, 0.0, 1.0);
    if (density <= 0.001)
        discard;

    // Screen-space position for depth sampling
    vec2 screenPos = vProjectedPos.xy / vProjectedPos.w * 0.5 + 0.5;

    // Sample depth texture for collision detection
    float depth = texture(uDepthTexture, screenPos).r;
    float projDepth = vProjectedPos.w / 1000.0;

    // Fade out near surfaces (soft particle effect)
    float depthDiff = clamp((depth - projDepth) / uDepthThreshold, 0.0, 1.0);

    // Rain texture with UV animation
    vec3 rainTex = texture(uRainTexture, vTexCoord).rgb;

    // Head fade for raindrop shape
    float headFade = clamp(1.0 - vCorner.y, 0.0, 1.0);
    float streakFade = clamp(0.35 + headFade * 0.65, 0.0, 1.0);

    // Shimmer with subtle variation
    float shimmer = 0.5 + 0.5 * sin((vLength * 0.6) + uTime * 12.0 + vCorner.y * 9.42);

    // Normal sampling for refraction effect
    vec2 normalUV = vTexCoord + vec2(uTime * 0.3, 0);
    vec3 normal = texture(uNormalTexture, normalUV).rgb * 2.0 - 1.0;

    // Refraction distortion
    vec2 refractCoord = screenPos + normal.xy * uRefractionStrength * 0.01;

    // Final color and alpha
    float alpha = clamp(vAlpha * density * streakFade * depthDiff, 0.0, 1.0);
    float brightness = clamp(0.6 + shimmer * 0.3, 0.0, 1.0);
    vec3 color = rainTex * vec3(0.55, 0.68, 0.9) * brightness;

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
uniform vec3 uWindDirection;
uniform float uTime;
uniform float uDropWidth;

// Output to Fragment Shader
out vec2 vTexCoord;
out vec2 vCorner;
out float vAlpha;
out float vLength;
out vec4 vProjectedPos;

void main()
{
    vec3 basePos = (uWorld * vec4(aPosition, 1.0)).xyz;
    vec3 right = normalize(uCameraRight);
    vec3 direction = normalize(uRainDirection);

    // Wind deformation per-drop
    float windStrength = sin(uTime * 1.5 + aPosition.z * 0.3) * 0.15;
    vec3 windOffset = uWindDirection * windStrength;

    float widthOffset = (aCorner.x - 0.5) * uDropWidth;
    float lengthOffset = aCorner.y * aLength;
    vec3 worldPos = basePos + right * widthOffset + direction * lengthOffset + windOffset;

    vec4 viewPos = uView * vec4(worldPos, 1.0);
    vec4 projPos = uProjection * viewPos;

    gl_Position = projPos;
    vProjectedPos = projPos;
    vCorner = aCorner;
    vAlpha = aAlpha;
    vLength = aLength;

    // UV mapping for rain texture
    vTexCoord = vec2(aCorner.x, fract(aCorner.y + uTime * 0.5));
}
