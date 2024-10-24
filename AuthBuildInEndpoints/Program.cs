using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var deployingDate = DateTime.UtcNow;

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOptions<BearerTokenOptions>(IdentityConstants.BearerScheme)
    .Configure(options => { options.BearerTokenExpiration = TimeSpan.FromDays(7); });


builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<AppUser>()
    .AddEntityFrameworkStores<MyDbContext>();

builder.Services.AddDbContext<MyDbContext>(c =>
{
    c.UseNpgsql(builder.Configuration.GetConnectionString("postgresdb"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapIdentityApi<AppUser>();

app.MapGet("/health", () => Results.Ok(new
{
    DeployedOn = deployingDate,
}));

app.MapGet("/cats", (MyDbContext dbContext) =>
    {
       var result = dbContext.Cats.ToList();

       return result;
    })
    .WithName("GetCats")
    .WithOpenApi();


app.MapPost("/cats", async ([FromQuery] string name, MyDbContext dbContext) =>
    {
        var cat = new Cat
        {
            Name = name
        };

        dbContext.Cats.Add(cat);
        await dbContext.SaveChangesAsync();

        return cat;
    })
    .RequireAuthorization()
    .WithName("AddCat")
    .WithOpenApi();



using var scope = app.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<MyDbContext>();
var migrations = await context.Database.GetPendingMigrationsAsync();
await context.Database.MigrateAsync();

app.Run();


record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public class Cat
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
}

public class MyDbContext(DbContextOptions<MyDbContext> options) : IdentityDbContext<AppUser>(options)
{
    
    public DbSet<Cat> Cats { get; set; }
}

public class AppUser : IdentityUser
{
    public IEnumerable<IdentityRole>? Roles { get; set; }
}