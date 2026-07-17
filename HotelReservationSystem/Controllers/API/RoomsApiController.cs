using HotelReservationSystem.DTOs;
using HotelReservationSystem.Models;
using HotelReservationSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelReservationSystem.Controllers.Api;

[ApiController]
[Route("api/rooms")]
public class RoomsApiController : ControllerBase
{
    private readonly RoomService _roomService;

    public RoomsApiController(RoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoomDto>>> GetRooms()
    {
        var rooms = await _roomService.GetAllAsync(includeDisabled: false);
        return Ok(rooms.Select(ToDto).ToList());
    }

    [HttpGet("available")]
    public async Task<ActionResult<RoomAvailabilityDto>> GetAvailable(
        [FromQuery] DateTime checkIn,
        [FromQuery] DateTime checkOut)
    {
        if (checkIn.Date < DateTime.Today || checkOut.Date <= checkIn.Date)
        {
            return BadRequest(new { message = "Invalid booking dates." });
        }

        var rooms = await _roomService.GetAvailableAsync(checkIn, checkOut);
        return Ok(new RoomAvailabilityDto(
            checkIn.Date,
            checkOut.Date,
            rooms.Select(ToDto).ToList()));
    }

    private static RoomDto ToDto(Room room) => new(
        room.Id,
        room.RoomNumber,
        room.Type,
        room.PricePerNight,
        room.IsAvailable);
}
