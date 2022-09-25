using System.Security.Cryptography.X509Certificates;
using System.Text;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var projectId = "dn6-firebase-demo";
var builder = WebApplication.CreateBuilder(args);

builder.Services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options => {
    var isProduction = builder.Environment.IsProduction();
    var issuer = $"https://securetoken.google.com/{projectId}";
    options.Authority = issuer;
    options.TokenValidationParameters.ValidateIssuer = isProduction;
    options.TokenValidationParameters.ValidIssuer = issuer;
    options.TokenValidationParameters.ValidateAudience = isProduction;
    options.TokenValidationParameters.ValidAudience = projectId;
    options.TokenValidationParameters.ValidateLifetime = isProduction;
    options.TokenValidationParameters.RequireSignedTokens = isProduction;

    if (isProduction) {
      var jwtKeySetUrl = "https://www.googleapis.com/robot/v1/metadata/x509/securetoken@system.gserviceaccount.com";
      options.TokenValidationParameters.IssuerSigningKeyResolver = (s, securityToken, identifier, parameters) => {
        // get JsonWebKeySet from AWS
        var keyset = new HttpClient()
            .GetFromJsonAsync<Dictionary<string, string>>(jwtKeySetUrl).Result;

        // serialize the result
        var keys = keyset!.Values.Select(
            d => new X509SecurityKey(new X509Certificate2(Encoding.UTF8.GetBytes(d))));

        // cast the result to be the type expected by IssuerSigningKeyResolver
        return keys;
      };
    }
  });

builder.Services.AddAuthorization();
builder.Services.AddCors();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Construct our app after setting up services.
var app = builder.Build();

if (app.Environment.IsDevelopment()) {
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseCors(options => {
  options.AllowAnyHeader();
  options.AllowAnyMethod();
  options.AllowAnyOrigin();
});

app.UseAuthentication();
app.UseAuthorization();

// app.UseHttpsRedirection();

// Our one and only route.
app.MapGet("/city/add/{state}/{name}",
  [Authorize] async (string state, string name) => {
    FirestoreDb db = new FirestoreDbBuilder {
      ProjectId = projectId,
      EmulatorDetection = Google.Api.Gax.EmulatorDetection.EmulatorOrProduction
    }.Build();

    var collection = db.Collection("cities");
    await collection.Document(Guid.NewGuid().ToString("N")).SetAsync(
        new City(name, state)
    );
  }).WithName("AddCity");

// Start our app here.
app.Run();

/// <summary>
/// Record class that represents a city.
/// </summary>
[FirestoreData]
public record City(
    [property: FirestoreProperty] string Name,
    [property: FirestoreProperty] string State
) {
  /// <summary>
  /// The Google APIs require a default, parameterless constructor to work.
  /// </summary>
  public City() : this("", "") { }
}
