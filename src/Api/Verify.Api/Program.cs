using Newtonsoft.Json;
using Verify.Api.Middleware;
using Verify.Infrastructure.Utilities;

var builder = WebApplication.CreateBuilder(args);

string corsOpenPolicy = "OpenCORSPolicy";

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddCors(options =>
{
    options.AddPolicy(
      name: corsOpenPolicy,
      corsPolicyBuilder => {
          corsPolicyBuilder
          .AllowAnyOrigin()
          .AllowAnyHeader()
          .AllowAnyMethod();
      });
});

builder.Services.AddHttpClient();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
       
    });

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
app.UseCors(corsOpenPolicy);
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
