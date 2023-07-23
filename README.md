https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new

mkdir GrpcMicroservices
dotnet new sln

dotnet new grpc -o Product/ProductGrpc
dotnet sln add Product/ProductGrpc
dotnet add ./Product/ProductGrpc/ProductGrpc.csproj package Microsoft.EntityFrameworkCore --version 7.0.2
dotnet add ./Product/ProductGrpc/ProductGrpc.csproj package Microsoft.EntityFrameworkCore.InMemory --version 7.0.2
dotnet add ./Product/ProductGrpc/ProductGrpc.csproj package AutoMapper.Extensions.Microsoft.DependencyInjection --version 12.0.1
dotnet new console -o Product/ProductGrpcClient
dotnet sln add Product/ProductGrpcClient
dotnet new worker -o Product/ProductWorkerService
dotnet sln add Product/ProductWorkerService

dotnet new grpc -o ShoppingCart/ShoppingCartGrpc
dotnet sln add ShoppingCart/ShoppingCartGrpc
dotnet add ./ShoppingCart/ShoppingCartGrpc/ShoppingCartGrpc.csproj package Microsoft.EntityFrameworkCore --version 7.0.2
dotnet add ./ShoppingCart/ShoppingCartGrpc/ShoppingCartGrpc.csproj package Microsoft.EntityFrameworkCore.InMemory --version 7.0.2
dotnet add ./ShoppingCart/ShoppingCartGrpc/ShoppingCartGrpc.csproj package AutoMapper.Extensions.Microsoft.DependencyInjection --version 12.0.1
dotnet add ./ShoppingCart/ShoppingCartGrpc/ShoppingCartGrpc.csproj package Microsoft.AspNetCore.Authentication.JwtBearer --version 7.0.2

dotnet new worker -o ShoppingCart/ShoppingCartWorkerService
dotnet sln add ShoppingCart/ShoppingCartWorkerService
dotnet add ./ShoppingCart/ShoppingCartWorkerService/ShoppingCartWorkerService.csproj package IdentityModel --version 6.1.0

dotnet new grpc -o Discount/DiscountGrpc
dotnet sln add Discount/DiscountGrpc
dotnet add ./Discount/DiscountGrpc/DiscountGrpc.csproj package Microsoft.EntityFrameworkCore --version 7.0.2
dotnet add ./Discount/DiscountGrpc/DiscountGrpc.csproj package Microsoft.EntityFrameworkCore.InMemory --version 7.0.2
dotnet add ./Discount/DiscountGrpc/DiscountGrpc.csproj package AutoMapper.Extensions.Microsoft.DependencyInjection --version 12.0.1

dotnet new web -o Authentication/IdentityServer
dotnet sln add Authentication/IdentityServer
dotnet add ./Authentication/IdentityServer/IdentityServer.csproj package IdentityServer4 --version 4.1.2