using System.ComponentModel;
using System.Text.Json;
using Azure.Core;
using Microsoft.Graph;
using Microsoft.Graph.Me.Messages.Item.ReplyAll;
using Microsoft.Graph.Models;

namespace DigitalWorkerWithTools.Tools;

public class MsGraphTools
{
    private GraphServiceClient _graphClient;

    public MsGraphTools(TokenCredential credential)
    {
        _graphClient = new GraphServiceClient(credential);
    }


    [Description("Get details about the agent identity from Microsoft Graph.")]
    public async Task<string> GetAgentDetailsAsync()
    {
        User? me = await _graphClient.Me.GetAsync();
        return $"Agent Details from MS Graph: {me?.DisplayName ?? "Unknown"}";
    }

    [Description("Get the Teams chats the agent is part of. The result is returned as a JSON string.")]
    public async Task<string> GetTeamsChatsAsync()
    {
        try
        {
            var chats = await _graphClient.Chats.GetAsync();
            return JsonSerializer.Serialize(chats, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            throw new Exception("Error retrieving Teams chats", ex);
        }
    }

    [Description("Retrieves the messages for a specific chat. The chatId parameter is required. The result is returned as a JSON string.")]
    public async Task<string> GetChatMessagesAsync(string chatId)
    {
        try
        {
            var messages = await _graphClient.Chats[chatId].Messages.GetAsync(
            config =>
                {
                    config.QueryParameters.Orderby = ["createdDateTime desc"];
                    config.QueryParameters.Top = 20;
                }
            );
            return JsonSerializer.Serialize(messages, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving messages for chat {chatId}", ex);
        }
    }

    [Description("Posts a message to a specific chat. The chatId and messageContent parameters are required.")]
    public async Task<string> PostChatMessageAsync(string chatId, string messageContent)
    {
        try
        {
            var chatMessage = new ChatMessage
            {
                Body = new ItemBody
                {
                    Content = messageContent,
                    ContentType = BodyType.Text
                }
            };

            await _graphClient.Chats[chatId].Messages.PostAsync(chatMessage);
            return $"Message posted to chat {chatId} successfully.";
        }
        catch (Exception ex)
        {
            throw new Exception($"Error posting message to chat {chatId}", ex);
        }
    }

    [Description("Retrieve unread e-mails")]
    public async Task<string> GetUnreadEmailsAsync()
    {
        try
        {
            var emails = await _graphClient.Me.Messages.GetAsync(
            config =>
                {
                    config.QueryParameters.Orderby = ["createdDateTime desc"];
                    config.QueryParameters.Top = 20;
                    config.QueryParameters.Filter = "isRead eq false";
                }
            );

            return JsonSerializer.Serialize(emails, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving unread emails", ex);
        }
    }

    [Description("Reply to an e-mail by emailId and providing the reply content in HTML format.")]
    public async Task<string> ReplyToEmailAsync(string emailId, string replyContent)
    {
        try
        {
            var requestBody = new ReplyAllPostRequestBody
            {
                Message = new Message
                {
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Html,   // or BodyType.Text
                        Content = replyContent
                    }
                }
            };

            await _graphClient.Me.Messages[emailId].ReplyAll.PostAsync(requestBody);
            await _graphClient.Me.Messages[emailId].PatchAsync(new Message
            {
                IsRead = true
            });
            
            return $"Replied to email {emailId} successfully.";
        }
        catch (Exception ex)
        {
            throw new Exception($"Error replying to email {emailId}", ex);
        }
    }
}