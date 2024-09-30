using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SUIA.API.Configuration;
using SUIA.API.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(o
    => o.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddServices();
builder.Services.AddEndpoints();
builder.Services.AddProblemDetails();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(o => o.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/api/identity").MapIdentityApi<IdentityUser>().WithTags("Identity");

app.UseEndpoints();

app.Run();