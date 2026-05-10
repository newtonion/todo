using System;
using Api.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure;

public class TodoDatabaseContext : DbContext
{
    public TodoDatabaseContext(DbContextOptions<TodoDatabaseContext> options)
        : base(options)
    {
    }

    public DbSet<ListEntity> Lists { get; set; }
    public DbSet<ListItemEntity> ListItems { get; set; }
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<CategoryEntity> Categories { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ListEntity -> ListItemEntity (cascade delete children when list deleted)
        modelBuilder.Entity<ListEntity>()
            .HasMany(e => e.Children)
            .WithOne(e => e.Parent)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Cascade);

        // UserEntity -> ListEntity (cascade delete lists when user deleted)
        modelBuilder.Entity<UserEntity>()
            .HasMany(e => e.Lists)
            .WithOne(e => e.Owner)
            .HasForeignKey(e => e.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        // CategoryEntity -> ListEntity (restrict delete if category in use)
        modelBuilder.Entity<CategoryEntity>()
            .HasMany<ListEntity>()
            .WithOne(e => e.Category)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // UserEntity -> ListItemEntity (cascade delete items when user deleted)
        modelBuilder.Entity<UserEntity>()
            .HasMany<ListItemEntity>()
            .WithOne(e => e.Owner)
            .HasForeignKey(e => e.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        // UserEntity -> CategoryEntity (cascade delete user categories when user deleted, null for global)
        modelBuilder.Entity<UserEntity>()
            .HasMany<CategoryEntity>()
            .WithOne(e => e.Owner)
            .HasForeignKey(e => e.OwnerId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        base.OnModelCreating(modelBuilder);
    }

}
