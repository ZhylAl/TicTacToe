using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using TicTacToe.Core.Entities;
using TicTacToe.Core.Enums;

namespace TicTacToe.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // not sure how its working, but it seems to be working fine, so I will leave it as is for now 
            var boardComparer = new ValueComparer<CellState[]>(
                (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToArray()); 

            modelBuilder.Entity<Game>(entity =>
            {
                entity.Property(g => g.Board)
                    .HasConversion(
                        board => JsonSerializer.Serialize(board, (JsonSerializerOptions?)null),
                        json => JsonSerializer.Deserialize<CellState[]>(json, (JsonSerializerOptions?)null) ?? new CellState[9]
                    )
                    .Metadata.SetValueComparer(boardComparer);
            });

            modelBuilder.Entity<GameInvite>(entity =>
            {
                entity.HasOne(x => x.Sender)
                    .WithMany()
                    .HasForeignKey(x => x.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.TargetUser)
                    .WithMany()
                    .HasForeignKey(x => x.TargetUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        public DbSet<Game> Games { get; set; }
        public DbSet<GameInvite> GameInvites { get; set; }
    }
}
