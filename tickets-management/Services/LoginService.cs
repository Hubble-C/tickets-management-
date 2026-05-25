using System.Data;
using Dapper;
using tickets_management.Data;
using tickets_management.Models;
using tickets_management.Response;
using BCrypt.Net;
using tickets_management.Services.Interfaces;

namespace tickets_management.Services;

public class LoginService : ILogin
{
    private readonly IDbConnection _dbAuthConnection;
    
    public LoginService(IDbConnection dbAuthConnection)
    {
        _dbAuthConnection = dbAuthConnection;
    }

    public async Task<ServiceResponse<AspNetUsers>> Login(string username, string password)
    {
        var response = new ServiceResponse<AspNetUsers>();
        var userExist =
            await _dbAuthConnection.QueryFirstOrDefaultAsync<AspNetUsers>(
                "SELECT UserName, PasswordHash AS password FROM AspNetUsers WHERE UserName = @username ", new { username = username} );
        
        // El BCrypt no usa Base64 entonces en un futuro puede presentar errores por la conveccion de
        // microsof de Identiry probar mañana si no funciona cambiar por el estandar de microsoft
        if (userExist != null && BCrypt.Net.BCrypt.Verify(password, userExist.Password))
        {
            response.Data = userExist;
            response.Success = true;
            response.Message = "Success entering to the system...";
            return response;
        }
        else
        {
            response.Success = false;
            response.Message = "Credentials incorrect or invalid";
            return response;
        }
    } 

}