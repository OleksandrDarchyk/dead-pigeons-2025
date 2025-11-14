using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using dataccess.Entities;

namespace dataccess;

public partial class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Board> Boards { get; set; }

    public virtual DbSet<Game> Games { get; set; }

    public virtual DbSet<Player> Players { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Board>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("board_pkey");

            entity.ToTable("board", "deadpigeons");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Createdat).HasColumnName("createdat");
            entity.Property(e => e.Deletedat).HasColumnName("deletedat");
            entity.Property(e => e.Gameid).HasColumnName("gameid");
            entity.Property(e => e.Iswinning)
                .HasDefaultValue(false)
                .HasColumnName("iswinning");
            entity.Property(e => e.Numbers).HasColumnName("numbers");
            entity.Property(e => e.Playerid).HasColumnName("playerid");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.Repeatactive)
                .HasDefaultValue(false)
                .HasColumnName("repeatactive");
            entity.Property(e => e.Repeatweeks)
                .HasDefaultValue(0)
                .HasColumnName("repeatweeks");

            entity.HasOne(d => d.Game).WithMany(p => p.Boards)
                .HasForeignKey(d => d.Gameid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("board_gameid_fkey");

            entity.HasOne(d => d.Player).WithMany(p => p.Boards)
                .HasForeignKey(d => d.Playerid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("board_playerid_fkey");
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("game_pkey");

            entity.ToTable("game", "deadpigeons");

            entity.HasIndex(e => new { e.Weeknumber, e.Year }, "idx_game_week_year").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Closedat).HasColumnName("closedat");
            entity.Property(e => e.Createdat).HasColumnName("createdat");
            entity.Property(e => e.Deletedat).HasColumnName("deletedat");
            entity.Property(e => e.Isactive)
                .HasDefaultValue(true)
                .HasColumnName("isactive");
            entity.Property(e => e.Weeknumber).HasColumnName("weeknumber");
            entity.Property(e => e.Winningnumbers).HasColumnName("winningnumbers");
            entity.Property(e => e.Year).HasColumnName("year");
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("player_pkey");

            entity.ToTable("player", "deadpigeons");

            entity.HasIndex(e => e.Email, "idx_player_email").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Activatedat).HasColumnName("activatedat");
            entity.Property(e => e.Createdat).HasColumnName("createdat");
            entity.Property(e => e.Deletedat).HasColumnName("deletedat");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Fullname).HasColumnName("fullname");
            entity.Property(e => e.Isactive)
                .HasDefaultValue(false)
                .HasColumnName("isactive");
            entity.Property(e => e.Phone).HasColumnName("phone");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("transactions_pkey");

            entity.ToTable("transactions", "deadpigeons");

            entity.HasIndex(e => e.Mobilepaynumber, "idx_transactions_mp").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.Approvedat).HasColumnName("approvedat");
            entity.Property(e => e.Createdat).HasColumnName("createdat");
            entity.Property(e => e.Deletedat).HasColumnName("deletedat");
            entity.Property(e => e.Mobilepaynumber).HasColumnName("mobilepaynumber");
            entity.Property(e => e.Playerid).HasColumnName("playerid");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Pending'::text")
                .HasColumnName("status");

            entity.HasOne(d => d.Player).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.Playerid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("transactions_playerid_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users", "deadpigeons");

            entity.HasIndex(e => e.Email, "idx_users_email").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Createdat).HasColumnName("createdat");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Passwordhash).HasColumnName("passwordhash");
            entity.Property(e => e.Role)
                .HasDefaultValueSql("'User'::text")
                .HasColumnName("role");
            entity.Property(e => e.Salt).HasColumnName("salt");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
