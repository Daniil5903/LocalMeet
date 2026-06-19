using LocalMeet.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LocalMeet.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories => Set<Category>();

        public DbSet<Event> Events => Set<Event>();

        public DbSet<Participation> Participations => Set<Participation>();

        public DbSet<FavoriteEvent> FavoriteEvents => Set<FavoriteEvent>();

        public DbSet<Notification> Notifications => Set<Notification>();

        public DbSet<EventMessage> EventMessages => Set<EventMessage>();

        public DbSet<Report> Reports => Set<Report>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.FirstName)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(u => u.LastName)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(u => u.AvatarPath)
                    .HasMaxLength(255);

                entity.Property(u => u.About)
                    .HasMaxLength(500);
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Categories");

                entity.HasKey(c => c.Id);

                entity.Property(c => c.Name)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasIndex(c => c.Name)
                    .IsUnique();
            });

            modelBuilder.Entity<Event>(entity =>
            {
                entity.ToTable("Events");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Title)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(e => e.Description)
                    .HasColumnType("text")
                    .IsRequired();

                entity.Property(e => e.Address)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(e => e.Latitude)
                    .HasPrecision(10, 7);

                entity.Property(e => e.Longitude)
                    .HasPrecision(10, 7);

                entity.Property(e => e.EventDate)
                    .IsRequired();

                entity.Property(e => e.MaxParticipants)
                    .IsRequired();

                entity.Property(e => e.Status)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                entity.Property(e => e.RejectReason)
                    .HasMaxLength(500);

                entity.HasOne(e => e.Creator)
                    .WithMany(u => u.CreatedEvents)
                    .HasForeignKey(e => e.CreatorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Category)
                    .WithMany(c => c.Events)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.EventDate);
                entity.HasIndex(e => e.CategoryId);
                entity.HasIndex(e => e.CreatorId);
            });

            modelBuilder.Entity<Participation>(entity =>
            {
                entity.ToTable("Participations");

                entity.HasKey(p => p.Id);

                entity.Property(p => p.CreatedAt)
                    .IsRequired();

                entity.HasOne(p => p.User)
                    .WithMany(u => u.Participations)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Event)
                    .WithMany(e => e.Participations)
                    .HasForeignKey(p => p.EventId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(p => new { p.UserId, p.EventId })
                    .IsUnique();

                entity.HasIndex(p => p.EventId);
                entity.HasIndex(p => p.UserId);
            });

            modelBuilder.Entity<FavoriteEvent>(entity =>
            {
                entity.ToTable("FavoriteEvents");

                entity.HasKey(f => f.Id);

                entity.Property(f => f.CreatedAt)
                    .IsRequired();

                entity.HasOne(f => f.User)
                    .WithMany(u => u.FavoriteEvents)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Event)
                    .WithMany(e => e.FavoriteEvents)
                    .HasForeignKey(f => f.EventId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(f => new { f.UserId, f.EventId })
                    .IsUnique();

                entity.HasIndex(f => f.UserId);
                entity.HasIndex(f => f.EventId);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");

                entity.HasKey(n => n.Id);

                entity.Property(n => n.Title)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(n => n.Message)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(n => n.Link)
                    .HasMaxLength(255);

                entity.Property(n => n.IsRead)
                    .IsRequired();

                entity.Property(n => n.CreatedAt)
                    .IsRequired();

                entity.HasOne(n => n.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(n => n.UserId);
                entity.HasIndex(n => n.IsRead);
                entity.HasIndex(n => n.CreatedAt);
            });

            modelBuilder.Entity<EventMessage>(entity =>
            {
                entity.ToTable("EventMessages");

                entity.HasKey(m => m.Id);

                entity.Property(m => m.Text)
                    .HasMaxLength(1000)
                    .IsRequired();

                entity.Property(m => m.CreatedAt)
                    .IsRequired();

                entity.Property(m => m.IsDeleted)
                    .IsRequired();

                entity.HasOne(m => m.Event)
                    .WithMany(e => e.Messages)
                    .HasForeignKey(m => m.EventId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.User)
                    .WithMany(u => u.EventMessages)
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(m => m.EventId);
                entity.HasIndex(m => m.UserId);
                entity.HasIndex(m => m.CreatedAt);
            });

            modelBuilder.Entity<Report>(entity =>
            {
                entity.ToTable("Reports");

                entity.HasKey(r => r.Id);

                entity.Property(r => r.TargetType)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(r => r.TargetId)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.Property(r => r.Description)
                    .HasMaxLength(1000)
                    .IsRequired();

                entity.Property(r => r.Status)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(r => r.CreatedAt)
                    .IsRequired();

                entity.Property(r => r.AdminComment)
                    .HasMaxLength(1000);

                entity.HasOne(r => r.Author)
                    .WithMany(u => u.Reports)
                    .HasForeignKey(r => r.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(r => r.AuthorId);
                entity.HasIndex(r => r.TargetType);
                entity.HasIndex(r => r.TargetId);
                entity.HasIndex(r => r.Status);
                entity.HasIndex(r => r.CreatedAt);
            });
        }
    }
}