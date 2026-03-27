using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<DisposalPoint> DisposalPoints { get; set; }
        public DbSet<WasteType> WasteTypes { get; set; }
        public DbSet<DisposalPointWasteType> DisposalPointWasteTypes { get; set; }
        public DbSet<DisposalRequest> DisposalRequests { get; set; }
        public DbSet<PointsTransaction> PointsTransactions { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<SupportChat> SupportChats { get; set; }
        public DbSet<SupportMessage> SupportMessages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserDevice> UserDevices { get; set; }

        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<UserAchievement> UserAchievements { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.PasswordHash)
                    .IsRequired();

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.HasIndex(e => e.Email)
                    .IsUnique();
            });

            
            modelBuilder.Entity<DisposalPoint>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);
            });

            
            modelBuilder.Entity<WasteType>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);
            });


            modelBuilder.Entity<DisposalPointWasteType>()
        .HasKey(pw => new { pw.DisposalPointId, pw.WasteTypeId });

            modelBuilder.Entity<DisposalPointWasteType>()
      .HasOne(pw => pw.WasteType)
      .WithMany(w => w.DisposalPointWasteTypes) 
      .HasForeignKey(pw => pw.WasteTypeId);

            modelBuilder.Entity<DisposalPointWasteType>()
                .HasOne(pw => pw.DisposalPoint)
                .WithMany(p => p.DisposalPointWasteTypes) 
                .HasForeignKey(pw => pw.DisposalPointId);


            modelBuilder.Entity<DisposalRequest>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.DisposalRequests)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.DisposalPoint)
                    .WithMany()
                    .HasForeignKey(e => e.DisposalPointId);
            });

            
            modelBuilder.Entity<PointsTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.PointsTransactions)
                    .HasForeignKey(e => e.UserId);
            });

           
            modelBuilder.Entity<Reminder>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Reminders)
                    .HasForeignKey(e => e.UserId);
            });

            
            modelBuilder.Entity<SupportChat>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.SupportChats)
                    .HasForeignKey(e => e.UserId);
            });

           
            modelBuilder.Entity<SupportMessage>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Chat)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(e => e.ChatId);
            });

            
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(e => e.UserId);
            });

           
            modelBuilder.Entity<UserDevice>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Devices)
                    .HasForeignKey(e => e.UserId);
            });
        }
    }
}