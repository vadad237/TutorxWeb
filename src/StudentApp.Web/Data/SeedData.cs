using Microsoft.AspNetCore.Identity;
using StudentApp.Web.Models.Entities;

namespace StudentApp.Web.Data;

public static class SeedData
{
    public static async Task InitializeAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var config = app.Configuration;

        // Seed admin user
        var adminUserName = config["SeedAdmin:UserName"] ?? "admin";
        var adminPassword = config["SeedAdmin:Password"] ?? "Admin@1234";

        if (await userManager.FindByNameAsync(adminUserName) == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminUserName,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(adminUser, adminPassword);
        }

        // Seed sample data for all parts of the application
        if (!context.Groups.Any())
        {
            var now = DateTime.UtcNow;

            // ── Groups ──
            var group1 = new Group
            {
                Name = "Informatika 1A",
                Description = "First year Computer Science group A",
                CreatedAt = now
            };
            var group2 = new Group
            {
                Name = "Informatika 1B",
                Description = "First year Computer Science group B",
                CreatedAt = now
            };
            var group3 = new Group
            {
                Name = "Matematika 2A",
                Description = "Second year Mathematics group A",
                CreatedAt = now,
                IsArchived = true
            };

            context.Groups.AddRange(group1, group2, group3);
            await context.SaveChangesAsync();

            // ── Students ──
            var students1 = new[]
            {
                new Student { FirstName = "Ján", LastName = "Novák", Email = "jan.novak@student.sk", CardNumber = "C001", Year = 1, GroupId = group1.Id },
                new Student { FirstName = "Eva", LastName = "Kováčová", Email = "eva.kovacova@student.sk", CardNumber = "C002", Year = 1, GroupId = group1.Id },
                new Student { FirstName = "Peter", LastName = "Horváth", Email = "peter.horvath@student.sk", CardNumber = "C003", Year = 1, GroupId = group1.Id },
                new Student { FirstName = "Mária", LastName = "Tóthová", Email = "maria.tothova@student.sk", CardNumber = "C004", Year = 1, GroupId = group1.Id },
                new Student { FirstName = "Tomáš", LastName = "Baláž", Email = "tomas.balaz@student.sk", CardNumber = "C005", Year = 1, GroupId = group1.Id }
            };

            var students2 = new[]
            {
                new Student { FirstName = "Lucia", LastName = "Szabová", Email = "lucia.szabova@student.sk", CardNumber = "C006", Year = 1, GroupId = group2.Id },
                new Student { FirstName = "Martin", LastName = "Molnár", Email = "martin.molnar@student.sk", CardNumber = "C007", Year = 1, GroupId = group2.Id },
                new Student { FirstName = "Anna", LastName = "Vargová", Email = "anna.vargova@student.sk", CardNumber = "C008", Year = 1, GroupId = group2.Id },
                new Student { FirstName = "Michal", LastName = "Fekete", Email = "michal.fekete@student.sk", CardNumber = "C009", Year = 1, GroupId = group2.Id },
                new Student { FirstName = "Zuzana", LastName = "Baloghová", Email = "zuzana.baloghova@student.sk", CardNumber = "C010", Year = 1, GroupId = group2.Id }
            };

            var students3 = new[]
            {
                new Student { FirstName = "Róbert", LastName = "Kráľ", Email = "robert.kral@student.sk", CardNumber = "C011", Year = 2, GroupId = group3.Id },
                new Student { FirstName = "Katarína", LastName = "Vlčková", Email = "katarina.vlckova@student.sk", CardNumber = "C012", Year = 2, GroupId = group3.Id },
                new Student { FirstName = "Ondrej", LastName = "Šimko", Email = "ondrej.simko@student.sk", CardNumber = "C013", Year = 2, GroupId = group3.Id, IsActive = false }
            };

            context.Students.AddRange(students1);
            context.Students.AddRange(students2);
            context.Students.AddRange(students3);
            await context.SaveChangesAsync();

            // ── Activities & Tasks – Group 1 ──
            var activity1 = new Activity
            {
                Name = "Seminárna práca 1",
                Description = "First seminar assignment",
                GroupId = group1.Id
            };
            var activity1b = new Activity
            {
                Name = "Laboratórne cvičenia",
                Description = "Practical lab exercises throughout the semester",
                GroupId = group1.Id
            };
            context.Activities.AddRange(activity1, activity1b);
            await context.SaveChangesAsync();

            var tasks1 = new[]
            {
                new TaskItem { Title = "Analýza požiadaviek", ActivityId = activity1.Id, PresentationDate = now.AddDays(30), IsPresentation = true },
                new TaskItem { Title = "Návrh databázy", ActivityId = activity1.Id },
                new TaskItem { Title = "Implementácia backendu", ActivityId = activity1.Id },
                new TaskItem { Title = "Implementácia frontendu", ActivityId = activity1.Id },
                new TaskItem { Title = "Testovanie a dokumentácia", ActivityId = activity1.Id, PresentationDate = now.AddDays(60), IsPresentation = true }
            };
            var tasks1b = new[]
            {
                new TaskItem { Title = "Lab 1 – Úvod do SQL", ActivityId = activity1b.Id },
                new TaskItem { Title = "Lab 2 – Pokročilé joiny", ActivityId = activity1b.Id },
                new TaskItem { Title = "Lab 3 – Indexy a optimalizácia", ActivityId = activity1b.Id }
            };
            context.TaskItems.AddRange(tasks1);
            context.TaskItems.AddRange(tasks1b);
            await context.SaveChangesAsync();

            // ── Activities & Tasks – Group 2 ──
            var activity2 = new Activity
            {
                Name = "Projektová práca",
                Description = "Semester team project",
                GroupId = group2.Id
            };
            context.Activities.Add(activity2);
            await context.SaveChangesAsync();

            var tasks2 = new[]
            {
                new TaskItem { Title = "Špecifikácia projektu", ActivityId = activity2.Id, PresentationDate = now.AddDays(14), IsPresentation = true },
                new TaskItem { Title = "Prototyp", ActivityId = activity2.Id },
                new TaskItem { Title = "Finálna prezentácia", ActivityId = activity2.Id, PresentationDate = now.AddDays(50), IsPresentation = true }
            };
            context.TaskItems.AddRange(tasks2);
            await context.SaveChangesAsync();

            // ── Activities & Tasks – Group 3 (archived) ──
            var activity3 = new Activity
            {
                Name = "Záverečný test",
                Description = "Final exam preparation",
                GroupId = group3.Id,
                IsArchived = true
            };
            context.Activities.Add(activity3);
            await context.SaveChangesAsync();

            var tasks3 = new[]
            {
                new TaskItem { Title = "Lineárna algebra", ActivityId = activity3.Id },
                new TaskItem { Title = "Matematická analýza", ActivityId = activity3.Id }
            };
            context.TaskItems.AddRange(tasks3);
            await context.SaveChangesAsync();

            // ── Assignments (student ↔ activity + optional task) ──
            // Group 1 – activity 1: each student gets an assignment with a specific task
            var assignments1 = new[]
            {
                new Assignment { StudentId = students1[0].Id, ActivityId = activity1.Id, TaskItemId = tasks1[0].Id, Note = "Vedúci tímu" },
                new Assignment { StudentId = students1[1].Id, ActivityId = activity1.Id, TaskItemId = tasks1[1].Id },
                new Assignment { StudentId = students1[2].Id, ActivityId = activity1.Id, TaskItemId = tasks1[2].Id },
                new Assignment { StudentId = students1[3].Id, ActivityId = activity1.Id, TaskItemId = tasks1[3].Id },
                new Assignment { StudentId = students1[4].Id, ActivityId = activity1.Id, TaskItemId = tasks1[4].Id }
            };

            // Group 1 – activity 1b: only some students assigned
            var assignments1b = new[]
            {
                new Assignment { StudentId = students1[0].Id, ActivityId = activity1b.Id, TaskItemId = tasks1b[0].Id },
                new Assignment { StudentId = students1[1].Id, ActivityId = activity1b.Id, TaskItemId = tasks1b[1].Id },
                new Assignment { StudentId = students1[2].Id, ActivityId = activity1b.Id, TaskItemId = tasks1b[2].Id }
            };

            // Group 2 – activity 2
            var assignments2 = new[]
            {
                new Assignment { StudentId = students2[0].Id, ActivityId = activity2.Id, TaskItemId = tasks2[0].Id, Note = "Zodpovedná za špecifikáciu" },
                new Assignment { StudentId = students2[1].Id, ActivityId = activity2.Id, TaskItemId = tasks2[1].Id },
                new Assignment { StudentId = students2[2].Id, ActivityId = activity2.Id, TaskItemId = tasks2[1].Id },
                new Assignment { StudentId = students2[3].Id, ActivityId = activity2.Id, TaskItemId = tasks2[2].Id },
                new Assignment { StudentId = students2[4].Id, ActivityId = activity2.Id, TaskItemId = tasks2[2].Id }
            };

            // Group 3 – activity 3
            var assignments3 = new[]
            {
                new Assignment { StudentId = students3[0].Id, ActivityId = activity3.Id, TaskItemId = tasks3[0].Id },
                new Assignment { StudentId = students3[1].Id, ActivityId = activity3.Id, TaskItemId = tasks3[1].Id }
            };

            context.Assignments.AddRange(assignments1);
            context.Assignments.AddRange(assignments1b);
            context.Assignments.AddRange(assignments2);
            context.Assignments.AddRange(assignments3);
            await context.SaveChangesAsync();

            // ── Evaluations (scores for completed tasks) ──
            var evaluations = new[]
            {
                // Group 1 – activity 1
                new Evaluation { StudentId = students1[0].Id, TaskItemId = tasks1[0].Id, Score = 9.5m, Comment = "Výborná analýza", EvaluatedAt = now.AddDays(-5) },
                new Evaluation { StudentId = students1[1].Id, TaskItemId = tasks1[1].Id, Score = 8.0m, Comment = "Dobrý návrh", EvaluatedAt = now.AddDays(-4) },
                new Evaluation { StudentId = students1[2].Id, TaskItemId = tasks1[2].Id, Score = 7.5m, EvaluatedAt = now.AddDays(-3) },
                new Evaluation { StudentId = students1[3].Id, TaskItemId = tasks1[3].Id, Score = 6.0m, Comment = "Chýba responzívny dizajn", EvaluatedAt = now.AddDays(-2) },
                new Evaluation { StudentId = students1[4].Id, TaskItemId = tasks1[4].Id, Score = 10.0m, Comment = "Kompletná dokumentácia", EvaluatedAt = now.AddDays(-1) },

                // Group 1 – activity 1b (labs)
                new Evaluation { StudentId = students1[0].Id, TaskItemId = tasks1b[0].Id, Score = 8.0m, EvaluatedAt = now.AddDays(-10) },
                new Evaluation { StudentId = students1[1].Id, TaskItemId = tasks1b[1].Id, Score = 9.0m, EvaluatedAt = now.AddDays(-7) },

                // Group 2
                new Evaluation { StudentId = students2[0].Id, TaskItemId = tasks2[0].Id, Score = 8.5m, Comment = "Kvalitná špecifikácia", EvaluatedAt = now.AddDays(-6) },
                new Evaluation { StudentId = students2[1].Id, TaskItemId = tasks2[1].Id, Score = 7.0m, EvaluatedAt = now.AddDays(-3) },
                new Evaluation { StudentId = students2[2].Id, TaskItemId = tasks2[1].Id, Score = 7.5m, EvaluatedAt = now.AddDays(-3) },

                // Group 3
                new Evaluation { StudentId = students3[0].Id, TaskItemId = tasks3[0].Id, Score = 5.5m, Comment = "Potrebuje zlepšenie", EvaluatedAt = now.AddDays(-20) },
                new Evaluation { StudentId = students3[1].Id, TaskItemId = tasks3[1].Id, Score = 9.0m, EvaluatedAt = now.AddDays(-20) }
            };
            context.Evaluations.AddRange(evaluations);
            await context.SaveChangesAsync();

            // ── Attendance (multiple dates, mixed statuses) ──
            var attendanceDates = new[]
            {
                DateOnly.FromDateTime(now.AddDays(-21)),
                DateOnly.FromDateTime(now.AddDays(-14)),
                DateOnly.FromDateTime(now.AddDays(-7)),
                DateOnly.FromDateTime(now)
            };

            var attendances = new List<Attendance>();
            var statuses = new[] { AttendanceStatus.Present, AttendanceStatus.Present, AttendanceStatus.Absent, AttendanceStatus.Excused, AttendanceStatus.Present };

            foreach (var date in attendanceDates)
            {
                for (var i = 0; i < students1.Length; i++)
                {
                    attendances.Add(new Attendance
                    {
                        StudentId = students1[i].Id,
                        GroupId = group1.Id,
                        Date = date,
                        Status = statuses[(i + date.DayNumber) % statuses.Length]
                    });
                }

                for (var i = 0; i < students2.Length; i++)
                {
                    attendances.Add(new Attendance
                    {
                        StudentId = students2[i].Id,
                        GroupId = group2.Id,
                        Date = date,
                        Status = statuses[(i + date.DayNumber + 1) % statuses.Length]
                    });
                }
            }

            // Archived group – 2 dates
            foreach (var date in attendanceDates.Take(2))
            {
                for (var i = 0; i < students3.Length; i++)
                {
                    attendances.Add(new Attendance
                    {
                        StudentId = students3[i].Id,
                        GroupId = group3.Id,
                        Date = date,
                        Status = i == 2 ? AttendanceStatus.Absent : AttendanceStatus.Present
                    });
                }
            }

            context.Attendances.AddRange(attendances);
            await context.SaveChangesAsync();

            // ── Draw History (random student draws) ──
            var drawHistories = new[]
            {
                new DrawHistory { StudentId = students1[2].Id, GroupId = group1.Id, ActivityId = activity1.Id, CycleNumber = 1, DrawnAt = now.AddDays(-14) },
                new DrawHistory { StudentId = students1[0].Id, GroupId = group1.Id, ActivityId = activity1.Id, CycleNumber = 1, DrawnAt = now.AddDays(-13) },
                new DrawHistory { StudentId = students1[4].Id, GroupId = group1.Id, ActivityId = activity1.Id, CycleNumber = 1, DrawnAt = now.AddDays(-12) },
                new DrawHistory { StudentId = students1[1].Id, GroupId = group1.Id, ActivityId = activity1.Id, CycleNumber = 1, DrawnAt = now.AddDays(-11) },
                new DrawHistory { StudentId = students1[3].Id, GroupId = group1.Id, ActivityId = activity1.Id, CycleNumber = 1, DrawnAt = now.AddDays(-10) },
                new DrawHistory { StudentId = students1[0].Id, GroupId = group1.Id, ActivityId = activity1.Id, CycleNumber = 2, DrawnAt = now.AddDays(-3) },
                new DrawHistory { StudentId = students1[2].Id, GroupId = group1.Id, ActivityId = activity1.Id, CycleNumber = 2, DrawnAt = now.AddDays(-2) },

                new DrawHistory { StudentId = students2[3].Id, GroupId = group2.Id, ActivityId = activity2.Id, CycleNumber = 1, DrawnAt = now.AddDays(-7) },
                new DrawHistory { StudentId = students2[0].Id, GroupId = group2.Id, ActivityId = activity2.Id, CycleNumber = 1, DrawnAt = now.AddDays(-6) },
                new DrawHistory { StudentId = students2[1].Id, GroupId = group2.Id, ActivityId = activity2.Id, CycleNumber = 1, DrawnAt = now.AddDays(-5) }
            };
            context.DrawHistories.AddRange(drawHistories);
            await context.SaveChangesAsync();

            // ── Presentation Students (linked to presentation tasks) ──
            var presentationStudents = new[]
            {
                // Activity 1 – task "Analýza požiadaviek" (presentation)
                new PresentationStudent { TaskItemId = tasks1[0].Id, StudentId = students1[0].Id },
                new PresentationStudent { TaskItemId = tasks1[0].Id, StudentId = students1[1].Id },
                // Activity 1 – task "Testovanie a dokumentácia" (presentation)
                new PresentationStudent { TaskItemId = tasks1[4].Id, StudentId = students1[3].Id },
                new PresentationStudent { TaskItemId = tasks1[4].Id, StudentId = students1[4].Id },
                // Activity 2 – task "Špecifikácia projektu" (presentation)
                new PresentationStudent { TaskItemId = tasks2[0].Id, StudentId = students2[0].Id },
                // Activity 2 – task "Finálna prezentácia" (presentation)
                new PresentationStudent { TaskItemId = tasks2[2].Id, StudentId = students2[3].Id },
                new PresentationStudent { TaskItemId = tasks2[2].Id, StudentId = students2[4].Id }
            };
            context.PresentationStudents.AddRange(presentationStudents);
            await context.SaveChangesAsync();

            // ── Activity Attributes & Options ──
            var attrStatus = new ActivityAttribute { Name = "Stav projektu", ActivityId = activity1.Id };
            var attrLanguage = new ActivityAttribute { Name = "Programovací jazyk", ActivityId = activity1.Id };
            var attrTeamRole = new ActivityAttribute { Name = "Rola v tíme", ActivityId = activity2.Id };

            context.ActivityAttributes.AddRange(attrStatus, attrLanguage, attrTeamRole);
            await context.SaveChangesAsync();

            // Options for "Stav projektu"
            var optNotStarted = new ActivityAttributeOption { Name = "Nezačaté", Color = "secondary", ActivityAttributeId = attrStatus.Id };
            var optInProgress = new ActivityAttributeOption { Name = "Rozpracované", Color = "warning", ActivityAttributeId = attrStatus.Id };
            var optDone = new ActivityAttributeOption { Name = "Dokončené", Color = "success", ActivityAttributeId = attrStatus.Id };
            var optLate = new ActivityAttributeOption { Name = "Oneskorené", Color = "danger", ActivityAttributeId = attrStatus.Id };

            // Options for "Programovací jazyk"
            var optCsharp = new ActivityAttributeOption { Name = "C#", Color = "primary", ActivityAttributeId = attrLanguage.Id };
            var optPython = new ActivityAttributeOption { Name = "Python", Color = "info", ActivityAttributeId = attrLanguage.Id };
            var optJava = new ActivityAttributeOption { Name = "Java", Color = "warning", ActivityAttributeId = attrLanguage.Id };

            // Options for "Rola v tíme"
            var optLeader = new ActivityAttributeOption { Name = "Vedúci", Color = "primary", ActivityAttributeId = attrTeamRole.Id };
            var optDeveloper = new ActivityAttributeOption { Name = "Vývojár", Color = "info", ActivityAttributeId = attrTeamRole.Id };
            var optTester = new ActivityAttributeOption { Name = "Tester", Color = "success", ActivityAttributeId = attrTeamRole.Id };

            context.ActivityAttributeOptions.AddRange(
                optNotStarted, optInProgress, optDone, optLate,
                optCsharp, optPython, optJava,
                optLeader, optDeveloper, optTester);
            await context.SaveChangesAsync();

            // ── Student Attribute Values ──
            var studentAttrValues = new[]
            {
                // "Stav projektu" for group 1 students
                new StudentAttributeValue { StudentId = students1[0].Id, ActivityAttributeId = attrStatus.Id, OptionId = optDone.Id },
                new StudentAttributeValue { StudentId = students1[1].Id, ActivityAttributeId = attrStatus.Id, OptionId = optDone.Id },
                new StudentAttributeValue { StudentId = students1[2].Id, ActivityAttributeId = attrStatus.Id, OptionId = optInProgress.Id },
                new StudentAttributeValue { StudentId = students1[3].Id, ActivityAttributeId = attrStatus.Id, OptionId = optLate.Id },
                new StudentAttributeValue { StudentId = students1[4].Id, ActivityAttributeId = attrStatus.Id, OptionId = optDone.Id },

                // "Programovací jazyk" for group 1 students
                new StudentAttributeValue { StudentId = students1[0].Id, ActivityAttributeId = attrLanguage.Id, OptionId = optCsharp.Id },
                new StudentAttributeValue { StudentId = students1[1].Id, ActivityAttributeId = attrLanguage.Id, OptionId = optCsharp.Id },
                new StudentAttributeValue { StudentId = students1[2].Id, ActivityAttributeId = attrLanguage.Id, OptionId = optPython.Id },
                new StudentAttributeValue { StudentId = students1[3].Id, ActivityAttributeId = attrLanguage.Id, OptionId = optJava.Id },
                new StudentAttributeValue { StudentId = students1[4].Id, ActivityAttributeId = attrLanguage.Id, OptionId = optPython.Id },

                // "Rola v tíme" for group 2 students
                new StudentAttributeValue { StudentId = students2[0].Id, ActivityAttributeId = attrTeamRole.Id, OptionId = optLeader.Id },
                new StudentAttributeValue { StudentId = students2[1].Id, ActivityAttributeId = attrTeamRole.Id, OptionId = optDeveloper.Id },
                new StudentAttributeValue { StudentId = students2[2].Id, ActivityAttributeId = attrTeamRole.Id, OptionId = optDeveloper.Id },
                new StudentAttributeValue { StudentId = students2[3].Id, ActivityAttributeId = attrTeamRole.Id, OptionId = optTester.Id },
                new StudentAttributeValue { StudentId = students2[4].Id, ActivityAttributeId = attrTeamRole.Id, OptionId = optTester.Id }
            };
            context.StudentAttributeValues.AddRange(studentAttrValues);
            await context.SaveChangesAsync();
        }
    }
}
