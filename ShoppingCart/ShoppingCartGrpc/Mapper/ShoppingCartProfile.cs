using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using ShoppingCartGrpc.Protos;

namespace ShoppingCartGrpc.Mapper
{
    public class ShoppingCartProfile : Profile
    {
        public ShoppingCartProfile()
        {
            CreateMap<Models.ShoppingCart, ShoppingCartModel>().ReverseMap();

            CreateMap<ShoppingCartModel, Models.ShoppingCart>().ReverseMap();

        }
    }
}
