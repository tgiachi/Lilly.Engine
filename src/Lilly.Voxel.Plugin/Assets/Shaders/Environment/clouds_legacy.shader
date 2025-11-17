#shader fragment
#version 330 core

// Input from Vertex Shader
in vec3 vNormal;

// Output
out vec4 FragColor;

// Uniforms
uniform vec3 uAmbient;
uniform vec3 uLightDirection;

void main()
{
    vec3 cloudColor = vec3(1.0, 1.0, 1.0);
    vec3 norm = normalize(vNormal);

    // Calculate directional lighting
    vec3 lightDir = normalize(-uLightDirection);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * vec3(1.0, 1.0, 1.0);

    // Combine ambient and diffuse lighting
    vec3 lighting = uAmbient + diffuse;

    // Apply lighting to cloud color
    cloudColor *= lighting;

    FragColor = vec4(cloudColor, 0.75);
}

#shader vertex
#version 330 core

// Vertex Shader Input
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;

// Uniforms
uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

// Output to Fragment Shader
out vec3 vNormal;

void main()
{
    // Transform normal to world space
    mat3 normalMatrix = transpose(mat3(uModel));
    vNormal = normalMatrix * aNormal;

    // Transform position
    vec4 worldPos = uModel * vec4(aPosition, 1.0);
    vec4 viewPos = uView * worldPos;
    gl_Position = uProjection * viewPos;
}
