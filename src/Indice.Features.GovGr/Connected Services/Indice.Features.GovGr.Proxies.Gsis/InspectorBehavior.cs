using System.Runtime.Serialization;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel;

namespace Indice.Features.GovGr.Proxies.Gsis;

/// <summary>Implements methods that can be used to extend run-time behavior for an endpoint in either a service or client application.</summary>
public class InspectorBehavior : IEndpointBehavior
{
    private readonly MessageInspector _messageInspector;
    /// <summary>Creates a InspectorBehavior</summary>
    public InspectorBehavior(MessageInspector messageInspector) {
        _messageInspector = messageInspector ?? throw new ArgumentNullException(nameof(messageInspector));
    }
    void IEndpointBehavior.AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }
    void IEndpointBehavior.ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime) {
        clientRuntime.ClientMessageInspectors.Add(_messageInspector);
    }
    void IEndpointBehavior.ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }
    void IEndpointBehavior.Validate(ServiceEndpoint endpoint) { }
}

/// <summary>Message inspector used to apply the required header before sending the message.</summary>
public class MessageInspector : IClientMessageInspector
{
    private readonly string _username;
    private readonly string _password;
    /// <summary>Creates a MessageInspector</summary>
    public MessageInspector(string username, string password) {
        _username = username ?? throw new ArgumentNullException(nameof(username));
        _password = password ?? throw new ArgumentNullException(nameof(password));
    }
    void IClientMessageInspector.AfterReceiveReply(ref Message reply, object correlationState) { }
    object? IClientMessageInspector.BeforeSendRequest(ref Message request, IClientChannel channel) {
        var header = new Header { UsernameToken = new UsernameToken() { Username = _username, Password = _password } };
        var messageHeader = new MessageHeader<Header>() { Actor = "", Content = header };
        request.Headers.Add(messageHeader.GetUntypedHeader("Security", "ns1"));

        return null;
    }
}

[DataContract(Namespace = "ns1")]
class Header
{
    [DataMember]
    public UsernameToken UsernameToken { get; set; } = null!;
}
/// <summary>Authentication credentials</summary>
public class UsernameToken
{
    /// <summary>Username</summary>
    [DataMember]
    public string Username { get; set; } = null!;
    /// <summary>Password</summary>
    [DataMember]
    public string Password { get; set; } = null!;
}
