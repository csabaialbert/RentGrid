using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RentGrid.Api.Models;

namespace RentGrid.Api.Services
{
    public interface IAuthService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);
    Task<string> GenerateTokenAsync(User user); // Feltételezve egy User modellt
}
}