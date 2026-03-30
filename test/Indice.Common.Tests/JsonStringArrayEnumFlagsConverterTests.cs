using System;
using System.Text.Json;
using Indice.Serialization;
using Xunit;

namespace Indice.Common.Tests;

public class JsonStringArrayEnumFlagsConverterTests
{
    public JsonStringArrayEnumFlagsConverterTests() {
        Options = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        Options.Converters.Insert(0, new JsonStringArrayEnumFlagsConverterFactory());
    }

    public JsonSerializerOptions Options { get; }

    [Fact]
    public void CanSerializeEnumFlagsToStringArray() {
        var expectedJson = @"{""id"":1,""name"":""Sales Christmas 2021"",""deliveryChannel"":[""Inbox"",""PushNotification""]}";
        Assert.Equal(JsonSerializer.Serialize(new Campaign {
            Id = 1,
            Name = "Sales Christmas 2021",
            DeliveryChannel = CampaignDeliveryChannel.Inbox | CampaignDeliveryChannel.PushNotification
        }, Options), expectedJson);
        Assert.Equal(JsonSerializer.Serialize(new Campaign2 {
            Id = 1,
            Name = "Sales Christmas 2021",
            DeliveryChannel = CampaignDeliveryChannel.Inbox | CampaignDeliveryChannel.PushNotification
        }, Options), expectedJson);
    }

    [Fact]
    public void CanDeserializeEnumFlagsToStringArray() {
        var json = @"{""id"":2,""name"":""Sales Christmas 2021"",""deliveryChannel"":[""Inbox"",""PushNotification"",""Email""]}";
        var campaign = JsonSerializer.Deserialize<Campaign>(json, Options)!;
        Assert.Equal(2, campaign.Id);
        Assert.Equal("Sales Christmas 2021", campaign.Name);
        Assert.Equal(CampaignDeliveryChannel.Inbox | CampaignDeliveryChannel.PushNotification | CampaignDeliveryChannel.Email, campaign.DeliveryChannel);
        var campaign2 = JsonSerializer.Deserialize<Campaign2>(json, Options)!;
        Assert.Equal(2, campaign2.Id);
        Assert.Equal("Sales Christmas 2021", campaign2.Name);
        Assert.Equal(CampaignDeliveryChannel.Inbox | CampaignDeliveryChannel.PushNotification | CampaignDeliveryChannel.Email, campaign2.DeliveryChannel);
    }

    [Fact]
    public void CanSerializeEnumFlagsToStringArrayWhenNull() {
        var campaign = new Campaign2 {
            Id = 1,
            Name = "Sales Christmas 2021"
        };
        var expectedJson = @"{""id"":1,""name"":""Sales Christmas 2021"",""deliveryChannel"":null}";
        var campaignJson = JsonSerializer.Serialize(campaign, Options);
        Assert.Equal(expectedJson, campaignJson);
    }

    [Fact]
    public void CanDeserializeEnumFlagsToStringArrayWhenNullable() {
        var expectedCampaign = new Campaign2 {
            Id = 1,
            Name = "Sales Christmas 2021"
        };
        var expectedCampaign2 = new Campaign2 {
            Id = 1,
            Name = "Sales Christmas 2021",
            DeliveryChannel = CampaignDeliveryChannel.Inbox | CampaignDeliveryChannel.PushNotification
        };
        var json = @"{""id"":1,""name"":""Sales Christmas 2021"",""deliveryChannel"":null}";
        var json2 = @"{""id"":1,""name"":""Sales Christmas 2021"",""deliveryChannel"": [""Inbox"", ""PushNotification""]}";
        var json3 = @"{""id"":1,""name"":""Sales Christmas 2021"",""deliveryChannel"": ""Email, SMS""}";
        var campaign = JsonSerializer.Deserialize<Campaign2>(json, Options)!;
        var campaign2 = JsonSerializer.Deserialize<Campaign2>(json2, Options)!;
        var campaign3 = JsonSerializer.Deserialize<Campaign2>(json3, Options)!;
        Assert.Equal(expectedCampaign.DeliveryChannel, campaign.DeliveryChannel);
        Assert.Equal(expectedCampaign2.DeliveryChannel, campaign2.DeliveryChannel);
        Assert.Equal(CampaignDeliveryChannel.Email | CampaignDeliveryChannel.SMS, campaign3.DeliveryChannel);
    }
}

public class Campaign
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public CampaignDeliveryChannel DeliveryChannel { get; set; } = CampaignDeliveryChannel.Inbox;
}

public class Campaign2
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public CampaignDeliveryChannel? DeliveryChannel { get; set; }
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
