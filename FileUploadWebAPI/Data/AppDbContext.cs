using Microsoft.EntityFrameworkCore;
using FileUploadWebAPI.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<FileDetail> FileDetails { get; set; }
}
