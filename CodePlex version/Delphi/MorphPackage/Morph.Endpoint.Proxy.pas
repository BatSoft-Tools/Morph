unit Morph.Endpoint.Proxy;

interface

uses
  System.TimeSpan, Generics.Collections, System.Contnrs, SyncObjs,
  Morph.Lib.Factories, Morph.Core.Containers,
  Morph.Endpoint.Servlet, Morph.Endpoint.Values, Morph.Core.Types, Morph.Core.Path;

type
  TApartmentProxy = class;

  //  Servlet proxy

  TServletProxy = class
  private
    fApartmentProxy: TApartmentProxy;
    fServletID: TServletID;
  protected
    constructor Create(const AApartmentProxy: TApartmentProxy; const AServletID : TServletID); virtual;
  public
    property  ApartmentProxy: TApartmentProxy read fApartmentProxy;
    property  ServletID     : TServletID      read fServletID;
    function  InvokeMethod(const MethodName: string; const Parameters: TMorphParameters): TMorphValue;
    function  InvokeGetter(const GetterName: string; const Parameters: TMorphParameters): TMorphValue;
    procedure InvokeSetter(const SetterName: string; const Parameters: TMorphParameters);
  end;
  TServletProxyClass = class of TServletProxy;

  //  Apartment proxy

  TApartmentProxy = class(TMultiton<TApartmentProxyID>)
  private type

    TServlets = class(TCannedIntegerObjectList<TServletProxy>)
    private
      fApartmentProxy: TApartmentProxy;
    public
      function  Add(const ServletID: TServletID): TServletProxy;
      procedure Remove(const ServletID: TServletID);
    end;

  private
    fPath: TMorphPath;
    fServlets: TServlets;
    fDefaultServlet: TServletProxy;
    constructor Create(const AApartmentProxyID: TApartmentProxyID; const AHasDefaultServlet: Boolean = False); overload;
  public
    destructor  Destroy; override;
    property  ApartmentProxyID  : TApartmentProxyID read fKey;
    property  Path              : TMorphPath        read fPath            write fPath;
    property  Servlets          : TServlets         read fServlets;
    property  DefaultServlet    : TServletProxy     read fDefaultServlet;
  end;

  AllApartmentProxies = class
  private type

    TMultitonCreator = class(TInterfacedObject, IMultitonCreator<TApartmentProxyID, TApartmentProxy>)
      fHasDefault: Boolean;
      constructor Create(const AHasDefault: Boolean);
      //  IMultitonCreator interface
      function  CreateMultiton(const ID: TApartmentProxyID): TApartmentProxy;
    end;

  private class var
    _Factory: TMultitonFactoryGenID<TApartmentProxy>;
  public
    class constructor Create;
    class destructor  Destroy;
    class function  Obtain(const HasDefault: Boolean): TApartmentProxy; overload; inline;
    class function  Request(const ID: TID): TApartmentProxy; inline;
    class procedure Release(const ApartmentProxy: TApartmentProxy); inline;
  end;

implementation

{ TServletProxy }

constructor TServletProxy.Create(const AApartmentProxy: TApartmentProxy; const AServletID: TServletID);
begin
  inherited Create;
  fApartmentProxy := AApartmentProxy;
  fServletID      := AServletID;
end;

function TServletProxy.InvokeGetter(const GetterName: string; const Parameters: TMorphParameters): TMorphValue;
begin
// TODO : TServletProxy.InvokeGetter
end;

function TServletProxy.InvokeMethod(const MethodName: string; const Parameters: TMorphParameters): TMorphValue;
begin
// TODO : TServletProxy.InvokeMethod
end;

procedure TServletProxy.InvokeSetter(const SetterName: string; const Parameters: TMorphParameters);
begin
// TODO : TServletProxy.InvokeSetter
end;

{ TApartmentProxy.TServlets }

function TApartmentProxy.TServlets.Add(const ServletID: TServletID): TServletProxy;
begin
  Result  := TServletProxy.Create(fApartmentProxy, ServletID);
  inherited Add(ServletID, Result);
end;

procedure TApartmentProxy.TServlets.Remove(const ServletID: TServletID);
begin
  inherited Delete(ServletID);
end;

{ TApartmentProxy }

constructor TApartmentProxy.Create(const AApartmentProxyID: TApartmentProxyID; const AHasDefaultServlet: Boolean);
begin
  inherited Create(AApartmentProxyID);
  fServlets := TServlets.Create(True);
  fServlets.fApartmentProxy := Self;
  if  AHasDefaultServlet  then
    fDefaultServlet  := fServlets.Add(NoID);
end;

destructor TApartmentProxy.Destroy;
begin
  fServlets.Free;
  inherited;
end;

{ AllApartmentProxies.TMultitonCreator }

constructor AllApartmentProxies.TMultitonCreator.Create(const AHasDefault: Boolean);
begin
  inherited Create;
  fHasDefault := AHasDefault;
end;

function AllApartmentProxies.TMultitonCreator.CreateMultiton(const ID: TApartmentProxyID): TApartmentProxy;
begin
  Result  := TApartmentProxy.Create(ID);
end;

{ AllApartmentProxies }

class constructor AllApartmentProxies.Create;
begin
  _Factory  := TMultitonFactoryGenID<TApartmentProxy>.Create;
end;

class destructor AllApartmentProxies.Destroy;
begin
  _Factory.Free;
end;

class function AllApartmentProxies.Obtain(const HasDefault: Boolean): TApartmentProxy;
begin
  Result  := _Factory.Obtain(TMultitonCreator.Create(HasDefault))
end;

class procedure AllApartmentProxies.Release(const ApartmentProxy: TApartmentProxy);
begin
  _Factory.Release(ApartmentProxy);
end;

class function AllApartmentProxies.Request(const ID: TID): TApartmentProxy;
begin
  Result  := _Factory.Request(ID)
end;

end.
