using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<MyDbContext>(c =>
{
    c.UseSqlite("Data Source=MyDatabase.db");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

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

public class MyDbContext(DbContextOptions<MyDbContext> options) : DbContext(options)
{
    
    public DbSet<Cat> Cats { get; set; }
}