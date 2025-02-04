﻿namespace DotNetStarter.Core.Services;

public class ProjectGeneratorService(
        ProjectArchitectureFactoryCreator projectArchitectureFactoryCreator,
        ProjectStructureBuilder structureBuilder)
{
    private readonly ProjectArchitectureFactoryCreator _factoryCreator = projectArchitectureFactoryCreator;
    private readonly ProjectStructureBuilder _builder = structureBuilder;
    //private string _csprojPath = string.Empty;

    public void CreateProject(string projectName, string architecture, string outputPath)
    {
        // Lista de etapas para exibição no console
        var steps = new List<(string StepName, string Status)>
        {
            ("Loading architecture...", "[yellow]In Progress[/]"),
            ("Building project structure...", "[yellow]In Progress[/]"),
            ($"Creating solution: {projectName}.sln", "[yellow]In Progress[/]"),
        };

        AnsiConsole.Live(new Table()
            .AddColumn("Step")
            .AddColumn("Status")
            .Expand()
            .Border(TableBorder.None)
            .HideHeaders())
            .Start(ctx =>
            {
                try
                {
                    // Obter a arquitetura específica através da factory
                    SetProgressUpdater.UpdateStep(ctx, steps, 0, "[yellow]In Progress[/]");
                    var projectArchitecture = _factoryCreator.Create(architecture);
                    SetProgressUpdater.UpdateStep(ctx, steps, 0, "[green]OK Completed[/]");

                    // Obter a estrutura hierárquica de pastas
                    SetProgressUpdater.UpdateStep(ctx, steps, 1, "[yellow]In Progress[/]");
                    var structure = projectArchitecture.GetStructure();
                    SetProgressUpdater.UpdateStep(ctx, steps, 1, "[green]OK Completed[/]");

                    // Criar a solução principal
                    SetProgressUpdater.UpdateStep(ctx, steps, 2, "[yellow]In Progress[/]");
                    string solutionPath = Path.Combine(outputPath, $"{projectName}.sln");
                    _builder.CreateSolution(projectName, outputPath);
                    SetProgressUpdater.UpdateStep(ctx, steps, 2, "[green]OK Completed[/]");

                    // Iterar pelas camadas (ex.: API, Core, Infrastructure, etc.)
                    int currentStepIndex = steps.Count;
                    foreach (var layer in structure)
                    {
                        string layerName = $"{projectName}.{layer.Key}";
                        string layerPath = Path.Combine(outputPath, layerName);

                        // Criar o projeto principal da camada (ex.: API, Core.Domain, etc.)
                        steps.Add(($"Creating project: {layerName}", "[yellow]In Progress[/]"));
                        SetProgressUpdater.UpdateStep(ctx, steps, currentStepIndex, "[yellow]In Progress[/]");
                        _builder.CreateClassLibraryProject(layerName, layerPath);
                        SetProgressUpdater.UpdateStep(ctx, steps, currentStepIndex, "[green]OK Completed[/]");
                        currentStepIndex++;

                        // Criar as pastas recursivamente dentro da camada
                        steps.Add(($"Creating folders in: {layerName}", "[yellow]In Progress[/]"));
                        SetProgressUpdater.UpdateStep(ctx, steps, currentStepIndex, "[yellow]In Progress[/]");
                        _builder.CreateFoldersAndUpdateCsproj(layerPath, layer.Value);
                        SetProgressUpdater.UpdateStep(ctx, steps, currentStepIndex, "[green]OK Completed[/]");
                        currentStepIndex++;

                        // Atualizar o arquivo .csproj com as pastas criadas
                        steps.Add(($"Adding {layerName} to solution", "[yellow]In Progress[/]"));
                        SetProgressUpdater.UpdateStep(ctx, steps, currentStepIndex, "[yellow]In Progress[/]");
                        string csprojPath = Path.Combine(layerPath, $"{layerName}.csproj");
                        //_builder.AddFoldersToCsproj(csprojPath, layer.Value);

                        // Adicionar o projeto à solução principal
                        _builder.AddProjectToSolution(solutionPath, csprojPath);
                        SetProgressUpdater.UpdateStep(ctx, steps, currentStepIndex, "[green]OK Completed[/]");
                        currentStepIndex++;
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[bold red]Error: {ex.Message}[/]");
                }
            });
    }
}
