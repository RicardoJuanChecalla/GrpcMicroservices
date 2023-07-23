using Microsoft.EntityFrameworkCore;
using ProductGrpc.Protos;
using ProductGrpc.Data;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ProductGrpc.Models;
using AutoMapper;

namespace ProductGrpc.Services
{
    public class ProductService : ProductProtoService.ProductProtoServiceBase
    {
        private readonly ProductsContext _productsContext;
        private readonly ILogger<ProductService> _logger;
        private readonly IMapper _mapper;
        
        public ProductService(ProductsContext productsContext, 
            IMapper mapper, ILogger<ProductService> logger)
        {
            _productsContext = productsContext ?? throw new ArgumentNullException(nameof(productsContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public override Task<Empty> Test(Empty request, ServerCallContext context)
        {
            return base.Test(request, context);
        }

        public override async Task<ProductModel> GetProduct(GetProductRequest request, 
            ServerCallContext context)
        {
            var product = await _productsContext.Products.FindAsync(request.ProductId);
            if (product == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, 
                    $"Product with ID={request.ProductId} is not found"));
            }
            var productModel = _mapper.Map<ProductModel>(product);
            return productModel;
        }

        public override async Task GetAllProducts(GetAllProductsRequest request, 
            IServerStreamWriter<ProductModel> responseStream, ServerCallContext context)
        {
            var productList = await _productsContext.Products.ToListAsync();
            foreach (var product in productList)
            {
                var productModel = _mapper.Map<ProductModel>(product);
                await responseStream.WriteAsync(productModel);
            }
        }

        public override async Task<ProductModel> AddProduct(AddProductRequest request, 
            ServerCallContext context)
        {
            var product = _mapper.Map<Product>(request.Product);
            _productsContext.Products.Add(product);
            await _productsContext.SaveChangesAsync();
            var productModel = _mapper.Map<ProductModel>(product);
            return productModel;
        }

        public override async Task<ProductModel> UpdateProduct(UpdateProductRequest request, ServerCallContext context)
        {
            var product = _mapper.Map<Product>(request.Product);
            bool isExist = await _productsContext.Products.AnyAsync(p=>p.ProductId==product.ProductId);
            if(!isExist)
            {
                throw new RpcException(new Status(StatusCode.NotFound, 
                    $"Product with ID={product.ProductId} is not found"));
            }
            _productsContext.Entry(product).State = EntityState.Modified;
            try
            {
                await _productsContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {                
                throw;
            }
            var productModel = _mapper.Map<ProductModel>(product);
            return productModel;
        }

        public override  async Task<DeleteProductResponse> DeleteProduct(DeleteProductRequest request, ServerCallContext context)
        {
            var product = await _productsContext.Products.FindAsync(request.ProductId);
            if(product ==  null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, 
                    $"Product with ID={request.ProductId} is not found"));
            }
            _productsContext.Products.Remove(product!);
            var deleteCount = await _productsContext.SaveChangesAsync();
            var response =  new DeleteProductResponse{
                Success = deleteCount > 0
            };
            return response;
        }

        public override async Task<InsertBulkProductResponse> InsertBulkProduct(IAsyncStreamReader<ProductModel> requestStream, 
            ServerCallContext context)
        {
            while (await requestStream.MoveNext())
            {
                var product = _mapper.Map<Product>(requestStream.Current);
                _productsContext.Products.Add(product);
            }
            var insertCount = await _productsContext.SaveChangesAsync();
            var response = new InsertBulkProductResponse
            {
                Success = insertCount > 0,
                InsertCount = insertCount
            };
            return response;
        }
    }
}