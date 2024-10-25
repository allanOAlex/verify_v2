using Microsoft.AspNetCore.Diagnostics;
using Verify.Api.Middleware;
using Verify.Infrastructure.Utilities;

var builder = WebApplication.CreateBuilder(args);

string CORSOpenPolicy = "OpenCORSPolicy";

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddCors(options =>
{
    options.AddPolicy(
      name: CORSOpenPolicy,
      builder => {
          builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
      });
});

//TODO - ConfigureHttpClientFactory
builder.Services.AddHttpClient("Node", client => 
{
    //client.BaseAddress = new Uri("https://localhost:7260/");
    client.Timeout = TimeSpan.FromSeconds(500);
    
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var shouldSeedDatabase = builder.Configuration.GetValue<bool>("SeedDatabase");

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

}

app.UseExceptionHandler((_ => { }));
app.UseHsts();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors(CORSOpenPolicy);
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
