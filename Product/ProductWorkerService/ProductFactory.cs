using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using ProductGrpc.Protos;

namespace ProductWorkerService
{
    public class ProductFactory
    {
        private readonly ILogger<ProductFactory> _logger;
        private readonly IConfiguration _configuration;
        public ProductFactory(ILogger<ProductFactory> logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
        public Task<AddProductRequest> Generate()
        {
            var productName = $"{_configuration.GetValue<string>("WorkerService:ProductName")}_{DateTimeOffset.Now}";            
            var productRequest = new AddProductRequest{
                Product = new ProductModel {
                    Name = productName,
                    Description = $"{productName}_Description",
                    Price = new Random().Next(1000),
                    Status = ProductStatus.Instock,
                    CreatedTime = Timestamp.FromDateTime(DateTime.UtcNow)
                }
            };
            return Task.FromResult(productRequest);
        }
    }
}