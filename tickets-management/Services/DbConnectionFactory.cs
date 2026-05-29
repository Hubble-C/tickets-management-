using System.Data;
using MySqlConnector;
using tickets_management.Services.Interfaces;

namespace tickets_management.Services;

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration _configuration;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IDbConnection GetAuthConnection() => 
        new MySqlConnection(_configuration.GetConnectionString("DbAuthConnection"));

    public IDbConnection GetCatalogConnection() => 
        new MySqlConnection(_configuration.GetConnectionString("DbCatalogConnection"));
    
    public IDbConnection GetPublicConnection() =>
        new MySqlConnection(_configuration.GetConnectionString("DbPublicConnection"));
}