#shader vertex
#version 330 core

layout (location = 0) in vec3 Position;

/// Transformation matrices from object to light space
uniform mat4 uWorld;
uniform mat4 uLightView;
uniform mat4 uLightProjection;

void main()
{
    /// Transform vertex to light space for shadow depth pass
    vec4 worldPos = uWorld * vec4(Position, 1.0);
    gl_Position = uLightProjection * uLightView * worldPos;
}

#shader fragment
#version 330 core

void main()
{
    /// OpenGL automatically writes depth to the depth buffer
    /// No need to write anything in the fragment shader
}
