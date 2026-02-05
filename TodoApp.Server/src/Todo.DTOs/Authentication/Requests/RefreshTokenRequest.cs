using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Todo.DTOs.Authentication.Requests
{
    public class RefreshTokenRequest
    {
        public string? AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
