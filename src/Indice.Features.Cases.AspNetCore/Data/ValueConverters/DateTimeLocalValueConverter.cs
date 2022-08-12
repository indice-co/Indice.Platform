using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Indice.Features.Cases.Data.ValueConverters
{
    internal static class DateTimeLocalValueConverter
    {
        /// <summary>
        /// Convert all <see cref="DateTime"/> instances to Local TimeZone.
        /// </summary>
        internal static void AddDateTimeLocalValueConverter(this ModelBuilder modelBuilder) {
            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => v.ToUniversalTime(),
                v => TimeZoneInfo.ConvertTimeFromUtc(v, TimeZoneInfo.Local));

            var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? v.Value.ToUniversalTime() : v,
                v => v.HasValue ? TimeZoneInfo.ConvertTimeFromUtc(v.Value, TimeZoneInfo.Local) : v);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes()) {
                if (entityType.IsKeyless) {
                    continue;
                }

                foreach (var property in entityType.GetProperties()) {
                    if (property.ClrType == typeof(DateTime)) {
                        property.SetValueConverter(dateTimeConverter);
                    } else if (property.ClrType == typeof(DateTime?)) {
                        property.SetValueConverter(nullableDateTimeConverter);
                    }
                }
            }
        }
    }
}