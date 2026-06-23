// REST

using EufoGestREST.Services;
using EufoGestREST.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// CONFIGURAZIONE
builder.Services.Configure<AppSettings>(options =>
{
    // Mappa la sezione "SettaggiGenerali" sulla proprietà corrispondente
    options.SettaggiGenerali = builder.Configuration
        .GetSection("SettaggiGenerali").Get<SettaggiGenerali>() ?? new();

    // Mappa la sezione "SettaggiWorkers" sulla proprietà "ServiziAutomatici"
    options.ServiziAutomatici = builder.Configuration
        .GetSection("SettaggiWorkers").Get<SettaggiServiziAutomatici>() ?? new();
});

builder.Services.Configure<SettaggiGenerali>(
    builder.Configuration.GetSection("SettaggiGenerali"));

builder.Services.Configure<SettaggiServiziAutomatici>(
    builder.Configuration.GetSection("SettaggiWorkers"));


// DIPENDENZE
builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddScoped<JwtService>();

//Rimosse le dipendenze ai DAL - evito eventuali problemi di dipendenze circolari - ricreo i dal quando mi servono
//builder.Services.AddScoped<AnagraficaDAL>();
//builder.Services.AddScoped<LinguaDAL>();

// SERVIZI
builder.Services.AddHostedService<AggiornaLezioniService>();


// JWT
var jwt = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwt["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            RoleClaimType = ClaimTypes.Role
        };
        /* 
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine("TOKEN RIFIUTATO: " + ctx.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                Console.WriteLine("TOKEN OK per: " + ctx.Principal?.Identity?.Name);
                return Task.CompletedTask;
            },
            OnMessageReceived = ctx =>
            {
                Console.WriteLine("TOKEN RICEVUTO: " + (ctx.Token ?? "NESSUNO"));
                return Task.CompletedTask;
            }
        };*/ //DEBUG
    });

builder.Services.AddAuthorization();

//CORS
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? throw new InvalidOperationException("AllowedOrigins non configurato.");
builder.Services.AddCors(options =>
{
    options.AddPolicy("EufoGestCorsPolicy", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});



// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(policyName: "RateLimitPolicy", opt =>
    {
        opt.PermitLimit = 5;            // Massimo 5 richieste
        opt.Window = TimeSpan.FromMinutes(1); // al minuto
        opt.QueueLimit = 0;             // Nessuna coda dopo il massimo richieste
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // Risposta personalizzata quando scatta il limite
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new { messaggio = "Troppe richieste. Riprova tra un minuto." });
    };
});


var app = builder.Build();

app.UseCors("EufoGestCorsPolicy");
app.UseRateLimiter();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
