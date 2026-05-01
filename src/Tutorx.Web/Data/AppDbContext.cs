using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Tutorx.Web.Models.Entities;

namespace Tutorx.Web.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<DrawHistory> DrawHistories => Set<DrawHistory>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Evaluation> Evaluations => Set<Evaluation>();
    public DbSet<PresentationStudent> PresentationStudents => Set<PresentationStudent>();
    public DbSet<ActivityAttribute> ActivityAttributes => Set<ActivityAttribute>();
    public DbSet<ActivityAttributeOption> ActivityAttributeOptions => Set<ActivityAttributeOption>();
    public DbSet<StudentAttributeValue> StudentAttributeValues => Set<StudentAttributeValue>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Group - unique name
        builder.Entity<Group>().HasIndex(g => g.Name).IsUnique();

        // Student - card number is not globally unique (same card can appear in different groups)
        builder.Entity<Student>().HasIndex(s => s.CardNumber)
            .HasFilter("[CardNumber] IS NOT NULL");

        // Attendance unique constraint — each student can have one record per (date, time) slot
        builder.Entity<Attendance>()
            .HasIndex(a => new { a.StudentId, a.GroupId, a.Date, a.Time }).IsUnique();

        // Evaluation unique constraint
        builder.Entity<Evaluation>()
            .HasIndex(e => new { e.StudentId, e.TaskItemId }).IsUnique();

        // Score precision
        builder.Entity<Evaluation>()
            .Property(e => e.Score).HasPrecision(5, 2);

        // Relationships with restrict delete to prevent cascading issues
        builder.Entity<Student>()
            .HasOne(s => s.Group)
            .WithMany(g => g.Students)
            .HasForeignKey(s => s.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Activity>()
            .HasOne(a => a.Group)
            .WithMany(g => g.Activities)
            .HasForeignKey(a => a.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TaskItem>()
            .HasOne(t => t.Activity)
            .WithMany(a => a.Tasks)
            .HasForeignKey(t => t.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Assignment>()
            .HasOne(a => a.Student)
            .WithMany(s => s.Assignments)
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Assignment>()
            .HasOne(a => a.Activity)
            .WithMany(act => act.Assignments)
            .HasForeignKey(a => a.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Assignment>().HasIndex(a => new { a.StudentId, a.ActivityId }).IsUnique();

        builder.Entity<Assignment>()
            .HasOne(a => a.TaskItem)
            .WithMany(t => t.Assignments)
            .HasForeignKey(a => a.TaskItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<DrawHistory>()
            .HasOne(d => d.Student)
            .WithMany(s => s.DrawHistories)
            .HasForeignKey(d => d.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<DrawHistory>()
            .HasOne(d => d.Group)
            .WithMany()
            .HasForeignKey(d => d.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<DrawHistory>()
            .HasOne(d => d.Activity).WithMany()
            .HasForeignKey(d => d.ActivityId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.Entity<DrawHistory>()
            .HasOne(d => d.TaskItem).WithMany()
            .HasForeignKey(d => d.TaskItemId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        builder.Entity<Attendance>()
            .HasOne(a => a.Student)
            .WithMany(s => s.Attendances)
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Attendance>()
            .HasOne(a => a.Group)
            .WithMany()
            .HasForeignKey(a => a.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Evaluation>()
            .HasIndex(e => new { e.StudentId, e.TaskItemId }).IsUnique();

        builder.Entity<Evaluation>()
            .HasOne(e => e.Student)
            .WithMany(s => s.Evaluations)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Evaluation>()
            .HasOne(e => e.TaskItem)
            .WithMany(t => t.Evaluations)
            .HasForeignKey(e => e.TaskItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PresentationStudent>()
            .HasOne(ps => ps.TaskItem).WithMany(t => t.PresentationStudents)
            .HasForeignKey(ps => ps.TaskItemId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<PresentationStudent>()
            .HasOne(ps => ps.Student).WithMany(s => s.PresentationStudents)
            .HasForeignKey(ps => ps.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PresentationStudent>()
            .HasIndex(ps => new { ps.TaskItemId, ps.StudentId, ps.Role }).IsUnique();

        // ActivityAttribute → Activity (cascade delete)
        builder.Entity<ActivityAttribute>()
            .HasOne(a => a.Activity).WithMany(act => act.OtherAttributes)
            .HasForeignKey(a => a.ActivityId).OnDelete(DeleteBehavior.Cascade);

        // ActivityAttributeOption → ActivityAttribute (cascade delete)
        builder.Entity<ActivityAttributeOption>()
            .HasOne(o => o.ActivityAttribute).WithMany(a => a.Options)
            .HasForeignKey(o => o.ActivityAttributeId).OnDelete(DeleteBehavior.Cascade);

        // StudentAttributeValue → ActivityAttribute (restrict; controller deletes values manually before deleting attribute)
        builder.Entity<StudentAttributeValue>()
            .HasOne(v => v.ActivityAttribute).WithMany(a => a.StudentValues)
            .HasForeignKey(v => v.ActivityAttributeId).OnDelete(DeleteBehavior.Restrict);

        // StudentAttributeValue → Student (restrict to avoid multi-cascade-path with activity)
        builder.Entity<StudentAttributeValue>()
            .HasOne(v => v.Student).WithMany(s => s.AttributeValues)
            .HasForeignKey(v => v.StudentId).OnDelete(DeleteBehavior.Restrict);

        // StudentAttributeValue → Option (set null when option deleted)
        builder.Entity<StudentAttributeValue>()
            .HasOne(v => v.Option).WithMany()
            .HasForeignKey(v => v.OptionId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);

        builder.Entity<StudentAttributeValue>()
            .HasIndex(v => new { v.StudentId, v.ActivityAttributeId }).IsUnique();
    }
}
