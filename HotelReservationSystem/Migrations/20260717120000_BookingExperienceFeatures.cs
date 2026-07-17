using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelReservationSystem.Migrations
{
    public partial class BookingExperienceFeatures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(name: "AirportPickup", table: "Bookings", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<bool>(name: "BreakfastIncluded", table: "Bookings", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>(name: "ConfirmationCode", table: "Bookings", type: "nvarchar(12)", maxLength: 12, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<DateTime>(name: "CreatedAt", table: "Bookings", type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()");
            migrationBuilder.AddColumn<decimal>(name: "DiscountAmount", table: "Bookings", type: "decimal(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<int>(name: "Guests", table: "Bookings", type: "int", nullable: false, defaultValue: 1);
            migrationBuilder.AddColumn<string>(name: "PaymentMethod", table: "Bookings", type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "PayAtHotel");
            migrationBuilder.AddColumn<string>(name: "PromoCode", table: "Bookings", type: "nvarchar(30)", maxLength: 30, nullable: true);
            migrationBuilder.AddColumn<string>(name: "SpecialRequests", table: "Bookings", type: "nvarchar(500)", maxLength: 500, nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AirportPickup", table: "Bookings");
            migrationBuilder.DropColumn(name: "BreakfastIncluded", table: "Bookings");
            migrationBuilder.DropColumn(name: "ConfirmationCode", table: "Bookings");
            migrationBuilder.DropColumn(name: "CreatedAt", table: "Bookings");
            migrationBuilder.DropColumn(name: "DiscountAmount", table: "Bookings");
            migrationBuilder.DropColumn(name: "Guests", table: "Bookings");
            migrationBuilder.DropColumn(name: "PaymentMethod", table: "Bookings");
            migrationBuilder.DropColumn(name: "PromoCode", table: "Bookings");
            migrationBuilder.DropColumn(name: "SpecialRequests", table: "Bookings");
        }
    }
}
