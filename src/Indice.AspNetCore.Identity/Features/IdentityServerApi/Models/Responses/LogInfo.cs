using System;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models a system log entry.
    /// </summary>
    public class LogInfo
    {
        /// <summary>
        /// The unique id of the log entry.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// An optional message.
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// The log level.
        /// </summary>
        public string Level { get; set; }
        /// <summary>
        /// The timestamp.
        /// </summary>
        public DateTime? Timestamp { get; set; }
        /// <summary>
        /// The exception that has occured.
        /// </summary>
        public string Exception { get; set; }
        /// <summary>
        /// The id of the user.
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// The user's machine name.
        /// </summary>
        public string MachineName { get; set; }
        /// <summary>
        /// The request URL.
        /// </summary>
        public string RequestUrl { get; set; }
        /// <summary>
        /// The request machine IP address.
        /// </summary>
        public string IpAddress { get; set; }
        /// <summary>
        /// The request method.
        /// </summary>
        public string RequestMethod { get; set; }
    }
}
