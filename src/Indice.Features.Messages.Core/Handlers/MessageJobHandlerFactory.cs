﻿using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Messages.Core.Handlers;

/// <summary>A factory class that creates instance of <see cref="ICampaignJobHandler{TEvent}"/> implementations based on the type of event.</summary>
public class MessageJobHandlerFactory
{
    /// <summary>Creates a new instance of <see cref="MessageJobHandlerFactory"/>.</summary>
    /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public MessageJobHandlerFactory(IServiceProvider serviceProvider) {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    private IServiceProvider ServiceProvider { get; }

    /// <summary>Creates the appropriate instance based on handled event.</summary>
    /// <typeparam name="TEvent">The type of event.</typeparam>
    public ICampaignJobHandler<TEvent> CreateFor<TEvent>() where TEvent : class {
        var handler = ServiceProvider.GetRequiredService<ICampaignJobHandler<TEvent>>();
        return handler;
    }
}
