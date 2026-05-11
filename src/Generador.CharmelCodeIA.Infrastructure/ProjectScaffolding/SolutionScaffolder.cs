using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Enums;
using Generador.CharmelCodeIA.Domain.Interfaces;
using Generador.CharmelCodeIA.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Generador.CharmelCodeIA.Infrastructure.ProjectScaffolding;

public sealed class SolutionScaffolder
{
    private readonly ILogger<SolutionScaffolder> _logger;
    private static readonly string[] StandardLayerNames = { "Domain", "Application", "Infrastructure", "WebApi" };

    public SolutionScaffolder(ILogger<SolutionScaffolder> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> ScaffoldAsync(
        ProjectConfiguration config, CancellationToken ct = default)
    {
        var createdPaths = new List<string>();
        var naming = config.Naming;

        var srcDir = Path.Combine(config.OutputPath, naming.SolutionName, "src");
        var testsDir = Path.Combine(config.OutputPath, naming.SolutionName, "tests");
        var solutionPath = Path.Combine(config.OutputPath, naming.SolutionName, $"{naming.SolutionName}.sln");

        // Solution file
        if (!File.Exists(solutionPath))
        {
            await CreateSolutionFileAsync(solutionPath, ct);
            createdPaths.Add(solutionPath);
        }

        // Source projects
        foreach (var layer in config.LayersToGenerate.Distinct())
        {
            if (layer == ProjectLayer.Presentation || layer == ProjectLayer.Shared) continue;

            var projectDir = Path.Combine(srcDir, naming.GetProjectName(layer));
            Directory.CreateDirectory(projectDir);

            var csprojPath = Path.Combine(projectDir, $"{naming.GetProjectName(layer)}.csproj");
            if (!File.Exists(csprojPath))
            {
                var csprojContent = GenerateCsproj(naming, layer, config);
                await File.WriteAllTextAsync(csprojPath, csprojContent, ct);
                createdPaths.Add(csprojPath);
            }
        }

        // Tests project
        if (config.GenerateUnitTestsProject)
        {
            var testDir = Path.Combine(testsDir, $"{naming.SolutionName}.Tests");
            Directory.CreateDirectory(testDir);
            var testCsprojPath = Path.Combine(testDir, $"{naming.SolutionName}.Tests.csproj");
            if (!File.Exists(testCsprojPath))
            {
                var testCsproj = GenerateTestCsproj(naming, config);
                await File.WriteAllTextAsync(testCsprojPath, testCsproj, ct);
                createdPaths.Add(testCsprojPath);
            }
        }

        // .gitignore
        var gitignorePath = Path.Combine(config.OutputPath, naming.SolutionName, ".gitignore");
        if (!File.Exists(gitignorePath))
        {
            await File.WriteAllTextAsync(gitignorePath, GetGitignoreContent(), ct);
            createdPaths.Add(gitignorePath);
        }

        _logger.LogInformation("Solution scaffolded: {ProjectCount} projects in {Path}",
            createdPaths.Count, solutionPath);

        return createdPaths;
    }

    private static string GenerateCsproj(NamingConvention naming, ProjectLayer layer, ProjectConfiguration config)
    {
        var projectName = naming.GetProjectName(layer);

        return layer switch
        {
            ProjectLayer.Domain =>
                $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>{config.TargetFramework}</TargetFramework>
                    <RootNamespace>{naming.GetRootNamespace(layer)}</RootNamespace>
                    <Nullable>enable</Nullable>
                    <ImplicitUsings>enable</ImplicitUsings>
                  </PropertyGroup>
                </Project>
                """,

            ProjectLayer.Application =>
                $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>{config.TargetFramework}</TargetFramework>
                    <RootNamespace>{naming.GetRootNamespace(layer)}</RootNamespace>
                    <Nullable>enable</Nullable>
                    <ImplicitUsings>enable</ImplicitUsings>
                  </PropertyGroup>
                  <ItemGroup>
                    <ProjectReference Include="..\{naming.GetProjectName(ProjectLayer.Domain)}\{naming.GetProjectName(ProjectLayer.Domain)}.csproj" />
                  </ItemGroup>
                  {(config.IncludeMediatR ? """
                  <ItemGroup>
                    <PackageReference Include="MediatR" Version="*" />
                  </ItemGroup>
                  """ : "")}
                  {(config.IncludeFluentValidation ? """
                  <ItemGroup>
                    <PackageReference Include="FluentValidation" Version="*" />
                    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="*" />
                  </ItemGroup>
                  """ : "")}
                </Project>
                """,

            ProjectLayer.Infrastructure =>
                $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>{config.TargetFramework}</TargetFramework>
                    <RootNamespace>{naming.GetRootNamespace(layer)}</RootNamespace>
                    <Nullable>enable</Nullable>
                    <ImplicitUsings>enable</ImplicitUsings>
                  </PropertyGroup>
                  <ItemGroup>
                    <ProjectReference Include="..\{naming.GetProjectName(ProjectLayer.Domain)}\{naming.GetProjectName(ProjectLayer.Domain)}.csproj" />
                    <ProjectReference Include="..\{naming.GetProjectName(ProjectLayer.Application)}\{naming.GetProjectName(ProjectLayer.Application)}.csproj" />
                  </ItemGroup>
                </Project>
                """,

            ProjectLayer.WebApi =>
                $"""
                <Project Sdk="Microsoft.NET.Sdk.Web">
                  <PropertyGroup>
                    <TargetFramework>{config.TargetFramework}</TargetFramework>
                    <RootNamespace>{naming.GetRootNamespace(layer)}</RootNamespace>
                    <Nullable>enable</Nullable>
                    <ImplicitUsings>enable</ImplicitUsings>
                  </PropertyGroup>
                  <ItemGroup>
                    <ProjectReference Include="..\{naming.GetProjectName(ProjectLayer.Infrastructure)}\{naming.GetProjectName(ProjectLayer.Infrastructure)}.csproj" />
                  </ItemGroup>
                  {(config.IncludeSwagger ? """
                  <ItemGroup>
                    <PackageReference Include="Swashbuckle.AspNetCore" Version="*" />
                  </ItemGroup>
                  """ : "")}
                </Project>
                """,

            _ => $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>{config.TargetFramework}</TargetFramework>
                    <RootNamespace>{naming.GetRootNamespace(layer)}</RootNamespace>
                    <Nullable>enable</Nullable>
                    <ImplicitUsings>enable</ImplicitUsings>
                  </PropertyGroup>
                </Project>
                """
        };
    }

    private static string GenerateTestCsproj(NamingConvention naming, ProjectConfiguration config) =>
        $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>{config.TargetFramework}</TargetFramework>
            <RootNamespace>{naming.SolutionName}.Tests</RootNamespace>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
            <IsPackable>false</IsPackable>
          </PropertyGroup>
          <ItemGroup>
            <ProjectReference Include="..\src\{naming.GetProjectName(ProjectLayer.Application)}\{naming.GetProjectName(ProjectLayer.Application)}.csproj" />
          </ItemGroup>
          <ItemGroup>
            <PackageReference Include="Microsoft.NET.Test.Sdk" Version="*" />
            <PackageReference Include="xunit" Version="*" />
            <PackageReference Include="xunit.runner.visualstudio" Version="*" />
          </ItemGroup>
        </Project>
        """;

    private static async Task CreateSolutionFileAsync(string solutionPath, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(solutionPath)!;
        var solutionName = Path.GetFileNameWithoutExtension(solutionPath);

        var content = $$"""
            
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            VisualStudioVersion = 17.0.31903.59
            MinimumVisualStudioVersion = 10.0.40219.1
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "{{solutionName}}.Domain", "src\{{solutionName}}.Domain\{{solutionName}}.Domain.csproj", "{1}"
            EndProject
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "{{solutionName}}.Application", "src\{{solutionName}}.Application\{{solutionName}}.Application.csproj", "{2}"
            EndProject
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "{{solutionName}}.Infrastructure", "src\{{solutionName}}.Infrastructure\{{solutionName}}.Infrastructure.csproj", "{3}"
            EndProject
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "{{solutionName}}.WebApi", "src\{{solutionName}}.WebApi\{{solutionName}}.WebApi.csproj", "{4}"
            EndProject
            Global
                GlobalSection(SolutionConfigurationPlatforms) = preSolution
                    Debug|Any CPU = Debug|Any CPU
                    Release|Any CPU = Release|Any CPU
                EndGlobalSection
                GlobalSection(ProjectConfigurationPlatforms) = postSolution
                    {1}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                    {1}.Debug|Any CPU.Build.0 = Debug|Any CPU
                    {1}.Release|Any CPU.ActiveCfg = Release|Any CPU
                    {1}.Release|Any CPU.Build.0 = Release|Any CPU
                    {2}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                    {2}.Debug|Any CPU.Build.0 = Debug|Any CPU
                    {2}.Release|Any CPU.ActiveCfg = Release|Any CPU
                    {2}.Release|Any CPU.Build.0 = Release|Any CPU
                    {3}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                    {3}.Debug|Any CPU.Build.0 = Debug|Any CPU
                    {3}.Release|Any CPU.ActiveCfg = Release|Any CPU
                    {3}.Release|Any CPU.Build.0 = Release|Any CPU
                    {4}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                    {4}.Debug|Any CPU.Build.0 = Debug|Any CPU
                    {4}.Release|Any CPU.ActiveCfg = Release|Any CPU
                    {4}.Release|Any CPU.Build.0 = Release|Any CPU
                EndGlobalSection
            EndGlobal
            """;

        await File.WriteAllTextAsync(solutionPath, content, ct);
    }

    private static string GetGitignoreContent() =>
        """
        ## .NET
        bin/
        obj/
        *.user
        *.suo
        *.cache
        *.vs/
        .idea/
        *.DotSettings.user

        ## NuGet
        **/packages/*
        !**/packages/build/
        *.nupkg

        ## Visual Studio
        .vscode/
        *.dbmdl
        *.jfm

        ## App settings
        appsettings.*.json
        !appsettings.Development.json
        """;
}
