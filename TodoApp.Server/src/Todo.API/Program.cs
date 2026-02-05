using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Todo.API;
using Todo.Models.Data;
using Todo.Models.Entities;
using Quartz;
using Todo.Services.Implementations;
using Todo.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDatabaseConfiguration(builder.Configuration);

builder.Services.AddApplicationServices().AddRepositories();
builder.Services.AddQuartzConfiguration();
builder.Services.AddRedisCache(builder.Configuration);

builder.Services.AddSwaggerConfiguration();

builder.Services.AddCorsConfiguration(builder.Configuration);

builder.Services.AddIdentityConfiguration();

builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
