using Microsoft.EntityFrameworkCore;
using SchoolFeesSystem.Models;

namespace SchoolFeesSystem.Data
{
    public class SchoolDbContext : DbContext
    {
        public SchoolDbContext(DbContextOptions<SchoolDbContext> options)
        : base(options)
        {
        }

        public DbSet<Class> Classes { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Guardian> Guardians { get; set; }
        public DbSet<Fee> Fees { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<SchoolInfo> SchoolInfos { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Student)
                .WithMany(s => s.Payments)
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict); //  FIX

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Fee)
                .WithMany(f => f.Payments)
                .HasForeignKey(p => p.FeeId)
                .OnDelete(DeleteBehavior.Restrict); // Optional but safe

            modelBuilder.Entity<Role>().HasData(
               new Role { RoleId = 1, RoleName = "Admin" },
               new Role { RoleId = 2, RoleName = "Accountant" },
               new Role { RoleId = 3, RoleName = "Staff" },
               new Role { RoleId = 4, RoleName = "Guardian" }

           );

            modelBuilder.Entity<SchoolInfo>().HasData(
                 new SchoolInfo
                 {
                     SchoolInfoId = 1,
                     SchoolName = "My Private School",
                     Address = "P.O Box 123, Accra",
                     Phone = "0241234567",
                     LogoPath = "/images/logo.png"
                 }
             );

        }

    }
}

