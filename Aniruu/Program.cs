using Aniruu;
using Aniruu.Database;
using Aniruu.Middleware;
using Aniruu.Utility.OAuth;
using Aniruu.Utility.Ratelimit;
using Microsoft.EntityFrameworkCore;
using Minio;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocument(c =>
{
    c.DocumentName = "v0";
    c.Title = "Aniruu API";
    c.Version = "v0.0.1";
    c.UseXmlDocumentation = true;
});

builder.Services.AddRateLimiter(lo => lo.AddPolicy<string, RateLimiter>("default"));
builder.Services.AddControllers();

#if DEBUG
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "testingPolicy",
        policyBuilder =>
        {
            policyBuilder.AllowAnyMethod();
            policyBuilder.AllowAnyOrigin();
            policyBuilder.AllowAnyHeader();
        });
});
#endif

builder.Services.AddDbContextFactory<AniruuContext>(o =>
    o.UseNpgsql(builder.Configuration["DB_CONN_STRING"]));
// .UseSnakeCaseNamingConvention()); TODO: Enable when this works in .NET 8
builder.Services.AddSingleton<OAuth2>();

builder.Services.AddSingleton<Limits>();
builder.Services.AddSingleton<IMinioClient, MinioClient>(_ =>
    new MinioClient()
        .WithEndpoint("localhost", 9000)
        .WithCredentials(
            builder.Configuration["MINIO_ACCESS_KEY"],
            builder.Configuration["MINIO_SECRET_KEY"]
        )
        .Build());

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi(c =>
    {
        c.Path = "/openapi/v0/openapi.json";
        c.DocumentName = "v0";
    });
    app.UseReDoc(c =>
    {
        c.DocumentPath = "/openapi/v0/openapi.json";
        c.Path = "/openapi/v0/redoc";
    });
}
else
{
    app.UseHttpsRedirection();
    app.UseHsts();
    app.UseForwardedHeaders();
}

app.UseRateLimiter();
app.UseMiddleware<AuthorizationMiddleware>();

#if DEBUG
app.UseCors("testingPolicy");
#endif

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.Run();
