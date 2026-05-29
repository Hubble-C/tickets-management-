using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using Dapper;
using tickets_management.Data;
using tickets_management.Models;
using tickets_management.Response;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using tickets_management.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using tickets_management.Validators;

namespace tickets_management.Services;

public class LoginService : ILogin
{
    private readonly IDbConnection _dbAuthConnection;
    //llama las propiedades secretas de nuestro appsettings.json para usarlas
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher<AspNetUsers> _passwordHasher;
    private readonly LoginValidator _loginValidator;
    
    public LoginService(IDbConnection dbAuthConnection,  IConfiguration configuration, IPasswordHasher<AspNetUsers> passwordHasher,  LoginValidator loginValidator)
    {
        _configuration = configuration;
        _dbAuthConnection = dbAuthConnection;
        _passwordHasher = passwordHasher;
        _loginValidator = loginValidator;
    }

    public async Task<ServiceResponse<AspNetUsers>> Login(string username, string password)
    {
        var response = new ServiceResponse<AspNetUsers>();
        var userExist =
            await _dbAuthConnection.QueryFirstOrDefaultAsync<AspNetUsers>(
                "SELECT UserName, PasswordHash AS Password FROM AspNetUsers WHERE UserName = @userName ", new { userName = username} );
        
        // conveccion de microsoft para usar hash de microsoft tipo 64 con BCrypt no funciona ya que no tiene los mismos
        // datos de hash
        var verificationResult = _passwordHasher.VerifyHashedPassword(userExist, userExist.Password, password);
        var userValidation = _loginValidator.Validate(userExist);

        if (userExist == null)
        {
           response.Success = false;
           response.Message = "Ingrese sus credenciales nuevamente";
           return response;
        }
        
        if (!userValidation.IsValid)
        {
            response.Success = false;
            response.Message = "Revisa tus credenciales nuevamente";
            return response;
        }
        
        if (userExist != null && verificationResult == PasswordVerificationResult.Success)
        {
            var TokenHandler = new JwtSecurityTokenHandler();
            
            var Key = System.Text.Encoding.UTF8.GetBytes("ticket-system-super-secret-key-2026!");
            var credentials = new SigningCredentials(new SymmetricSecurityKey(Key), SecurityAlgorithms.HmacSha256Signature);

            var Data = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, userExist.Username),
                    new Claim("role", "Seller"),
                }),
                IssuedAt = DateTime.UtcNow,         
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddMinutes(30),
                SigningCredentials = credentials
            };
            
            
            // lee el descriptor del token que trae todos los parametros claims, credenciales y expiracion y lo suelta
            var Object = TokenHandler.CreateToken(Data);
            // lo pasa a url web JWT
            
            var tokenString = TokenHandler.WriteToken(Object);
            userExist.Token = tokenString;
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