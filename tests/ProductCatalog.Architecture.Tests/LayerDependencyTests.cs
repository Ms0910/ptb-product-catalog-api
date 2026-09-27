using System.Reflection;
using NetArchTest.Rules;
using ProductCatalog.Application;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Architecture.Tests;

/// <summary>
/// Enforces the Clean Architecture dependency rule: dependencies only point inwards.
/// </summary>
public sealed class LayerDependencyTests
{
    private static readonly Assembly Domain = typeof(Product).Assembly;
    private static readonly Assembly Application = typeof(DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void Domain_DependsOnNothingElse()
    {
        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny("ProductCatalog.Application", "ProductCatalog.Infrastructure", "ProductCatalog.Api", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrPresentation()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny("ProductCatalog.Infrastructure", "ProductCatalog.Api", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Npgsql")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnPresentation()
    {
        var result = Types.InAssembly(Infrastructure)
            .ShouldNot()
            .HaveDependencyOn("ProductCatalog.Api")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Controllers_DependOnApplicationServicesNotOnInfrastructure()
    {
        var result = Types.InAssembly(typeof(Program).Assembly)
            .That().ResideInNamespace("ProductCatalog.Api.Controllers")
            .ShouldNot()
            .HaveDependencyOnAny("ProductCatalog.Infrastructure", "Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    private static string Describe(TestResult result) =>
        "Violating types: " + string.Join(", ", result.FailingTypeNames ?? []);
}
