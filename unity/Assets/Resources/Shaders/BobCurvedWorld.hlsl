// Battle of the Brooms: the curved-world displacement shared by BOB/CurvedWorld (and any shader that wants the
// mini-planet look). The play plane stays flat for physics, NavMesh and movement; only the vertex positions drawn
// on screen are lowered by curvature * squared X-Z distance from the main camera, so the ground falls away into a
// small dome under the player. BobCurvedWorldController.cs writes the global _BobCurvature every frame; the
// per-material _CurvatureScale lets a piece opt out (0) or bend harder (2) without breaking the shared value.
#ifndef BOB_CURVED_WORLD_INCLUDED
#define BOB_CURVED_WORLD_INCLUDED

float _BobCurvature; // global, not a material property

/// <summary>World position with the mini-planet drop applied. Scale 0 keeps the point flat.</summary>
float3 BobCurvePosition(float3 worldPos, float scale)
{
    float2 d = worldPos.xz - _WorldSpaceCameraPos.xz;
    worldPos.y -= _BobCurvature * scale * dot(d, d);
    return worldPos;
}

/// <summary>Ground normals tilt outward with the dome so the toon ramp follows the curve; walls keep their normal.</summary>
float3 BobCurveNormal(float3 worldNormal, float3 worldPos, float scale)
{
    if (worldNormal.y > 0.5)
    {
        float2 d = worldPos.xz - _WorldSpaceCameraPos.xz;
        worldNormal = normalize(worldNormal + float3(2.0 * _BobCurvature * scale * d.x, 0.0, 2.0 * _BobCurvature * scale * d.y));
    }
    return worldNormal;
}

#endif
