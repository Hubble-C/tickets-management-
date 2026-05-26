using System.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using tickets_management.Data;
using tickets_management.Services;
using tickets_management.Services.Interfaces;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddControllersWithViews();


builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ITicketCodeGenerator, TicketCodeGenerator>();
builder.Services.AddSingleton<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ITicketService, TicketService>();

var DbSalesConnection = builder.Configuration["DB_SALES_CONNECTION"]
                        ?? throw new InvalidOperationException("Connection string 'DbSales' was not found.");
builder.Services.AddDbContext<MySqlDbContext>(options =>
    options.UseMySql(DbSalesConnection, new MySqlServerVersion(new Version(8, 0, 36))));

var DbCatalogConnection = builder.Configuration["DB_CATALOG_CONNECTION"]
                        ?? throw new InvalidOperationException("Connection string 'DbCatalogConnection' was not found.");
builder.Services.AddTransient<IDbConnection>(sp => new MySqlConnection(DbCatalogConnection) );

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseStaticFiles(); 

app.UseRouting();

app.UseAuthorization();



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();