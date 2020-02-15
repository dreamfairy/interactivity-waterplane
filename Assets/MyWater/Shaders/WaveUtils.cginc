#ifndef WAVE_UTILS
#define WAVE_UTILS

float4 EncodeHeight(float height) {
	float2 rg = EncodeFloatRG(height > 0 ? height : 0);
	float2 ba = EncodeFloatRG(height <= 0 ? -height : 0);

	return float4(rg, ba);
}

float DecodeHeight(float4 rgba) {
	float h1 = DecodeFloatRG(rgba.rg);
	float h2 = DecodeFloatRG(rgba.ba);

	int c = step(h2, h1);
	return lerp(h2, h1, c);
}
            
#endif