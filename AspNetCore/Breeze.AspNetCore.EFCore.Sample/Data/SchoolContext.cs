using Breeze.AspNetCore.EFCore.Sample.Models;
using Microsoft.EntityFrameworkCore;

namespace Breeze.AspNetCore.EFCore.Sample.Data
{
    /// <summary>
    /// This example data was repoduced based on this article: https://docs.microsoft.com/en-us/aspnet/core/data/ef-mvc/intro
    /// </summary>
    public class SchoolContext : DbContext
    {
        public SchoolContext(DbContextOptions<SchoolContext> options) : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Student> Students { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Course>().ToTable("Course");
            modelBuilder.Entity<Enrollment>().ToTable("Enrollment");
            modelBuilder.Entity<Student>().ToTable("Student");
        }
    }
}
