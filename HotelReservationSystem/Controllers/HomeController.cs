using System.Diagnostics;
using HotelReservationSystem.Data;
using HotelReservationSystem.Models;
using HotelReservationSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var model = new HomeViewModel
        {
            FeaturedRooms = await _db.Rooms
                .AsNoTracking()
                .Where(room => room.IsAvailable)
                .OrderBy(room => room.PricePerNight)
                .Take(3)
                .ToListAsync(),
            RoomsCount = await _db.Rooms.CountAsync(room => room.IsAvailable),
            BookingsCount = await _db.Bookings.CountAsync(),
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(2)
        };

        return View(model);
    }

    public IActionResult Privacy() => View();

    public IActionResult AccessDenied() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
