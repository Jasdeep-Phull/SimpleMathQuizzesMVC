using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SimpleMathQuizzes.CustomAuthorization;
using SimpleMathQuizzes.Data;
using SimpleMathQuizzes.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    // this app uses a Postgresql database, and uses the NpgSql EF Core provider
    options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// add identity
builder.Services.AddDefaultIdentity<User>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;

    options.User.RequireUniqueEmail = true;
	
	options.Lockout.MaxFailedAccessAttempts = 3;
	options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(2);
})
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

// add authentication
builder.Services.AddAuthentication();

builder.Services.AddAuthorizationBuilder()
    // all pages require authentication
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build())
    /* this policy ensures that only authorized users can access (read, edit, delete) quizzes
     * currently the only authorized user for a quiz is the quiz creator
     */
    .AddPolicy("CanAccessQuiz", policy =>
    {
        policy.Requirements.Add(new CanAccessQuizRequirement());
    });
// add authorization handler for the "CanAccessQuiz" requirement to the service container
builder.Services.AddScoped<IAuthorizationHandler, IsQuizCreatorAuthorizationHandler>();


// This method of adding timestamps is deprecated, but it is a quick and easy way to add a timestamp to logs
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.TimestampFormat = "[dd/MM/yy HH:mm:ss:fff]";
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();