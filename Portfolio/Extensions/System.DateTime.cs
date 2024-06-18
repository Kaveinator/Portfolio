using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System {
    public static class DateTimeExt {
        public static string ToDurationString(this DateTime d1, DateTime? d2) {
            return !d2.HasValue ? $"{d1:MMM yyyy} - Current"
                : (d1.Year == d2.Value.Year && d1.Month == d2.Value.Month)
                 ? $"{d1:MMM yyyy}"
                : (d1.Year == d2.Value.Year)
                 ? $"{d1:MMM} - {d2.Value:MMM yyyy}"
                : (d1.Month == d2.Value.Month)
                 ? $"{d1:MMM yyyy} - {d2.Value:yyyy}"
                : $"{d1:MMM yyyy} - {d2.Value:MMM yyyy}";
        }
    }
}
