using HotelReservationSystem.Models;

namespace HotelReservationSystem.ViewModels;

public class RoomListViewModel
{
    public IReadOnlyList<Room> Rooms { get; init; } = Array.Empty<Room>();
    public DateTime? CheckInDate { get; init; }
    public DateTime? CheckOutDate { get; init; }
    public string? Type { get; init; }
    public int Guests { get; init; } = 1;
    public decimal? MaxPrice { get; init; }
    public string Sort { get; init; } = "recommended";
    public bool HasSearch => CheckInDate.HasValue && CheckOutDate.HasValue;
    public int ResultCount => Rooms.Count;
}
