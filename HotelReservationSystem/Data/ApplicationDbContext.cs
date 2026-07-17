using HotelReservationSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
 


namespace HotelReservationSystem.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {


        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }


        public DbSet<Room> Rooms { get; set; }


        public DbSet<Booking> Bookings { get; set; }


    }
}