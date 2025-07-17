This is a minimal backend service built in .NET 8 / C# for the Infonetica software engineer intern take-home assignment.

It implements a simple, configurable workflow (state-machine) API that allows clients to:

- Define workflows with states and actions.
- Start workflow instances.
- Execute actions to move between states, with full validation.
- Inspect workflow definitions and running instances.

## Features

- Enforces state machine rules: only one initial state per workflow, valid transitions, and no invalid operations.
- All data is stored in-memory (no database).
- Uses ASP.NET Core Minimal API.
- Designed to be easy to read, validate, and extend.

## Requirements

  .NET 8 SDK installed  
  Check your version with: dotnet --version

## How to run

Clone this repository and run the project using the .NET CLI.

```bash
git clone https://github.com/usoshidey/Infonetica_Assignment-Task
cd InfoneticaWorkflow
dotnet run
````

By default, the API runs on `http://localhost:5090` (or another random port depending on your machine).
Check your terminal output — look for `Now listening on:` to see the actual URL and port.

## API Endpoints

| Method | Endpoint                                              | Description                                      |
| ------ | ----------------------------------------------------- | ------------------------------------------------ |
| POST   | `/workflow-definitions`                               | Create a new workflow definition                 |
| GET    | `/workflow-definitions/{id}`                          | Retrieve a workflow definition by ID             |
| POST   | `/workflow-instances/{definitionId}`                  | Start a new instance of a workflow definition    |
| POST   | `/workflow-instances/{instanceId}/execute/{actionId}` | Execute an action on an instance                 |
| GET    | `/workflow-instances/{instanceId}`                    | Get the current state and history of an instance |

## Example usage (cURL)

Below are some quick examples to test the API using `curl`. Replace the port if yours is different.

1. **Create a workflow definition**

```bash
curl -X POST http://localhost:5090/workflow-definitions \
  -H "Content-Type: application/json" \
  -d '{
    "id": "wf1",
    "name": "Sample Workflow",
    "states": [
      { "id": "start", "name": "Start", "isInitial": true, "isFinal": false, "enabled": true },
      { "id": "end", "name": "End", "isInitial": false, "isFinal": true, "enabled": true }
    ],
    "actions": [
      { "id": "go", "name": "GoToEnd", "enabled": true, "fromStates": ["start"], "toState": "end" }
    ]
  }'
```

2. **Start a workflow instance**

```bash
curl -X POST http://localhost:5090/workflow-instances/wf1
```

This returns a JSON object with an `id`. Copy that ID for the next step.

3. **Execute an action**

```bash
curl -X POST http://localhost:5090/workflow-instances/INSTANCE_ID/execute/go
```

Replace `INSTANCE_ID` with the ID returned from the previous step.

4. **Check instance status**

```bash
curl http://localhost:5090/workflow-instances/INSTANCE_ID
```

## Assumptions & Notes

* All workflow and instance data is stored in-memory only. Restarting the server clears all data.
* No authentication or authorization is implemented.
* Validation is done for duplicate IDs, invalid transitions, and invalid requests.
* No database is used by design.
* Unit tests are not included due to the time limit; the focus is on clear design and validation logic.
