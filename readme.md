# AI Agent framework sample
This is a base sample for using Azure AI Framework. There are three projects:
 - A class library - the actual AI Agent implementation with MS Graph Tools
 - A console app - to run the AI Agent locally
 - An Azure Function project - to deploy the AI Agent as time triggered Azure Function

# Implementation of Microosft Entra Agent ID
The MS Graph Tools implementation for the AI Agent relies on the Microsoft Entra Agent ID. The agent is designed to work as Agent User.
The Agent User must have active M365 license assigned with at least Teams and EXO Plan 1. 
The Agent User will check its teams' group chats and e-mails and will provide answers as needed.