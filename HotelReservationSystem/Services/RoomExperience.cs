using HotelReservationSystem.Models;

namespace HotelReservationSystem.Services;

public sealed record RoomProfile(
    string ImagePath,
    int Capacity,
    int Size,
    string BedKey,
    string DescriptionKey,
    decimal Rating,
    int ReviewCount,
    IReadOnlyList<string> AmenityKeys);

public static class RoomExperience
{
    public static RoomProfile For(Room room)
    {
        var baseProfile = room.Type.Trim().ToLowerInvariant() switch
        {
            "deluxe" => new RoomProfile(
                "/images/room-deluxe.jpg", 2, 32, "KingBed", "DeluxeDescription", 4.8m, 126,
                new[] { "FeatureWifi", "FeatureBreakfast", "FeatureCityView", "FeatureCoffee", "FeatureCleaning", "FeatureReception" }),
            "suite" => new RoomProfile(
                "/images/room-suite.jpg", 3, 48, "KingSofaBed", "SuiteDescription", 4.9m, 184,
                new[] { "FeatureWifi", "FeatureBreakfast", "FeatureLivingArea", "FeatureCityView", "FeatureCoffee", "FeatureReception" }),
            "family" => new RoomProfile(
                "/images/room-family.jpg", 5, 42, "FamilyBeds", "FamilyDescription", 4.7m, 98,
                new[] { "FeatureWifi", "FeatureBreakfast", "FeatureFamilySpace", "FeatureMiniFridge", "FeatureCleaning", "FeatureReception" }),
            _ => new RoomProfile(
                "/images/room-standard.jpg", 2, 24, "QueenBed", "StandardDescription", 4.6m, 82,
                new[] { "FeatureWifi", "FeatureCleaning", "FeatureCoffee", "FeatureReception" })
        };

        var ratingOffset = (room.Id % 3) * 0.03m;
        return baseProfile with
        {
            Rating = Math.Min(5m, baseProfile.Rating + ratingOffset),
            ReviewCount = baseProfile.ReviewCount + (room.Id * 7)
        };
    }

    public static int Capacity(string roomType) => roomType.Trim().ToLowerInvariant() switch
    {
        "suite" => 3,
        "family" => 5,
        _ => 2
    };

    public static string ImageFor(string roomType) => roomType.Trim().ToLowerInvariant() switch
    {
        "deluxe" => "/images/room-deluxe.jpg",
        "suite" => "/images/room-suite.jpg",
        "family" => "/images/room-family.jpg",
        _ => "/images/room-standard.jpg"
    };
}
