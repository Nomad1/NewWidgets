/* vertex parameters */ 
LOCATION(0) in vec3 in_position;
LOCATION(1) in vec2 in_texcoord;
/* misc data */
LOCATION(2) in vec2 in_flags;
LOCATION(3) in vec4 in_color;
LOCATION(4) in vec4 in_clip;

uniform mat4 s_pmvMatrix;

out vec2 TexCoord;

flat out vec4 Color;
flat out uint Flag;
flat out vec4 Clip;

void main(void)
{
    Flag = uint(in_flags[0]);
    TexCoord = in_texcoord;
    Color = in_color.bgra;
    Clip = in_clip;

    gl_Position = s_pmvMatrix * vec4(in_position, 1.0);

}