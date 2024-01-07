unit Morph.Link_Servlet;

interface

uses
  Morph.Core.Types, Morph.Core.Link, Morph.Core.Reader, Morph.Core.Action,
  Morph.Core.Streams, Morph.Link_Message, Morph.Endpoint.Servlet;

type
  TLink_Servlet = class(TMorphLink)
  private
    fServletID: TServletID;
  protected
    function GetLinkType: TLinkType; override;
    function GetSize: TMorphLinkSize; override;
  public
    constructor Create(const AServletID: TServletID);
    procedure Write(const Writer: TMorphStreamWriter); override;
    property  ServletID: TServletID read fServletID;
    function  Equals(Obj: TObject): Boolean; override;
    function  ToString: string; override;
  end;

  TLinkReader_Servlet = class(TInterfacedObject, IMorphLinkReader)
  public
    function Read(const Reader: TMorphStreamReader; const FlagX, FlagY, FlagZ: Boolean): TMorphLink;
  end;

  TLinkAction_Servlet = class(TInterfacedObject, IMorphLinkAction)
  public
    procedure Action(const Message: TMessage);
  end;

implementation

uses
  SysUtils, Morph.Core.Errors, Morph.Endpoint.Apartment;

{ TLink_Servlet }

constructor TLink_Servlet.Create(const AServletID: TServletID);
begin
  inherited Create;
  fServletID  := AServletID;
end;

function TLink_Servlet.Equals(Obj: TObject): Boolean;
begin
  Result  :=
    (Obj is TLink_Servlet) and
    (TLink_Servlet(Obj).ServletID = Self.ServletID)
end;

function TLink_Servlet.GetLinkType: TLinkType;
begin
  Result  := linkServlet
end;

function TLink_Servlet.GetSize: TMorphLinkSize;
begin
  Result  := 1 + SizeOf(TServletID)
end;

function TLink_Servlet.ToString: string;
begin
  Result  := '{Servlet ' + IntToStr(ServletID) + '}'
end;

procedure TLink_Servlet.Write(const Writer: TMorphStreamWriter);
begin
  Writer.WriteLinkByte(LinkType, False, False, False);
  Writer.WriteInt32(fServletID);
end;

{ TLinkReader_Servlet }

function TLinkReader_Servlet.Read(const Reader: TMorphStreamReader; const FlagX, FlagY, FlagZ: Boolean): TMorphLink;
begin
  Result  := TLink_Servlet.Create(Reader.ReadInt32);
end;

{ TLinkAction_Servlet }

procedure TLinkAction_Servlet.Action(const Message: TMessage);
var
  ServletID: TServletID;
  Servlet: TServlet;
begin
  //  Servlets exist only within apartments
  if  not (Message.Context is TApartment) then
    raise EMorphUsage.Create('Servlet without apartment.');
  //
  ServletID := (Message.Current as TLink_Servlet).ServletID;
  Servlet   := (Message.Context as TApartment).Servlets.ByKey(ServletID);
  if  not Assigned(Servlet) then
    raise EMorphUsage.Create('Servlet not found.');
  Message.Context := Servlet;
  //  Move along
  Message.PathNext;
  MorphActions.Action(Message);
end;

end.
