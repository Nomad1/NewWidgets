/* vertex parameters */ 
LOCATION(0) in vec3 in_position;
LOCATION(1) in vec2 in_texcoord;
/* misc data */
LOCATION(2) in vec2 in_flags;
LOCATION(3) in vec4 in_color;

uniform mat4 s_pmvMatrix;

out vec2 TexCoord;
flat out vec4 Color;

void vs_main(void)
{
    vec3 pos = in_position;
    TexCoord = in_texcoord;

    gl_Position = s_pmvMatrix * vec4(pos, 1.0);

    Color = in_color.bgra;
}