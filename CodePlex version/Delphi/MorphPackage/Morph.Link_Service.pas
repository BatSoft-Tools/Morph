unit Morph.Link_Service;

interface

uses
  Morph.Core.Types, Morph.Core.Link, Morph.Core.Reader, Morph.Core.Action,
  Morph.Core.Streams, Morph.Link_Message, Morph.Endpoint.Apartment;

type
  TLink_Service = class(TMorphLink)
  private
    fServiceName: string;
  protected
    function GetLinkType: TLinkType; override;
    function GetSize: TMorphLinkSize; override;
  public
    constructor Create(const AServiceName: string);
    procedure Write(const Writer: TMorphStreamWriter); override;
    property  ServiceName: string read fServiceName;
    function  Equals(Obj: TObject): Boolean; override;
    function  ToString: string; override;
  end;

  TLink_Apartment = class(TMorphLink)
  private
    fApartmentID: Int32;
    fIsSession: Boolean;
    fSessionID: Int64;
  protected
    function GetLinkType: TLinkType; override;
    function GetSize: TMorphLinkSize; override;
  public
    constructor Create(const AApartment: TApartment); overload;
    constructor Create(const AApartmentID: TApartmentID); overload;
    constructor Create(const AApartmentID: TApartmentID; const ASessionID: TSessionID); overload;
    procedure Write(const Writer: TMorphStreamWriter); override;
    property  ApartmentID : Int32   read fApartmentID;
    property  IsSession   : Boolean read fIsSession;
    property  SessionID   : Int64   read fSessionID;
    function  Equals(Obj: TObject): Boolean; override;
    function  ToString: string; override;
  end;

  TLink_ApartmentProxy = class(TMorphLink)
  private
    fApartmentProxyID: Int32;
  protected
    function GetLinkType: TLinkType; override;
    function GetSize: TMorphLinkSize; override;
  public
    constructor Create(const AApartmentProxyID: Int32);
    procedure Write(const Writer: TMorphStreamWriter); override;
    property  ApartmentProxyID: Int32  read fApartmentProxyID;
    function  Equals(Obj: TObject): Boolean; override;
    function  ToString: string; override;
  end;

  TLinkReader_Service = class(TInterfacedObject, IMorphLinkReader)
  public
    function Read(const Reader: TMorphStreamReader; const FlagX, FlagY, FlagZ: Boolean): TMorphLink;
  end;

  TLinkAction_Service = class(TInterfacedObject, IMorphLinkAction)
  protected
    procedure ActionService(const Message: TMessage; const Link: TLink_Service); virtual;
    procedure ActionApartment(const Message: TMessage; const Link: TLink_Apartment); virtual;
    procedure ActionApartmentProxy(const Message: TMessage; const Link: TLink_ApartmentProxy); virtual;
  public
    procedure Action(const Message: TMessage);
  end;

implementation

uses
  SysUtils, Morph.Core.Errors, Morph.Endpoint.Service, Morph.Endpoint.Proxy;

{ TLinkService }

constructor TLink_Service.Create(const AServiceName: string);
begin
  inherited Create;
  fServiceName  := AServiceName;
end;

function TLink_Service.Equals(Obj: TObject): Boolean;
begin
  Result  :=
    (Obj is TLink_Service) and
    (TLink_Service(Obj).ServiceName = Self.ServiceName)
end;

function TLink_Service.GetLinkType: TLinkType;
begin
  Result  := linkService
end;

function TLink_Service.GetSize: TMorphLinkSize;
begin
  Result  := 1 + IdentifierByteLength(fServiceName);
end;

function TLink_Service.ToString: string;
begin
  Result  := '{Service ' + ServiceName + '}'
end;

procedure TLink_Service.Write(const Writer: TMorphStreamWriter);
begin
  Writer.WriteLinkByte(LinkType, False, False, False);
  Writer.WriteIdentifier(fServiceName);
end;

{ TLinkApartment }

constructor TLink_Apartment.Create(const AApartment: TApartment);
begin
  inherited Create;
  fApartmentID  := AApartment.ApartmentID;
  fIsSession    := AApartment is TApartmentSession;
  if  fIsSession  then
    fSessionID    := (AApartment as TApartmentSession).SessionID;
end;

constructor TLink_Apartment.Create(const AApartmentID: TApartmentID);
begin
  inherited Create;
  fApartmentID  := AApartmentID;
end;

constructor TLink_Apartment.Create(const AApartmentID: TApartmentID; const ASessionID: TSessionID);
begin
  inherited Create;
  fApartmentID  := AApartmentID;
  fIsSession    := True;
  fSessionID    := ASessionID;
end;

function TLink_Apartment.Equals(Obj: TObject): Boolean;
begin
  Result  :=
    (Obj is TLink_Apartment) and
    (TLink_Apartment(Obj).ApartmentID = Self.ApartmentID)
end;

function TLink_Apartment.GetLinkType: TLinkType;
begin
  Result  := linkService
end;

function TLink_Apartment.GetSize: TMorphLinkSize;
begin
  Result  := 1 + SizeOf(TApartmentID);
  if  IsSession then
    inc(Result, SizeOf(TSessionID))
end;

function TLink_Apartment.ToString: string;
begin
  Result  := '{Apartment ' + IntToStr(ApartmentID) + '}'
end;

procedure TLink_Apartment.Write(const Writer: TMorphStreamWriter);
begin
  Writer.WriteLinkByte(LinkType, True, False, IsSession);
  Writer.WriteInt32(fApartmentID);
  if  IsSession then
    Writer.WriteInt64(fSessionID);
end;

{ TLinkApartmentProxy }

constructor TLink_ApartmentProxy.Create(const AApartmentProxyID: Int32);
begin
  inherited Create;
  fApartmentProxyID := AApartmentProxyID;
end;

function TLink_ApartmentProxy.Equals(Obj: TObject): Boolean;
begin
  Result  :=
    (Obj is TLink_ApartmentProxy) and
    (TLink_ApartmentProxy(Obj).ApartmentProxyID = Self.ApartmentProxyID)
end;

function TLink_ApartmentProxy.GetLinkType: TLinkType;
begin
  Result  := linkService
end;

function TLink_ApartmentProxy.GetSize: TMorphLinkSize;
begin
  Result  := 1 + SizeOf(TApartmentProxyID);
end;

function TLink_ApartmentProxy.ToString: string;
begin
  Result  := '{ApartmentProxy ' + IntToStr(ApartmentProxyID) + '}'
end;

procedure TLink_ApartmentProxy.Write(const Writer: TMorphStreamWriter);
begin
  Writer.WriteLinkByte(LinkType, True, True, False);
  Writer.WriteInt32(fApartmentProxyID);
end;

{ TLinkReaderService }

function TLinkReader_Service.Read(const Reader: TMorphStreamReader; const FlagX, FlagY, FlagZ: Boolean): TMorphLink;
var
  ApartmentID: Int32;
begin
  if  not FlagX and not FlagY then
    Result  := TLink_Service.Create(Reader.ReadIdentifier)
  else if FlagX and not FlagY then
    begin
      ApartmentID := Reader.ReadInt32;
      if  FlagZ then
        Result  := TLink_Apartment.Create(ApartmentID, Reader.ReadInt64)
      else
        Result  := TLink_Apartment.Create(ApartmentID);
    end
  else if FlagX and     FlagY then
    Result  := TLink_ApartmentProxy.Create(Reader.ReadInt32)
  else
    raise EMorphUsage.Create('Unrecognised service link');
end;

{ TLinkAction_Service }

procedure TLinkAction_Service.Action(const Message: TMessage);
begin
  //  Do specific action
  if  Message.Current is TLink_Service  then
    ActionService(Message, Message.Current as TLink_Service) else
  if  Message.Current is TLink_Apartment  then
    ActionApartment(Message, Message.Current as TLink_Apartment) else
  if  Message.Current is TLink_ApartmentProxy then
    ActionApartmentProxy(Message, Message.Current as TLink_ApartmentProxy);
  //  Move along
  Message.PathNext;
  MorphActions.Action(Message);
end;

procedure TLinkAction_Service.ActionApartment(const Message: TMessage; const Link: TLink_Apartment);
var
  Apartment: TApartment;
begin
  //  Find apartment
  Apartment := AllApartments.Request(Link.ApartmentID);
  if  not Assigned(Apartment) then
    raise EMorph.Create('Apartment not found');
  //  Set the context
  Message.Context := Apartment;
end;

procedure TLinkAction_Service.ActionApartmentProxy(const Message: TMessage; const Link: TLink_ApartmentProxy);
var
  ApartmentProxy: TApartmentProxy;
begin
  //  Find apartment proxy
  ApartmentProxy  := AllApartmentProxies.Request(Link.ApartmentProxyID);
  if  not Assigned(ApartmentProxy) then
    raise EMorph.Create('Apartment proxy not found');
  //  Might need to update path to apartment
  if  Message.HasPathFrom then
    ApartmentProxy.Path := Message.PathFrom;
  //  Set the context
  Message.Context := ApartmentProxy;
end;

procedure TLinkAction_Service.ActionService(const Message: TMessage; const Link: TLink_Service);
var
  Service: TService;
  Apartment: TApartment;
begin
  //  Find service
  Service := AllServices.Values[Link.ServiceName];
  if  not Assigned(Service) then
    raise EMorph.Create('Service not registered: ' + Link.ServiceName);
  //  Obtain apartment
  Apartment := Service.ObtainApartment;
  if  not Assigned(Apartment) then
    raise EMorph.Create('Apartment not obtained from service: ' + Link.ServiceName);
  //  Update the path to the client side
  if  Apartment is TApartmentSession  then
    (Apartment as TApartmentSession).PathToProxy  := Message.PathFrom;
  //  Replace service link with apartment link
  Message.PathTo.Pop;
  Message.PathTo.Push(TLink_Apartment.Create(Apartment));
  //  Set the context
  Message.Context := Apartment;
end;

end.
