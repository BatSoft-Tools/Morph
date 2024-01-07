unit Morph.Link_Internet;

interface

uses
  Morph.Core.Types, Morph.Core.Link, Morph.Core.Reader, Morph.Core.Action,
  Morph.Core.Streams, Morph.Link_Message;

type
  TPort = UInt16;
const
  MorphPort = $E000;  //  Should be of type TPort, but then compiler doesn't like MorphPort as a default parameter.

type
  TLinkInternet = class(TMorphLink)
  private
    fPort: TPort;
    function GetHasPort: Boolean;
  protected
    function GetLinkType: TLinkType; override;
    function GetSize: TMorphLinkSize; override;
  public
    property  Port    : TPort   read fPort  write fPort;
    property  HasPort : Boolean read GetHasPort;
  end;

  TIPv4 = packed array [$0..$3] of byte;
  TIPv6 = packed array [$0..$7] of UInt16;

  TLink_URI = class(TLinkInternet)
  private
    fAddress: string;
  protected
    function GetSize: TMorphLinkSize; override;
  public
    constructor Create(const AAddress: string);
    procedure Write(const Writer: TMorphStreamWriter); override;
    property  Address: string read fAddress;
    function  Equals(Obj: TObject): Boolean; override;
    function  ToString: string; override;
  end;

  TLink_IPv4 = class(TLinkInternet)
  private
    fAddress: TIPv4;
  protected
    function GetSize: TMorphLinkSize; override;
  public
    constructor Create(const AAddress: TIPv4);
    procedure Write(const Writer: TMorphStreamWriter); override;
    property  Address: TIPv4  read fAddress;
    function  Equals(Obj: TObject): Boolean; override;
    function  ToString: string; override;
  end;

  TLink_IPv6 = class(TLinkInternet)
  private
    fAddress: TIPv6;
  protected
    function GetSize: TMorphLinkSize; override;
  public
    constructor Create(const AAddress: TIPv6);
    procedure Write(const Writer: TMorphStreamWriter); override;
    property  Address: TIPv6  read fAddress;
    function  Equals(Obj: TObject): Boolean; override;
    function  ToString: string; override;
  end;

  TLinkReader_Internet = class(TInterfacedObject, IMorphLinkReader)
  public
    function Read(const Reader: TMorphStreamReader; const FlagX, FlagY, FlagZ: Boolean): TMorphLink;
  end;

  TLinkAction_Internet = class(TInterfacedObject, IMorphLinkAction)
  private
    procedure ActionURI(const Message: TMessage; const Link: TLink_URI);
    procedure ActionIPv4(const Message: TMessage; const Link: TLink_IPv4);
    procedure ActionIPv6(const Message: TMessage; const Link: TLink_IPv6);
  public
    procedure Action(const Message: TMessage);
  end;

implementation

uses
  SysUtils, Morph.Core.Errors;

{ TLink_Internet }

function TLinkInternet.GetHasPort: Boolean;
begin
  Result  := fPort <> MorphPort
end;

function TLinkInternet.GetLinkType: TLinkType;
begin
  Result  := linkInternet
end;

function TLinkInternet.GetSize: TMorphLinkSize;
begin
  Result  := 1;
  if  HasPort then
    inc(Result, 2);
end;

{ TLink_URI }

constructor TLink_URI.Create(const AAddress: string);
begin
  inherited Create;
  fAddress  := fAddress;
end;

function TLink_URI.Equals(Obj: TObject): Boolean;
begin
  Result  :=
    (Obj is TLink_URI) and
    SameText(TLink_URI(Obj).Address, Self.Address)
end;

function TLink_URI.GetSize: TMorphLinkSize;
begin
  Result  := inherited GetSize + IdentifierByteLength(fAddress);
end;

function TLink_URI.ToString: string;
begin
  Result  := '{Internet ' + fAddress;
  if  HasPort then
    Result  := Result + ':' + IntToStr(fPort);
  Result  := Result + '}'
end;

procedure TLink_URI.Write(const Writer: TMorphStreamWriter);
begin
  Writer.WriteLinkByte(LinkType, False, True, HasPort);
  Writer.WriteIdentifier(fAddress);
  if  HasPort then
    Writer.WriteInt16(fPort);
end;

{ TLink_IPv4 }

constructor TLink_IPv4.Create(const AAddress: TIPv4);
begin
  inherited Create;
  fAddress  := fAddress;
end;

function TLink_IPv4.Equals(Obj: TObject): Boolean;
begin
  Result  :=
    (Obj is TLink_IPv4) and
    (TLink_IPv4(Obj).Address[0] = Self.Address[0]) and
    (TLink_IPv4(Obj).Address[1] = Self.Address[1]) and
    (TLink_IPv4(Obj).Address[2] = Self.Address[2]) and
    (TLink_IPv4(Obj).Address[3] = Self.Address[3])
end;

function TLink_IPv4.GetSize: TMorphLinkSize;
begin
  Result  := inherited GetSize + SizeOf(TIPv4)
end;

function TLink_IPv4.ToString: string;
begin
  Result  := Format('{Internet %d.%d.%d.%d', [fAddress[0], fAddress[1], fAddress[2], fAddress[3]]);
  if  HasPort then
    Result  := Result + ':' + IntToStr(fPort);
  Result  := Result + '}'
end;

procedure TLink_IPv4.Write(const Writer: TMorphStreamWriter);
begin
  Writer.WriteLinkByte(LinkType, False, False, HasPort);
  Writer.WriteBytes(fAddress, SizeOf(TIPv4));
  if  HasPort then
    Writer.WriteInt16(fPort);
end;

{ TLink_IPv6 }

constructor TLink_IPv6.Create(const AAddress: TIPv6);
begin
  inherited Create;
  fAddress  := fAddress;
end;

function TLink_IPv6.Equals(Obj: TObject): Boolean;
begin
  Result  :=
    (Obj is TLink_IPv6) and
    (TLink_IPv6(Obj).Address[0] = Self.Address[0]) and
    (TLink_IPv6(Obj).Address[1] = Self.Address[1]) and
    (TLink_IPv6(Obj).Address[2] = Self.Address[2]) and
    (TLink_IPv6(Obj).Address[3] = Self.Address[3]) and
    (TLink_IPv6(Obj).Address[4] = Self.Address[4]) and
    (TLink_IPv6(Obj).Address[5] = Self.Address[5]) and
    (TLink_IPv6(Obj).Address[6] = Self.Address[6]) and
    (TLink_IPv6(Obj).Address[7] = Self.Address[7])
end;

function TLink_IPv6.GetSize: TMorphLinkSize;
begin
  Result  := inherited GetSize + SizeOf(TIPv6)
end;

function TLink_IPv6.ToString: string;
type
  TZeroState = (zNone, zFound, zDone);
const
  Colon = ':';
var
  i: integer;
  ZeroState: TZeroState;

  procedure AddValue(const Value: UInt16);

    function Join(const Str1, Str2: string): string;
    begin
      if  Str1 = '' then
        Exit(Str2);
      if  Str2 = '' then
        Exit(Str1);
      Result  := Str1 + Colon + Str2;
    end;

  begin
    if  Value <> 0  then
      begin
        if  ZeroState = zFound  then
          ZeroState := zDone;
        Result  := Join(Result, IntToHex(Value, 1))
      end
    else if ZeroState = zNone then
      begin
        ZeroState := zFound;
        Result  := Result + Colon;
      end;
  end;

begin
  ZeroState := zNone;
  Result    := '';
  for i := 0 to 7 do
    AddValue(fAddress[i]);
  if  ZeroState = zFound  then
    Result  := Result + Colon;
  Result  := '{Internet [' + Result + ']';
  if  HasPort then
    Result  := Result + ':' + IntToStr(fPort);
  Result  := Result + '}'
end;

procedure TLink_IPv6.Write(const Writer: TMorphStreamWriter);
begin
  Writer.WriteLinkByte(LinkType, True, False, HasPort);
  Writer.WriteBytes(fAddress, SizeOf(TIPv6));
  if  HasPort then
    Writer.WriteInt16(fPort);
end;

{ TLinkReader_Internet }

function TLinkReader_Internet.Read(const Reader: TMorphStreamReader; const FlagX, FlagY, FlagZ: Boolean): TMorphLink;
var
  IPv4: TIPv4;
  IPv6: TIPv6;
begin
  if  FlagY then
    Result  := TLink_URI.Create(Reader.ReadIdentifier)
  else if FlagX then
    begin
      Reader.ReadBytes(IPv6, SizeOf(TLink_IPv6));
      Result  := TLink_IPv6.Create(IPv6);
    end
  else
    begin
      Reader.ReadBytes(IPv4, SizeOf(TLink_IPv4));
      Result  := TLink_IPv4.Create(IPv4);
    end;
  try
    if  FlagZ then
      (Result as TLinkInternet).Port  := Reader.ReadInt16;
  except
    Result.Free;
    raise;
  end;
end;

{ TLinkAction_Internet }

procedure TLinkAction_Internet.Action(const Message: TMessage);
var
  Link: TLinkInternet;
begin
  Link  := Message.Current as TLinkInternet;
  if  Link is TLink_URI   then  ActionURI (Message, TLink_URI (Link)) else
  if  Link is TLink_IPv4  then  ActionIPv4(Message, TLink_IPv4(Link)) else
  if  Link is TLink_IPv6  then  ActionIPv6(Message, TLink_IPv6(Link)) else
    raise EMorphImplementation.Create('Expected internet link.');
end;

procedure TLinkAction_Internet.ActionIPv4(const Message: TMessage; const Link: TLink_IPv4);
begin
//TODO : TLinkAction_Internet.Action
end;

procedure TLinkAction_Internet.ActionIPv6(const Message: TMessage; const Link: TLink_IPv6);
begin
//TODO : TLinkAction_Internet.Action
end;

procedure TLinkAction_Internet.ActionURI(const Message: TMessage; const Link: TLink_URI);
begin
//TODO : TLinkAction_Internet.Action
end;

end.
