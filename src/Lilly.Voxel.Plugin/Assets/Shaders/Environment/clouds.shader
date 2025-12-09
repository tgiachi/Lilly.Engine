#shader fragment
#version 330 core

// Input from Vertex Shader
in vec3 vNormal;

// Output
out vec4 FragColor;

// Uniforms
uniform vec3 ambient;
uniform vec3 lightDirection;
uniform vec3 uCloudColor;

void main()
{
vec3 cloudColor = uCloudColor;
vec3 norm = normalize(vNormal);

// Calculate directional lighting
vec3 lightDir = normalize(-lightDirection);
float diff = max(dot(norm, lightDir), 0.0);
vec3 diffuse = diff * vec3(1.0, 1.0, 1.0);

// Combine ambient and diffuse lighting
vec3 lighting = ambient + diffuse;

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
uniform mat4 uWorld;
uniform mat4 uView;
uniform mat4 uProjection;
uniform float uTime;
uniform vec2 uWindVelocity;

// Output to Fragment Shader
out vec3 vNormal;

void main()
{
// Apply a simple planar drift on XZ based on time to move the cloud
vec3 displacedPosition = aPosition + vec3(uWindVelocity.x, 0.0, uWindVelocity.y) * uTime;

// Transform normal to world space
mat3 normalMatrix = transpose(inverse(mat3(uWorld)));
vNormal = normalize(normalMatrix * aNormal);

// Transform position
vec4 worldPos = uWorld * vec4(displacedPosition, 1.0);
vec4 viewPos = uView * worldPos;
gl_Position = uProjection * viewPos;
}
