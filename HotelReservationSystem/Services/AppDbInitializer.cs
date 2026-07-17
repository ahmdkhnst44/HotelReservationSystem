using HotelReservationSystem.Data;
using HotelReservationSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Services;

public static class AppDbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var roleName in new[] { AppRoles.Admin, AppRoles.Customer })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        await EnsureUserAsync(userManager, "admin@hotel.local", "Hotel Administrator", "Admin123", AppRoles.Admin);
        var customer = await EnsureUserAsync(userManager, "guest@hotel.local", "Demo Guest", "Guest123", AppRoles.Customer);

        if (!await db.Rooms.AnyAsync())
        {
            db.Rooms.AddRange(
                new Room { RoomNumber = "101", Type = "Standard", PricePerNight = 65, IsAvailable = true },
                new Room { RoomNumber = "102", Type = "Standard", PricePerNight = 70, IsAvailable = true },
                new Room { RoomNumber = "201", Type = "Deluxe", PricePerNight = 105, IsAvailable = true },
                new Room { RoomNumber = "202", Type = "Deluxe", PricePerNight = 115, IsAvailable = true },
                new Room { RoomNumber = "301", Type = "Suite", PricePerNight = 180, IsAvailable = true },
                new Room { RoomNumber = "302", Type = "Family", PricePerNight = 145, IsAvailable = true });
            await db.SaveChangesAsync();
        }

        if (!await db.Bookings.AnyAsync() && customer is not null)
        {
            var sampleRoom = await db.Rooms.OrderBy(room => room.Id).FirstAsync();
            db.Bookings.Add(new Booking
            {
                RoomId = sampleRoom.Id,
                UserId = customer.Id,
                CheckInDate = DateTime.Today.AddDays(7),
                CheckOutDate = DateTime.Today.AddDays(9),
                Guests = 2,
                BreakfastIncluded = true,
                PaymentMethod = "PayAtHotel",
                ConfirmationCode = "AU-DEMO01",
                CreatedAt = DateTime.UtcNow,
                TotalPrice = (sampleRoom.PricePerNight + (BookingService.BreakfastPerGuestPerNight * 2)) * 2
            });
            await db.SaveChangesAsync();
        }

        var missingCodes = await db.Bookings.Where(booking => booking.ConfirmationCode == "").ToListAsync();
        foreach (var booking in missingCodes)
        {
            var generatedCode = $"AU-{Guid.NewGuid():N}".ToUpperInvariant();
            booking.ConfirmationCode = generatedCode[..10];
            if (booking.CreatedAt == default)
            {
                booking.CreatedAt = DateTime.UtcNow;
            }
        }

        if (missingCodes.Count > 0)
        {
            await db.SaveChangesAsync();
        }
    }

    private static async Task<ApplicationUser?> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string fullName,
        string password,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                return null;
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }
}
