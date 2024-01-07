unit Morph.Link_Data;

interface

uses
  System.SysUtils, Morph.Core.Types, Morph.Core.Link, Morph.Core.Reader,
  Morph.Core.Action, Morph.Core.Streams;

type
  TErrorCode = Int32;

  TLink_Data = class(TMorphLink)
  private
    fData: TBytes;
    function GetDataSize: Int32;
  protected
    function GetLinkType: TLinkType; override;
    function GetSize: TMorphLinkSize; override;
  public
    constructor Create(const AData: TBytes);
    property  Data      : TBytes  read fData;
    property  DataSize  : Int32   read GetDataSize;
    procedure Write(const Writer: TMorphStreamWriter); override;
    function  ToString: string; override;
  end;

  TLink_Error = class(TLink_Data)
  private
    fErrorCode: TErrorCode;
  protected
    function GetSize: TMorphLinkSize; override;
  public
    constructor Create(const AErrorCode: TErrorCode; const AData: TBytes);
    property  ErrorCode : TErrorCode  read fErrorCode;
    function  ToString: string; override;
  end;

  TLinkReader_Data = class(TInterfacedObject, IMorphLinkReader)
    //  IMorphLinkReader interface
    function  Read(const Reader: TMorphStreamReader; const IsException, FlagY, FlagZ: Boolean): TMorphLink;
  end;

implementation

{ TLink_Data }

constructor TLink_Data.Create(const AData: TBytes);
begin
  inherited Create;
  fData := AData;
end;

function TLink_Data.GetDataSize: Int32;
begin
  Result  := Length(fData)
end;

function TLink_Data.GetLinkType: TLinkType;
begin
  Result  := TLinkType.linkData
end;

function TLink_Data.GetSize: TMorphLinkSize;
begin
  Result  := 5 + DataSize
end;

function TLink_Data.ToString: string;
begin
  Result  := '{Data Length=' + IntToStr(DataSize) + '}'
end;

procedure TLink_Data.Write(const Writer: TMorphStreamWriter);
begin
  Writer.WriteLinkByte(LinkType, False, False, False);
  Writer.WriteInt32(DataSize);
  Writer.WriteBytes(fData[0], DataSize);
end;

{ TLink_Error }

constructor TLink_Error.Create(const AErrorCode: TErrorCode; const AData: TBytes);
begin
  inherited Create(AData);
  fErrorCode  := AErrorCode
end;

function TLink_Error.GetSize: TMorphLinkSize;
begin
  Result  := inherited GetSize + 4
end;

function TLink_Error.ToString: string;
begin
  Result  := '{Data ErrorCode=' + IntToHex(ErrorCode, 8) + ' Length=' + IntToStr(DataSize) + '}'
end;

{ TLinkReader_Data }

function TLinkReader_Data.Read(const Reader: TMorphStreamReader; const IsException, FlagY, FlagZ: Boolean): TMorphLink;

  function  ReadData: TBytes;
  var
    DataSize: Int32;
  begin
    DataSize  := Reader.ReadInt32;
    SetLength(Result, DataSize);
    Reader.ReadBytes(Result[0], DataSize);
  end;

var
  ErrorCode: TErrorCode;
begin
  if  IsException then
    begin
      ErrorCode := Reader.ReadInt32;
      Result  := TLink_Error.Create(ErrorCode, ReadData)
    end
  else
    Result  := TLink_Data.Create(ReadData)
end;

end.
