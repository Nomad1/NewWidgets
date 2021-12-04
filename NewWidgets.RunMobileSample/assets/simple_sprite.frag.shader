in vec2 TexCoord;
flat in vec4 Color;
flat in uint Flag;

uniform sampler2D s_texture_0;

void fs_main(void)
{
    gl_FragColor = texture(s_texture_0, TexCoord) * Color;
    gl_FragColor.rgb *= Flag == 0x10u ? 1.5 : Flag == 0x40u ? 2.25 : gl_FragColor.a;
}