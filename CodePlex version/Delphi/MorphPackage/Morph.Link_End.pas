unit Morph.Link_End;

interface

uses
  Morph.Core.Types, Morph.Core.Link, Morph.Core.Reader, Morph.Core.Action,
  Morph.Core.Streams, Morph.Link_Message;

type
  TLink_End = class(TMorphLink)
  protected
    function GetLinkType: TLinkType; override;
    function GetSize: TMorphLinkSize; override;
  public
    procedure Write(const Writer: TMorphStreamWriter); override;
    function  Equals(Obj: TObject): Boolean; override;
    function  ToString: string; override;
  end;

  TLinkReader_End = class(TInterfacedObject, IMorphLinkReader)
  public
    function Read(const Reader: TMorphStreamReader; const FlagX, FlagY, FlagZ: Boolean): TMorphLink;
  end;

  TLinkAction_End = class(TInterfacedObject, IMorphLinkAction)
  public
    procedure Action(const Message: TMessage);
  end;

  IActionEnd = interface
    ['{74617048-30FA-46FD-900B-E0052F07718E}']
    procedure ActionEnd();
  end;

implementation

{ TLinkEnd }

function TLink_End.Equals(Obj: TObject): Boolean;
begin
  Result  := Obj is TLink_End
end;

function TLink_End.GetLinkType: TLinkType;
begin
  Result  := linkEnd
end;

function TLink_End.GetSize: TMorphLinkSize;
begin
  Result  := 1;
end;

function TLink_End.ToString: string;
begin
  Result  := '{End}'
end;

procedure TLink_End.Write(const Writer: TMorphStreamWriter);
begin
  Writer.WriteInt8(0);
end;

{ TNodeEnd }

function TLinkReader_End.Read(const Reader: TMorphStreamReader; const FlagX, FlagY, FlagZ: Boolean): TMorphLink;
begin
  Result  := TLink_End.Create;
end;

{ TLinkEndAction }

procedure TLinkAction_End.Action(const Message: TMessage);
var
  Endable: IActionEnd;
begin
  if  Message.ContextAs(IActionEnd, Endable)  then
    Endable.ActionEnd;
end;

end.
