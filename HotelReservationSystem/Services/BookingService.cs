using HotelReservationSystem.Data;
using HotelReservationSystem.Models;
using HotelReservationSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Services;

public class BookingService
{
    public const decimal BreakfastPerGuestPerNight = 12m;
    public const decimal AirportPickupPrice = 35m;

    private readonly ApplicationDbContext _db;
    private readonly RoomService _roomService;

    public BookingService(ApplicationDbContext db, RoomService roomService)
    {
        _db = db;
        _roomService = roomService;
    }

    public async Task<List<Booking>> GetForUserAsync(string userId, bool isAdmin, string status = "all", string? search = null)
    {
        var today = DateTime.Today;
        var query = _db.Bookings
            .AsNoTracking()
            .Include(booking => booking.Room)
            .Include(booking => booking.User)
            .AsQueryable();

        if (!isAdmin)
        {
            query = query.Where(booking => booking.UserId == userId);
        }

        query = status.ToLowerInvariant() switch
        {
            "upcoming" => query.Where(booking => booking.CheckInDate > today),
            "current" => query.Where(booking => booking.CheckInDate <= today && booking.CheckOutDate > today),
            "completed" => query.Where(booking => booking.CheckOutDate <= today),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(booking =>
                booking.Room.RoomNumber.Contains(term) ||
                booking.ConfirmationCode.Contains(term) ||
                booking.User.FullName.Contains(term) ||
                (booking.User.Email != null && booking.User.Email.Contains(term)));
        }

        return await query
            .OrderByDescending(booking => booking.CheckInDate)
            .ThenByDescending(booking => booking.Id)
            .ToListAsync();
    }

    public async Task<(int Upcoming, int Current, int Completed, decimal Revenue)> GetSummaryAsync(string userId, bool isAdmin)
    {
        var today = DateTime.Today;
        var query = _db.Bookings.AsNoTracking().AsQueryable();
        if (!isAdmin)
        {
            query = query.Where(booking => booking.UserId == userId);
        }

        return (
            await query.CountAsync(booking => booking.CheckInDate > today),
            await query.CountAsync(booking => booking.CheckInDate <= today && booking.CheckOutDate > today),
            await query.CountAsync(booking => booking.CheckOutDate <= today),
            await query.SumAsync(booking => (decimal?)booking.TotalPrice) ?? 0m
        );
    }

    public Task<Booking?> GetByIdAsync(int id)
    {
        return _db.Bookings
            .AsNoTracking()
            .Include(booking => booking.Room)
            .Include(booking => booking.User)
            .FirstOrDefaultAsync(booking => booking.Id == id);
    }

    public async Task<(bool Success, string ErrorKey, Booking? Booking)> CreateAsync(
        BookingCreateViewModel model,
        string userId)
    {
        var validation = await ValidateAsync(model, null);
        if (!validation.Success || validation.Room is null)
        {
            return (false, validation.ErrorKey, null);
        }

        var totals = CalculateTotal(validation.Room.PricePerNight, model);
        var booking = new Booking
        {
            RoomId = model.RoomId,
            UserId = userId,
            CheckInDate = model.CheckInDate.Date,
            CheckOutDate = model.CheckOutDate.Date,
            Guests = model.Guests,
            BreakfastIncluded = model.BreakfastIncluded,
            AirportPickup = model.AirportPickup,
            SpecialRequests = NormalizeOptional(model.SpecialRequests),
            PaymentMethod = NormalizePaymentMethod(model.PaymentMethod),
            PromoCode = NormalizeOptional(model.PromoCode)?.ToUpperInvariant(),
            DiscountAmount = totals.Discount,
            TotalPrice = totals.Total,
            ConfirmationCode = CreateConfirmationCode(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        return (true, string.Empty, booking);
    }

    public async Task<(bool Success, string ErrorKey)> UpdateAsync(
        int bookingId,
        BookingCreateViewModel model,
        string userId,
        bool isAdmin)
    {
        var booking = await _db.Bookings.Include(item => item.Room).FirstOrDefaultAsync(item => item.Id == bookingId);
        if (booking is null || (!isAdmin && booking.UserId != userId))
        {
            return (false, "NotFound");
        }

        if (!isAdmin && booking.CheckInDate.Date <= DateTime.Today)
        {
            return (false, "BookingCannotEdit");
        }

        model.RoomId = booking.RoomId;
        var validation = await ValidateAsync(model, bookingId);
        if (!validation.Success || validation.Room is null)
        {
            return (false, validation.ErrorKey);
        }

        var totals = CalculateTotal(validation.Room.PricePerNight, model);
        booking.CheckInDate = model.CheckInDate.Date;
        booking.CheckOutDate = model.CheckOutDate.Date;
        booking.Guests = model.Guests;
        booking.BreakfastIncluded = model.BreakfastIncluded;
        booking.AirportPickup = model.AirportPickup;
        booking.SpecialRequests = NormalizeOptional(model.SpecialRequests);
        booking.PaymentMethod = NormalizePaymentMethod(model.PaymentMethod);
        booking.PromoCode = NormalizeOptional(model.PromoCode)?.ToUpperInvariant();
        booking.DiscountAmount = totals.Discount;
        booking.TotalPrice = totals.Total;

        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<bool> DeleteAsync(int id, string userId, bool isAdmin)
    {
        var booking = await _db.Bookings.FindAsync(id);
        if (booking is null || (!isAdmin && booking.UserId != userId))
        {
            return false;
        }

        _db.Bookings.Remove(booking);
        await _db.SaveChangesAsync();
        return true;
    }

    public static (decimal Subtotal, decimal Extras, decimal Discount, decimal Total) CalculateTotal(
        decimal pricePerNight,
        BookingCreateViewModel model)
    {
        var nights = Math.Max(0, (model.CheckOutDate.Date - model.CheckInDate.Date).Days);
        var subtotal = pricePerNight * nights;
        var extras = (model.BreakfastIncluded ? BreakfastPerGuestPerNight * Math.Max(1, model.Guests) * nights : 0m)
                     + (model.AirportPickup ? AirportPickupPrice : 0m);
        var discountRate = GetDiscountRate(model.PromoCode, nights);
        var discount = Math.Round(subtotal * discountRate, 2);
        return (subtotal, extras, discount, Math.Max(0m, subtotal + extras - discount));
    }

    public static decimal GetDiscountRate(string? promoCode, int nights)
    {
        var code = promoCode?.Trim().ToUpperInvariant();
        return code switch
        {
            "AURORA10" => 0.10m,
            "STAY3" when nights >= 3 => 0.15m,
            _ => 0m
        };
    }

    private async Task<(bool Success, string ErrorKey, Room? Room)> ValidateAsync(
        BookingCreateViewModel model,
        int? excludeBookingId)
    {
        var checkIn = model.CheckInDate.Date;
        var checkOut = model.CheckOutDate.Date;

        if (checkIn < DateTime.Today)
        {
            return (false, "PastDate", null);
        }

        if (checkOut <= checkIn)
        {
            return (false, "InvalidDates", null);
        }

        var room = await _db.Rooms.AsNoTracking().FirstOrDefaultAsync(item => item.Id == model.RoomId);
        if (room is null)
        {
            return (false, "NotFound", null);
        }

        if (model.Guests > RoomExperience.Capacity(room.Type))
        {
            return (false, "TooManyGuests", room);
        }

        if (!await _roomService.IsAvailableAsync(model.RoomId, checkIn, checkOut, excludeBookingId))
        {
            return (false, "RoomNotAvailable", room);
        }

        return (true, string.Empty, room);
    }

    private static string NormalizePaymentMethod(string? paymentMethod) =>
        paymentMethod is "CardAtHotel" ? "CardAtHotel" : "PayAtHotel";

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string CreateConfirmationCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var code = new char[7];
        for (var i = 0; i < code.Length; i++)
        {
            code[i] = chars[Random.Shared.Next(chars.Length)];
        }

        return $"AU-{new string(code)}";
    }
}
