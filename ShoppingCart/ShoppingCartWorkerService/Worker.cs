using Grpc.Core;
using Grpc.Net.Client;
using IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using ProductGrpc.Protos;
using ShoppingCartGrpc.Protos;

namespace ShoppingCartWorkerService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;

    public Worker(ILogger<Worker> logger,IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Waiting for servier is running..");
        Thread.Sleep(2000);
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            using var scChannel = GrpcChannel
                .ForAddress(_configuration.GetValue<string>("WorkerService:ShoppingCartServerUrl")!);
            var scClient = new ShoppingCartProtoService.ShoppingCartProtoServiceClient(scChannel);

            var token = await GetTokenFromIS4();

            var scModel = await GetOrCreateShoppingCartAsync(scClient, token);
            using var scClientStream = scClient.AddItemIntoShoppingCart();
            using var productChannel = GrpcChannel
                .ForAddress(_configuration.GetValue<string>("WorkerService:ProductServerUrl")!);
            var productClient = new ProductProtoService.ProductProtoServiceClient(productChannel);
            _logger.LogInformation("GetAllProducts started..");
            using var clientData = productClient.GetAllProducts(new GetAllProductsRequest());
            await foreach (var responseData in clientData.ResponseStream.ReadAllAsync())
            {
                _logger.LogInformation("GetAllProducts Stream Response: {responseData}", responseData);
                var addNewScItem = new AddItemIntoShoppingCartRequest
                {
                    UserName = _configuration.GetValue<string>("WorkerService:UserName"),
                    DiscountCode = "CODE_100",
                    NewCartItem = new ShoppingCartItemModel
                    {
                        ProductId = responseData.ProductId,
                        ProductName = responseData.Name,
                        Price = responseData.Price,
                        Color = "Black",
                        Quantity = 1,
                    }
                };
                await scClientStream.RequestStream.WriteAsync(addNewScItem);
                _logger.LogInformation("ShoppingCart Client Stream Added New Item : {addNewScItem}", addNewScItem);
            }
            await scClientStream.RequestStream.CompleteAsync();
            var addItemIntoShoppingCartResponse = await scClientStream;
            _logger.LogInformation("AddItemIntoShoppingCart Client Stream Response: {addItemIntoShoppingCartResponse}", addItemIntoShoppingCartResponse);
            await Task.Delay(_configuration.GetValue<int>("WorkerService:TaskInterval"), stoppingToken);
        }
    }

    private async Task<string> GetTokenFromIS4()
    {
        var client = new HttpClient();
        var disco = await client.GetDiscoveryDocumentAsync
            (_configuration.GetValue<string>("WorkerService:IdentityServerUrl")!);
        if (disco.IsError)
        {
            Console.WriteLine(disco.Error);
            return string.Empty;
        }
        var tokenResponse = await client.RequestClientCredentialsTokenAsync
            (new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = "ShoppingCartClient",
                ClientSecret = "secret",
                Scope = "ShoppingCartAPI",
            });
        if(tokenResponse.IsError) 
        {
            Console.WriteLine(tokenResponse.Error);
            return string.Empty;
        }
        return tokenResponse.AccessToken!;
    }

    private async Task<ShoppingCartModel> GetOrCreateShoppingCartAsync(
            ShoppingCartProtoService.ShoppingCartProtoServiceClient scClient, string token)
    {
        ShoppingCartModel shoppingCartModel;
        try
        {
            _logger.LogInformation("GetShoppingCartAsync started..");

            var headers = new Metadata();
            headers.Add("Authorization", $"Bearer {token}");

            shoppingCartModel = await scClient.GetShoppingCartAsync
                (new GetShoppingCartRequest { UserName = _configuration.GetValue<string>("WorkerService:UserName") }
                ,headers);
            _logger.LogInformation("GetShoppingCartAsync Response : {ShoppingCartModel}", shoppingCartModel);
        }
        catch (RpcException exception)
        {
            if(exception.StatusCode == StatusCode.NotFound) 
            {
                _logger.LogInformation("CreateShoppingCartAsync started..");
                shoppingCartModel = await scClient.CreateShoppingCartAsync
                    (new ShoppingCartModel { UserName = _configuration.GetValue<string>("WorkerService:UserName") });
                _logger.LogInformation("CreateShoppingCartAsync Response : {ShoppingCartModel}", shoppingCartModel);
            }
            else
            {
                throw exception;
            }
        }
        return shoppingCartModel;
    }
}
