using System;
using Indice.Types;
using Xunit;

namespace Indice.Common.Tests
{
    public class GeoPointTests
    {
        [Theory]
        [InlineData("geo:13.4125,103.8667", 13.4125, 103.8667, null, true)]
        [InlineData("geo:13.4125,103.8667,14;cgen=map", 13.4125, 103.8667, 14.0, true)]
        [InlineData("13.4125,103.8667", 13.4125, 103.8667, null, true)]
        [InlineData("13.4125,103.8667,14", 13.4125, 103.8667, 14.0, true)]
        [InlineData("POINT (103.8667 13.4125 14.3)", 13.4125, 103.8667, 14.3, true)]
        [InlineData("POINT(103.8667 13.4125)", 13.4125, 103.8667, null, true)]
        [InlineData("APOINT(103.8667 13.4125)", 13.4125, 103.8667, null, false)]
        public void GeoPointParseTests(string text, double latitude, double longitude, double? elevation, bool ok) {
            if (!ok) {
                Assert.ThrowsAny<Exception>(() => GeoPoint.Parse(text));
            } else {
                var point = GeoPoint.Parse(text);
                Assert.Equal(latitude, point.Latitude);
                Assert.Equal(longitude, point.Longitude);
                Assert.Equal(elevation, point.Elevation);
            }
        }

        [Theory]
        [InlineData("13.4125,103.8667,14.34", 13.4125, 103.8667, 14.34)]
        [InlineData("13.4125,103.8667", 13.4125, 103.8667, null)]
        public void GeoPointTostringTests(string text, double latitude, double longitude, double? elevation) {
            var point = new GeoPoint() {
                Latitude = latitude,
                Longitude = longitude,
                Elevation = elevation
            };
            Assert.Equal(text, point.ToString());
        }
    }
}
