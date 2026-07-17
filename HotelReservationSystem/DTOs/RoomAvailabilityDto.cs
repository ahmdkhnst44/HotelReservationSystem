namespace HotelReservationSystem.DTOs;

public record RoomAvailabilityDto(
    DateTime CheckInDate,
    DateTime CheckOutDate,
    IReadOnlyList<RoomDto> Rooms);
