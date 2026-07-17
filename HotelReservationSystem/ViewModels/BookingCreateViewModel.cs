using System.ComponentModel.DataAnnotations;
using HotelReservationSystem.Models;

namespace HotelReservationSystem.ViewModels;

public class BookingCreateViewModel
{
    public int? BookingId { get; set; }

    [Required]
    public int RoomId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime CheckInDate { get; set; } = DateTime.Today.AddDays(1);

    [Required]
    [DataType(DataType.Date)]
    public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(2);

    [Range(1, 6)]
    public int Guests { get; set; } = 1;

    public bool BreakfastIncluded { get; set; }

    public bool AirportPickup { get; set; }

    [StringLength(500)]
    public string? SpecialRequests { get; set; }

    [StringLength(30)]
    public string PaymentMethod { get; set; } = "PayAtHotel";

    [StringLength(30)]
    public string? PromoCode { get; set; }

    public Room? Room { get; set; }
}
