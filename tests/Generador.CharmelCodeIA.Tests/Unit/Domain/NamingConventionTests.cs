using Generador.CharmelCodeIA.Domain.Enums;
using Generador.CharmelCodeIA.Domain.ValueObjects;

namespace Generador.CharmelCodeIA.Tests.Unit.Domain;

public sealed class NamingConventionTests
{
    [Fact]
    public void SolutionName_ReturnsCompanyDotProject()
    {
        var naming = new NamingConvention("Charmel", "ECommerce");
        Assert.Equal("Charmel.ECommerce", naming.SolutionName);
    }

    [Fact]
    public void GetProjectName_Domain_ReturnsFullName()
    {
        var naming = new NamingConvention("Charmel", "ECommerce");
        var name = naming.GetProjectName(ProjectLayer.Domain);
        Assert.Equal("Charmel.ECommerce.Domain", name);
    }

    [Fact]
    public void Sanitize_RemovesInvalidCharacters()
    {
        var naming = new NamingConvention("Charmel Corp!", "E-Commerce 2024");
        Assert.Equal("CharmelCorp", naming.CompanyName);
        Assert.Equal("ECommerce2024", naming.ProjectName);
    }

    [Fact]
    public void GetEntityNamespace_ReturnsCorrectFormat()
    {
        var naming = new NamingConvention("Charmel", "ECommerce");
        var ns = naming.GetEntityNamespace();
        Assert.Equal("Charmel.ECommerce.Domain.Entities", ns);
    }

    [Fact]
    public void GetUseCaseNamespace_ReturnsCorrectFormat()
    {
        var naming = new NamingConvention("Charmel", "ECommerce");
        var ns = naming.GetUseCaseNamespace("User", "Create");
        Assert.Equal("Charmel.ECommerce.Application.User.Create", ns);
    }

    [Fact]
    public void Equality_SameValues_ReturnsTrue()
    {
        var n1 = new NamingConvention("Charmel", "ECommerce");
        var n2 = new NamingConvention("Charmel", "ECommerce");
        Assert.Equal(n1, n2);
    }
}
