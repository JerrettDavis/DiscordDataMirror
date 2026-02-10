using DiscordDataMirror.Application.Services;
using DiscordDataMirror.Domain.Entities;
using DiscordDataMirror.Domain.Repositories;
using DiscordDataMirror.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DiscordDataMirror.Infrastructure.Services;

/// <summary>
/// Implementation of message sync service using repositories.
/// </summary>
public class MessageSyncService : IMessageSyncService
{
    private readonly IMessageRepository _messageRepository;
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly IEmbedRepository _embedRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MessageSyncService> _logger;

    public MessageSyncService(
        IMessageRepository messageRepository,
        IAttachmentRepository attachmentRepository,
        IEmbedRepository embedRepository,
        IUnitOfWork unitOfWork,
        ILogger<MessageSyncService> logger)
    {
        _messageRepository = messageRepository;
        _attachmentRepository = attachmentRepository;
        _embedRepository = embedRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Message> SyncMessageAsync(
        Snowflake messageId,
        Snowflake channelId,
        Snowflake authorId,
        string? content,
        string? cleanContent,
        MessageType type,
        bool isPinned,
        bool isTts,
        DateTime timestamp,
        DateTime? editedTimestamp,
        Snowflake? referencedMessageId,
        string? rawJson = null,
        CancellationToken ct = default)
    {
        var existingMessage = await _messageRepository.GetByIdAsync(messageId, ct);

        if (existingMessage is null)
        {
            _logger.LogDebug("Creating new message: {MessageId}", messageId);

            var message = new Message(messageId, channelId, authorId, timestamp);
            message.Update(content, cleanContent, type, isPinned, isTts, editedTimestamp, referencedMessageId, rawJson);

            await _messageRepository.AddAsync(message, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return message;
        }
        else
        {
            _logger.LogDebug("Updating existing message: {MessageId}", messageId);

            existingMessage.Update(content, cleanContent, type, isPinned, isTts, editedTimestamp, referencedMessageId, rawJson);

            await _messageRepository.UpdateAsync(existingMessage, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return existingMessage;
        }
    }

    public async Task DeleteMessageAsync(Snowflake messageId, CancellationToken ct = default)
    {
        var message = await _messageRepository.GetByIdAsync(messageId, ct);
        if (message is not null)
        {
            _logger.LogDebug("Deleting message: {MessageId}", messageId);
            await _messageRepository.DeleteAsync(message, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    public async Task SyncAttachmentsAsync(
        Snowflake messageId,
        IEnumerable<AttachmentData> attachments,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Syncing attachments for message {MessageId}", messageId);

        // Get existing attachments
        var existing = await _attachmentRepository.GetByMessageIdAsync(messageId, ct);
        var existingDict = existing.ToDictionary(a => a.Id);
        var incomingIds = new HashSet<Snowflake>();

        foreach (var attachmentData in attachments)
        {
            incomingIds.Add(attachmentData.Id);

            if (existingDict.TryGetValue(attachmentData.Id, out var existingAttachment))
            {
                // Update existing
                existingAttachment.Update(
                    attachmentData.ProxyUrl,
                    attachmentData.Width,
                    attachmentData.Height,
                    attachmentData.ContentType);
                await _attachmentRepository.UpdateAsync(existingAttachment, ct);
            }
            else
            {
                // Create new
                var attachment = new Attachment(
                    attachmentData.Id,
                    messageId,
                    attachmentData.Filename,
                    attachmentData.Url,
                    attachmentData.Size);
                attachment.Update(
                    attachmentData.ProxyUrl,
                    attachmentData.Width,
                    attachmentData.Height,
                    attachmentData.ContentType);
                await _attachmentRepository.AddAsync(attachment, ct);
            }
        }

        // Remove attachments no longer present
        foreach (var existingAttachment in existing)
        {
            if (!incomingIds.Contains(existingAttachment.Id))
            {
                await _attachmentRepository.DeleteAsync(existingAttachment, ct);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task SyncEmbedsAsync(
        Snowflake messageId,
        IEnumerable<EmbedData> embeds,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Syncing embeds for message {MessageId}", messageId);

        // Delete all existing embeds for this message (embeds are replaced entirely)
        await _embedRepository.DeleteByMessageIdAsync(messageId, ct);

        // Add new embeds
        foreach (var embedData in embeds)
        {
            var embed = new Embed(messageId, embedData.Index, embedData.JsonData ?? "{}");
            embed.Update(
                embedData.Type,
                embedData.Title,
                embedData.Description,
                embedData.Url,
                embedData.Timestamp,
                embedData.Color,
                embedData.JsonData ?? "{}");
            await _embedRepository.AddAsync(embed, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<Snowflake?> GetLastMessageIdAsync(Snowflake channelId, CancellationToken ct = default)
    {
        return await _messageRepository.GetLastMessageIdAsync(channelId, ct);
    }

    public async Task SyncMessageBatchAsync(IEnumerable<MessageData> messages, CancellationToken ct = default)
    {
        var messageList = messages.ToList();
        _logger.LogDebug("Batch syncing {Count} messages", messageList.Count);

        foreach (var messageData in messageList)
        {
            ct.ThrowIfCancellationRequested();

            // Sync the message
            await SyncMessageAsync(
                messageData.Id,
                messageData.ChannelId,
                messageData.AuthorId,
                messageData.Content,
                messageData.CleanContent,
                messageData.Type,
                messageData.IsPinned,
                messageData.IsTts,
                messageData.Timestamp,
                messageData.EditedTimestamp,
                messageData.ReferencedMessageId,
                messageData.RawJson,
                ct);

            // Sync attachments if any
            if (messageData.Attachments?.Any() == true)
            {
                await SyncAttachmentsAsync(messageData.Id, messageData.Attachments, ct);
            }

            // Sync embeds if any
            if (messageData.Embeds?.Any() == true)
            {
                await SyncEmbedsAsync(messageData.Id, messageData.Embeds, ct);
            }
        }

        _logger.LogDebug("Batch sync complete");
    }
}
