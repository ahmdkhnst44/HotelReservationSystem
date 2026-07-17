using HotelReservationSystem.Models;

namespace HotelReservationSystem.ViewModels;

public class BookingListViewModel
{
    public IReadOnlyList<Booking> Bookings { get; init; } = Array.Empty<Booking>();
    public bool IsAdmin { get; init; }
    public string Status { get; init; } = "all";
    public string? Search { get; init; }
    public int UpcomingCount { get; init; }
    public int CurrentCount { get; init; }
    public int CompletedCount { get; init; }
    public decimal TotalRevenue { get; init; }
}
