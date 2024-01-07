unit Morph.Link_Message;

interface

uses
  Morph.Core.Types, Morph.Core.Link, Morph.Core.Reader,
  Morph.Core.Streams, Morph.Core.Path;

type
  TCallNumber = UInt32;

  TMessage = class(TMorphLink)
  private
    fHasCallNumber: Boolean;
    fCallNumber: Int32;
    fIsForceful: Boolean;
    fPathFrom: TMorphPath;
    fPathTo: TMorphPath;
    fContext: TObject;
    function GetCallNumber: TCallNumber;
    function GetCurrent: TMorphLink;
    function GetHasPathFrom: Boolean;
  protected
    function GetLinkType: TLinkType; override;
    function GetSize: TMorphLinkSize; override;
  public
    destructor  Destroy; override;
    property  HasCallNumber : Boolean     read fHasCallNumber;
    property  CallNumber    : TCallNumber read GetCallNumber;
    property  IsForceful    : Boolean     read fIsForceful;
    property  HasPathFrom   : Boolean     read GetHasPathFrom;
    property  PathFrom      : TMorphPath  read fPathFrom;
    property  PathTo        : TMorphPath  read fPathTo;
    procedure PathNext;
    property  Current       : TMorphLink  read GetCurrent;
    property  Context       : TObject     read fContext   write fContext;
    function  ContextAs(const IID: TGUID; out Obj): Boolean;
    procedure Write(const Writer: TMorphStreamWriter); override;
    function  Equals(Obj: TObject): Boolean; override;
    function  ToString: string; override;
  end;

  TLinkReader_Message = class(TInterfacedObject, IMorphLinkReader)
  public
    function Read(const Reader: TMorphStreamReader; const HasCallNumber, IsForceful, HasFromPath: Boolean): TMorphLink;
  end;

implementation

uses
  SysUtils, Morph.Core.Errors;

{ TMessage }

function TMessage.ContextAs(const IID: TGUID; out Obj): Boolean;
begin
  Result  := Assigned(fContext) and fContext.GetInterface(IID, Obj);
end;

destructor TMessage.Destroy;
begin
  fPathFrom.Free;
  fPathTo.Free;
  inherited;
end;

function TMessage.Equals(Obj: TObject): Boolean;
var
  Other: TMessage absolute Obj;
begin
  Result  := False;
  if  not (Obj is TMessage) then
    exit;
  if  (Other.HasCallNumber <> Self.HasCallNumber) or
      (Other.IsForceful <> Self.IsForceful) or
      (Other.HasPathFrom <> Self.HasPathFrom)
  then
    exit;
  if  HasCallNumber then
    if  Other.CallNumber <> Self.CallNumber then
      exit;
  if  not Other.PathTo.Equals(Self.PathTo)  then
    exit;
  if  HasCallNumber then
    if  not Other.PathFrom.Equals(Self.PathFrom)  then
      exit;
  Result  := True;
end;

function TMessage.GetCallNumber: TCallNumber;
begin
  if  not HasCallNumber then
    raise EMorph.Create('Message has no CallNumber');
  Result  := fCallNumber;
end;

function TMessage.GetCurrent: TMorphLink;
begin
  Result  := PathTo.Peek;
end;

function TMessage.GetHasPathFrom: Boolean;
begin
  Result  := Assigned(fPathFrom)
end;

function TMessage.GetLinkType: TLinkType;
begin
  Result  := linkMessage
end;

function TMessage.GetSize: TMorphLinkSize;
begin
  //  Link byte
  Result  := 1 + SizeOf(TCallNumber);
  //  CallNumber
  if  HasCallNumber then
    inc(Result, 4);
  //  PathTo
  inc(Result, fPathTo.Size);
  //  PathFrom
  if  HasPathFrom   then
    begin
      inc(Result, 4);
      inc(Result, fPathFrom.Size);
    end;
end;

procedure TMessage.PathNext;
begin
  if  HasPathFrom then
    PathFrom.Push(PathTo.Peek);
  PathTo.Pop;
end;

function TMessage.ToString: string;
begin
  Result  := '{Message';
  if  HasCallNumber then
    Result  := Result + ' CallNumber=' + IntToStr(CallNumber);
  if  IsForceful  then
    Result  := Result + ' IsForceful';
  Result  := Result + ' To=' + PathTo.ToString;
  if  HasPathFrom then
    Result  := Result + ' From=' + PathFrom.ToString;
  Result  := Result + '}';
end;

procedure TMessage.Write(const Writer: TMorphStreamWriter);
begin
  Writer.WriteLinkByte(LinkType, HasCallNumber, IsForceful, HasPathFrom);
  if  HasCallNumber then
    Writer.WriteInt32(CallNumber);
  Writer.WriteInt32(PathTo.Size);
  if  HasPathFrom then
    Writer.WriteInt32(PathFrom.Size);
  PathTo.Write(Writer);
  if  HasPathFrom then
    PathFrom.Write(Writer);
end;

{ TLinkReaderMessage }

function TLinkReader_Message.Read(const Reader: TMorphStreamReader; const HasCallNumber, IsForceful, HasFromPath: Boolean): TMorphLink;
var
  PathToSize, PathFromSize: integer;
  Message: TMessage;
begin
  Message := TMessage.Create;
  try
    //  CallNumber
    Message.fHasCallNumber  := HasCallNumber;
    if  HasCallNumber then
      Message.fCallNumber := Reader.ReadInt32;
    //  IsForceful
    Message.fIsForceful     := IsForceful;
    //  PathToSize
    PathToSize  := Reader.ReadInt32;
    //  PathFromSize
    if  HasFromPath then
      PathFromSize  := Reader.ReadInt32
    else
      PathFromSize  := 0; //  Silence compiler
    //  PathTo
    Message.fPathTo := TMorphPath.Create(Reader, PathToSize);
    //  PathFrom
    if  HasFromPath then
      Message.fPathFrom := TMorphPath.Create(Reader, PathFromSize);
    //  Done
    Result  := Message;
  except
    Message.Free;
    raise;
  end;
end;

end.
