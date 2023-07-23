using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using ProductGrpc.Protos;

namespace ProductWorkerService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly ProductFactory _factory;

    public Worker(ILogger<Worker> logger, IConfiguration configuration, ProductFactory factory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Waiting for servier is running..");
        Thread.Sleep(2000);
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            using var channel = GrpcChannel.ForAddress(_configuration.GetValue<string>("WorkerService:ServerUrl")!);    
            var client = new ProductProtoService.ProductProtoServiceClient(channel);

            _logger.LogInformation("AddProductAsync started..");
            var addProductResponse = await client.AddProductAsync(await _factory.Generate());
            _logger.LogInformation("AddProduct Response: {product}", addProductResponse.ToString());

            await Task.Delay(_configuration.GetValue<int>("WorkerService:TaskInterval"), 
                stoppingToken);
        }
    }
}
