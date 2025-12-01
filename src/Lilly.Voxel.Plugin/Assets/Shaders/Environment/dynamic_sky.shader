#shader fragment
#version 330 core

// Input from Vertex Shader
in vec3 vTexCoords;

// Output
out vec4 FragColor;

// Uniforms
uniform float uTime;// Time of day (0.0 to 1.0)
uniform float uRealTime;// Real elapsed time in seconds (for animations)
uniform vec3 uSunDirection;// Normalized sun direction
uniform vec3 uMoonDirection;// Normalized moon direction
uniform float uUseTexture;
uniform float uTextureStrength;
uniform float uEnableAurora;// Enable aurora borealis (1.0 or 0.0)
uniform float uAuroraIntensity;// Aurora intensity (0.0 to 1.0)
uniform sampler2D uSkyTexture;
uniform sampler2D uSunMoonAtlas;
uniform vec2 uSunDayBase;
uniform vec2 uSunDaySize;
uniform vec2 uSunHorizonBase;
uniform vec2 uSunHorizonSize;
uniform vec2 uMoonBase;
uniform vec2 uMoonSize;

const float PI = 3.14159265359;

// Hash function for procedural stars generation
float hash(vec3 p)
{
    p = fract(p * 0.3183099 + 0.1);
    p *= 17.0;
    return fract(p.x * p.y * p.z * (p.x + p.y + p.z));
}

// Sample sky texture
vec3 SampleSkyTexture(vec3 direction)
{
    vec3 dir = normalize(direction);
    float longitude = atan(dir.x, dir.z);
    float latitude = asin(clamp(dir.y, -1.0, 1.0));

    float u = longitude / (2.0 * PI) + 0.5;
    float v = 0.5 - latitude / PI;

    return texture(uSkyTexture, vec2(u, v)).rgb;
}

vec4 SampleAtlas(vec2 baseUV, vec2 sizeUV, vec2 uv)
{
    return texture(uSunMoonAtlas, baseUV + uv * sizeUV);
}

void BuildTangentBasis(vec3 dir, out vec3 right, out vec3 up)
{
    vec3 worldUp = vec3(0.0, 1.0, 0.0);
    vec3 fallbackUp = vec3(0.0, 0.0, 1.0);
    vec3 ref = abs(dot(worldUp, dir)) > 0.99 ? fallbackUp : worldUp;
    right = normalize(cross(ref, dir));
    up = normalize(cross(dir, right));
}

vec2 ProjectToSpritePlane(vec3 dir, vec3 targetDir, float radius, out float distanceFromCenter)
{
    vec3 right;
    vec3 up;
    BuildTangentBasis(targetDir, right, up);
    vec2 local = vec2(dot(dir, right), dot(dir, up));
    distanceFromCenter = length(local);
    return local / radius * 0.5 + 0.5;
}

// Sky color calculation
vec3 GetSkyColor(vec3 direction, float t)
{
    vec3 dir = normalize(direction);

    float vertical_pos = dir.y;
    float sun_angle = t * 2.0 * PI;
    float sun_height = sin(sun_angle);

    // Color definitions
    vec3 day_top = vec3(0.502, 0.659, 1.0);
    vec3 day_bottom = vec3(0.753, 0.847, 1.0);

    vec3 sunset_top = vec3(1.0, 0.4, 0.1);// Rosso/Arancio vivido
    vec3 sunset_bottom = vec3(0.8, 0.2, 0.15);// Arancio/Rosa profondo

    vec3 night_top = vec3(0.004, 0.004, 0.008);
    vec3 night_bottom = vec3(0.035, 0.043, 0.075);

    float day_blend = smoothstep(0.0, 0.6, sun_height);
    float sunset_window = smoothstep(-0.4, 0.2, sun_height);
    float night_blend = smoothstep(-0.6, -0.1, sun_height);

    vec3 high_sky = mix(sunset_top, day_top, day_blend);
    vec3 low_sky = mix(sunset_bottom, day_bottom, day_blend);

    vec3 top_color = mix(night_top, high_sky, sunset_window);
    vec3 bottom_color = mix(night_bottom, low_sky, sunset_window);

    // Reinforce full night when sun is far below horizon
    top_color = mix(night_top, top_color, night_blend);
    bottom_color = mix(night_bottom, bottom_color, night_blend);

    // Horizon rotation effect
    float horizon_rotation = sun_angle * 0.1;
    float rotated_vertical_pos = vertical_pos + sin(horizon_rotation) * 0.1;

    float transition_width = 0.08;
    float horizon_center = 0.0;

    float blend_factor = smoothstep(-transition_width, transition_width, horizon_center - rotated_vertical_pos);
    vec3 sky_color = mix(top_color, bottom_color, blend_factor);

    // Horizontal variation
    float horizontal_variation = sin(dir.x * PI) * cos(dir.z * PI) * 0.02;
    sky_color += horizontal_variation;

    // Horizon glow during sunrise/sunset
    if (sun_height > -0.3 && sun_height < 0.3)
    {
        float glow_intensity = 1.0 - abs(sun_height) / 0.3;
        float horizon_distance = abs(rotated_vertical_pos - horizon_center);
        float glow = exp(-horizon_distance * 8.0) * glow_intensity * 0.3;
        sky_color += vec3(glow * 1.0, glow * 0.6, glow * 0.2);
    }

    return sky_color;
}

// Renders enhanced twilight with multiple color layers
vec3 RenderTwilight(vec3 direction, vec3 sky_color)
{
    vec3 dir = normalize(direction);
    vec3 sun_dir = normalize(uSunDirection);

    // Only render twilight during transition periods
    float sun_height = sun_dir.y;

    // Check if we're in twilight zone (sun between -0.5 and 0.3)
    if (sun_height > 0.3 || sun_height < -0.5)
    return sky_color;

    // Calculate twilight intensity
    float twilight_blend = 0.0;
    if (sun_height > -0.3)
    {
        // Sunrise/sunset zone (0.3 to -0.3)
        twilight_blend = (sun_height + 0.3) / 0.6;
    }
    else
    {
        // Deep twilight zone (-0.3 to -0.5)
        twilight_blend = (sun_height + 0.5) / 0.2;
    }

    twilight_blend = clamp(twilight_blend, 0.0, 1.0);

    // Get vertical position in sky
    float vertical_pos = dir.y;

    // Define twilight color layers (from top to bottom)
    vec3 twilight_top = vec3(0.1, 0.05, 0.3);// Deep violet
    vec3 twilight_mid_upper = vec3(0.3, 0.15, 0.5);// Purple-blue
    vec3 twilight_mid = vec3(0.4, 0.25, 0.6);// Blue-violet
    vec3 twilight_mid_lower = vec3(0.7, 0.3, 0.4);// Pink-blue
    vec3 twilight_horizon = vec3(1.0, 0.5, 0.3);// Orange-pink at horizon

    // Blend between color layers based on vertical position
    vec3 twilight_color;

    if (vertical_pos > 0.5)
    {
        // Top part: deep violet
        twilight_color = mix(twilight_mid_upper, twilight_top, (vertical_pos - 0.5) * 2.0);
    }
    else if (vertical_pos > 0.2)
    {
        // Upper-middle: purple-blue transition
        twilight_color = mix(twilight_mid, twilight_mid_upper, (vertical_pos - 0.2) / 0.3);
    }
    else if (vertical_pos > 0.0)
    {
        // Lower-middle: blue to pink
        twilight_color = mix(twilight_mid_lower, twilight_mid, vertical_pos / 0.2);
    }
    else
    {
        // Horizon: orange-pink
        twilight_color = mix(twilight_horizon, twilight_mid_lower, -vertical_pos);
    }

    // Blend twilight colors with existing sky
    return mix(sky_color, twilight_color, twilight_blend * 0.7);
}

// Renders aurora borealis (Northern Lights)
vec3 RenderAurora(vec3 direction, vec3 sky_color)
{
    // Only render if enabled
    if (uEnableAurora < 0.5)
    return sky_color;

    vec3 dir = normalize(direction);
    vec3 sun_dir = normalize(uSunDirection);

    // Only show aurora at night (sun below horizon)
    if (sun_dir.y > -0.2)
    return sky_color;

    // Aurora appears mostly in the northern hemisphere (positive Z, positive Y)
    // Get spherical coordinates
    float latitude = asin(clamp(dir.y, -1.0, 1.0));
    float longitude = atan(dir.x, dir.z);

    // Aurora visibility - strongest in northern part of sky
    float aurora_zone = clamp((latitude - 0.3) / 0.3, 0.0, 1.0);// Peaks above horizon
    if (aurora_zone < 0.01)
    return sky_color;

    // Create wave patterns with multiple sine waves using RealTime for smooth animation
    float wave1 = sin(longitude * 2.0 + uRealTime * 0.8) * 0.5 + 0.5;
    float wave2 = sin(longitude * 3.0 - uRealTime * 0.5) * 0.3 + 0.7;
    float wave3 = sin(longitude * 1.5 + uRealTime * 1.1) * 0.4 + 0.6;

    float aurora_wave = wave1 * wave2 * wave3;

    // Vertical falloff (stronger at horizon, weaker above)
    float vertical_strength = 1.0 - (dir.y - 0.3) * 1.5;
    vertical_strength = clamp(vertical_strength, 0.0, 1.0);

    // Create smooth aurora colors using continuous sine wave transitions
    float phase = aurora_wave * PI;// Convert wave to phase (0 to PI)

    // Use sine waves offset by 120 degrees for smooth RGB transitions
    float red = sin(phase + PI * 0.66) * 0.4 + 0.3;// Red peaks late
    float green = sin(phase + PI * 1.33) * 0.4 + 0.4;// Green peaks early
    float blue = sin(phase) * 0.5 + 0.5;// Blue peaks in middle

    vec3 aurora_color = clamp(vec3(red, green, blue), 0.0, 1.0);

    // Calculate final aurora intensity with all factors
    float aurora_strength = aurora_wave * vertical_strength * aurora_zone * uAuroraIntensity;

    // Add glow/halo effect using RealTime
    float aurora_glow = sin(uRealTime * 0.7 + longitude * 1.5) * 0.2 + 0.3;
    aurora_strength *= (1.0 + aurora_glow);

    // Blend aurora with sky
    return mix(sky_color, sky_color + aurora_color * aurora_strength, aurora_strength * 0.6);
}

// Renders procedural stars in the night sky
vec3 RenderStars(vec3 direction, vec3 sky_color)
{
    vec3 dir = normalize(direction);

    // Only show stars at night (when sun is below horizon)
    vec3 sun_dir = normalize(uSunDirection);
    if (sun_dir.y > -0.1)
    return sky_color;// No stars during day/twilight

    // Fade in stars as night progresses
    float star_visibility = clamp((-sun_dir.y - 0.1) / 0.3, 0.0, 1.0);

    // Create star field using hash function
    // Scale direction to create grid cells
    vec3 scaled_dir = dir * 100.0;
    vec3 cell = floor(scaled_dir);

    // Generate stars in current cell
    float star_hash = hash(cell);

    // Only some cells have stars (sparse distribution)
    if (star_hash > 0.87)
    {
        // Star position within cell
        vec3 star_pos = cell + vec3(
        hash(cell + 1.0),
        hash(cell + 2.0),
        hash(cell + 3.0)
        );

        // Distance from current direction to star
        float dist = length(normalize(star_pos) - dir);

        // Create sharp star point
        float star_brightness = 1.0 - smoothstep(0.0, 0.003, dist);

        // Star intensity varies (some brighter than others)
        float intensity = hash(cell + 4.0) * 0.4 + 0.6;
        star_brightness *= intensity;

        // Add twinkling effect based on time
        float twinkle1 = sin(uTime * 80.0 + hash(cell + 5.0) * 100.0) * 0.5 + 0.5;
        float twinkle2 = sin(uTime * 30.0 + hash(cell + 6.0) * 100.0) * 0.3 + 0.7;
        float twinkle = twinkle1 * twinkle2;
        star_brightness *= twinkle;

        // Apply visibility fade
        star_brightness *= star_visibility;

        // Star color (slightly bluish-white)
        vec3 star_color = vec3(0.95, 0.98, 1.0) * star_brightness * 1.8;

        return sky_color + star_color;
    }

    return sky_color;
}

// Renders the sun corona (halo effect around sun at sunset)
vec3 RenderSunCorona(vec3 direction, vec3 sky_color)
{
    vec3 dir = normalize(direction);
    vec3 sun_dir = normalize(uSunDirection);

    // Only render corona when sun is near horizon (sunset/sunrise)
    float sunset_intensity = clamp(1.0 - abs(sun_dir.y) * 2.5, 0.0, 1.0);
    if (sunset_intensity < 0.1)
    return sky_color;

    // Calculate angular distance from sun center
    float sun_dot = dot(dir, sun_dir);

    // Corona parameters (very large halo)
    float corona_threshold = 0.96;// ~16 degrees - outer corona
    float corona_inner = 0.998;// ~3.6 degrees - inner limit

    if (sun_dot > corona_threshold && sun_dot < corona_inner)
    {
        // Normalized distance from inner to outer corona
        float corona_distance = (sun_dot - corona_threshold) / (corona_inner - corona_threshold);

        // Smooth falloff for corona
        float corona_strength = (1.0 - corona_distance) * (1.0 - corona_distance);

        // Corona color - golden/orange at sunset
        vec3 corona_color = mix(
        vec3(0.5, 0.3, 0.1), // Day corona (subtle)
        vec3(1.0, 0.6, 0.2), // Sunset corona (golden)
        sunset_intensity
        );

        // Blend corona with sky, more intense at sunset
        float corona_alpha = corona_strength * sunset_intensity * 0.4;
        return mix(sky_color, corona_color, corona_alpha);
    }

    return sky_color;
}

// Renders the sun as a bright disc in the sky
vec3 RenderSun(vec3 direction, vec3 sky_color)
{
    vec3 dir = normalize(direction);
    vec3 sun_dir = normalize(uSunDirection);
    float sun_height = sun_dir.y;

    // Only render sun when it's above horizon (Y > 0)
    if (sun_height <= -0.05)
    return sky_color;

    // Larger disc near horizon, tighter at midday
    float angular_radius = mix(0.24, 0.10, clamp((sun_height + 0.2) / 0.8, 0.0, 1.0));

    float distance_from_center;
    vec2 sprite_uv = ProjectToSpritePlane(dir, sun_dir, angular_radius, distance_from_center);

    if (distance_from_center > angular_radius || any(lessThan(sprite_uv, vec2(0.0))) ||
        any(greaterThan(sprite_uv, vec2(1.0))))
    {
        return sky_color;
    }

    float day_blend = smoothstep(-0.05, 0.3, sun_height);
    vec4 sun_day = SampleAtlas(uSunDayBase, uSunDaySize, sprite_uv);
    vec4 sun_horizon = SampleAtlas(uSunHorizonBase, uSunHorizonSize, sprite_uv);
    vec4 sun_sample = mix(sun_horizon, sun_day, day_blend);

    float alpha = sun_sample.a * (1.0 - smoothstep(angular_radius * 0.5, angular_radius, distance_from_center));

    vec3 glow_color = mix(vec3(1.0, 0.75, 0.55), vec3(1.0, 0.55, 0.25), 1.0 - day_blend);
    float glow = exp(-distance_from_center * 60.0) * (0.4 + (1.0 - day_blend) * 0.4);

    vec3 color = mix(sky_color, sun_sample.rgb, alpha);
    color += glow_color * glow * alpha;

    return color;
}

// Renders the moon as a silver disc in the night sky
vec3 RenderMoon(vec3 direction, vec3 sky_color)
{
    vec3 dir = normalize(direction);
    vec3 moon_dir = normalize(uMoonDirection);

    // Only render moon when it's above horizon (Y > 0)
    if (moon_dir.y <= -0.1)
    return sky_color;

    float angular_radius = 0.16;
    float distance_from_center;
    vec2 sprite_uv = ProjectToSpritePlane(dir, moon_dir, angular_radius, distance_from_center);

    if (distance_from_center > angular_radius || any(lessThan(sprite_uv, vec2(0.0))) ||
        any(greaterThan(sprite_uv, vec2(1.0))))
    {
        return sky_color;
    }

    vec4 moon_sample = SampleAtlas(uMoonBase, uMoonSize, sprite_uv);
    float alpha = moon_sample.a * (1.0 - smoothstep(angular_radius * 0.55, angular_radius, distance_from_center));

    vec3 glow_color = vec3(0.75, 0.8, 0.9);
    float glow = exp(-distance_from_center * 50.0) * 0.25;

    vec3 color = mix(sky_color, moon_sample.rgb, alpha);
    color += glow_color * glow * alpha;

    return color;
}

void main()
{
    // Get base sky color (procedural gradient)
    vec3 procedural_color = GetSkyColor(vTexCoords, uTime);

    // Optionally blend with texture
    vec3 texture_color = SampleSkyTexture(vTexCoords);
    float blend = clamp(uTextureStrength * uUseTexture, 0.0, 1.0);
    vec3 sky_color = mix(procedural_color, texture_color, blend);

    // Add twilight colors during sunset/sunrise
    sky_color = RenderTwilight(vTexCoords, sky_color);

    // Add aurora borealis if enabled
    sky_color = RenderAurora(vTexCoords, sky_color);

    // Add stars (rendered after aurora)
    sky_color = RenderStars(vTexCoords, sky_color);

    // Add the moon (visible at night)
    sky_color = RenderMoon(vTexCoords, sky_color);

    // Add the sun corona (halo at sunset/sunrise)
    sky_color = RenderSunCorona(vTexCoords, sky_color);

    // Add the sun (visible during day)
    sky_color = RenderSun(vTexCoords, sky_color);

    FragColor = vec4(sky_color, 1.0);
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
out vec3 vTexCoords;

void main()
{
    // Pass position as texture coordinates (direction vector for procedural sky)
    vTexCoords = aPosition;

    // Transform vertex position through standard WVP pipeline
    vec4 worldPos = uWorld * vec4(aPosition, 1.0);
    vec4 viewPos = uView * worldPos;
    vec4 projPos = uProjection * viewPos;

    // CRITICAL: Set z = w to ensure depth becomes 1.0 after perspective divide
    // This renders the skybox at maximum depth (far plane)
    gl_Position = projPos.xyww;
}
