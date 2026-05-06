using System;

namespace VenueBooking.Extensions
{
    public static class DateTimeExtensions
    {
        public static string ToRelativeTime(this DateTime dateTime)
        {
            var span = DateTime.Now - dateTime;
            if (span.TotalMinutes < 1)
                return "just now";
            if (span.TotalHours < 1)
                return $"{(int)span.TotalMinutes} minute{(span.TotalMinutes >= 2 ? "s" : "")} ago";
            if (span.TotalDays < 1)
                return $"{(int)span.TotalHours} hour{(span.TotalHours >= 2 ? "s" : "")} ago";
            if (span.TotalDays < 30)
                return $"{(int)span.TotalDays} day{(span.TotalDays >= 2 ? "s" : "")} ago";
            if (span.TotalDays < 365)
                return $"{(int)(span.TotalDays / 30)} month{(span.TotalDays / 30 >= 2 ? "s" : "")} ago";
            return $"{(int)(span.TotalDays / 365)} year{(span.TotalDays / 365 >= 2 ? "s" : "")} ago";
        }
    }
}