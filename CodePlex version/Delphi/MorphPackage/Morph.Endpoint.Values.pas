unit Morph.Endpoint.Values;

interface

uses
  Classes, Rtti, Generics.Collections, Morph.Core.Containers, Morph.Core.Types;

type
  TMorphValue = class
  end;

  TMorphValueBase = class(TMorphValue);

  TMorphValueStream = class(TMorphValue);

  TMorphValueObject = class(TMorphValue)
  private
    fTypeName: string;
  public
    constructor Create(const ATypeName: string);
    property  TypeName  : string  read fTypeName;
  end;

  TMorphValueObjectReference = class(TMorphValueObject)
  private
    fServletID: Int32;
  public
    constructor Create(const ATypeName: string; const AServletID: Int32);
    property  ServletID : Int32 read fServletID;
  end;

  TMorphValueObjectReferenceIndexed = class(TMorphValueObjectReference)
  private
    fIndex: TMorphValue;
  public
    constructor Create(const ATypeName: string; const AServletID: TServletID; const AIndex : TMorphValue);
    property  Index : TMorphValue read fIndex;
  end;

  TArrayValues = class(TCannedObjectList<TMorphValue>);

  TStrucTMorphValues = class(TCannedStringObjectList<TMorphValue>);

  TMorphValueObjectInstance = class(TMorphValueObject)
  private
    fArrayValues: TArrayValues;
    fStrucTMorphValues: TStrucTMorphValues;
    function GetIsArray: Boolean;
    function GetIsStruct: Boolean;
  public
    constructor Create(const ATypeName: string; const AIsArray, AIsStruct: Boolean);
    destructor  Destroy; override;
    property  IsArray : Boolean read GetIsArray;
    property  IsStruct: Boolean read GetIsStruct;
    property  ArrayValues : TArrayValues  read fArrayValues;
    property  StrucTMorphValues: TStrucTMorphValues read fStrucTMorphValues;
  end;

  TMorphParameter = record
    Name: string;
    Value: TValue;
  end;

  TMorphParameters = class(TList<TMorphParameter>);

implementation

{ TMorphValueObject }

constructor TMorphValueObject.Create(const ATypeName: string);
begin
  inherited Create;
  fTypeName := ATypeName;
end;

{ TMorphValueObjectReference }

constructor TMorphValueObjectReference.Create(const ATypeName: string; const AServletID: Int32);
begin
  inherited Create(ATypeName);
  fServletID  := AServletID;
end;

{ TMorphValueObjectReferenceIndexed }

constructor TMorphValueObjectReferenceIndexed.Create(const ATypeName: string; const AServletID: TServletID; const AIndex: TMorphValue);
begin
  inherited Create(ATypeName, AServletID);
  fIndex  := AIndex;
end;

{ TMorphValueObjectInstance }

constructor TMorphValueObjectInstance.Create(const ATypeName: string; const AIsArray, AIsStruct: Boolean);
begin
  inherited Create(ATypeName);
  if  AIsArray  then
    fArrayValues  := TArrayValues.Create;
  if  AIsStruct then
    fStrucTMorphValues := TStrucTMorphValues.Create;
end;

destructor TMorphValueObjectInstance.Destroy;
begin
  fArrayValues.Free;
  fStrucTMorphValues.Free;
  inherited;
end;

function TMorphValueObjectInstance.GetIsArray: Boolean;
begin
  Result  := Assigned(fArrayValues)
end;

function TMorphValueObjectInstance.GetIsStruct: Boolean;
begin
  Result  := Assigned(fStrucTMorphValues)
end;

end.
