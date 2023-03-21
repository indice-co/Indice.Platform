using Indice.Hosting.Models;

namespace Indice.Hosting.Services;

/// <summary>Represents a first-in, first-out collection of objects.</summary>
/// <typeparam name="T">The type of the queue item.</typeparam>
public interface IMessageQueue<T> where T : class
{
    /// <summary>Removes and returns the object at the beginning of the <see cref="IMessageQueue{T}"/>.</summary>
    /// <returns>The object that is removed from the beginning of the <see cref="IMessageQueue{T}"/>.</returns>
    Task<QMessage<T>> Dequeue();
    /// <summary>Adds an object to the end of the <see cref="IMessageQueue{T}"/>.</summary>
    /// <param name="item">The object to add to the <see cref="IMessageQueue{T}"/>.</param>
    /// <param name="isPoison">Marks the given <paramref name="item"/> as poison message.</param>
    Task Enqueue(QMessage<T> item, bool isPoison);
    /// <summary>Adds a range of items to the end of the <see cref="IMessageQueue{T}"/>.</summary>
    /// <param name="items">The collection of objects to add to the <see cref="IMessageQueue{T}"/>.</param>
    Task EnqueueRange(IEnumerable<QMessage<T>> items);
    /// <summary>Returns the object at the beginning of the <see cref="IMessageQueue{T}"/> without removing it.</summary>
    /// <returns>The object at the beginning of the <see cref="IMessageQueue{T}"/>.</returns>
    Task<T> Peek();
    /// <summary>Gets the number of elements contained in the <see cref="IMessageQueue{T}"/>.</summary>
    /// <returns>The number of elements contained in the <see cref="IMessageQueue{T}"/>.</returns>
    Task<int> Count();
    /// <summary>Does cleanup of the <see cref="IMessageQueue{T}"/>.</summary>
    /// <param name="batchSize">If specified will cleanup the first <paramref name="batchSize"/> number of items.</param>
    Task Cleanup(int? batchSize = null);
}

/// <summary>Extension methods on <see cref="IMessageQueue{T}"/>.</summary>
public static class IMessageQueueExtensions
{
    /// <summary>Shorthand dequeue to extract the payload directly.</summary>
    /// <typeparam name="T">The type of message.</typeparam>
    /// <param name="queue">The message queue.</param>
    public static async Task<T> DequeueValue<T>(this IMessageQueue<T> queue) where T : class => (await queue.Dequeue())?.Value;

    /// <summary>Enqueue a new item.</summary>
    /// <typeparam name="T">The type of message.</typeparam>
    /// <param name="queue">The message queue.</param>
    /// <param name="item">The message item.</param>
    public static async Task Enqueue<T>(this IMessageQueue<T> queue, T item) where T : class => await queue.Enqueue(item, TimeSpan.Zero);

    /// <summary>Enqueue a new item.</summary>
    /// <typeparam name="T">The type of message.</typeparam>
    /// <param name="queue">The message queue.</param>
    /// <param name="item">The message item.</param>
    /// <param name="visibilityWindow">Postpones processing of the message as specified by the <paramref name="visibilityWindow"/>.</param>
    public static async Task Enqueue<T>(this IMessageQueue<T> queue, T item, TimeSpan visibilityWindow) where T : class => await queue.Enqueue(item, DateTime.UtcNow.Add(visibilityWindow));

    /// <summary>Enqueue a new item.</summary>
    /// <typeparam name="T">The type of message.</typeparam>
    /// <param name="queue">The message queue.</param>
    /// <param name="item">The message item.</param>
    /// <param name="enqueueAt">The <see cref="DateTime"/> that the item is added to the queue.</param>
    public static async Task Enqueue<T>(this IMessageQueue<T> queue, T item, DateTime enqueueAt) where T : class {
        var message = new QMessage<T> {
            Id = Guid.NewGuid().ToString(),
            Date = enqueueAt,
            Value = item,
            IsNew = true
        };
        await queue.Enqueue(message, isPoison: false);
    }

    /// <summary>Adds a range of items to the end of the <see cref="IMessageQueue{T}"/>.</summary>
    /// <typeparam name="T">The type of message.</typeparam>
    /// <param name="queue">The message queue.</param>
    /// <param name="items">The collection of objects to add to the <see cref="IMessageQueue{T}"/>.</param>
    public static async Task EnqueueRange<T>(this IMessageQueue<T> queue, IEnumerable<T> items) where T : class => await queue.EnqueueRange(items, TimeSpan.Zero);

    /// <summary>Adds a range of items to the end of the <see cref="IMessageQueue{T}"/>.</summary>
    /// <typeparam name="T">The type of message.</typeparam>
    /// <param name="queue">The message queue.</param>
    /// <param name="items">The collection of objects to add to the <see cref="IMessageQueue{T}"/>.</param>
    /// <param name="visibilityWindow">Postpones processing of the message as specified by the <paramref name="visibilityWindow"/>.</param>
    public static async Task EnqueueRange<T>(this IMessageQueue<T> queue, IEnumerable<T> items, TimeSpan visibilityWindow) where T : class => await queue.EnqueueRange(items, DateTime.UtcNow.Add(visibilityWindow));

    /// <summary>Adds a range of items to the end of the <see cref="IMessageQueue{T}"/>.</summary>
    /// <typeparam name="T">The type of message.</typeparam>
    /// <param name="queue">The message queue.</param>
    /// <param name="items">The collection of objects to add to the <see cref="IMessageQueue{T}"/>.</param>
    /// <param name="enqueueAt">The <see cref="DateTime"/> that the item is added to the queue.</param>
    public static async Task EnqueueRange<T>(this IMessageQueue<T> queue, IEnumerable<T> items, DateTime enqueueAt) where T : class {
        var message = items.Select(item => new QMessage<T> {
            Id = Guid.NewGuid().ToString(),
            Date = enqueueAt,
            Value = item,
            IsNew = true
        });
        await queue.EnqueueRange(message);
    }

    /// <summary>Enqueue an existing item.</summary>
    /// <typeparam name="T">The type of message.</typeparam>
    /// <param name="queue">The message queue.</param>
    /// <param name="message">The message.</param>
    public static async Task ReEnqueue<T>(this IMessageQueue<T> queue, QMessage<T> message) where T : class {
        message.DequeueCount++;
        await queue.Enqueue(message, isPoison: false);
    }

    /// <summary>Marks an existing message as poison, after three consecutive failures.</summary>
    /// <typeparam name="T">The type of message.</typeparam>
    /// <param name="queue">The message queue.</param>
    /// <param name="message">The message.</param>
    public static async Task MarkPoison<T>(this IMessageQueue<T> queue, QMessage<T> message) where T : class => await queue.Enqueue(message, isPoison: true);
}
