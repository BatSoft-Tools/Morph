unit Morph.Link.Test;

interface

uses
  TestFramework, Morph.Core.Link, Morph.Core.Reader;

type
  TestLinks = class(TTestCase)
  private
    procedure TestStreamWriter(const Link: TMorphLink; const Expected: string);
    procedure TestReader(LinkReader: IMorphLinkReader; FlagX, FlagY, FlagZ: Boolean; const Expected: string);
  published
    procedure Test_End;
    procedure Test_Message;
//    procedure Test_Data;
//    procedure Test_Information;
    procedure Test_Service;
    procedure Test_Servlet;
    procedure Test_Member;
//    procedure Test__E;
//    procedure Test_Process;
    procedure Test_Internet;
    procedure Test_Internet_Domain;
    procedure Test_Internet_IPv4;
    procedure Test_Internet_IPv6;
//    procedure Test__5;
//    procedure Test__D;
//    procedure Test_Sequence;
//    procedure Test_Encoding;
//    procedure Test_Stream;
//    procedure Test__F;
  end;

implementation

uses
  Classes, uStreamTest, Morph.Core.Streams,
  Morph.Link_End, Morph.Link_Internet, Morph.Link_Member, Morph.Link_Servlet,
  Morph.Link_Service, Morph.Link_Message;

{ TestLinks }

procedure TestLinks.TestReader(LinkReader: IMorphLinkReader; FlagX, FlagY, FlagZ: Boolean; const Expected: string);
var
  Stream: TBytesStream;
  Reader: TMorphStreamReader;
  Link: TMorphLink;
begin
  Stream  := TBytesStream.Create;
  try
    //  Populate stream
    Stream.WriteHex(Expected);
    Stream.Position := 0;
    //  Read from stream
    Reader  := TMorphStreamReader.Create(Stream);
    try
      Link  := LinkReader.Read(Reader, FlagX, FlagY, FlagZ);
      TestStreamWriter(Link, Expected);
    finally
      Reader.Free;
    end;
  finally
    Stream.Free;
  end;
end;

procedure TestLinks.TestStreamWriter(const Link: TMorphLink; const Expected: string);
var
  Stream: TBytesStream;
  Writer: TMorphStreamWriter;
begin
  Stream  := TBytesStream.Create;
  try
    Writer  := TMorphStreamWriter.Create(Stream);
    try
      Link.Write(Writer);
    finally
      Writer.Free;
    end;
    //  Compare output
    StreamTest.IsSameData(Stream, Expected);
  finally
    Stream.Free;
  end;
end;

procedure TestLinks.Test_End;
begin
  TestReader(TLinkReader_End.Create, False, False, False, '00');
end;

procedure TestLinks.Test_Internet;
begin

end;

procedure TestLinks.Test_Internet_Domain;
begin

end;

procedure TestLinks.Test_Internet_IPv4;
begin

end;

procedure TestLinks.Test_Internet_IPv6;
begin

end;

procedure TestLinks.Test_Member;
begin

end;

procedure TestLinks.Test_Message;
begin

end;

procedure TestLinks.Test_Service;
begin

end;

procedure TestLinks.Test_Servlet;
begin
  TestReader(TLinkReader_Servlet.Create, False, False, False, '8A11223344');
end;

initialization
  // Register any test cases with the test runner
  RegisterTest(TestLinks.Suite);
end.
