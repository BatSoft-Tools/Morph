unit Morph.Core.Streams;

interface

{$DEFINE MSB}

uses
  SysUtils, Classes, Morph.Core.Errors, Morph.Core.Types;

type
  TStringCounter = (sc8Bit, sc16Bit, sc32Bit, sc64Bit);

  TMorphStreamWriter = class
  private
    fStream: TBytesStream;
  public
    constructor Create(const AStream: TBytesStream);
    property  Stream: TBytesStream read fStream;

    class function LinkByte(const LinkType: TLinkType; const FlagX, FlagY, FlagZ: Boolean): Byte;
    procedure WriteLinkByte(const LinkType: TLinkType; const FlagX, FlagY, FlagZ: Boolean);

    procedure WriteBytes(const Buffer; const Count: integer);

    procedure WriteInt8(const Value: Byte);
    procedure WriteInt16(const Value: Int16);
    procedure WriteInt32(const Value: Int32);
    procedure WriteInt64(const Value: Int64);

    procedure WriteIdentifier(const Value: string);
    function  WriteString(const Value: string): TStringCounter;
  end;

  TMorphStreamReader = class
  private
    fStream: TBytesStream;
    fLastPosition: integer;
    fMSB: Boolean;
    function GetIsEOS: Boolean;
    function GetRemaining: Int64;
  private
    procedure CheckIfEOS(const ByteCount: Int64 = 0);
  public
    constructor Create(const AStream: TBytesStream); overload;
    constructor Create(const AStream: TBytesStream; const ACount: integer); overload;
    constructor Create(const AStream: TMorphStreamReader; const ACount: integer); overload;
//    property  Stream    : TBytesStream  read fStream;
    property  MSB       : Boolean       read fMSB;
    property  IsEOS     : Boolean       read GetIsEOS;
    property  Remaining : Int64         read GetRemaining;

    procedure ReadLinkByte(out LinkType: TLinkType; out FlagX, FlagY, FlagZ: Boolean);

    procedure ReadBytes(var Buffer; const Count: integer);

    function  ReadInt8: Byte;
    function  ReadInt16: Int16;
    function  ReadInt32: Int32;
    function  ReadInt64: Int64;

    function  ReadIdentifier: string;
    function  ReadString(const CountSize: TStringCounter): string;

    procedure WriteRemainingTo(const Writer: TMorphStreamWriter);
  end;

  TMorphEncoding = class(TEncoding)
  public
    function  DecodeString(const Stream: TCustomMemoryStream; const ByteCount: integer): string;
    procedure EncodeString(const Stream: TCustomMemoryStream; const ByteCount: integer; const Value: string);
  end;

var
  MorphEncoding: TMorphEncoding;

const
{$IFDEF MSB}
  IsMSB = True;
{$ELSE}
  IsMSB = False;
{$ENDIF}

implementation

const
  BitMask_LinkType  = $0F;
  BitMask_FlagX     = $10;
  BitMask_FlagY     = $20;
  BitMask_FlagZ     = $40;
  BitMask_FlagMSB   = $80;

{ TMorphStreamWriter }

constructor TMorphStreamWriter.Create(const AStream: TBytesStream);
begin
  inherited Create;
  fStream := AStream;
end;

class function TMorphStreamWriter.LinkByte(const LinkType: TLinkType; const FlagX, FlagY, FlagZ: Boolean): Byte;
begin
{$IFDEF MSB}
  Result  := Byte(LinkType) or BitMask_FlagMSB;
{$ELSE}
  Result  := Byte(LinkType);
{$ENDIF}
  if  FlagX then  Result  := Result or BitMask_FlagX;
  if  FlagY then  Result  := Result or BitMask_FlagY;
  if  FlagZ then  Result  := Result or BitMask_FlagZ;
end;

procedure TMorphStreamWriter.WriteBytes(const Buffer; const Count: integer);
begin
{ TODO : Secure programming: streams }
  fStream.Write(Buffer, Count)
end;

procedure TMorphStreamWriter.WriteIdentifier(const Value: string);
var
  ByteCount: integer;
begin
  ByteCount := MorphEncoding.GetByteCount(Value);
  if  ByteCount > $7FFF  then
    raise EMorphUsage.Create('Identifier is too long: "' + Value + '"');
  WriteInt16(ByteCount);
  if  ByteCount > 0  then
    MorphEncoding.EncodeString(fStream, ByteCount, Value);
end;

procedure TMorphStreamWriter.WriteInt16(const Value: Int16);
begin
  fStream.WriteBuffer(Value, 2);
end;

procedure TMorphStreamWriter.WriteInt32(const Value: Int32);
begin
  fStream.WriteBuffer(Value, 4);
end;

procedure TMorphStreamWriter.WriteInt64(const Value: Int64);
begin
  fStream.WriteBuffer(Value, 8);
end;

procedure TMorphStreamWriter.WriteInt8(const Value: Byte);
begin
  fStream.WriteBuffer(Value, 1);
end;

procedure TMorphStreamWriter.WriteLinkByte(const LinkType: TLinkType; const FlagX, FlagY, FlagZ: Boolean);
begin
  WriteInt8(LinkByte(LinkType, FlagX, FlagY, FlagZ));
end;

function  TMorphStreamWriter.WriteString(const Value: string): TStringCounter;
var
  ByteCount: Int64;
begin
  ByteCount := MorphEncoding.GetByteCount(Value);
  if  ByteCount <= $7F then
    begin
      Result  := sc8Bit;
      WriteInt8(ByteCount);
    end
  else if ByteCount <= $7FFF then
    begin
      Result  := sc16Bit;
      WriteInt16(ByteCount);
    end
  else if ByteCount <= $7FFFFFFF then
    begin
      Result  := sc32Bit;
      WriteInt32(ByteCount);
    end
  else
    begin
      Result  := sc64Bit;
      WriteInt64(ByteCount);
    end;
  if  ByteCount > 0  then
    MorphEncoding.EncodeString(fStream, ByteCount, Value);
end;

{ TMorphStreamReader }

procedure TMorphStreamReader.CheckIfEOS(const ByteCount: Int64);
begin
  if  ByteCount < 0 then
    raise EMorph.Create('Negative byte count');
  if  Remaining < ByteCount then
    raise EMorph.Create('Attempted buffer overrun');
end;

constructor TMorphStreamReader.Create(const AStream: TBytesStream);
begin
  Create(AStream, AStream.Size - AStream.Position);
end;

constructor TMorphStreamReader.Create(const AStream: TBytesStream; const ACount: integer);
begin
  inherited Create;
  fStream := AStream;
  fLastPosition := fStream.Position + ACount - 1;
end;

constructor TMorphStreamReader.Create(const AStream: TMorphStreamReader; const ACount: integer);
begin
  AStream.CheckIfEOS(ACount);
  Create(AStream.fStream, ACount);
end;

function TMorphStreamReader.GetIsEOS: Boolean;
begin
  Result  := fLastPosition <= fStream.Position;
end;

function TMorphStreamReader.GetRemaining: Int64;
begin
  Result  := fLastPosition - fStream.Position;
end;

procedure TMorphStreamReader.ReadBytes(var Buffer; const Count: integer);
begin
{ TODO : Secure programming: streams }
  fStream.Read(Buffer, Count)
end;

function TMorphStreamReader.ReadIdentifier: string;
var
  ByteCount: Int16;
begin
  ByteCount := ReadInt16;
  CheckIfEOS(ByteCount);
  Result  := MorphEncoding.DecodeString(fStream, ByteCount);
  fStream.Position  := fStream.Position + ByteCount;
end;

function TMorphStreamReader.ReadInt16: Int16;
type
  TRec16 = packed record
  case Boolean of
    False : (b0, b1: Byte);
    True :  (Value: Int16);
  end;
var
  Rec: TRec16;
begin
  if  MSB then
    fStream.ReadBuffer(Result, 2)
  else
    begin
      Rec.b0  := ReadInt8;
      Rec.b1  := ReadInt8;
      Result  := Rec.Value;
    end;
end;

function TMorphStreamReader.ReadInt32: Int32;
type
  TRec32 = packed record
  case Boolean of
    False : (b0, b1, b2, b3: Byte);
    True :  (Value: Int32);
  end;
var
  Rec: TRec32;
begin
  if  MSB then
    fStream.ReadBuffer(Result, 4)
  else
    begin
      Rec.b0  := ReadInt8;
      Rec.b1  := ReadInt8;
      Rec.b2  := ReadInt8;
      Rec.b3  := ReadInt8;
      Result  := Rec.Value;
    end;
end;

function TMorphStreamReader.ReadInt64: Int64;
type
  TRec64 = packed record
  case Boolean of
    False : (b0, b1, b2, b3, b4, b5, b6, b7: Byte);
    True :  (Value: Int64);
  end;
var
  Rec: TRec64;
begin
  if  MSB then
    fStream.ReadBuffer(Result, 8)
  else
    begin
      Rec.b0  := ReadInt8;
      Rec.b1  := ReadInt8;
      Rec.b2  := ReadInt8;
      Rec.b3  := ReadInt8;
      Rec.b4  := ReadInt8;
      Rec.b5  := ReadInt8;
      Rec.b6  := ReadInt8;
      Rec.b7  := ReadInt8;
      Result  := Rec.Value;
    end;
end;

function TMorphStreamReader.ReadInt8: Byte;
begin
  fStream.ReadBuffer(Result, 1);
end;

procedure TMorphStreamReader.ReadLinkByte(out LinkType: TLinkType; out FlagX, FlagY, FlagZ: Boolean);
var
  LinkByte: Byte;
begin
  LinkByte  := ReadInt8;
  LinkType  := TLinkType(BitMask_LinkType and LinkByte);
  FlagX     := (LinkByte and BitMask_FlagX)   <> 0;
  FlagY     := (LinkByte and BitMask_FlagY)   <> 0;
  FlagZ     := (LinkByte and BitMask_FlagZ)   <> 0;
  fMSB      := (LinkByte and BitMask_FlagMSB) <> 0;
end;

function TMorphStreamReader.ReadString(const CountSize: TStringCounter): string;
var
  ByteCount: Int64;
begin
  case CountSize of
    sc8Bit :  ByteCount := ReadInt8;
    sc16Bit : ByteCount := ReadInt16;
    sc32Bit : ByteCount := ReadInt32;
    sc64Bit : ByteCount := ReadInt64;
  else raise EMorphImplementation.Create('Invalid CountSize');
  end;
  CheckIfEOS(ByteCount);
  Result  := MorphEncoding.DecodeString(fStream, ByteCount);
  fStream.Position  := fStream.Position + ByteCount;
end;

procedure TMorphStreamReader.WriteRemainingTo(const Writer: TMorphStreamWriter);
begin
{ TODO : Secure programming: streams }
  Writer.fStream.Write(fStream.Bytes[fStream.Position], Remaining);
end;

{ TMorphEncoding }

function TMorphEncoding.DecodeString(const Stream: TCustomMemoryStream; const ByteCount: integer): string;
var
  Len: Integer;
  Mem: PByte;
begin
  Mem := Stream.Memory;
  inc(Mem, Stream.Position);
  Len := GetCharCount(Mem, ByteCount);
  SetLength(Result, Len);
  GetChars(Mem, ByteCount, PChar(Result), Len);
end;

procedure TMorphEncoding.EncodeString(const Stream: TCustomMemoryStream; const ByteCount: integer; const Value: string);
var
  Bytes: TBytes;
begin
  Bytes := GetBytes(Value);
  if  GetByteCount(Value) <> Length(Bytes)  then
    raise EMorphImplementation.Create('ByteCount mismatch when writing string');
  Stream.WriteBuffer(Bytes[0], ByteCount);
end;

initialization
  MorphEncoding := TMorphEncoding(TEncoding.UTF8);

end.
