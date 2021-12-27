using System;
using System.Text.Json;
using Indice.Serialization;
using Xunit;

namespace Indice.Common.Tests
{
    public class JsonStringArrayEnumFlagsConverterTests
    {
        public JsonStringArrayEnumFlagsConverterTests() {
            Options = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            Options.Converters.Add(new JsonStringArrayEnumFlagsConverterFactory());
        }

        public JsonSerializerOptions Options { get; }

        [Fact]
        public void CanSerializeEnumFlagsToStringArray() {
            var campaign = new Campaign {
                Id = 1,
                Name = "Sales Christmas 2021",
                DeliveryChannel = CampaignDeliveryChannel.Inbox | CampaignDeliveryChannel.PushNotification
            };
            var expectedJson = @"{""id"":1,""name"":""Sales Christmas 2021"",""deliveryChannel"":[""Inbox"",""PushNotification""]}";
            var campaignJson = JsonSerializer.Serialize(campaign, Options);
            Assert.Equal(campaignJson, expectedJson);
        }

        [Fact]
        public void CanDeserializeEnumFlagsToStringArray() {
            var json = @"{""id"":2,""name"":""Sales Christmas 2021"",""deliveryChannel"":[""Inbox"",""PushNotification"",""Email""]}";
            var campaign = JsonSerializer.Deserialize<Campaign>(json, Options);
            Assert.Equal(2, campaign.Id);
            Assert.Equal("Sales Christmas 2021", campaign.Name);
            Assert.Equal(CampaignDeliveryChannel.Inbox | CampaignDeliveryChannel.PushNotification | CampaignDeliveryChannel.Email, campaign.DeliveryChannel);
        }
    }

    public class Campaign
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public CampaignDeliveryChannel DeliveryChannel { get; set; } = CampaignDeliveryChannel.Inbox;
    }

    [Flags]
    public enum CampaignDeliveryChannel : byte
    {
        None = 0,
        Inbox = 1,
        PushNotification = 2,
        Email = 4,
        SMS = 8
    }
}
