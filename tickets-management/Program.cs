using System.Data;
using MySqlConnector;
using tickets_management.Services;
using tickets_management.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using tickets_management.Data;
using tickets_management.Models;
using tickets_management.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddTransient<ILogin, LoginService>();
builder.Services.AddTransient<IPasswordHasher<AspNetUsers>, PasswordHasher<AspNetUsers>>();
builder.Services.AddSingleton<IOrderService, OrderService>();
builder.Services.AddTransient<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddTransient<LoginValidator>();
builder.Services.AddTransient<IEventService, EventService>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var SalesConnectionString = builder.Configuration.GetConnectionString("DbSalesConnection");
builder.Services.AddDbContext<MySqlDbContext>(options =>
    options.UseMySql(SalesConnectionString, ServerVersion.AutoDetect(SalesConnectionString)));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{   
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ticket-system-super-secret-key-2026!")),
        ValidateIssuer = true,
        ValidIssuer = "ticket-admin",
        ValidateAudience = true,
        ValidAudience = "ticket-internal",
        
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        
        RoleClaimType = "role"
    };

    options.Events = new JwtBearerEvents
    {
        
        OnChallenge = context =>
        {
            
            context.HandleResponse();
            context.Response.Redirect($"/BoxOffice/Login");
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            var token = context.HttpContext.Session.GetString("JWToken");
            context.Token = token;
            return Task.CompletedTask;
        }
    };
    

});
    
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthentication();




app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=BoxOffice}/{action=Login}/{id?}");

app.Run();