unit Morph.Link_Member;

interface

uses
  Morph.Core.Types, Morph.Core.Link, Morph.Core.Reader, Morph.Core.Action,
  Morph.Core.Streams, Morph.Link_Message;

type
  TLinkMember = class(TMorphLink)
  private
    fName: string;
  protected
    function GetLinkType: TLinkType; override;
    function GetSize: TMorphLinkSize; override;
  public
    constructor Create(const AName: string);
    procedure Write(const Writer: TMorphStreamWriter); override;
    property  Name: string  read fName;
  end;

  TLink_Method = class(TLinkMember)
  public
    function  ToString: string; override;
    function  Equals(Obj: TObject): Boolean; override;
  end;

  TLink_Property = class(TLinkMember)
  private
    fIsSetter: Boolean;
    fHasIndex: Boolean;
  public
    constructor Create(const AName: string; const AIsSetter, AHasIndex: Boolean);
    property  IsSetter  : Boolean read fIsSetter;
    property  HasIndex  : Boolean read fHasIndex;
    function  Equals(Obj: TObject): Boolean; override;
    function  ToString: string; override;
  end;

  TLinkReader_Member = class(TInterfacedObject, IMorphLinkReader)
  public
    function Read(const Reader: TMorphStreamReader; const FlagX, FlagY, FlagZ: Boolean): TMorphLink;
  end;

  TLinkAction_Member = class(TInterfacedObject, IMorphLinkAction)
  public
    procedure Action(const Message: TMessage);
  end;

implementation

uses
  System.Rtti,
  Morph.Core.Errors, Morph.Link_Data, Morph.Endpoint.Apartment, Morph.Endpoint.Servlet, Morph.Endpoint.Values;

{ TLink_Member }

constructor TLinkMember.Create(const AName: string);
begin
  inherited Create;
  fName  := AName;
end;

function TLinkMember.GetLinkType: TLinkType;
begin
  Result  := linkMember
end;

function TLinkMember.GetSize: TMorphLinkSize;
begin
  Result  := 1 + IdentifierByteLength(fName)
end;

procedure TLinkMember.Write(const Writer: TMorphStreamWriter);
begin
  Writer.WriteLinkByte(LinkType, Self is TLink_Property, False, False);
  Writer.WriteIdentifier(fName);
end;

{ TLink_Method }

function TLink_Method.Equals(Obj: TObject): Boolean;
begin
  Result  :=
    (Obj is TLink_Method) and
    (TLinkMember(Obj).Name = Self.Name)
end;

function TLink_Method.ToString: string;
begin
  Result  := '{Method ' + Name + '}'
end;

{ TLink_Property }

constructor TLink_Property.Create(const AName: string; const AIsSetter, AHasIndex: Boolean);
begin
  inherited Create(AName);
  fIsSetter := AIsSetter;
  fHasIndex := AHasIndex;
end;

function TLink_Property.Equals(Obj: TObject): Boolean;
begin
  Result  :=
    (Obj is TLink_Property) and
    (TLink_Property(Obj).IsSetter = Self.IsSetter) and
    (TLink_Property(Obj).HasIndex = Self.HasIndex)
end;

function TLink_Property.ToString: string;
begin
  Result  := '{Property ';
  if  IsSetter  then
    Result  := Result + 'Set '
  else
    Result  := Result + 'Get ';
  Result  := Result + Name;
  if  HasIndex  then
    Result  := Result + '[]';
  Result  := Result + '}'
end;

{ TLinkReader_Member }

function TLinkReader_Member.Read(const Reader: TMorphStreamReader; const FlagX, FlagY, FlagZ: Boolean): TMorphLink;
begin
  if  not FlagX then
    Result  := TLink_Method.Create(Reader.ReadIdentifier)
  else
    Result  := TLink_Property.Create(Reader.ReadIdentifier, FlagY, FlagZ);
end;

{ TLinkAction_Member }

procedure TLinkAction_Member.Action(const Message: TMessage);
var
  Servlet: TServlet;
begin
  //  Obtain servlet
  if  Message.Context is TApartment then
    Servlet := (Message.Context as TApartment).Servlets.ByKey(NoID)
  else if Message.Context is TServlet then
    Servlet := TServlet(Message.Context)
  else
    Servlet := nil;
  if  not Assigned(Servlet) then
    raise EMorphUsage.Create('Member without servlet.');
  //  Obtain parameters
  Message.PathNext;
  if  Message.Current is TLink_Data then


  //  Invoke

  //  Send reply

  //TODO : TLinkAction_Member.Action
end;

end.
