#shader vertex
#version 330 core
  layout (location = 0) in vec3 Position;
  layout (location = 1) in vec3 Normal;
  layout (location = 2) in vec2 TexCoords;

  uniform mat4 World;
  uniform mat4 View;
  uniform mat4 Projection;

  out VS_OUT {
      vec3 FragPos;
      vec3 Normal;
      vec2 TexCoords;
  } vs_out;

  void main()
  {
      vec4 worldPos = World * vec4(Position, 1.0);
      vs_out.FragPos = worldPos.xyz;
      vs_out.Normal = mat3(transpose(inverse(World))) * Normal;
      vs_out.TexCoords = TexCoords;
      gl_Position = Projection * View * worldPos;
  }

#shader fragment
  #version 330 core

  // Input from vertex shader
  in VS_OUT {
      vec3 FragPos;
      vec3 Normal;
      vec2 TexCoords;
  } fs_in;

  // Output
  out vec4 FragColor;

  // Material textures
  uniform sampler2D uAlbedoMap;
  uniform sampler2D uNormalMap;
  uniform sampler2D uRoughnessMap;
  uniform sampler2D uMetallicMap;
  uniform sampler2D uEmissiveMap;

  // Material properties
  uniform vec4 uTint = vec4(1.0, 1.0, 1.0, 1.0);
  uniform float uRoughness = 0.5;
  uniform float uMetallic = 0.0;
  uniform vec3 uEmissiveColor = vec3(0.0, 0.0, 0.0);
  uniform float uEmissiveIntensity = 0.0;

  // Light structures
  struct DirectionalLight {
      vec3 direction;
      vec4 color;
      bool castShadows;
  };

  struct PointLight {
      vec3 position;
      vec4 color;
      float radius;
      float constant;
      float linear;
      float quadratic;
  };

  struct SpotLight {
      vec3 position;
      vec3 direction;
      vec4 color;
      float innerCutoff;
      float outerCutoff;
      float range;
      float constant;
      float linear;
      float quadratic;
  };

  // Light arrays
  #define MAX_DIRECTIONAL_LIGHTS 4
  #define MAX_POINT_LIGHTS 32
  #define MAX_SPOT_LIGHTS 16

  uniform int uDirectionalLightCount = 0;
  uniform int uPointLightCount = 0;
  uniform int uSpotLightCount = 0;

  uniform DirectionalLight uDirectionalLights[MAX_DIRECTIONAL_LIGHTS];
  uniform PointLight uPointLights[MAX_POINT_LIGHTS];
  uniform SpotLight uSpotLights[MAX_SPOT_LIGHTS];

  uniform vec3 uAmbient = vec3(0.1, 0.1, 0.1);
  uniform vec3 uCameraPos;

  // Calculate directional light contribution
  vec3 CalcDirectionalLight(DirectionalLight light, vec3 normal, vec3 viewDir, vec3 albedo, float roughness, float metallic)
  {
      vec3 lightDir = normalize(-light.direction);

      // Diffuse
      float diff = max(dot(normal, lightDir), 0.0);

      // Specular (Blinn-Phong)
      vec3 halfwayDir = normalize(lightDir + viewDir);
      float shininess = (1.0 - roughness) * 128.0;
      float spec = pow(max(dot(normal, halfwayDir), 0.0), shininess);

      vec3 lightColor = light.color.rgb * light.color.a;
      vec3 diffuse = lightColor * diff * albedo;
      vec3 specular = lightColor * spec * mix(vec3(0.04), albedo, metallic);

      return diffuse + specular;
  }

  // Calculate point light contribution
  vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir, vec3 albedo, float roughness, float metallic)
  {
      vec3 lightDir = normalize(light.position - fragPos);
      float distance = length(light.position - fragPos);

      // Check radius
      if (distance > light.radius)
          return vec3(0.0);

      // Attenuation
      float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * distance * distance);

      // Diffuse
      float diff = max(dot(normal, lightDir), 0.0);

      // Specular (Blinn-Phong)
      vec3 halfwayDir = normalize(lightDir + viewDir);
      float shininess = (1.0 - roughness) * 128.0;
      float spec = pow(max(dot(normal, halfwayDir), 0.0), shininess);

      vec3 lightColor = light.color.rgb * light.color.a;
      vec3 diffuse = lightColor * diff * albedo * attenuation;
      vec3 specular = lightColor * spec * mix(vec3(0.04), albedo, metallic) * attenuation;

      return diffuse + specular;
  }

  // Calculate spot light contribution
  vec3 CalcSpotLight(SpotLight light, vec3 normal, vec3 fragPos, vec3 viewDir, vec3 albedo, float roughness, float metallic)
  {
      vec3 lightDir = normalize(light.position - fragPos);
      float distance = length(light.position - fragPos);

      if (distance > light.range)
          return vec3(0.0);

      // Spotlight intensity
      float theta = dot(lightDir, normalize(-light.direction));
      float epsilon = light.innerCutoff - light.outerCutoff;
      float intensity = clamp((theta - light.outerCutoff) / epsilon, 0.0, 1.0);

      if (intensity == 0.0)
          return vec3(0.0);

      // Attenuation
      float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * distance * distance);

      // Diffuse
      float diff = max(dot(normal, lightDir), 0.0);

      // Specular (Blinn-Phong)
      vec3 halfwayDir = normalize(lightDir + viewDir);
      float shininess = (1.0 - roughness) * 128.0;
      float spec = pow(max(dot(normal, halfwayDir), 0.0), shininess);

      vec3 lightColor = light.color.rgb * light.color.a;
      vec3 diffuse = lightColor * diff * albedo * attenuation * intensity;
      vec3 specular = lightColor * spec * mix(vec3(0.04), albedo, metallic) * attenuation * intensity;

      return diffuse + specular;
  }

  void main()
  {
      // Sample material textures
      vec4 albedoSample = texture(uAlbedoMap, fs_in.TexCoords);
      vec3 albedo = albedoSample.rgb * uTint.rgb;

      // Normal mapping (simplified - assumes tangent space is world space)
      vec3 normalSample = texture(uNormalMap, fs_in.TexCoords).rgb;
      normalSample = normalize(normalSample * 2.0 - 1.0);
      vec3 normal = normalize(fs_in.Normal); // Simplified - use world normal

      float roughness = texture(uRoughnessMap, fs_in.TexCoords).r * uRoughness;
      float metallic = texture(uMetallicMap, fs_in.TexCoords).r * uMetallic;
      vec3 emissive = texture(uEmissiveMap, fs_in.TexCoords).rgb * uEmissiveColor * uEmissiveIntensity;

      vec3 viewDir = normalize(uCameraPos - fs_in.FragPos);

      // Start with ambient
      vec3 result = uAmbient * albedo;

      // Add directional lights
      for (int i = 0; i < uDirectionalLightCount && i < MAX_DIRECTIONAL_LIGHTS; i++)
      {
          result += CalcDirectionalLight(uDirectionalLights[i], normal, viewDir, albedo, roughness, metallic);
      }

      // Add point lights
      for (int i = 0; i < uPointLightCount && i < MAX_POINT_LIGHTS; i++)
      {
          result += CalcPointLight(uPointLights[i], normal, fs_in.FragPos, viewDir, albedo, roughness, metallic);
      }

      // Add spot lights
      for (int i = 0; i < uSpotLightCount && i < MAX_SPOT_LIGHTS; i++)
      {
          result += CalcSpotLight(uSpotLights[i], normal, fs_in.FragPos, viewDir, albedo, roughness, metallic);
      }

      // Add emissive
      result += emissive;

      FragColor = vec4(result, albedoSample.a * uTint.a);
  }
