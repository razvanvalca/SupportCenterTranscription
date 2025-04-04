## README.md

```markdown
# SupportCenterTranscription

A .NET-based solution for automatically joining Microsoft Teams calls/meetings for a specific support user and streaming real-time audio to an Azure Function for transcription or further processing.

**Contents**  
- [Overview](#overview)  
- [Architecture Diagram](#architecture-diagram)  
- [Solution Structure](#solution-structure)  
- [Prerequisites](#prerequisites)  
- [Setup and Configuration](#setup-and-configuration)  
- [Building and Running Locally](#building-and-running-locally)  
- [Deploying to Azure](#deploying-to-azure)  
- [Usage and Scenarios](#usage-and-scenarios)  
- [License](#license)

---

## Overview

This repository implements a special Microsoft Teams calling bot (the **TeamsStreamerBot**) that:

1. Subscribes to a target user’s presence via Microsoft Graph.  
2. Automatically joins the user’s calls/meetings when their status indicates they’re in a call or meeting.  
3. Captures real-time audio from the call using the Microsoft Graph Real-time Media Platform (RMP).  
4. Streams the audio to a backend Azure Function for processing, transcription, or storage.  

The Azure Function receives the audio data (via HTTP POST) and can perform actions like transcription (using Cognitive Services) or archiving for compliance.

---

## Architecture Diagram

```
                +--------------------+
                |  Support Person    |
                | (User in Teams)    |
                +---------+----------+
                          |
        Presence Changes  |  InACall / Busy / ...
                          |
     +---------------------v-----------------------+
     |   Microsoft Graph Presence Subscription     |
     |   +  Bot receives presence notifications    |
     +---------------------+-----------------------+
                           |
     +---------------------v-----------------------+
     |               TeamsStreamerBot             |
     | (ASP.NET Core Web App, Registered as Bot)  |
     |   - GraphPresenceService                   |
     |   - CallingBotService (Joins Calls, RMP)   |
     +---------------------+-----------------------+
                           |
                   Real-time Audio
                           |
     +---------------------v-----------------------+
     |         Azure Function (AudioReceiver)     |
     |   Receives audio data, process/transcribe  |
     +--------------------------------------------+
```

---

## Solution Structure

```
SupportCenterTranscription
│  README.md
│  .gitignore
│  SupportCenterTranscription.sln
│
├── TeamsStreamerBot
│   ├── Controllers
│   ├── Services
│   ├── Program.cs
│   ├── Startup.cs
│   ├── appsettings.json
│   └── TeamsStreamerBot.csproj
│
└── AudioReceiverFunction
    ├── AudioReceiverFunction.cs
    ├── local.settings.json
    └── AudioReceiverFunction.csproj
```

- **TeamsStreamerBot**: The core ASP.NET Core Web API that handles Teams calling bot logic, presence subscription, and call notifications.  
- **AudioReceiverFunction**: An Azure Function that receives audio data streamed by the bot.

---

## Prerequisites

1. **.NET 6 SDK or higher** installed.  
2. **Azure Subscription** for deploying the bot and function.  
3. **Azure AD App Registration** with permissions for Microsoft Graph calling & presence.  
4. **Azure Bot Registration** (Bot Channels Registration) if you want to host your bot in Azure.  
5. **Microsoft Teams** environment where this bot will be installed or used.

---

## Setup and Configuration

1. **Clone the repository**:
   ```bash
   git clone https://github.com/your-org/SupportCenterTranscription.git
   cd SupportCenterTranscription
   ```

2. **Open the solution** in Visual Studio or your preferred IDE:
   ```bash
   code SupportCenterTranscription.sln
   ```
   (or `visual studio` approach)

3. **Update appsettings.json** in **TeamsStreamerBot**:
   - `AzureAd:TenantId` => Your tenant (GUID)  
   - `AzureAd:ClientId` => The App (client) ID from Azure AD  
   - `AzureAd:ClientSecret` => The client secret or use Managed Identity approach  
   - `Bot:BotBaseUrl` => The public URL where your bot is hosted  
   - `Bot:NotificationUrl` => Your endpoint for presence notifications (e.g., `<BotBaseUrl>/api/notifications`)  
   - `AudioReceiverFunction:Endpoint` => The URL of your Azure Function  

4. **Azure Function**: Check `local.settings.json` for the function, ensure `FUNCTIONS_WORKER_RUNTIME="dotnet"` and a valid `AzureWebJobsStorage` if needed.

---

## Building and Running Locally

### TeamsStreamerBot

From the **TeamsStreamerBot** folder:
```bash
dotnet build
dotnet run
```
This will listen on `https://localhost:5001` (or similar) for presence notifications and call events.

### AudioReceiverFunction

From the **AudioReceiverFunction** folder:
```bash
dotnet build
func start
```
This starts the function locally.  
Update your `AudioReceiverFunction:Endpoint` in the bot to be `http://localhost:7071/api/AudioReceiver` if you want local streaming.

---

## Deploying to Azure

### 1. Deploy the TeamsStreamerBot

1. Create an Azure Web App (or Container App):
   ```bash
   az webapp create --name <YourBotAppName> --resource-group <YourResourceGroup> --plan <YourAppServicePlan>
   ```
2. Publish:
   ```bash
   dotnet publish -c Release
   az webapp deploy --name <YourBotAppName> --resource-group <YourResourceGroup> --src-path .\bin\Release\net6.0\publish
   ```
3. Configure the Azure Bot resource (Bot Channels Registration) with your Web App’s endpoint(s):
   - **Messaging endpoint**: `https://<YourBotAppName>.azurewebsites.net/api/calls/callback`  
   - **Presence notification endpoint**: `https://<YourBotAppName>.azurewebsites.net/api/notifications/presence`

### 2. Deploy the AudioReceiverFunction

1. Create an Azure Function App:
   ```bash
   az functionapp create --name <YourFunctionAppName> --resource-group <YourResourceGroup> --storage-account <YourStorageAccount> --runtime dotnet
   ```
2. Publish using the Azure Functions Core Tools:
   ```bash
   cd AudioReceiverFunction
   func azure functionapp publish <YourFunctionAppName>
   ```
3. Update your **TeamsStreamerBot**’s `appsettings.json` to point `AudioReceiverFunction:Endpoint` to the new Function URL:
   ```
   "AudioReceiverFunction": {
     "Endpoint": "https://<YourFunctionAppName>.azurewebsites.net/api/AudioReceiver"
   }
   ```

---

## Usage and Scenarios

1. **Subscribe to Presence**:  
   In your startup or service code, call:
   ```csharp
   await graphPresenceService.SubscribeToUserPresenceAsync("<UserId or UPN>");
   ```
   This will trigger presence notifications from Microsoft Graph for that user.

2. **Presence Changes**:  
   When the user enters a `Busy`, `InACall`, or `InAMeeting` state, the bot attempts to join the call using the **CallingBotService**.

3. **Audio Streaming**:  
   After joining, the real-time media pipeline receives audio data, which gets posted to the Azure Function at `AudioReceiverFunction/AudioReceiver`.

4. **Transcription**:  
   In the function, you can process or forward audio to Azure Cognitive Services for real-time speech-to-text transcription.

---

## License

This project is licensed under the [MIT License](LICENSE).  
Feel free to modify and adapt according to your needs.
