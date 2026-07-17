using HotelReservationSystem.Data;
using HotelReservationSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Services;

public class RoomService
{
    private readonly ApplicationDbContext _db;

    public RoomService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Room>> GetAllAsync(bool includeDisabled = true)
    {
        var query = _db.Rooms.AsNoTracking().AsQueryable();
        if (!includeDisabled)
        {
            query = query.Where(room => room.IsAvailable);
        }

        return await query.OrderBy(room => room.RoomNumber).ToListAsync();
    }

    public async Task<List<Room>> GetAvailableAsync(DateTime checkIn, DateTime checkOut, int? excludeBookingId = null)
    {
        checkIn = checkIn.Date;
        checkOut = checkOut.Date;

        return await _db.Rooms
            .AsNoTracking()
            .Where(room => room.IsAvailable)
            .Where(room => !_db.Bookings.Any(booking =>
                booking.RoomId == room.Id &&
                (!excludeBookingId.HasValue || booking.Id != excludeBookingId.Value) &&
                booking.CheckInDate < checkOut &&
                booking.CheckOutDate > checkIn))
            .OrderBy(room => room.PricePerNight)
            .ThenBy(room => room.RoomNumber)
            .ToListAsync();
    }

    public Task<Room?> GetByIdAsync(int id, bool includeBookings = false)
    {
        IQueryable<Room> query = _db.Rooms;
        if (includeBookings)
        {
            query = query.Include(room => room.Bookings);
        }

        return query.AsNoTracking().FirstOrDefaultAsync(room => room.Id == id);
    }

    public Task<bool> RoomNumberExistsAsync(string roomNumber, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(roomNumber))
        {
            return Task.FromResult(false);
        }

        var normalized = roomNumber.Trim().ToUpper();
        return _db.Rooms.AnyAsync(room =>
            room.RoomNumber.ToUpper() == normalized &&
            (!excludeId.HasValue || room.Id != excludeId.Value));
    }

    public async Task AddAsync(Room room)
    {
        room.RoomNumber = room.RoomNumber.Trim();
        room.Type = room.Type.Trim();
        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(Room room)
    {
        var current = await _db.Rooms.FindAsync(room.Id);
        if (current is null)
        {
            return false;
        }

        current.RoomNumber = room.RoomNumber.Trim();
        current.Type = room.Type.Trim();
        current.PricePerNight = room.PricePerNight;
        current.IsAvailable = room.IsAvailable;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var room = await _db.Rooms.Include(item => item.Bookings).FirstOrDefaultAsync(item => item.Id == id);
        if (room is null || room.Bookings.Count > 0)
        {
            return false;
        }

        _db.Rooms.Remove(room);
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<bool> IsAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeBookingId = null)
    {
        checkIn = checkIn.Date;
        checkOut = checkOut.Date;

        return _db.Rooms.AnyAsync(room =>
            room.Id == roomId &&
            room.IsAvailable &&
            !_db.Bookings.Any(booking =>
                booking.RoomId == roomId &&
                (!excludeBookingId.HasValue || booking.Id != excludeBookingId.Value) &&
                booking.CheckInDate < checkOut &&
                booking.CheckOutDate > checkIn));
    }
}
