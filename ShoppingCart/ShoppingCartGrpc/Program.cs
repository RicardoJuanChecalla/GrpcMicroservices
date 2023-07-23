using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShoppingCartGrpc.Data;
using ShoppingCartGrpc.Services;
using DiscountGrpc.Protos;

var builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration; 

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
builder.Services.AddGrpc( opt => { opt.EnableDetailedErrors = true;});
builder.Services.AddDbContext<ShoppingCartContext>(options => 
    options.UseInMemoryDatabase("ShoppingCarts"));
builder.Services.AddGrpcClient<DiscountProtoService.DiscountProtoServiceClient>
    (o => o.Address =  new Uri(configuration["GrpcConfigs:DiscountUrl"]!));   
builder.Services.AddScoped<DiscountService>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "http://localhost:5089";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false
        };
    });
builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var shoppingCartContext = services.GetRequiredService<ShoppingCartContext>();
    ShoppingCartContextSeed.SeedAsync(shoppingCartContext);
}

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
app.MapGrpcService<ShoppingCartService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
