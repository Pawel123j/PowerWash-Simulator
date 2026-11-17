#ifndef ZMYWAK_HAND_WEAR_INCLUDED
#define ZMYWAK_HAND_WEAR_INCLUDED

// What a shift does to the hand, drawn procedurally on top of the plain material: noise in the part's own space
// (metres, so the marks keep their size and stay put on a moving arm), a little relief turned into a bumped normal
// with screen-space derivatives (no tangents or extra textures needed), and the albedo and gloss pushed around.

float HandHash(float3 p)
{
    p = frac(p * 0.1031);
    p += dot(p, p.zyx + 31.32);
    return frac((p.x + p.y) * p.z);
}

float HandNoise(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    float3 u = f * f * (3.0 - 2.0 * f);
    float a = lerp(HandHash(i), HandHash(i + float3(1, 0, 0)), u.x);
    float b = lerp(HandHash(i + float3(0, 1, 0)), HandHash(i + float3(1, 1, 0)), u.x);
    float c = lerp(HandHash(i + float3(0, 0, 1)), HandHash(i + float3(1, 0, 1)), u.x);
    float d = lerp(HandHash(i + float3(0, 1, 1)), HandHash(i + float3(1, 1, 1)), u.x);
    return lerp(lerp(a, b, u.y), lerp(c, d, u.y), u.z);
}

float HandFbm(float3 p)
{
    return HandNoise(p) * 0.5 + HandNoise(p * 2.03 + 17.1) * 0.3 + HandNoise(p * 4.01 + 31.7) * 0.2;
}

// Bare forearm, p in the arm's own space: z runs from the wrist (0) back up the arm (negative).
void WearSkin(float3 p, inout half3 albedo, inout half smoothness, inout float height)
{
    float fromCuff = max(0.0, _WristZ - p.z);      // metres up the arm from the glove's cuff
    float wrist = saturate(1.0 - fromCuff / 0.09); // 1 at the cuff, gone 9 cm up

    // Years of wet work: dry, rough, paler patches, worst at the wrist where the cuff rubs.
    float dry = _HandWeathering * smoothstep(0.48, 0.68, HandFbm(p * 55.0 + 7.3)) * saturate(0.25 + wrist);
    albedo = lerp(albedo, albedo * half3(1.04, 0.97, 0.95) + 0.07, dry * 0.7);
    smoothness = lerp(smoothness, 0.12, dry);
    height += dry * 0.00018 * HandNoise(p * 520.0);

    // Heat: hot water and scrubbing flush the skin red in blotches, most at the wrist where the water runs in.
    float blotch = HandFbm(p * 38.0);
    float flush = _HandHeat * saturate(0.35 + 0.65 * wrist) * smoothstep(0.3, 0.7, blotch + 0.3 * _HandHeat);
    albedo = lerp(albedo, albedo * half3(1.03, 0.64, 0.6), flush * 0.85);

    // Tiredness: veins rising along the forearm - soft, bluish, meandering the way the arm runs, here and there.
    float3 q = float3(p.x, p.y, p.z * 0.25) * 55.0;
    float n = HandNoise(q) * 0.67 + HandNoise(q * 2.1 + 5.3) * 0.33;
    float ridge = 1.0 - abs(n * 2.0 - 1.0);
    float stretch = smoothstep(0.4, 0.6, HandNoise(p * 18.0 + 2.7));
    float vein = smoothstep(0.82, 0.95, ridge) * stretch * smoothstep(0.3, 0.85, _HandTiredness) * (1.0 - wrist * 0.6);
    albedo = lerp(albedo, albedo * half3(0.84, 0.88, 1.03), vein * 0.55);
    height += vein * 0.0004;

    // ...and sweat: a sheen over all of it, beads standing on the skin.
    float sheen = smoothstep(0.2, 0.9, _HandTiredness);
    smoothness = lerp(smoothness, 0.62, sheen * 0.7);
    float bead = smoothstep(1.0 - 0.14 * sheen, 1.0 - 0.06 * sheen, HandNoise(p * 300.0)) * step(0.01, sheen);
    smoothness = lerp(smoothness, 0.95, bead);
    height += bead * 0.0004;

    // Soak: a wet band above the cuff that creeps up the arm through the shift - darker, glossy, the skin pruned
    // into fine creases across the wrist.
    float reach = 0.012 + 0.05 * _HandSoak;
    float wet = smoothstep(0.35, 0.65, saturate(1.0 - fromCuff / reach) + (HandFbm(p * 90.0) - 0.5) * 0.6)
        * smoothstep(0.0, 0.15, _HandSoak);
    albedo *= lerp(1.0, 0.8, wet);
    smoothness = lerp(smoothness, 0.84, wet);
    float crease = abs(sin(p.z * 1100.0 + HandFbm(p * 60.0) * 6.0));
    height += _HandSoak * saturate(1.0 - fromCuff / 0.03) * 0.00008 * crease;
}

// The glove, p its rest position (metres) and tip how far out along a finger (0 the palm, 1 the fingertip).
void WearGlove(float3 p, float tip, inout half3 albedo, inout half smoothness, inout float height)
{
    // Sweat inside the nitrile: where the damp hand presses on it the blue goes darker, deeper and wet-looking.
    float damp = saturate(_HandTiredness * 0.8 + _HandHeat * 0.4);
    float stuck = smoothstep(0.62 - 0.3 * damp, 0.78 - 0.3 * damp, HandFbm(p * 70.0 + 3.1)) * smoothstep(0.1, 0.5, damp);
    albedo = lerp(albedo, albedo * half3(0.6, 0.68, 0.8), stuck * 0.85);
    smoothness = lerp(smoothness, 0.8, stuck * 0.6);

    // A shift's scrubbing scuffs the fingertips: the dye rubbed thin and pale, the shine gone.
    float scuff = smoothstep(0.45, 0.9, tip) * smoothstep(0.45, 0.7, HandFbm(p * 220.0) + _HandSoak * 0.55 - 0.2)
        * smoothstep(0.05, 0.4, _HandSoak);
    albedo = lerp(albedo, half3(0.42, 0.55, 0.78), scuff * 0.7);
    smoothness = lerp(smoothness, 0.22, scuff);
    height -= scuff * 0.00012 * HandNoise(p * 600.0);
}

// The rolled sleeve, p in the roll's own space: z = 0 its edge by the wrist. Splashes soak into it through the
// shift as grey, darker spots, mostly near the edge.
void WearSleeve(float3 p, inout half3 albedo, inout half smoothness)
{
    float near = saturate(1.0 - max(0.0, -p.z) / 0.1);
    float spots = smoothstep(0.68 - 0.16 * _HandSoak, 0.74 - 0.16 * _HandSoak, HandFbm(p * 60.0 + 11.0))
        * near * smoothstep(0.05, 0.3, _HandSoak);
    albedo = lerp(albedo, albedo * half3(0.76, 0.78, 0.8), spots);
    smoothness = lerp(smoothness, 0.34, spots);
}

// Bumps a world-space normal by a height field (world units) with the screen-space derivatives of both.
float3 HandBump(float3 normalWS, float3 positionWS, float height)
{
    float3 dpdx = ddx(positionWS);
    float3 dpdy = ddy(positionWS);
    float dhdx = ddx(height);
    float dhdy = ddy(height);
    float3 r1 = cross(dpdy, normalWS);
    float3 r2 = cross(normalWS, dpdx);
    float det = dot(dpdx, r1);
    if (abs(det) < 1e-12)
    {
        return normalWS;
    }

    float3 gradient = sign(det) * (dhdx * r1 + dhdy * r2);
    return normalize(abs(det) * normalWS - gradient);
}

#endif
