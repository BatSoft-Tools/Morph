unit uStreamTest;

interface

uses
  SysUtils, Classes;

type
  EStreamTest = class(Exception);

  TBytesStreamWriter = class helper for TBytesStream
  private
    function GetToBytes: TBytes;
  public
    property  ToBytes: TBytes read GetToBytes;
    function  ToHex: string;
    procedure WriteHex(const Hex: string);
  end;

  StreamTest = class
  private
    class function  ByteToStr(const b: Byte): string;
    class function  CharToByte(const n1, n2: Char): Byte;
    class function  StrToBytes(const Str: string): TBytes;
  public
    class procedure IsSameData(const TestData, Expected: TBytesStream); overload;
    class procedure IsSameData(const TestData: TBytesStream; const Expected: string); overload;
    class procedure IsSameData(const TestData, Expected: TBytes); overload;
    class procedure IsSameData(const TestData, Expected: string); overload;
  end;

implementation

uses
  Morph.Core.Streams;

const
  nl  = #$D#$A;

function Join(const Str1, Str2: string): string;
begin
  if  Str1 = '' then  Exit(Str2);
  if  Str2 = '' then  Exit(Str1);
  Result  := Str1 + nl + Str2;
end;

{ TBytesStreamWriter }

function TBytesStreamWriter.GetToBytes: TBytes;
var
  Pos: Int64;
  i: integer;
begin
  Pos := Self.Position;
  try
    SetLength(Result, Size);
    i := 0;
    while i < Size do
      begin
        Self.Read(Result[i], 1);
        inc(i);
      end;
  finally
    Self.Position := Pos;
  end;
end;

function TBytesStreamWriter.ToHex: string;

  function NibbleToHex(const n: Byte): Char;
  begin
    if  n < $A  then
      Exit(Char(Ord('0') + n))
    else
      Exit(Char(Ord('A') + n - $0A))
  end;

var
  Pos: LongInt;
  b: Byte;
  Str: TStringBuilder;
begin
  Str := TStringBuilder.Create;
  try
    Pos := Self.Position;
    try
      while Self.Read(b, 1) = 1 do
        begin
          Str.Append(NibbleToHex((b shr 4) and $0F));
          Str.Append(NibbleToHex( b        and $0F));
        end;
    finally
      Self.Position := Pos;
    end;
  finally
    Str.Free;
  end;
end;

procedure TBytesStreamWriter.WriteHex(const Hex: string);
var
  i: integer;

  function NibbleToByte: Byte;
  var
    c: Char;
  begin
    c := Hex[i];
    inc(i);
    if  ('0' <= c) and (c <= '9') then
      Exit(Ord(c) - Ord('0'));
    if  ('A' <= c) and (c <= 'F') then
      Exit(Ord(c) - Ord('A') + $A);
    raise EStreamTest.Create('"Expected" data contains invalid char at: '+IntToStr(Self.Position));
  end;

var
  Len: integer;
  n1, n2, b: byte;
begin
  //  Get string length
  Len := Length(Hex);
  if  Odd(Len)  then
    raise EStreamTest.Create('"Expected" data has odd number of bytes.');
  //
  //
  i := 1;
  while i <= Len do
    begin
      n1  := NibbleToByte;
      n2  := NibbleToByte;
      if  IsMSB then
        b := (n1 shl 4) or n2
      else
        b := (n2 shl 4) or n1;
      Self.Write(b, 1);
    end;
end;

{ StreamTest }

class function StreamTest.ByteToStr(const b: Byte): string;

  function NibbleToChar(n: Byte): Char;
  begin
    n := n and $0F;
    case n of
      $0..$9 :  Result  := Char(Ord('0') + n);
      $A..$F :  Result  := Char(Ord('A') + n - $A);
    else  raise EStreamTest.Create('Test error');
    end;
  end;

begin
  if  IsMSB then
    Result  := NibbleToChar(b shr 4) + NibbleToChar(b)
  else
    Result  := NibbleToChar(b) + NibbleToChar(b shr 4);
end;

class function StreamTest.CharToByte(const n1, n2: Char): Byte;

  function CharToNibble(const c: Char): Byte;
  begin
    case c of
      '0'..'9' :  Result  := Ord(c) - Ord('0');
      'A'..'F' :  Result  := Ord(c) - Ord('A');
      'a'..'f' :  Result  := Ord(c) - Ord('a');
    else  raise EStreamTest.Create('Invalid character: '''+c+'''');
    end;
  end;

begin
  if  IsMSB then
    Result  := (CharToNibble(n1) shl 4) or CharToNibble(n2)
  else
    Result  := (CharToNibble(n2) shl 4) or CharToNibble(n1)
end;

class procedure StreamTest.IsSameData(const TestData: TBytesStream; const Expected: string);
begin
  IsSameData(TestData.ToBytes, StrToBytes(Expected));
end;

class procedure StreamTest.IsSameData(const TestData, Expected: string);
begin
  IsSameData(StrToBytes(TestData), StrToBytes(Expected));
end;

class procedure StreamTest.IsSameData(const TestData, Expected: TBytes);
var
  Error: string;
  Len, i: integer;
begin
  //  Compare lengths
  Error := '';
  if  Length(TestData) <> Length(Expected) then
    Error := 'Different lengths.  Test data: ' + IntToStr(Length(TestData)) + ' Expected: ' + IntToStr(Length(Expected));
  //  Compare content
  Len := Length(TestData);
  if  Length(Expected) < Len  then
    Len := Length(Expected);
  for i := 1 to Len do
    if  TestData[i] <> Expected[i] then
      begin
        Error := Join(Error, 'Difference at: '+IntToStr(i));
      end;
  //  Show error
  if  Error <> '' then
    raise EStreamTest.Create(Error);
end;

class procedure StreamTest.IsSameData(const TestData, Expected: TBytesStream);
begin
  IsSameData(TestData.ToBytes, Expected.ToBytes);
end;

class function StreamTest.StrToBytes(const Str: string): TBytes;
var
  Stream: TBytesStream;
  i: integer;
  b: Byte;
begin
  if  Odd(Length(Str))  then
    raise EStreamTest.Create('Odd number of nibbles in hex string: ' + Str);
  Stream  := TBytesStream.Create;
  try
    i := 1;
    while i <= Length(Str) do
      begin
        b := CharToByte(Str[i], Str[i+1]);
        Stream.Write(b, 1);
        inc(i, 2);
      end;
    Result  := Stream.ToBytes;
  finally
    Stream.Free;
  end;
end;

end.
