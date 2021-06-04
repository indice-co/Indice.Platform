using System;

namespace Indice.AspNetCore.Identity.Api.Models
{
    /// <summary>
    /// Contains summary information about the system.
    /// </summary>
    public class SummaryInfo
    {
        /// <summary>
        /// Indicates the point in time where the statistics where last updated.
        /// </summary>
        public DateTime LastUpdatedAt { get; set; }
        /// <summary>
        /// The total number of users.
        /// </summary>
        public int TotalUsers { get; set; }
        /// <summary>
        /// The total number of clients.
        /// </summary>
        public int TotalClients { get; set; }
        /// <summary>
        /// The total number of roles.
        /// </summary>
        public int TotalRoles { get; set; }
        /// <summary>
        /// Contains percentage of user activity.
        /// </summary>
        public UsersActivityInfo Activity { get; set; }
        /// <summary>
        /// User statistics.
        /// </summary>
        public UsersStatisticsInfo Stats { get; set; }
    }

    /// <summary>
    /// Models percentage of user activity.
    /// </summary>
    public class UsersActivityInfo
    {
        /// <summary>
        /// Daily basis.
        /// </summary>
        public SummaryStatistic Day { get; set; }
        /// <summary>
        /// Weekly basis.
        /// </summary>
        public SummaryStatistic Week { get; set; }
        /// <summary>
        /// Monthly basis.
        /// </summary>
        public SummaryStatistic Month { get; set; }
    }

    /// <summary>
    /// Models various user statistics.
    /// </summary>
    public class UsersStatisticsInfo
    {
        /// <summary>
        /// Users with verified emails.
        /// </summary>
        public SummaryStatistic EmailsVerified { get; set; }
        /// <summary>
        /// Users with verified phone numbers.
        /// </summary>
        public SummaryStatistic PhoneNumbersVerified { get; set; }
    }

    /// <summary>
    /// Models a statistic value,
    /// </summary>
    public class SummaryStatistic
    {
        /// <summary>
        /// Creates a new instance of <see cref="SummaryStatistic"/>.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <param name="percent">The percent.</param>
        public SummaryStatistic(int count, double percent) {
            Count = count;
            Percent = percent;
        }

        /// <summary>
        /// The count.
        /// </summary>
        public int Count { get; set; }
        /// <summary>
        /// The percent.
        /// </summary>
        public double Percent { get; set; }
    }
}
