using Generador.CharmelCodeIA.Application;
using Generador.CharmelCodeIA.Infrastructure;
using Generador.CharmelCodeIA.Infrastructure.AI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure();
builder.Services.AddApplication();

var aiSection = builder.Configuration.GetSection("AI");
if (aiSection.Exists())
{
    builder.Services.AddSemanticKernel(config =>
    {
        config.Provider = aiSection["Provider"] ?? "DeepSeek";
        config.ModelId = aiSection["ModelId"] ?? "deepseek-chat";
        config.ApiKey = aiSection["ApiKey"] ?? string.Empty;
        var endpoint = aiSection["Endpoint"];
        if (!string.IsNullOrEmpty(endpoint))
            config.Endpoint = new Uri(endpoint);
    });
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
app.UseSwagger();
app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();

app.Run();
