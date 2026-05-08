using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using webapi.Models;
using webapi.Models.ModelsImpl;

namespace webapi.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Example DbSet - add your entities here
    public DbSet<ExampleEntity> Examples { get; set; }

    public DbSet<Problem> Problems { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<MagicLink> MagicLinks { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
        });

        builder.Entity<Problem>().OwnsMany(p => p.Coordinates, b => b.ToJson());

        builder.Entity<Attachment>()
            .HasOne(a => a.Problem)
            .WithMany(p => p.Attachments)
            .HasForeignKey(a => a.ProblemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Attachment>()
            .HasOne(a => a.Event)
            .WithMany(e => e.Attachments)
            .HasForeignKey(a => a.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
