using Grpc.Net.Client;

namespace MediatX.GRPC;

public class RemoteServiceDefinition
{
  public string Uri { get; set; }
  public GrpcChannelOptions ChannelOptions { get; set; }
}
