using System;

namespace Indice.Hosting.Tasks
{
    /// <summary>
    /// 
    /// </summary>
    public class MessageQueueFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public MessageQueueFactory(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IMessageQueue<T> Create<T>() where T : class {
            var services = _serviceProvider.GetService(typeof(IMessageQueue<>));
            return null;
        } 
    }
}
