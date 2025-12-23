using HighAgentsBackend.Services;
using System.IO;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// Carrega arquivo .env com variáveis de ambiente
Env.Load();

// Configura logging básico no console
builder.Logging.AddConsole();

// Adiciona serviços da API
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// Registra HttpClient para serviços externos
builder.Services.AddHttpClient<OpenAIService>();
builder.Services.AddHttpClient<PineconeService>();

// Registra serviços da aplicação
builder.Services.AddScoped<AgentService>();

var app = builder.Build();

// Configura pipeline de requisições HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
