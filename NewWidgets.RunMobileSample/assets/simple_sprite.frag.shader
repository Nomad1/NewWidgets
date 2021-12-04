uniform sampler2D s_texture_0;

in vec2 TexCoord;
flat in vec4 Color;
flat in uint Flag;
flat in vec4 Clip;

void main(void)
{
    if (step(Clip.x, gl_FragCoord.x) * step(Clip.y, gl_FragCoord.y) * step(gl_FragCoord.x, Clip.z) * step(gl_FragCoord.y, Clip.w) == 0.0) // scissor test
        discard;

    gl_FragColor = texture(s_texture_0, TexCoord) * Color;
    gl_FragColor.rgb *= Flag == 0x10u ? 1.5 : Flag == 0x40u ? 2.25 : gl_FragColor.a;
}