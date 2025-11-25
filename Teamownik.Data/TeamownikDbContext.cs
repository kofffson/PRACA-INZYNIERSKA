using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Teamownik.Data.Models;

namespace Teamownik.Data;

public class TeamownikDbContext : IdentityDbContext<ApplicationUser>
{
    public TeamownikDbContext(DbContextOptions<TeamownikDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<GroupMessage> GroupMessages { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<GameParticipant> GameParticipants { get; set; }
    public DbSet<Settlement> Settlements { get; set; }
    public DbSet<GroupInvitation> GroupInvitations { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<ApplicationUser>().ToTable("AspNetUsers");
        
        builder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.GroupId);
            entity.Property(e => e.GroupName).IsRequired().HasMaxLength(200);
            
            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        builder.Entity<GroupMember>(entity =>
        {
            entity.HasKey(e => e.GroupMemberId);
            
            entity.HasOne(e => e.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.User)
                .WithMany(u => u.GroupMemberships)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.GroupId, e.UserId }).IsUnique();
        });
        
        builder.Entity<Game>(entity =>
        {
            entity.HasKey(e => e.GameId);
            entity.Property(e => e.GameName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Cost).HasPrecision(10, 2);
            
            entity.HasOne(e => e.Organizer)
                .WithMany(u => u.OrganizedGames)
                .HasForeignKey(e => e.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Group)
                .WithMany(g => g.Games)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        
        builder.Entity<GameParticipant>(entity =>
        {
            entity.HasKey(e => e.ParticipantId);
            
            entity.HasOne(e => e.Game)
                .WithMany(g => g.Participants)
                .HasForeignKey(e => e.GameId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.User)
                .WithMany(u => u.GameParticipations)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.GameId, e.UserId }).IsUnique();
        });
        
        builder.Entity<Settlement>(entity =>
        {
            entity.HasKey(e => e.SettlementId);
            entity.Property(e => e.Amount).HasPrecision(10, 2);
            entity.Property(e => e.BankAccountNumber).HasMaxLength(34);
            
            entity.HasOne(e => e.Game)
                .WithMany(g => g.Settlements)
                .HasForeignKey(e => e.GameId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Payer)
                .WithMany(u => u.PaymentsToMake)
                .HasForeignKey(e => e.PayerId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Recipient)
                .WithMany(u => u.PaymentsToReceive)
                .HasForeignKey(e => e.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        builder.Entity<GroupInvitation>(entity =>
        {
            entity.HasKey(e => e.InvitationId);
            entity.Property(e => e.InvitedEmail).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(100);
            
            entity.HasIndex(e => e.Token).IsUnique();
            
            entity.HasOne(e => e.Group)
                .WithMany(g => g.Invitations)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Inviter)
                .WithMany(u => u.SentInvitations)
                .HasForeignKey(e => e.InvitedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        builder.Entity<GroupMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId);
            entity.Property(e => e.MessageText).IsRequired();
            
            entity.HasOne(e => e.Group)
                .WithMany(g => g.Messages)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.User)
                .WithMany(u => u.Messages)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}