# AI Agent framework sample
This is a base sample for using Azure AI Framework. There are three projects:
 - A class library [DigitialWorkerWithTools](./DigitialWorkerWithTools) - the actual AI Agent implementation with [MS Graph Tools](./DigitialWorkerWithTools/Tools/MsGraphTools.cs)
 - A console app [AgentLocalRun](./AgentLocalRun) - to run the AI Agent locally as console app
 - An Azure Function project [AzureAgentFunctions](./AzureAgentFunctions) - to deploy the AI Agent as time triggered Azure Function

# Implementation of Microsoft Entra Agent ID
The MS Graph Tools implementation for the AI Agent relies on the Microsoft Entra Agent ID. The agent is designed to work as an Agent User.
The Agent User must have active M365 license assigned with at least Teams and EXO Plan 1. 
The Agent User will check its teams' group chats and e-mails and will provide answers as needed.
The implementation is based on [MSAL.NET](https://learn.microsoft.com/en-us/entra/msal/dotnet/)