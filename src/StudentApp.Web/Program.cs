using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using StudentApp.Web.Data;
using StudentApp.Web.Models.Entities;
using StudentApp.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Configure login path
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// MVC with global [Authorize] filter
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser().Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

// Session
var sessionTimeoutHours = builder.Configuration.GetValue<int>("AppSettings:SessionTimeoutHours", 8);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(sessionTimeoutHours);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Register services
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IEvaluationService, EvaluationService>();
builder.Services.AddScoped<IActivityAttributeService, ActivityAttributeService>();
builder.Services.AddScoped<IDrawService, DrawService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<ICustomExportService, CustomExportService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();

var app = builder.Build();

// Seed data
await SeedData.InitializeAsync(app);

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.UseStatusCodePages(async ctx =>
{
    if (ctx.HttpContext.Response.StatusCode == 404)
    {
        var isAuthenticated = ctx.HttpContext.User.Identity?.IsAuthenticated == true;
        var redirectUrl = isAuthenticated ? "/Groups" : "/Account/Login";
        ctx.HttpContext.Response.Redirect(redirectUrl);
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Groups}/{action=Index}/{id?}");

app.Run();
