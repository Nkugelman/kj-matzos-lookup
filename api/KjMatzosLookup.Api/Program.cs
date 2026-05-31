using KjMatzosLookup.Application;
using KjMatzosLookup.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Clean Architecture composition root: wire the application + infrastructure layers.
builder.Services.AddKjMatzosApplication(builder.Configuration);
builder.Services.AddKjMatzosInfrastructure(builder.Configuration);

builder.Services.AddControllers()
    .AddNewtonsoftJson();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Kiosk", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Kiosk");
app.MapControllers();
app.Run();
