using HotelReservationSystem.Models;
using HotelReservationSystem.Services;
using HotelReservationSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelReservationSystem.Controllers;

public class RoomsController : Controller
{
    private readonly RoomService _roomService;
    private readonly UiLocalizer _localizer;

    public RoomsController(RoomService roomService, UiLocalizer localizer)
    {
        _roomService = roomService;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        DateTime? checkIn,
        DateTime? checkOut,
        string? type,
        int guests = 1,
        decimal? maxPrice = null,
        string sort = "recommended")
    {
        IReadOnlyList<Room> rooms;
        guests = Math.Clamp(guests, 1, 6);

        if (checkIn.HasValue || checkOut.HasValue)
        {
            if (!checkIn.HasValue || !checkOut.HasValue ||
                checkIn.Value.Date < DateTime.Today ||
                checkOut.Value.Date <= checkIn.Value.Date)
            {
                ViewData["SearchError"] = _localizer["SearchError"];
                rooms = await _roomService.GetAllAsync(includeDisabled: User.IsInRole(AppRoles.Admin));
            }
            else
            {
                rooms = await _roomService.GetAvailableAsync(checkIn.Value, checkOut.Value);
            }
        }
        else
        {
            rooms = await _roomService.GetAllAsync(includeDisabled: User.IsInRole(AppRoles.Admin));
        }

        var filtered = rooms.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(type) && !type.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(room => room.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
        }

        filtered = filtered.Where(room => RoomExperience.Capacity(room.Type) >= guests);

        if (maxPrice.HasValue && maxPrice.Value > 0)
        {
            filtered = filtered.Where(room => room.PricePerNight <= maxPrice.Value);
        }

        filtered = sort.ToLowerInvariant() switch
        {
            "price-low" => filtered.OrderBy(room => room.PricePerNight),
            "price-high" => filtered.OrderByDescending(room => room.PricePerNight),
            "rating" => filtered.OrderByDescending(room => RoomExperience.For(room).Rating),
            _ => filtered.OrderByDescending(room => RoomExperience.For(room).Rating)
                         .ThenBy(room => room.PricePerNight)
        };

        return View(new RoomListViewModel
        {
            Rooms = filtered.ToList(),
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            Type = type,
            Guests = guests,
            MaxPrice = maxPrice,
            Sort = sort
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, DateTime? checkIn, DateTime? checkOut, int guests = 1)
    {
        var room = await _roomService.GetByIdAsync(id);
        if (room is null)
        {
            return NotFound();
        }

        ViewData["CheckIn"] = checkIn?.ToString("yyyy-MM-dd");
        ViewData["CheckOut"] = checkOut?.ToString("yyyy-MM-dd");
        ViewData["Guests"] = Math.Clamp(guests, 1, RoomExperience.Capacity(room.Type));
        return View(room);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet]
    public IActionResult Create() => View(new Room { IsAvailable = true, Type = "Standard" });

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Room room)
    {
        if (!string.IsNullOrWhiteSpace(room.RoomNumber) &&
            await _roomService.RoomNumberExistsAsync(room.RoomNumber))
        {
            ModelState.AddModelError(nameof(Room.RoomNumber), _localizer["RoomNumberExists"]);
        }

        if (!ModelState.IsValid)
        {
            return View(room);
        }

        await _roomService.AddAsync(room);
        TempData["Success"] = _localizer["RoomSaved"];
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var room = await _roomService.GetByIdAsync(id);
        return room is null ? NotFound() : View(room);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Room room)
    {
        if (id != room.Id)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(room.RoomNumber) &&
            await _roomService.RoomNumberExistsAsync(room.RoomNumber, room.Id))
        {
            ModelState.AddModelError(nameof(Room.RoomNumber), _localizer["RoomNumberExists"]);
        }

        if (!ModelState.IsValid)
        {
            return View(room);
        }

        if (!await _roomService.UpdateAsync(room))
        {
            return NotFound();
        }

        TempData["Success"] = _localizer["RoomSaved"];
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var room = await _roomService.GetByIdAsync(id, includeBookings: true);
        return room is null ? NotFound() : View(room);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!await _roomService.DeleteAsync(id))
        {
            TempData["Error"] = _localizer["RoomHasBookings"];
            return RedirectToAction(nameof(Delete), new { id });
        }

        TempData["Success"] = _localizer["RoomDeleted"];
        return RedirectToAction(nameof(Index));
    }
}
