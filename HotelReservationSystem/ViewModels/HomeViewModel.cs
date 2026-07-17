using HotelReservationSystem.Models;

namespace HotelReservationSystem.ViewModels;

public class HomeViewModel
{
    public IReadOnlyList<Room> FeaturedRooms { get; init; } = Array.Empty<Room>();
    public int RoomsCount { get; init; }
    public int BookingsCount { get; init; }
    public DateTime CheckInDate { get; init; } = DateTime.Today.AddDays(1);
    public DateTime CheckOutDate { get; init; } = DateTime.Today.AddDays(2);
}
