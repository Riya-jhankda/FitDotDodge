
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;


namespace Persistence
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<School> Schools { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<ScannerDevice> ScannerDevices { get; set; }
        public DbSet<ClassEnrollment> ClassEnrollments { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<WorkoutLog> WorkoutLogs { get; set; }




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==== ApplicationUser ====
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("Users");

                entity.HasIndex(u => new { u.SchoolName, u.UserName })
                      .IsUnique();

                entity.Property(u => u.Role)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.HasOne(u => u.School)
                      .WithMany(s => s.Users)
                      .HasForeignKey(u => u.SchoolId)
                      .OnDelete(DeleteBehavior.Restrict);
            });



            // ==== School ====
            modelBuilder.Entity<School>(entity =>
            {
                entity.ToTable("Schools");

                entity.HasKey(s => s.SchoolId);

                entity.Property(s => s.Name)
                      .IsRequired()
                      .HasMaxLength(150);
            });

            // ==== Class ====
            modelBuilder.Entity<Class>(entity =>
            {
                entity.ToTable("Classes");

                entity.HasKey(c => c.ClassId);

                entity.Property(c => c.ClassType)
                      .HasConversion<string>();

                entity.Property(c => c.Name)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.HasOne(c => c.Coach)
                      .WithMany() // if you later add a Coach.Classes collection, change to .WithMany(x => x.Classes)
                      .HasForeignKey(c => c.CoachId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ==== ScannerDevice ====
            modelBuilder.Entity<ScannerDevice>(entity =>
            {
                entity.ToTable("ScannerDevices");

                entity.HasKey(s => s.ScanId);

                entity.Property(s => s.Name)
                      .HasMaxLength(150)
                      .IsRequired(false);

                entity.HasOne(s => s.School)
                      .WithMany(sch => sch.ScannerDevices)
                      .HasForeignKey(s => s.SchoolId)
                      .OnDelete(DeleteBehavior.Cascade);

            });

            // ==== ClassEnrollment ====
            modelBuilder.Entity<ClassEnrollment>(entity =>
            {
                entity.HasOne(e => e.User)
                      .WithMany(u => u.ClassEnrollments) // ✅ connect to collection
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Class)
                      .WithMany() // you can later add c.Enrollments
                      .HasForeignKey(e => e.ClassId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ==== Attendance ====
            modelBuilder.Entity<Attendance>(entity =>
            {
                entity.ToTable("Attendances");

                entity.HasKey(a => a.AttendanceId);

                entity.HasIndex(a => new { a.UserId, a.ClassId, a.Date }).IsUnique();

                entity.Property(a => a.IsPresent).IsRequired();
                entity.Property(a => a.Date).HasColumnType("date");

                entity.HasOne(a => a.User)
                      .WithMany(u => u.Attendances) // ✅ connect to collection
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Class)
                      .WithMany()
                      .HasForeignKey(a => a.ClassId)
                      .OnDelete(DeleteBehavior.Restrict);
            });


            // ==== WorkoutLog ====
            modelBuilder.Entity<WorkoutLog>(entity =>
            {
                entity.ToTable("WorkoutLogs");

                entity.HasKey(w => w.Id);

                entity.HasOne(w => w.User)
                      .WithMany() // you can later add u.WorkoutLogs collection if needed
                      .HasForeignKey(w => w.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(w => w.Category)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(w => w.ExerciseName)
                      .HasMaxLength(150)
                      .IsRequired();

                entity.Property(w => w.LoggedDate)
                      .HasColumnType("datetime");
            });




        }


    }
}
