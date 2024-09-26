﻿using Indice.AspNetCore.Views.Models;
using Indice.Services;
using Moq;
using Xunit;

namespace Indice.AspNetCore.Tests;

public class EmailRenderingTests
{
    public EmailRenderingTests() {
        TestServer = new 
            ();
    }

    private TestServerFixture TestServer { get; set; }

    [Fact]
    public async Task Can_Render_Simple_Template() {
        var renderingEngine = TestServer.GetRequiredService<IHtmlRenderingEngine>();
        Assert.IsType<HtmlRenderingEngineMvcRazor>(renderingEngine);
        var model = new EmailModel {
            Salutation = "Mr.",
            FullName = "Georgios",
            Message = "Good for you to write some tests. Be a man!"
        };
        var output = await renderingEngine.RenderAsync("Simple", model);
        var outputFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Outputs\\simple.html");
        if (!File.Exists(outputFilePath)) {
            throw new FileNotFoundException($"Output file '{outputFilePath}' does not exist");
        }
        var expectedOutput = await File.ReadAllTextAsync(outputFilePath);
        Assert.Equal(expectedOutput, output);
    }

    [Fact]
    public async Task Can_Render_Simple_Template_With_Anonymous_Model() {
        var renderingEngine = TestServer.GetRequiredService<IHtmlRenderingEngine>();
        Assert.IsType<HtmlRenderingEngineMvcRazor>(renderingEngine);
        var model = new {
            Salutation = "Mr.",
            FullName = "Georgios",
            Message = "Good for you to write some tests. Be a man!"
        };
        var output = await renderingEngine.RenderAsync("SimpleWithAnonymousModel", model);
        var outputFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Outputs\\simple.html");
        if (!File.Exists(outputFilePath)) {
            throw new FileNotFoundException($"Output file '{outputFilePath}' does not exist");
        }
        var expectedOutput = await File.ReadAllTextAsync(outputFilePath);
        Assert.Equal(expectedOutput, output);
    }

    [Fact]
    public async Task Can_Send_Email_With_Simple_Template() {
        var emailServiceMock = new Mock<IEmailService>();
        var renderingEngine = TestServer.GetRequiredService<IHtmlRenderingEngine>();
        emailServiceMock.Setup(x => x.HtmlRenderingEngine).Returns(renderingEngine);
        await emailServiceMock.Object.SendAsync(messageBuilder => messageBuilder
            .To("g.manoltzas@indice.gr")
            .WithSubject("Verification")
            .WithData(new EmailModel {
                Salutation = "Mr.",
                FullName = "Georgios",
                Message = "Good for you to write some tests. Be a man!"
            })
            .UsingTemplate("Simple")
        );
        var outputFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Outputs\\simple.html");
        if (!File.Exists(outputFilePath)) {
            throw new FileNotFoundException($"Output file '{outputFilePath}' does not exist");
        }
        var expectedBody = await File.ReadAllTextAsync(outputFilePath);
        emailServiceMock.Verify(p => p.SendAsync(
            It.Is<string[]>(recipients => recipients[0] == "g.manoltzas@indice.gr"),
            It.Is<string>(subject => subject == "Verification"),
            It.Is<string>(body => body == expectedBody),
            It.Is<EmailAttachment[]>(attachments => attachments.Count() == 0),
            It.Is<EmailSender>(from => from == null)
        ), Times.Once);
    }

    [Fact]
    public async Task Can_Send_Email_Without_Template() {
        const string STATIC_BODY = "<!DOCTYPE html><html><head><title>Page Title</title></head><body><p>Hello my friend!</p></body></html>";
        var emailServiceMock = new Mock<IEmailService>();
        await emailServiceMock.Object.SendAsync(messageBuilder => messageBuilder
            .To("g.manoltzas@indice.gr")
            .WithSubject("Verification")
            .WithBody(STATIC_BODY)
        );
        emailServiceMock.Verify(p => p.SendAsync(
            It.Is<string[]>(recipients => recipients[0] == "g.manoltzas@indice.gr"),
            It.Is<string>(subject => subject == "Verification"),
            It.Is<string>(body => body == STATIC_BODY),
            It.Is<EmailAttachment[]>(attachments => attachments.Count() == 0),
            It.Is<EmailSender>(from => from == null)
        ), Times.Once);
    }
}
