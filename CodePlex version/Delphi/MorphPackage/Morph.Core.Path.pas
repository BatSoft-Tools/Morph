unit Morph.Core.Path;

interface

uses
  Generics.Collections, Morph.Core.Streams, Morph.Core.Link;

type
  TMorphPath = class
  private
    fPath: TObjectList<TMorphLink>;
    fStream: TMorphStreamReader;
    function GetPeek: TMorphLink;
  private
    procedure ReadAll;
    function GetSize: TMorphLinkSize;
  protected
    procedure Insert(const Index: integer; const Value: TMorphLink); virtual;
    procedure Add(const Value: TMorphLink); virtual;
  public
    constructor Create; overload;
    constructor Create(const AStream: TMorphStreamReader; const ACount: integer); overload;
    destructor  Destroy; override;
    procedure Append(const Link: TMorphLink); overload;
    procedure Append(const Path: TMorphPath); overload;
    procedure Push(const Link: TMorphLink); overload;
    procedure Push(const Path: TMorphPath); overload;
    property  Peek : TMorphLink  read GetPeek;
    procedure Pop;
    property  Size: TMorphLinkSize read GetSize;
    procedure Write(const Writer: TMorphStreamWriter);
    function  Equals(Obj: TObject): Boolean; override;
    function  ToString: string; override;
  end;

implementation

uses
  SysUtils, Morph.Core.Reader;

{ TMorphPath }

procedure TMorphPath.Append(const Link: TMorphLink);
begin
  Insert(0, Link)
end;

procedure TMorphPath.Add(const Value: TMorphLink);
begin
  fPath.Add(Value);
end;

procedure TMorphPath.Append(const Path: TMorphPath);
var
  i: integer;
begin
  Path.ReadAll;
  for i := 0 to Path.fPath.Count - 1 do
    Insert(i, Path.fPath[i]);
end;

constructor TMorphPath.Create;
begin
  fPath   := TObjectList<TMorphLink>.Create(True);
end;

constructor TMorphPath.Create(const AStream: TMorphStreamReader; const ACount: integer);
begin
  Create;
  fStream := TMorphStreamReader.Create(AStream, ACount);
end;

destructor TMorphPath.Destroy;
begin
  fStream.Free;
  fPath.Free;
  inherited;
end;

function TMorphPath.Equals(Obj: TObject): Boolean;
var
  Other: TMorphPath absolute Obj;
  i: integer;
begin
  Result  := False;
  if  not (Obj is TMorphPath) then
    exit;
  Self.ReadAll;
  Other.ReadAll;
  if  Self.fPath.Count <> Other.fPath.Count then
    exit;
  for i := fPath.Count - 1 downto 0 do
    if  not Self.fPath[i].Equals(Other.fPath[i])  then
      exit;
  Result  := True;
end;

function TMorphPath.GetPeek: TMorphLink;
begin
  if  fPath.Count > 0 then
    Result  := fPath[fPath.Count - 1]
  else if fStream.IsEOS then
    Result  := nil
  else
    begin
      Result  := MorphReaders.ReadLink(fStream);
      Push(Result);
    end;
end;

function TMorphPath.GetSize: TMorphLinkSize;
var
  i: integer;
begin
  //  Remaining stream size
  if  Assigned(fStream) then
    Result  := fStream.Remaining
  else
    Result  := 0;
  //  Sum of all links
  for i := 0 to fPath.Count - 1 do
    inc(Result, fPath[i].Size);
end;

procedure TMorphPath.Insert(const Index: integer; const Value: TMorphLink);
begin
  fPath.Insert(Index, Value)
end;

procedure TMorphPath.Pop;
begin
  fPath.Delete(fPath.Count - 1);
end;

procedure TMorphPath.Push(const Link: TMorphLink);
begin
  Add(Link)
end;

procedure TMorphPath.Push(const Path: TMorphPath);
var
  i: integer;
begin
  Path.ReadAll;
  for i := 0 to Path.fPath.Count - 1 do
    Add(Path.fPath[i]);
end;

procedure TMorphPath.ReadAll;
begin
  while not fStream.IsEOS do
    Append(MorphReaders.ReadLink(fStream));
end;

function TMorphPath.ToString: string;
var
  i: integer;
begin
  Result  := '(';
  for i := fPath.Count - 1 downto 0 do
    Result  := Result + fPath[i].ToString;
  if  not fStream.IsEOS then
    Result  := Result + '...[' + IntToStr(fStream.Remaining) + ' B]';
  Result  := Result + ')';
end;

procedure TMorphPath.Write(const Writer: TMorphStreamWriter);
var
  i: integer;
begin
  for i := fPath.Count - 1 downto 0 do
    fPath[i].Write(Writer);
  if  Assigned(fStream) then
    fStream.WriteRemainingTo(Writer);
end;

end.
