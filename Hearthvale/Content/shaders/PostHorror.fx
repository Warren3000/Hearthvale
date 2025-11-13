// PostHorror.fx - simple vignette + grain + desaturation
// Technique Name: PostHorror

Texture2D SceneTexture;
SamplerState SceneSampler
{
    Filter = Point; // MGFX expects 'Point', 'Linear', or 'Anisotropic'
    AddressU = Clamp;
    AddressV = Clamp;
};

float DesaturateAmount = 0.35;   // 0..1
float VignetteIntensity = 0.45;  // 0..1
float GrainIntensity = 0.05;     // 0..1
float2 Resolution = float2(1280, 720);

float rand(float2 co) {
    return frac(sin(dot(co, float2(12.9898,78.233))) * 43758.5453);
}

float4 PS(float4 position : SV_Position, float4 colorIn : COLOR0, float2 texCoord : TEXCOORD0) : SV_Target
{
    float4 color = SceneTexture.Sample(SceneSampler, texCoord);

    // Desaturate
    float gray = dot(color.rgb, float3(0.299, 0.587, 0.114));
    color.rgb = lerp(color.rgb, gray.xxx, DesaturateAmount);

    // Vignette
    float2 pos = texCoord - 0.5;
    float dist = dot(pos, pos) * 2.0; // 0 center .. ~1 corners
    float vignette = saturate(1.0 - dist * VignetteIntensity);
    color.rgb *= vignette;

    // Grain
    float noise = rand(texCoord * Resolution + frac(Resolution.xy));
    color.rgb = saturate(color.rgb + (noise - 0.5) * GrainIntensity);

    return color;
}

technique PostHorror
{
    pass P0
    {
        PixelShader = compile ps_4_0_level_9_1 PS();
    }
}
