namespace HotelReservationSystem.DTOs;

public record RoomDto(
    int Id,
    string RoomNumber,
    string Type,
    decimal PricePerNight,
    bool IsAvailable);
