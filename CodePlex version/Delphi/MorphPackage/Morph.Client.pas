unit Morph.Client;

interface

uses
  Morph.Endpoint.Proxy, Morph.Link_Internet;

type
  MorphClient = class
  public
    class function  CreateClient(const ServiceName: string; const DomainName: string; const Port: TPort = MorphPort): TApartmentProxy; overload;
    class function  CreateClient(const ServiceName: string; const IPv4      : TIPv4;  const Port: TPort = MorphPort): TApartmentProxy; overload;
    class function  CreateClient(const ServiceName: string; const IPv6      : TIPv6;  const Port: TPort = MorphPort): TApartmentProxy; overload;
  end;

implementation

uses
  Morph.Core.Types, Morph.Core.Reader, Morph.Core.Action, Morph.Core.Path,
  Morph.Link_End, Morph.Link_Message, Morph.Link_Service, Morph.Link_Servlet,
  Morph.Link_Member,
  Morph.Endpoint.Apartment;

procedure InitialiseMorph;

  procedure Register(const LinkType: TLinkType; Reader: IMorphLinkReader; Action: IMorphLinkAction);
  begin
    MorphReaders.RegisterReader(LinkType, Reader);
    MorphActions.RegisterAction(LinkType, Action);
  end;

begin
  Register(linkEnd,       TLinkReader_End.Create,       TLinkAction_End.Create);
  Register(linkMessage,   TLinkReader_Message.Create,   nil);
  Register(linkService,   TLinkReader_Service.Create,   TLinkAction_Service.Create);
  Register(linkServlet,   TLinkReader_Servlet.Create,   TLinkAction_Servlet.Create);
  Register(linkMember,    TLinkReader_Member.Create,    TLinkAction_Member.Create);
  Register(linkInternet,  TLinkReader_Internet.Create,  TLinkAction_Internet.Create);
end;

{ MorphClient }

class function MorphClient.CreateClient(const ServiceName: string; const DomainName: string; const Port: TPort): TApartmentProxy;
begin
  Result      := AllApartmentProxies.Obtain(True);
  Result.Path := TMorphPath.Create;
  Result.Path.Push(TLink_URI.Create(DomainName));
  Result.Path.Push(TLink_Service.Create(ServiceName));
end;

class function MorphClient.CreateClient(const ServiceName: string; const IPv4: TIPv4; const Port: TPort): TApartmentProxy;
begin
  Result  := AllApartmentProxies.Obtain(True);
  Result.Path.Push(TLink_IPv4.Create(IPv4));
  Result.Path.Push(TLink_Service.Create(ServiceName));
end;

class function MorphClient.CreateClient(const ServiceName: string; const IPv6: TIPv6; const Port: TPort): TApartmentProxy;
begin
  Result  := AllApartmentProxies.Obtain(True);
  Result.Path.Push(TLink_IPv6.Create(IPv6));
  Result.Path.Push(TLink_Service.Create(ServiceName));
end;

end.
