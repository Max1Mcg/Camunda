using Cloudstarter.Services;
using dotenv.net.DependencyInjection.Microsoft;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddEnv(builder => {
    builder
        .AddEnvFile("CamundaCloud.env")
        .AddThrowOnError(false)
        .AddEncoding(Encoding.ASCII);
});
builder.Services.AddEnvReader();
builder.Services.AddSingleton<IZeebeService, ZeebeService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
var zeebeService = app.Services.GetService<IZeebeService>();
zeebeService.Deploy("test-process.bpmn");
zeebeService.StartWorkers();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
