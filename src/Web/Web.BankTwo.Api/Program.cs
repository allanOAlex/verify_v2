var builder = WebApplication.CreateBuilder(args);

string corsOpenPolicy = "OpenCORSPolicy";

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

builder.Services.AddHttpClient("DHT", client =>
{
    client.BaseAddress = new Uri("https://localhost:7260/");
    client.Timeout = TimeSpan.FromSeconds(1000);

});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHsts();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors(corsOpenPolicy);
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();