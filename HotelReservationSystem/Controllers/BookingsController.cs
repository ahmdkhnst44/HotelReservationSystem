using HotelReservationSystem.Models;
using HotelReservationSystem.Services;
using HotelReservationSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HotelReservationSystem.Controllers;

[Authorize]
public class BookingsController : Controller
{
    private readonly BookingService _bookingService;
    private readonly RoomService _roomService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UiLocalizer _localizer;

    public BookingsController(
        BookingService bookingService,
        RoomService roomService,
        UserManager<ApplicationUser> userManager,
        UiLocalizer localizer)
    {
        _bookingService = bookingService;
        _roomService = roomService;
        _userManager = userManager;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string status = "all", string? search = null)
    {
        var userId = _userManager.GetUserId(User)!;
        var isAdmin = User.IsInRole(AppRoles.Admin);
        var summary = await _bookingService.GetSummaryAsync(userId, isAdmin);

        return View(new BookingListViewModel
        {
            Bookings = await _bookingService.GetForUserAsync(userId, isAdmin, status, search),
            IsAdmin = isAdmin,
            Status = status,
            Search = search,
            UpcomingCount = summary.Upcoming,
            CurrentCount = summary.Current,
            CompletedCount = summary.Completed,
            TotalRevenue = summary.Revenue
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(int roomId, DateTime? checkIn, DateTime? checkOut, int guests = 1)
    {
        var room = await _roomService.GetByIdAsync(roomId);
        if (room is null || !room.IsAvailable)
        {
            return NotFound();
        }

        return View(new BookingCreateViewModel
        {
            RoomId = room.Id,
            Room = room,
            Guests = Math.Clamp(guests, 1, RoomExperience.Capacity(room.Type)),
            CheckInDate = checkIn.HasValue && checkIn.Value.Date >= DateTime.Today
                ? checkIn.Value.Date
                : DateTime.Today.AddDays(1),
            CheckOutDate = checkOut.HasValue && checkOut.Value.Date > (checkIn?.Date ?? DateTime.Today)
                ? checkOut.Value.Date
                : DateTime.Today.AddDays(2)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingCreateViewModel model)
    {
        model.Room = await _roomService.GetByIdAsync(model.RoomId);
        if (model.Room is null)
        {
            return NotFound();
        }

        if (model.Guests > RoomExperience.Capacity(model.Room.Type))
        {
            ModelState.AddModelError(nameof(model.Guests), _localizer["TooManyGuests"]);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = _userManager.GetUserId(User)!;
        var result = await _bookingService.CreateAsync(model, userId);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, _localizer[result.ErrorKey]);
            return View(model);
        }

        TempData["Success"] = _localizer["BookingCreated"];
        return RedirectToAction(nameof(Details), new { id = result.Booking!.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var booking = await GetAuthorizedBookingAsync(id);
        if (booking is null)
        {
            return NotFound();
        }

        if (!User.IsInRole(AppRoles.Admin) && booking.CheckInDate.Date <= DateTime.Today)
        {
            TempData["Error"] = _localizer["BookingCannotEdit"];
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(new BookingCreateViewModel
        {
            BookingId = booking.Id,
            RoomId = booking.RoomId,
            Room = booking.Room,
            CheckInDate = booking.CheckInDate,
            CheckOutDate = booking.CheckOutDate,
            Guests = booking.Guests,
            BreakfastIncluded = booking.BreakfastIncluded,
            AirportPickup = booking.AirportPickup,
            SpecialRequests = booking.SpecialRequests,
            PaymentMethod = booking.PaymentMethod,
            PromoCode = booking.PromoCode
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BookingCreateViewModel model)
    {
        var booking = await GetAuthorizedBookingAsync(id);
        if (booking is null)
        {
            return NotFound();
        }

        model.BookingId = id;
        model.RoomId = booking.RoomId;
        model.Room = booking.Room;

        if (model.Guests > RoomExperience.Capacity(booking.Room.Type))
        {
            ModelState.AddModelError(nameof(model.Guests), _localizer["TooManyGuests"]);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _bookingService.UpdateAsync(
            id,
            model,
            _userManager.GetUserId(User)!,
            User.IsInRole(AppRoles.Admin));

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, _localizer[result.ErrorKey]);
            return View(model);
        }

        TempData["Success"] = _localizer["BookingUpdated"];
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var booking = await GetAuthorizedBookingAsync(id);
        return booking is null ? NotFound() : View(booking);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var booking = await GetAuthorizedBookingAsync(id);
        return booking is null ? NotFound() : View(booking);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var deleted = await _bookingService.DeleteAsync(id, userId, User.IsInRole(AppRoles.Admin));
        if (!deleted)
        {
            return NotFound();
        }

        TempData["Success"] = _localizer["BookingCancelled"];
        return RedirectToAction(nameof(Index));
    }

    private async Task<Booking?> GetAuthorizedBookingAsync(int id)
    {
        var booking = await _bookingService.GetByIdAsync(id);
        if (booking is null)
        {
            return null;
        }

        var userId = _userManager.GetUserId(User)!;
        return User.IsInRole(AppRoles.Admin) || booking.UserId == userId ? booking : null;
    }
}
