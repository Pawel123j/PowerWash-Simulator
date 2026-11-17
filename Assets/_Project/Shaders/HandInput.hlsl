#ifndef ZMYWAK_HAND_INPUT_INCLUDED
#define ZMYWAK_HAND_INPUT_INCLUDED

// Shared by every pass of Zmywak/Hand: the material's properties (all in UnityPerMaterial, so the SRP batcher takes
// it) and the textures URP's own depth and shadow passes read.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half _Smoothness;
    half _BumpScale;
    half _HandPart;  // 0 the glove, 1 bare skin, 2 the sleeve, 3 the glove's cuff (rigid, not skinned)
    half _WristZ;    // skin: the local z (metres) where the glove's cuff ends and the arm shows
    half _Cutoff;    // only for URP's shared passes (never alpha-tested)
CBUFFER_END

// How Kuba's hands are faring, 0-1, set for the whole kitchen by HandWear (globals, not per material).
half _HandTiredness;  // low energy and a long shift: sweat, veins, a tremor in the hand
half _HandHeat;       // hot water and scrubbing: the skin flushes
half _HandSoak;       // time at the sink this shift: a wet wrist, scuffed fingertips, a splashed sleeve
half _HandWeathering; // years of wet work: dry, rough patches at the wrist

#endif
