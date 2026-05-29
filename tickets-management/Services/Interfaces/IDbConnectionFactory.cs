using System.Data;

namespace tickets_management.Services.Interfaces;

public interface IDbConnectionFactory
{
    IDbConnection GetAuthConnection();
    IDbConnection GetCatalogConnection();
}