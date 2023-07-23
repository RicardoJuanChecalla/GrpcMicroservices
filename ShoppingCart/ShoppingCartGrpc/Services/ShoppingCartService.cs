using AutoMapper;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ShoppingCartGrpc.Data;
using ShoppingCartGrpc.Models;
using ShoppingCartGrpc.Protos;

namespace ShoppingCartGrpc.Services
{
    [Authorize]
    public class ShoppingCartService : ShoppingCartProtoService.ShoppingCartProtoServiceBase 
    {
        private readonly ShoppingCartContext _shoppingCartDbContext;
        private readonly DiscountService _discountService;
        private readonly ILogger<ShoppingCartService> _logger;
        private readonly IMapper _mapper;
        
        public ShoppingCartService(ShoppingCartContext shoppingCartDbContext, DiscountService discountService, 
            IMapper mapper, ILogger<ShoppingCartService> logger)
        {
            _shoppingCartDbContext = shoppingCartDbContext ?? throw new ArgumentNullException(nameof(shoppingCartDbContext));
            _discountService = discountService ?? throw new ArgumentNullException(nameof(discountService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));   
        }

        public override async Task<ShoppingCartModel> GetShoppingCart(GetShoppingCartRequest request, 
            ServerCallContext context)
        {
            var shoppingCart = await _shoppingCartDbContext.ShoppingCarts
                .FirstOrDefaultAsync(s => s.UserName == request.UserName);
            if ( shoppingCart == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound,
                    $"ShoppingCart with UserName={request.UserName} is not found"));
            }
            var shoppingCartModel = _mapper.Map<ShoppingCartModel>(shoppingCart);
            return shoppingCartModel;
        }

        public override async Task<ShoppingCartModel> CreateShoppingCart(ShoppingCartModel request, 
            ServerCallContext context)
        {
            var shoppingCart = _mapper.Map<ShoppingCart>(request);
            var isExist = await _shoppingCartDbContext.ShoppingCarts
                .AnyAsync(s => s.UserName == shoppingCart.UserName);
            if ( isExist)
            {
                _logger.LogError("Invalid UserName for ShoppingCart creation. UserName:{userName}", 
                    shoppingCart.UserName);
                throw new RpcException(new Status(StatusCode.NotFound, 
                    $"ShoppingCart with UserName={request.UserName} is alredy exist."));
            }
            _shoppingCartDbContext.ShoppingCarts.Add(shoppingCart);
            await _shoppingCartDbContext.SaveChangesAsync();
            _logger.LogInformation("ShoppingCart is successfully created. UserName : {userName}", 
                shoppingCart.UserName);
            var shoppingCartModel = _mapper.Map<ShoppingCartModel>(shoppingCart);
            return shoppingCartModel;
        }

        [AllowAnonymous]
        public override async Task<RemovetemIntoShoppingCartResponse> RemovetemIntoShoppingCart
            (RemovetemIntoShoppingCartRequest request, ServerCallContext context)
        {
            var shoppingCart = await _shoppingCartDbContext.ShoppingCarts
                .FirstOrDefaultAsync(s => s.UserName == request.UserName);
            if (shoppingCart == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound,
                    $"ShoppingCart with UserName={request.UserName} is not found"));
            }
            var removeCartItem = shoppingCart.Items
                .FirstOrDefault(i => i.ProductId == request.RemoveCartItem.ProductId);
            if (removeCartItem == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound,
                    $"CartItem with ProductId={request.RemoveCartItem.ProductId} is not found"));
            }
            shoppingCart.Items.Remove(removeCartItem);
            var removeCount = await _shoppingCartDbContext.SaveChangesAsync();
            var response = new RemovetemIntoShoppingCartResponse
            {
                Success = removeCount > 0,
            };
            return response;
        }

        [AllowAnonymous]
        public override async Task<AddItemIntoShoppingCartResponse> AddItemIntoShoppingCart
            (IAsyncStreamReader<AddItemIntoShoppingCartRequest> requestStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext())
            {
                var shoppingCart = await _shoppingCartDbContext.ShoppingCarts
                    .FirstOrDefaultAsync(s => s.UserName == requestStream.Current.UserName);
                if (shoppingCart == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound,
                        $"ShoppingCart with UserName={requestStream.Current.UserName} is not found"));
                }
                var newAddedCartItem = _mapper.Map<ShoppingCartItem>(requestStream.Current.NewCartItem);
                var cartItem = shoppingCart.Items.FirstOrDefault
                    (i => i.ProductId == newAddedCartItem.ProductId);
                if (cartItem != null) 
                {
                    cartItem.Quantity++;
                }
                else
                {
                    var discount = await _discountService.GetDiscount(requestStream.Current.DiscountCode);
                    // float discount = 100;
                    newAddedCartItem.Price -= discount.Amount;
                    shoppingCart.Items.Add(newAddedCartItem);
                }
            }
            var insertCount = await _shoppingCartDbContext.SaveChangesAsync();
            var response = new AddItemIntoShoppingCartResponse
            {
                Success = insertCount > 0,
                InsertCount = insertCount,
            };
            return response;
        }


    }
}