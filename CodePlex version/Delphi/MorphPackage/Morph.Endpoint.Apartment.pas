unit Morph.Endpoint.Apartment;

interface

uses
  Classes, RTTI, SyncObjs, Generics.Collections, Morph.Core.Containers, Morph.Core.Path,
  Morph.Endpoint.Servlet, Morph.Core.Types, Morph.Lib.Factories;

type
  TApartment = class(TMultiton<TApartmentID>)
  public type

    TServlets = class(TCannedIntegerObjectList<TServlet>)
    private
      fApartment: TApartment;
      fIDGen: TIDGenerator<TServlet>;
      function  Add(const ID: TServletID; const Obj: TObject): TServlet;
      procedure Del(const Servlet: TServlet);
      constructor Create(const AApartment: TApartment);
    public
      destructor  Destroy; override;
      function  ByObj(const Obj: TObject): TServlet;
      function  Register(const Obj: TObject): TServlet;
      procedure Unregister(const Obj: TObject); overload;
      procedure Unregister(const ServletID: TServletID); overload;
    end;

  private
    fApartmentID: TApartmentID;
    fFactories: TServletFactories;
    fServlets: TServlets;
    fDefaultServlet: TServlet;
  protected
    constructor Create(const AID: TApartmentID; const AServletFactories: TServletFactories; const ADefaultObj: TObject = nil);
  public
    destructor  Destroy; override;
    property  ApartmentID     : TApartmentID      read fApartmentID;
    property  Factories       : TServletFactories read fFactories;
    property  Servlets        : TServlets         read fServlets;
    property  DefaultServlet  : TServlet          read fDefaultServlet;
  end;

  TApartmentSession = class(TApartment)
  private
    fSessionID: TSessionID;
    fPathToProxy: TMorphPath;
    procedure SetPathToProxy(const Value: TMorphPath);
  protected
    constructor Create(const AID: TApartmentID; const AServletFactories: TServletFactories; const ADefaultObj: TObject = nil);
  public
    destructor  Destroy; override;
    property SessionID    : TSessionID  read fSessionID;
    property PathToProxy  : TMorphPath  read fPathToProxy write SetPathToProxy;
  end;

  AllApartments = class
  private type

    TMultitonCreator = class(TInterfacedObject, IMultitonCreator<TApartmentID, TApartment>)
      fIsSession: Boolean;
      fFactories: TServletFactories;
      fDefault: TObject;
      constructor Create(const AIsSession: Boolean; const AFactories: TServletFactories; const ADefault: TObject);
      //  IMultitonCreator interface
      function  CreateMultiton(const ID: TApartmentID): TApartment;
    end;

  private class var
    _Factory: TMultitonFactoryGenID<TApartment>;
  public
    class constructor Create;
    class destructor  Destroy;
    class function  Obtain(const AIsSession: Boolean; const AFactories: TServletFactories; const ADefault: TObject): TApartment; overload; inline;
    class function  Request(const ID: TID): TApartment; inline;
    class procedure Release(const Apartment: TApartment); inline;
  end;

implementation

uses
  SysUtils, Morph.Core.Errors;

{ TApartment.TServlets }

function TApartment.TServlets.Add(const ID: TServletID; const Obj: TObject): TServlet;
begin
  Result  := fApartment.fFactories.CreateServlet(ID, Obj);
  Add(Result.ServletID, Result);
end;

function TApartment.TServlets.ByObj(const Obj: TObject): TServlet;
var
  i: integer;
begin
  for i := Self.Count - 1 to 0 do
    begin
      Result  := Self.ItemByIndex(i).Value;
      if  Result.IsThisObject(Obj)  then
        exit;
    end;
  Result  := nil
end;

constructor TApartment.TServlets.Create(const AApartment: TApartment);
begin
  inherited Create(True);
  fApartment  := AApartment;
  fIDGen      := TIDGenerator<TServlet>.Create(Self);
end;

procedure TApartment.TServlets.Del(const Servlet: TServlet);
begin
  if  Assigned(Servlet) then
    Delete(Servlet.ServletID);
end;

destructor TApartment.TServlets.Destroy;
begin
  fIDGen.Free;
  inherited;
end;

function TApartment.TServlets.Register(const Obj: TObject): TServlet;
begin
  //  Don't repeat create servlets for the same business object
  Result  := ByObj(Obj);
  //  Not yet registered, so create it
  if  not Assigned(Result)  then
    Result  := Add(fIDGen.Generate, Obj)
end;

procedure TApartment.TServlets.Unregister(const ServletID: TServletID);
begin
  Del(ByKey(ServletID))
end;

procedure TApartment.TServlets.Unregister(const Obj: TObject);
begin
  Del(ByObj(Obj))
end;

{ TApartment }

constructor TApartment.Create(const AID: TApartmentID; const AServletFactories: TServletFactories; const ADefaultObj: TObject);
begin
  inherited Create(AID);
  fFactories  := AServletFactories;
  fServlets   := TServlets.Create(Self);
  fServlets.fApartment := Self;
  if  Assigned(ADefaultObj) then
    fDefaultServlet := fServlets.Add(NoID, ADefaultObj);
end;

destructor TApartment.Destroy;
begin
  fServlets.Free;
  inherited;
end;

{ TApartmentSession }

constructor TApartmentSession.Create(const AID: TApartmentID; const AServletFactories: TServletFactories; const ADefaultObj: TObject);
begin
  inherited Create(AID, AServletFactories, ADefaultObj);
  fSessionID    := (Random(MaxInt) shl 32) + Random(MaxInt);
end;

destructor TApartmentSession.Destroy;
begin
  fPathToProxy.Free;
  inherited;
end;

procedure TApartmentSession.SetPathToProxy(const Value: TMorphPath);
begin
  //  Clear
  if  Assigned(fPathToProxy)  then
    FreeAndNil(fPathToProxy);
  //  Populate
  fPathToProxy  := TMorphPath.Create;
  fPathToProxy.Append(Value);
end;

{ AllApartments.TMultitonCreator }

constructor AllApartments.TMultitonCreator.Create(const AIsSession: Boolean; const AFactories: TServletFactories; const ADefault: TObject);
begin
  inherited Create;
  fIsSession  := AIsSession;
  fFactories  := AFactories;
  fDefault    := ADefault;
end;

function AllApartments.TMultitonCreator.CreateMultiton(const ID: TApartmentID): TApartment;
begin
  if  fIsSession  then
    Result  := TApartmentSession.Create(ID, fFactories, fDefault)
  else
    Result  := TApartment.Create(ID, fFactories, fDefault);
end;

{ AllApartments }

class constructor AllApartments.Create;
begin
  _Factory  := TMultitonFactoryGenID<TApartment>.Create;
end;

class destructor AllApartments.Destroy;
begin
  _Factory.Free;
end;

class function AllApartments.Obtain(const AIsSession: Boolean; const AFactories: TServletFactories; const ADefault: TObject): TApartment;
begin
  Result  := _Factory.Obtain(TMultitonCreator.Create(AIsSession, AFactories, ADefault))
end;

class procedure AllApartments.Release(const Apartment: TApartment);
begin
  _Factory.Release(Apartment);
end;

class function AllApartments.Request(const ID: TID): TApartment;
begin
  Result  := _Factory.Request(ID)
end;

initialization
  Randomize;

end.
