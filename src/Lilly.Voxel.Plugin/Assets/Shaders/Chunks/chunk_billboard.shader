#shader fragment
#version 330 core

// Input from Vertex Shader
in vec2 vTexCoord;
in vec2 vTileBase;
in vec2 vTileSize;
in vec4 vColor;
in float vFogFactor;
in vec3 vBlockCoord;

// Output
out vec4 FragColor;

// Uniforms
uniform float uTexMultiplier;
uniform sampler2D uTexture;
uniform bool uFogEnabled;
uniform vec3 uFogColor;
uniform vec3 uAmbient;

void main()
{
    // Calculate the atlas texture coordinates using tile base and size
    vec2 tiledCoord = fract(vec2(vTexCoord.x, 1.0 - vTexCoord.y));
    vec2 atlasCoord = vTileBase + tiledCoord * vTileSize;
    vec4 texResult = texture(uTexture, atlasCoord * uTexMultiplier);

    if (texResult.a < 0.001)
        discard;

    // Billboards are camera-facing, so use simplified lighting
    vec3 vertexLight = vColor.rgb;
    float lightFactor = max(max(vertexLight.r, vertexLight.g), vertexLight.b);
    // Expand range to 0.3-1.0 to show AO shadows more clearly
    lightFactor = mix(0.3, 1.0, clamp(lightFactor, 0.0, 1.0));
    vec3 diffuse = vec3(0.5, 0.5, 0.5); // Fixed diffuse for billboards
    vec3 color = texResult.rgb * (uAmbient + diffuse) * lightFactor;

    if (uFogEnabled)
    {
        color = mix(uFogColor, color, vFogFactor);
    }

    FragColor = vec4(color, texResult.a);
}

#shader vertex
#version 330 core

// Vertex Shader Input
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec4 aColor;
layout(location = 2) in vec2 aTexCoords;
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
out vec2 vTexCoord;
out vec2 vTileBase;
out vec2 vTileSize;
out vec4 vColor;
out float vFogFactor;
out vec3 vBlockCoord;

void main()
{
    vec4 worldPosition = vec4(aPosition + uModel, 1.0);
    vec4 viewPosition = uView * worldPosition;
    gl_Position = uProjection * viewPosition;

    vTexCoord = aTexCoords;
    vTileBase = aTileBase;
    vTileSize = aTileSize;
    vColor = aColor;
    vBlockCoord = aBlockCoord;

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
