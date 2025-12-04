using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Data;
using System.Text;
using VelsatBackendAPI.Controllers;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Data.Services;
using VelsatBackendAPI.Hubs;
using MySqlConfiguration = VelsatBackendAPI.Data.MySqlConfiguration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Configuration.AddJsonFile("appsettings.json");

var secretkey = builder.Configuration.GetSection("settings").GetSection("secretkey").Value;
var keyBytes = Encoding.UTF8.GetBytes(secretkey);

builder.Services.AddAuthentication(config =>
{
    config.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    config.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(config =>
{
    config.RequireHttpsMetadata = false;
    config.SaveToken = true;
    config.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never;

});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var mysqlConfiguration = new MySqlConfiguration(
    builder.Configuration.GetConnectionString("DefaultConnection"),
    builder.Configuration.GetConnectionString("SecondConnection")
);
builder.Services.AddSingleton(mysqlConfiguration);

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IReadOnlyUnitOfWork, ReadOnlyUnitOfWork>();

builder.Services.AddSingleton<IUnitOfWorkFactory, UnitOfWorkFactory>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", builder =>
    {
        builder
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials()
               .SetIsOriginAllowed(origin => true);
    });
});

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
});

//Comentar SIGNALR temporal
//builder.Services.AddSignalR(o =>
//{
//    o.EnableDetailedErrors = true;
//});
//

//Descomentar para 107 - Env�o de correos

//builder.Services.AddHostedService<AlertaCorreoService>();

//HASTA AC�

var app = builder.Build();

// ✅ Limpiar todos los pools de MySQL al iniciar la aplicación
try
{
    MySql.Data.MySqlClient.MySqlConnection.ClearAllPools();
    Console.WriteLine("✅ [Startup] Pools de MySQL limpiados correctamente");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ [Startup] Error limpiando pools: {ex.Message}");
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors("AllowSpecificOrigin");

app.UseSession();

app.MapControllers();

//Comentar para SIGNALR
//app.MapHub<ActualizacionTiempoReal>("/dataHubDevice/{username}");
//

app.Run();