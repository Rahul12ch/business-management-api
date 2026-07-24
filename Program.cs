using client.Data;
using client.Models;
using Resend;
using System.Net.Http.Headers;
using client.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddResponseCompression(options => { options.EnableForHttps = true; });

builder.Services.AddOptions<EmailSettings>() .Bind(builder.Configuration.GetSection("Email"))
    .ValidateDataAnnotations() .ValidateOnStart();
builder.Services .AddOptions<SupabaseSettings>() .Bind(builder.Configuration.GetSection("Supabase"))
    .ValidateOnStart();
builder.Services.AddHttpClient("Supabase", (serviceProvider, client) =>
{ var config = serviceProvider .GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri( $"{config["Supabase:Url"]}/storage/v1/");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue( "Bearer", config["Supabase:ServiceRoleKey"]);
    client.DefaultRequestHeaders.Add( "apikey", config["Supabase:ServiceRoleKey"]);
});
var apiKey = builder.Configuration["Email:ApiKey"];

Console.WriteLine($"ApiKey length: {apiKey?.Length}");
Console.WriteLine($"Starts with re_: {apiKey?.StartsWith("re_")}");
Console.WriteLine($"Contains CR: {apiKey?.Contains('\r')}");
Console.WriteLine($"Contains LF: {apiKey?.Contains('\n')}");

builder.Services.AddResend(options =>
{
    options.ApiToken = apiKey!;
});
builder.Services.AddScoped<EmailSender>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<SupabaseStorageService>();
builder.Services.AddHostedService<ReminderService>();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services .AddAuthentication( JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"], ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey( Encoding.UTF8.GetBytes( builder.Configuration["Jwt:Key"]!))
    };
});
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy( "AllowAngular", policy =>{
        policy.WithOrigins( "http://localhost:4200",
                "https://business-management-ui.vercel.app" )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();