unit Morph.Client.Test;

interface

uses
  TestFramework, Morph.Link_Internet, Morph.Client, Morph.Endpoint.Proxy;

type
  // Test methods for class TMorphClient

  TestTMorphClient = class(TTestCase)
  private
    procedure TestApartmentProxy(const ApartmentProxy: TApartmentProxy; const ExpectedData: string);
  published
    procedure TestCreateClient_DomainName;
    procedure TestCreateClient_IPv4;
    procedure TestCreateClient_IPv6;
  end;

implementation

uses
  Classes, uStreamTest, Morph.Core.Streams;

procedure TestTMorphClient.TestApartmentProxy(const ApartmentProxy: TApartmentProxy; const ExpectedData: string);
var
  Stream, ExpectedStream: TBytesStream;
  Writer: TMorphStreamWriter;
begin
  //  Set up
  ExpectedStream    := TBytesStream.Create;
  Stream  := TBytesStream.Create;
  try
    //  Write test data
    ExpectedStream.WriteHex(ExpectedData);
    Stream.Position := 0;
    //  Write to stream
    Writer  := TMorphStreamWriter.Create(Stream);
    try
      ApartmentProxy.Path.Write(Writer);
    finally
      Writer.Free;
    end;    
    //  Verify stream
    StreamTest.IsSameData(Stream, ExpectedStream);
    //  Tidy up
  finally      
    ExpectedStream.Free;
    Stream.Free;
  end;
end;

procedure TestTMorphClient.TestCreateClient_DomainName;
const
  Service = 'abc';
  Domain  = 'xyz.com';
var
  ExpectedData: string;
  ApartmentProxy: TApartmentProxy;
begin
  ExpectedData  := '';
  ApartmentProxy  := MorphClient.CreateClient(Service, Domain);
  try
    TestApartmentProxy(ApartmentProxy, ExpectedData);
  finally
    ApartmentProxy.Free;
  end;
end;

procedure TestTMorphClient.TestCreateClient_IPv4;
const
  Service = 'abc';
  IPv4    : TIPv4 = (1, 2, 3, 4);
var
  ExpectedData: string;
  ApartmentProxy: TApartmentProxy;
begin
  ExpectedData  :=
      '8901020304' + //  Internet node
      '820003'; //  Service node
  ApartmentProxy  := MorphClient.CreateClient(Service, IPv4);
  try
    TestApartmentProxy(ApartmentProxy, ExpectedData);
  finally
    ApartmentProxy.Free;
  end;
end;

procedure TestTMorphClient.TestCreateClient_IPv6;
const
  Service = 'abc';
  IPv6    : TIPv6 = (1, 2, 3, 4, 5, 6, 7, 8);
var
  ExpectedData: string;
  ApartmentProxy: TApartmentProxy;
begin
  ExpectedData  := '';
  ApartmentProxy  := MorphClient.CreateClient(Service, IPv6);
  try
    TestApartmentProxy(ApartmentProxy, ExpectedData);
  finally
    ApartmentProxy.Free;
  end;
end;

initialization
  // Register any test cases with the test runner
  RegisterTest(TestTMorphClient.Suite);
end.
