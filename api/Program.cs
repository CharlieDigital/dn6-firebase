using Google.Cloud.Firestore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(options =>
{
    options.AllowAnyHeader();
    options.AllowAnyMethod();
    options.AllowAnyOrigin();
});

// app.UseHttpsRedirection();

var projectId = "dn6-firebase-demo";

app.MapGet("/city/add/{state}/{name}",
    async (string state, string name) =>
    {
        FirestoreDb db = new FirestoreDbBuilder
        {
            ProjectId = projectId,
            EmulatorDetection = Google.Api.Gax.EmulatorDetection.EmulatorOrProduction

        }.Build();

        var collection = db.Collection("cities");
        await collection.Document(Guid.NewGuid().ToString("N")).SetAsync(
            new City (name, state)
        );
    })
    .WithName("AddCity");

app.Run();

/// <summary>
/// Record class that represents a city.
/// </summary>
[FirestoreData]
public record City(
    [property: FirestoreProperty] string Name,
    [property: FirestoreProperty] string State
)
{
    /// <summary>
    /// The Google APIs require a default, parameterless constructor to work.
    /// </summary>
    public City() : this("", "") { }
}