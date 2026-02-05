using MayNghien.Infrastructures.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Todo.DTOs.Authentication.Requests;
using Todo.DTOs.Authentication.Responses;

namespace Todo.Services.Interfaces
{
    public interface IAuthenticationService
    {
        Task<AppResponse<LoginResponse>> LoginAsync(LoginRequest request);
        Task<AppResponse<RegisterResponse>> RegisterAsync(RegisterRequest request);

        Task LogoutAsync();
    }
}
