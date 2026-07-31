using System.Reflection;
using System.Text;
using MemeTokenHub.SocialService.Api;
using MemeTokenHub.SocialService.Api.Health;
using MemeTokenHub.SocialService.Api.Middleware;
using MemeTokenHub.SocialService.Application.Interfaces;
using MemeTokenHub.SocialService.Application.Services;
using MemeTokenHub.SocialService.Infrastructure.Configuration;
using MemeTokenHub.SocialService.Infrastructure.Health;
using MemeTokenHub.SocialService.Infrastructure.Messaging;
using MemeTokenHub.SocialService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MongoDB.Driver;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
MongoDbOptions mongoOptions = builder.Configuration.GetSection(MongoDbOptions.SectionName).Get<MongoDbOptions>() ?? throw new InvalidOperationException("MongoDb configuration is required.");
ServiceBusOptions serviceBusOptions = builder.Configuration.GetSection(ServiceBusOptions.SectionName).Get<ServiceBusOptions>() ?? new ServiceBusOptions();
string jwtSecret = builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey configuration is required.");

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoOptions.ConnectionString));
builder.Services.AddSingleton(provider => provider.GetRequiredService<IMongoClient>().GetDatabase(mongoOptions.DatabaseName));
builder.Services.AddScoped<IMongoOperationContext, MongoOperationContext>();
builder.Services.AddScoped<ISocialRepository, MongoSocialRepository>();
builder.Services.AddScoped<IUnitOfWork, MongoUnitOfWork>();
builder.Services.AddScoped<ISocialService, SocialService>();
builder.Services.AddSingleton<IMongoIndexInitializer, MongoIndexInitializer>();
builder.Services.AddSingleton<IndexInitializationState>();
builder.Services.AddHostedService<MongoIndexInitializerHostedService>();
builder.Services.AddSingleton(serviceBusOptions);
builder.Services.AddScoped<IEventPublisher, MongoEventOutbox>();
builder.Services.AddHostedService<OutboxDeliveryWorker>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Meme Token Hub Social Service", Version = "v1", Description = "Community interactions, sentiment, content, and reputation APIs." });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header, Description = "Enter a platform JWT." });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
    string xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFileName));
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters { ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)), ValidateIssuer = true, ValidIssuer = builder.Configuration["Jwt:Issuer"], ValidateAudience = true, ValidAudience = builder.Configuration["Jwt:Audience"], ValidateLifetime = true, ClockSkew = TimeSpan.FromMinutes(1) });
builder.Services.AddAuthorization(options => options.AddPolicy(AuthorizationPolicies.WriteSupport, policy =>
    policy.RequireAuthenticatedUser().RequireAssertion(context =>
        context.User.HasClaim("capability", AuthorizationPolicies.WriteSupport) ||
        context.User.Claims.Where(claim => claim.Type is "scope" or "capabilities").Any(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(AuthorizationPolicies.WriteSupport, StringComparer.Ordinal)))));
builder.Services.AddHealthChecks()
    .AddCheck("application", () => HealthCheckResult.Healthy("The application is running."), ["live"])
    .AddMongoDb(
        _ => new MongoClient(mongoOptions.ConnectionString),
        name: "mongodb",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "dependency", "database"],
        timeout: TimeSpan.FromSeconds(5))
    .AddCheck<ServiceBusHealthCheck>(
        "azure-service-bus",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "dependency", "messaging"],
        timeout: TimeSpan.FromSeconds(5))
    .AddCheck<IndexInitializationHealthCheck>(
        "mongodb-indexes",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "dependency", "database"]);

WebApplication app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(options => { options.SwaggerEndpoint("/swagger/v1/swagger.json", "Meme Token Hub Social Service v1"); options.RoutePrefix = "swagger"; options.DisplayRequestDuration(); });
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
    ResponseWriter = HealthCheckResponseWriter.WriteAsync,
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteAsync,
});
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = HealthCheckResponseWriter.WriteAsync,
});
app.Run();

public partial class Program;
