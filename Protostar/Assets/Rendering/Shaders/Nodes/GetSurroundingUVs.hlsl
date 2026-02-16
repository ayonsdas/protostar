#ifndef SURROUNDING_UV_INCLUDED
#define SURROUNDING_UV_INCLUDED

void GetSurrounding_float(
    float4 UV, float2 TexelSize, float OffsetMultiplier,
    out float2 UVTopLeft, out float2 UVTopMiddle, out float2 UVTopRight,
    out float2 UVMiddleLeft, out float2 UVMiddleMiddle, out float2 UVMiddleRight,
    out float2 UVBottomLeft, out float2 UVBottomMiddle, out float2 UVBottomRight
) {
    float topOffset = TexelSize.y;
    float bottomOffset = -TexelSize.y;
    float leftOffset = -TexelSize.x;
    float rightOffset = TexelSize.x;
    float middleOffset = 0;

    UVTopLeft = UV.xy + float2(leftOffset, topOffset) * OffsetMultiplier;
    UVTopMiddle = UV.xy + float2(middleOffset, topOffset) * OffsetMultiplier;
    UVTopRight = UV.xy + float2(rightOffset, topOffset) * OffsetMultiplier;

    UVMiddleLeft = UV.xy + float2(leftOffset, middleOffset) * OffsetMultiplier;
    UVMiddleMiddle = UV.xy + float2(middleOffset, middleOffset) * OffsetMultiplier;
    UVMiddleRight = UV.xy + float2(rightOffset, middleOffset) * OffsetMultiplier;

    UVBottomLeft = UV.xy + float2(leftOffset, bottomOffset) * OffsetMultiplier;
    UVBottomMiddle = UV.xy + float2(middleOffset, bottomOffset) * OffsetMultiplier;
    UVBottomRight = UV.xy + float2(rightOffset, bottomOffset) * OffsetMultiplier;
}

#endif // SURROUNDING_UV_INCLUDED