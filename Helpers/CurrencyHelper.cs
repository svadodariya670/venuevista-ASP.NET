using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VenueBooking.Helpers
{
    public static class CurrencyHelper
    {
        // Use in Razor as: @Html.Rupee() or @Html.RupeeString(model.Amount)
        public static IHtmlContent Rupee(this IHtmlHelper html) =>
            new HtmlString("&#8377;");

        public static IHtmlContent RupeeString(this IHtmlHelper html, decimal amount) =>
            new HtmlString($"&#8377;{amount:N0}");
    }
}