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
       modelBuilder.Entity<OrderItem>().Property(oi => oi.Total).HasPrecision(18, 2);

       modelBuilder.Entity<Order>().Property(o => o.Nit).HasMaxLength(50);

       modelBuilder.Entity<Ticket>(ticket =>
       {
           ticket.Property(t => t.TicketCode).HasMaxLength(32).IsRequired();
           ticket.Property(t => t.Seat).HasMaxLength(50);
           // Guarantees no two tickets can ever share a code at the door.
           ticket.HasIndex(t => t.TicketCode).IsUnique();
       });
    }

    public MySqlDbContext(DbContextOptions<MySqlDbContext> options) : base(options)
    {
        
    }
    
    public DbSet<Order> Orders { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
}