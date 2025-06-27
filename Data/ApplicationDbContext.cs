using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyApi.Models;
namespace MyApi.Data;
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Forum> Forums { get; set; }
    public DbSet<Threads> Threads { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<PostLike> PostLikes { get; set; }
    public DbSet<ThreadLike> ThreadLikes { get; set; }
      protected override void OnModelCreating(ModelBuilder builder)
      {
            base.OnModelCreating(builder);
            builder.Entity<ApplicationUser>(entity =>
            {
                  entity.ToTable(name: "Users");
                  entity.Property(u => u.FirstName).HasMaxLength(100);
                  entity.Property(u => u.LastName).HasMaxLength(100);
                  entity.Property(u => u.Bio).HasMaxLength(500);
                  entity.Property(u => u.ProfileImageUrl).HasMaxLength(300);
                  entity.Property(u => u.BannedAt).HasColumnType("datetime");
                  entity.HasMany(u => u.Posts)
                    .WithOne(p => p.Author)
                    .HasForeignKey(p => p.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                  entity.HasMany(u => u.Threads)
                    .WithOne(t => t.Author)
                    .HasForeignKey(t => t.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                  entity.HasMany(u => u.Notifications)
                    .WithOne(n => n.Recipient)
                    .HasForeignKey(n => n.RecipientId)
                    .OnDelete(DeleteBehavior.Cascade);

                  entity.HasMany(u => u.RefreshTokens)
                        .WithOne(t => t.User)
                        .HasForeignKey(t => t.UserId)
                        .IsRequired();
            });

            builder.Entity<IdentityRole>(entity =>
            {
                  entity.ToTable(name: "Roles");
            });

            builder.Entity<Threads>(entity =>
            {
                  entity.ToTable("Threads");

              entity.Property(t => t.Title)
                .IsRequired();
              entity.Property(t => t.Content);

                  entity.HasOne(t => t.Forum)
                    .WithMany(f => f.Threads)
                    .HasForeignKey(t => t.ForumId)
                    .OnDelete(DeleteBehavior.Cascade);

                  entity.HasOne(t => t.Author)
                    .WithMany(u => u.Threads)
                    .HasForeignKey(t => t.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                  entity.HasMany(t => t.Posts)
                    .WithOne(p => p.Thread)
                    .HasForeignKey(p => p.ThreadId)
                    .OnDelete(DeleteBehavior.Cascade);

                  entity.HasMany(t => t.Likes)
                        .WithOne(tl => tl.Thread)
                        .HasForeignKey(tl => tl.ThreadId)
                        .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Post>(entity =>
            {
              entity.ToTable("Posts");

              entity.Property(p => p.Content)
                .IsRequired();

              entity.HasOne(p => p.Author)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
              entity.HasOne(p => p.Thread)
                .WithMany(t => t.Posts)
                .HasForeignKey(p => p.ThreadId)
                .OnDelete(DeleteBehavior.Cascade);

              entity.HasMany(p => p.Likes)
                    .WithOne(pl => pl.Post)
                    .HasForeignKey(pl => pl.PostId)
                    .OnDelete(DeleteBehavior.Cascade);
              entity.HasOne(p => p.ParentPost)
                    .WithMany(p => p.Replies)
                    .HasForeignKey(p => p.ParentPostId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Forum>(entity =>
            {
                entity.ToTable("Forums");

              entity.Property(f => f.Title)
                .IsRequired();

              entity.Property(f => f.Description);

              entity.Property(f => f.ImageUrl);

              entity.HasMany(f => f.Threads)
                .WithOne(t => t.Forum)
                .HasForeignKey(t => t.ForumId)
                .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Notification>(entity =>
            {
                  entity.ToTable("Notifications");

                  entity.Property(n => n.Message)
                    .IsRequired()
                    .HasMaxLength(1000);

                  entity.Property(n => n.Url)
                        .HasMaxLength(1000);

                  entity.HasOne(n => n.Recipient)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(n => n.RecipientId)
                    .OnDelete(DeleteBehavior.Cascade);

                  entity.HasOne(n => n.Sender)
                    .WithMany()
                    .HasForeignKey(n => n.SenderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            builder.Entity<RefreshToken>(token =>
            {
                  token.ToTable("RefreshTokens");

                  token.Property(rt => rt.Token)
                       .HasMaxLength(1000);

                  token.HasOne(rt => rt.User)
                       .WithMany(u => u.RefreshTokens)
                       .HasForeignKey(rt => rt.UserId)
                       .OnDelete(DeleteBehavior.Cascade);
            });
            builder.Entity<PostLike>()
                  .HasKey(pl => new { pl.ApplicationUserId, pl.PostId });

            builder.Entity<PostLike>()
                  .HasOne(pl => pl.User)
                  .WithMany()
                  .HasForeignKey(pl => pl.ApplicationUserId);

            builder.Entity<PostLike>()
                  .HasOne(pl => pl.Post)
                  .WithMany(p => p.Likes)
                  .HasForeignKey(pl => pl.PostId);
      }
}