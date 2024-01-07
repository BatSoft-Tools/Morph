unit Morph.Core.Link;

interface

uses
  Morph.Core.Types, Morph.Core.Streams;

type
  TMorphLinkSize  = type UInt32;

  TMorphLink = class abstract(TObject)
  protected
    function  GetLinkType: TLinkType; virtual; abstract;
    function  GetSize: TMorphLinkSize; virtual; abstract;
    function  IdentifierByteLength(const Str: string): Cardinal;
  public
    destructor  Destroy; override;
    property  LinkType  : TLinkType       read GetLinkType;
    property  Size      : TMorphLinkSize  read GetSize;
    procedure Write(const Writer: TMorphStreamWriter); virtual; abstract;
  end;

implementation

{ TMorphLink }

destructor TMorphLink.Destroy;
begin
  inherited;
end;

function TMorphLink.IdentifierByteLength(const Str: string): Cardinal;
begin
  Result  := MorphEncoding.GetByteCount(Str);
end;

end.
