using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelReservationSystem.Models;

public class Room
{
    public int Id { get; set; }

    [Required]
    public string RoomNumber { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = "Standard";

    [Range(1, 100000)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerNight { get; set; }

    public bool IsAvailable { get; set; } = true;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
