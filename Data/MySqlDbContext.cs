using Microsoft.EntityFrameworkCore;
using tickets_management.Models;

namespace tickets_management.Data;

public class MySqlDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
       modelBuilder.Entity<OrderItem>().HasOne(o => o.Order).WithMany(i => i.Items).HasForeignKey(o => o.OrderId);
       modelBuilder.Entity<Ticket>().HasOne(t => t.OrderItem).WithMany(oi => oi.Tickets).HasForeignKey(oi => oi.OrderItemId);
       modelBuilder.Entity<OrderItem>().Property(oi => oi.PriceTicket).HasPrecision(18, 2);
    }

    public MySqlDbContext(DbContextOptions<MySqlDbContext> options) : base(options)
    {
        
    }
    
    public DbSet<Order> Orders { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
}