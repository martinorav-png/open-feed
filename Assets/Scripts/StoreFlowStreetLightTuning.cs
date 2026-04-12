using UnityEngine;

/// <summary>
/// Brightness for parking-lot / roadway pole lights (highway lamps, storefront row, flanking poles).
/// Editor generators and runtime <see cref="StoreInteriorLightMood"/> use the same values.
/// </summary>
public static class StoreFlowStreetLightTuning
{
    public const float HighwayPointIntensity = 2.65f;
    public const float HighwayPointRange = 36f;

    public const float StorefrontRowPointIntensity = 2.35f;
    public const float StorefrontRowPointRange = 15f;

    public const float FlankPolePointIntensity = 2.55f;
    public const float FlankPolePointRange = 15f;

    /// <summary>ParkingLot template: poles near the store (indices 0–1).</summary>
    public const float ParkingLotFrontPointIntensity = 2.85f;
    public const float ParkingLotRearPointIntensity = 1.65f;
    public const float ParkingLotPointRange = 16f;

    public const float ParkingLotFrontSpotIntensity = 4f;
    public const float ParkingLotRearSpotIntensity = 2.55f;
    public const float ParkingLotSpotRange = 14f;

    /// <summary>SixTwelve exterior block uses slightly stronger fill at the same pole height.</summary>
    public const float SixTwelveFlankPointIntensity = 3.6f;
    public const float SixTwelveFlankPointRange = 17f;
}
