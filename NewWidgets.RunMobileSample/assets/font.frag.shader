in vec2 TexCoord;
flat in vec4 Color;
flat in uint Flag;

uniform sampler2D s_texture_0;

#if defined( COMPAT_QUALITY )
    const float FontWidth = 0.20;      // ideal thickness value for smoothstep 
#elif defined( LOW_QUALITY )
    const float FontWidth = 0.25;      // ideal thickness value for dx/dy method   
#else
    const float FontWidth = 0.3;      // ideal thickness value for complex derivatives 
#endif

void main(void)
{
    vec4 texColor = texture(s_texture_0, TexCoord);

#if defined( SDF_CHANNEL_RED )
    float dist = texColor.r; // on mobile devices switch back to regular SDF
#else
    float dist = max(min(texColor.r, texColor.g), min(max(texColor.r, texColor.g), texColor.b)); // median for MSDF 
#endif
    
    vec4 color = Color.rgba;
    float alpha;

    float threshold = 0.4 + (Flag == 0x04u || Flag == 0x05u ? 0.0 : 0.1); // if 0 >= Flag (i.e. Flag is 0x01 or 0x02) then add extra width

#if defined( COMPAT_QUALITY )

    alpha = smoothstep( threshold - FontWidth, threshold + FontWidth, dist);

#elif defined( LOW_QUALITY )

    vec2 d = vec2(dFdx(TexCoord.x), dFdy(TexCoord.y)) * vec2(textureSize(s_texture_0, 0));
    float toPixels = 32.0 * FontWidth / length(d);

    float sigDist = dist - threshold;

    alpha = saturate(sigDist * toPixels + threshold);

#else

    // texture size-based derivative calculation
    // taken from Cinder-SdfText project

    // Convert normalized texcoords to absolute texcoords.
    vec2 uv = TexCoord * vec2(textureSize(s_texture_0, 0));
    // Calculate derivates
    vec2 Jdx = dFdx( uv );
    vec2 Jdy = dFdy( uv );
    // calculate signed distance (in texels).
    float sigDist = dist - threshold;
    // For proper anti-aliasing, we need to calculate signed distance in pixels. We do this using derivatives.
    vec2 gradDist = normalize( vec2( dFdx( sigDist ), dFdy( sigDist ) ) );
    vec2 grad = vec2( gradDist.x * Jdx.x + gradDist.y * Jdy.x, gradDist.x * Jdx.y + gradDist.y * Jdy.y );
    // Apply anti-aliasing.
    const float kThickness = FontWidth * 0.5;
    const float kNormalization = kThickness * 0.5 * sqrt( 2.0 );
    float afFontWidth = min( kNormalization * length( grad ), threshold );
    alpha = smoothstep( 0.0 - afFontWidth, 0.0 + afFontWidth, sigDist );
   
    if (Flag == 0x05u) // outlined font  
    {
        float outline = smoothstep(0.0 - afFontWidth / 32.0, 0.0 + afFontWidth / 32.0, pow(sigDist, 3.35));
        color *= smoothstep(0.0 - afFontWidth / 32.0, 0.0 + afFontWidth / 32.0, pow(sigDist, 3.35));
    }
#endif

    color.a *= alpha;

    gl_FragColor = color;
    gl_FragColor.rgb *= gl_FragColor.a;
}