#shader fragment
#version 330 core

// Input from Vertex Shader
in vec3 vRayDir;
in vec3 vViewPos;

// Output
out vec4 FragColor;

// Uniforms
uniform vec3 uCameraPosition;
uniform vec3 uLightDirection;
uniform vec3 uLightColor;
uniform vec3 uAmbientColor;
uniform float uTime;
uniform float uCloudDensity;
uniform float uCloudScale;
uniform float uCloudCoverage;
uniform float uHorizonFade;
uniform sampler2D uNoiseTexture;
uniform sampler2D uDetailTexture;

// Improved hash function for procedural noise
float hash(vec3 p)
{
    p = fract(p * 0.3183099 + 0.1);
    p *= 17.0;
    return fract(p.x * p.y * p.z * (p.x + p.y + p.z));
}

// Perlin-like noise function
float noise(vec3 p)
{
    vec3 i = floor(p);
    vec3 f = fract(p);

    // Smooth interpolation curve
    vec3 u = f * f * (3.0 - 2.0 * f);

    // Hash 8 corners
    float n000 = hash(i + vec3(0, 0, 0));
    float n100 = hash(i + vec3(1, 0, 0));
    float n010 = hash(i + vec3(0, 1, 0));
    float n110 = hash(i + vec3(1, 1, 0));
    float n001 = hash(i + vec3(0, 0, 1));
    float n101 = hash(i + vec3(1, 0, 1));
    float n011 = hash(i + vec3(0, 1, 1));
    float n111 = hash(i + vec3(1, 1, 1));

    // Trilinear interpolation
    float n00 = mix(n000, n100, u.x);
    float n10 = mix(n010, n110, u.x);
    float n01 = mix(n001, n101, u.x);
    float n11 = mix(n011, n111, u.x);

    float n0 = mix(n00, n10, u.y);
    float n1 = mix(n01, n11, u.y);

    return mix(n0, n1, u.z);
}

// Fractional Brownian motion for cloud-like texture
float fbm(vec3 p)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    value += amplitude * noise(p * frequency);
    amplitude *= 0.5;
    frequency *= 2.0;

    value += amplitude * noise(p * frequency);
    amplitude *= 0.5;
    frequency *= 2.0;

    value += amplitude * noise(p * frequency);

    return value;
}

// Sample cloud density at position
float SampleCloudDensity(vec3 pos)
{
    // FBM sampling for main cloud shape
    float density = fbm(pos * uCloudScale);

    // Apply coverage threshold
    density = max(0.0, density - uCloudCoverage);

    // Temporal animation
    float temporal = sin(uTime * 0.5 + pos.y * 0.3) * 0.1;
    density += temporal;

    // Detail layer
    float detail = fbm(pos * uCloudScale * 3.0 + uTime * 0.2);
    density *= mix(0.7, 1.0, detail);

    return clamp(density * uCloudDensity, 0.0, 1.0);
}

// Compute light transmittance through cloud
float SampleTransmittance(vec3 pos, vec3 lightDir)
{
    float transmittance = 1.0;
    float samplingDist = 2.0;
    vec3 samplePos = pos + lightDir * samplingDist;

    // Sample density along light direction
    for (int i = 0; i < 2; i++)
    {
        float density = SampleCloudDensity(samplePos);
        transmittance *= exp(-density * samplingDist);
        samplePos += lightDir * samplingDist;
    }

    return transmittance;
}

void main()
{
    // Normalize ray direction
    vec3 rayDir = normalize(vRayDir);

    // Ray marching parameters
    float stepSize = 0.5;
    int maxSteps = 16;
    float transmittance = 1.0;
    vec3 lightAccum = vec3(0, 0, 0);
    vec3 pos = uCameraPosition;

    // March along ray
    for (int step = 0; step < maxSteps; step++)
    {
        float density = SampleCloudDensity(pos);

        if (density > 0.001)
        {
            // Calculate light contribution
            float lightTransmittance = SampleTransmittance(pos, normalize(uLightDirection));
            vec3 lightContrib = density * uLightColor * lightTransmittance;

            // Accumulate light (back-to-front blending)
            lightAccum += transmittance * lightContrib;
            transmittance *= exp(-density * stepSize);
        }

        // Stop if fully opaque
        if (transmittance < 0.01)
            break;

        // Step along ray
        pos += rayDir * stepSize;
    }

    // Add ambient lighting
    vec3 cloudColor = lightAccum + transmittance * uAmbientColor;

    // Horizon fade
    float horizonDist = abs(vRayDir.y);
    float horizonFade = clamp(pow(horizonDist, uHorizonFade), 0.0, 1.0);
    cloudColor *= horizonFade;

    FragColor = vec4(cloudColor, 1.0 - transmittance);
}

#shader vertex
#version 330 core

// Vertex Shader Input
layout(location = 0) in vec3 aPosition;

// Uniforms
uniform mat4 uWorld;
uniform mat4 uView;
uniform mat4 uProjection;

// Output to Fragment Shader
out vec3 vRayDir;
out vec3 vViewPos;

void main()
{
    // Pass position as ray direction (for ray marching)
    vRayDir = aPosition;

    // Transform vertex position
    vec4 worldPos = uWorld * vec4(aPosition, 1.0);
    vec4 viewPos = uView * worldPos;
    vViewPos = viewPos.xyz;
    gl_Position = uProjection * viewPos;
}
