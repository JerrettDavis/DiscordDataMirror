# API Reference

This section contains the automatically generated API documentation for DiscordDataMirror.

## Namespaces

### Domain Layer
- **DiscordDataMirror.Domain** — Core entities, value objects, and domain events
- **DiscordDataMirror.Domain.Entities** — Guild, Channel, Message, User, etc.
- **DiscordDataMirror.Domain.ValueObjects** — Snowflake, Permission, EmoteRef
- **DiscordDataMirror.Domain.Events** — Domain events for CQRS

### Application Layer
- **DiscordDataMirror.Application** — Commands, queries, and handlers
- **DiscordDataMirror.Application.Commands** — Write operations (sync, upsert, etc.)
- **DiscordDataMirror.Application.Queries** — Read operations (get, search, list)
- **DiscordDataMirror.Application.DTOs** — Data transfer objects

### Infrastructure Layer
- **DiscordDataMirror.Infrastructure** — Database, Discord client, repositories
- **DiscordDataMirror.Infrastructure.Persistence** — Entity Framework configuration
- **DiscordDataMirror.Infrastructure.Discord** — Discord.Net integration

### Presentation Layer
- **DiscordDataMirror.Bot** — Discord bot worker service
- **DiscordDataMirror.Dashboard** — Blazor web dashboard

## Getting Started with the API

If you want to extend DiscordDataMirror or build integrations, start with:

1. **Domain entities** — Understand the data model
2. **Application commands/queries** — Learn the CQRS patterns
3. **Repository interfaces** — Access data layer

## Code Examples

### Query messages programmatically

```csharp
// Inject IMediator
public class MyService
{
    private readonly IMediator _mediator;
    
    public async Task<IEnumerable<MessageDto>> SearchMessages(string query)
    {
        var result = await _mediator.Send(new SearchMessagesQuery
        {
            SearchTerm = query,
            Limit = 100
        });
        
        return result.Messages;
    }
}
```

### Subscribe to domain events

```csharp
public class MessageReceivedHandler : INotificationHandler<MessageCreatedEvent>
{
    public async Task Handle(MessageCreatedEvent notification, CancellationToken ct)
    {
        // React to new messages
        Console.WriteLine($"New message in {notification.ChannelId}");
    }
}
```

## Building the Documentation

To regenerate API documentation locally:

```bash
cd docs
docfx metadata
docfx build
docfx serve _site
```

This requires the [docfx CLI](https://dotnet.github.io/docfx/).
