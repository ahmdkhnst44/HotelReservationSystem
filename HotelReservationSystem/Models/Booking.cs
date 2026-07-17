using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelReservationSystem.Models;

public class Booking
{
    public int Id { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime CheckInDate { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime CheckOutDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; }

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

    [StringLength(12)]
    public string ConfirmationCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
}
