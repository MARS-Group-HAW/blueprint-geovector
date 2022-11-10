using System;
using System.IO;
using GeoVectorBlueprint.Model;
using Mars.Components.Starter;
using Mars.Interfaces.Model;

namespace GeoVectorBlueprint;

internal static class Program
{
    public static void Main(string[] args)
    {
        // The scenario consists of the model (represented by the model description)
        // and the simulation configuration (see config.json).
            
        // Create a new model description that holds all parts of the model (agents, entities, layers).
        var description = new ModelDescription();
            
        // Add layer types that are part of the scenario.
        description.AddLayer<GraphLayer>();
        description.AddLayer<PoiLayer>();

        // Add agent types that are part of the scenario. For each, specify the layer type on which it lives.
        description.AddAgent<Human, GraphLayer>();
            
        // Scenario definition: Specify the configuration file that contains the specification of the scenario.
        var file = File.ReadAllText("config.json");
        var config = SimulationConfig.Deserialize(file);
            
        // Create simulation task
        var task = SimulationStarter.Start(description, config);
            
        // Run simulation
        var loopResults = task.Run();
            
        // Feedback to user that simulation run was successful
        Console.WriteLine($"Simulation execution finished after {loopResults.Iterations} steps");
    }
}