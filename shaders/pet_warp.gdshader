shader_type canvas_item;

uniform float idle_phase = 0.0;
uniform vec2 eye_offset = vec2(0.0);

float ellipse_mask(vec2 uv, vec2 center, vec2 radius) {
	vec2 p = (uv - center) / radius;
	return 1.0 - smoothstep(0.72, 1.0, dot(p, p));
}

void fragment() {
	vec2 uv = UV;

	float hair = smoothstep(0.62, 0.16, uv.y) *
		(1.0 - ellipse_mask(uv, vec2(0.5, 0.50), vec2(0.30, 0.34)));
	uv.x += sin(idle_phase + uv.y * 7.0) * 0.0028 * hair;

	float drool = ellipse_mask(uv, vec2(0.47, 0.80), vec2(0.16, 0.12));
	uv.x += sin(idle_phase * 0.72) * 0.0035 * drool;

	vec4 base = texture(TEXTURE, uv);
	float left_eye = ellipse_mask(uv, vec2(0.322, 0.582), vec2(0.095, 0.085));
	float right_eye = ellipse_mask(uv, vec2(0.623, 0.584), vec2(0.095, 0.085));
	float eye_region = max(left_eye, right_eye);
	vec4 shifted = texture(TEXTURE, uv - eye_offset * eye_region);
	float chroma = max(shifted.r, max(shifted.g, shifted.b)) -
		min(shifted.r, min(shifted.g, shifted.b));
	base = mix(base, shifted, eye_region * smoothstep(0.10, 0.28, chroma));

	COLOR = base;
}
