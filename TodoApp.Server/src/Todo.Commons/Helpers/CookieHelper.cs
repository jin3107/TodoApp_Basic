using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Todo.Commons.Helpers
{
    public static class CookieHelper
    {
        public static CookieOptions GetSecureCookieOptions(IHostEnvironment environment)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = !environment.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(24),
                Path = "/",
                IsEssential = true
            };
        }

        public static CookieOptions GetDeleteCookieOptions(IHostEnvironment environment)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = !environment.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(-1)
            };
        }
    }
}
