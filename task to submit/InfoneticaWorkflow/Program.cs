using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System;
using System.Linq;

namespace InfoneticaWorkflow
{
    public record State(string Id, string Name, bool IsInitial, bool IsFinal, bool Enabled);

    public record ActionTransition(string Id, string Name, bool Enabled, List<string> FromStates, string ToState);

    public record WorkflowDefinition(string Id, string Name, List<State> States, List<ActionTransition> Actions);

    public class WorkflowInstance
    {
        public string Id { get; set; } = default!;
        public string DefinitionId { get; set; } = default!;
        public string CurrentState { get; set; } = default!;
        public List<ExecutionHistory> History { get; set; } = new();
    }

    public record ExecutionHistory(string ActionId, DateTime Timestamp);

    internal class Program
    {
        static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();

            var workflowDefinitions = new Dictionary<string, WorkflowDefinition>();
            var workflowInstances = new Dictionary<string, WorkflowInstance>();

            // === Create workflow definition ===
            app.MapPost("/workflow-definitions", (WorkflowDefinition def) =>
            {
                if (workflowDefinitions.ContainsKey(def.Id))
                    return Results.BadRequest($"Definition with id '{def.Id}' already exists.");

                if (def.States.Count(s => s.IsInitial) != 1)
                    return Results.BadRequest("Workflow must have exactly one initial state.");

                var stateIds = def.States.Select(s => s.Id).ToHashSet();
                foreach (var action in def.Actions)
                {
                    if (!stateIds.Contains(action.ToState))
                        return Results.BadRequest($"Action '{action.Id}' targets unknown state '{action.ToState}'.");

                    foreach (var fromState in action.FromStates)
                    {
                        if (!stateIds.Contains(fromState))
                            return Results.BadRequest($"Action '{action.Id}' has unknown fromState '{fromState}'.");
                    }
                }

                workflowDefinitions[def.Id] = def;
                return Results.Ok(def);
            });

            // === Get workflow definition ===
            app.MapGet("/workflow-definitions/{id}", (string id) =>
            {
                if (!workflowDefinitions.TryGetValue(id, out var def))
                    return Results.NotFound($"Workflow definition '{id}' not found.");

                return Results.Ok(def);
            });

            // === Start workflow instance ===
            app.MapPost("/workflow-instances/{definitionId}", (string definitionId) =>
            {
                if (!workflowDefinitions.TryGetValue(definitionId, out var def))
                    return Results.NotFound($"Workflow definition '{definitionId}' not found.");

                var initialState = def.States.First(s => s.IsInitial);
                var instance = new WorkflowInstance
                {
                    Id = Guid.NewGuid().ToString(),
                    DefinitionId = def.Id,
                    CurrentState = initialState.Id,
                    History = new List<ExecutionHistory>()
                };

                workflowInstances[instance.Id] = instance;
                return Results.Ok(instance);
            });

            // === Execute action ===
            app.MapPost("/workflow-instances/{instanceId}/execute/{actionId}", (string instanceId, string actionId) =>
            {
                if (!workflowInstances.TryGetValue(instanceId, out var instance))
                    return Results.NotFound($"Instance '{instanceId}' not found.");

                var def = workflowDefinitions[instance.DefinitionId];
                var action = def.Actions.FirstOrDefault(a => a.Id == actionId);
                if (action is null)
                    return Results.BadRequest($"Action '{actionId}' does not exist in workflow definition.");

                if (!action.Enabled)
                    return Results.BadRequest($"Action '{actionId}' is disabled.");

                if (!action.FromStates.Contains(instance.CurrentState))
                    return Results.BadRequest($"Action '{actionId}' cannot be executed from state '{instance.CurrentState}'.");

                var currentStateDef = def.States.First(s => s.Id == instance.CurrentState);
                if (currentStateDef.IsFinal)
                    return Results.BadRequest("Cannot execute actions on a final state.");

                instance.CurrentState = action.ToState;
                instance.History.Add(new ExecutionHistory(action.Id, DateTime.UtcNow));

                return Results.Ok(instance);
            });

            // === Get workflow instance ===
            app.MapGet("/workflow-instances/{instanceId}", (string instanceId) =>
            {
                if (!workflowInstances.TryGetValue(instanceId, out var instance))
                    return Results.NotFound($"Instance '{instanceId}' not found.");

                return Results.Ok(instance);
            });

            app.Run();
        }
    }
}
