using Microsoft.AspNetCore.Identity.Data;
using tickets_management.Models;
using tickets_management.Response;

namespace tickets_management.Services.Interfaces;

public interface ILogin
{
    public Task<ServiceResponse<AspNetUsers>> Login(string username, string password);
}