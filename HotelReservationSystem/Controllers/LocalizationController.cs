using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace HotelReservationSystem.Controllers;

public class LocalizationController : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ChangeCulture(string culture, string? returnUrl)
    {
        culture = culture is "en" or "ar" ? culture : "ar";

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

        var safeReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : "/";

        return LocalRedirect(safeReturnUrl);
    }
}
