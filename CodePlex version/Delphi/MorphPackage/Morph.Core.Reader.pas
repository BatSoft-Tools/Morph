unit Morph.Core.Reader;

interface

uses
  Morph.Core.Types, Morph.Core.Streams, Morph.Core.Link;

type
  IMorphLinkReader = interface
    ['{7FEC4E09-A5D6-408C-A4AA-7C3BB7ECBBB9}']
    function  Read(const Reader: TMorphStreamReader; const FlagX, FlagY, FlagZ: Boolean): TMorphLink;
  end;

  MorphReaders = class
  private
    class var _Readers: array [TLinkType] of IMorphLinkReader;
  public
    class procedure RegisterReader(const LinkType: TLinkType; const LinkReader: IMorphLinkReader);
    class function  ReadLink(const Reader: TMorphStreamReader): TMorphLink;
  end;

implementation

uses
  Morph.Core.Errors;

{ MorphReaders }

class function MorphReaders.ReadLink(const Reader: TMorphStreamReader): TMorphLink;
var
  LinkType: TLinkType;
  FlagX, FlagY, FlagZ: Boolean;
begin
  Reader.ReadLinkByte(LinkType, FlagX, FlagY, FlagZ);
  if  not Assigned(_Readers[LinkType]) then
    raise EMorphUsage.Create('Link type is not registered');
  Result  := _Readers[LinkType].Read(Reader, FlagX, FlagY, FlagZ);
end;

class procedure MorphReaders.RegisterReader(const LinkType: TLinkType; const LinkReader: IMorphLinkReader);
begin
  if  Assigned(_Readers[LinkType]) then
    raise EMorphUsage.Create('Link reader is already registered');
  _Readers[LinkType] := LinkReader;
end;

end.
