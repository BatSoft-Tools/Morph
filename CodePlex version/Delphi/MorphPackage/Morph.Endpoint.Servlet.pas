unit Morph.Endpoint.Servlet;

interface

uses
  RTTI, Morph.Core.Containers, Morph.Endpoint.Values, Morph.Core.Types,
  Morph.Lib.Factories;

type
  TValueArray = TArray<TValue>;

  TServlet = class abstract (TMultiton<TServletID>)
  protected
    function  GenerateParams(const Parameters: TMorphParameters): TValueArray;
    constructor Create(const AServletID : TServletID);
  public
    destructor  Destroy; override;
    property  ServletID : TServletID  read fKey;
    function  IsThisObject(const Obj): Boolean; virtual; abstract;
    function  InvokeMethod(const MethodName: string; const Parameters: TMorphParameters): TValue; virtual; abstract;
    function  InvokeGetter(const PropertyName: string; const Parameters: TMorphParameters): TValue; virtual; abstract;
    procedure InvokeSetter(const PropertyName: string; const Parameters: TMorphParameters; const Value: TMorphValue); virtual; abstract;
  end;
  TServletClass = class of TServlet;

  TServletObject = class(TServlet)
  private
    fServletObj: TObject;
  protected
    constructor Create(const AServletID : TServletID; const AServletObj: TObject);
  public
    property  ServletObj: TObject     read fServletObj;
    function  IsThisObject(const Obj): Boolean; override;
    function  InvokeMethod(const MethodName: string; const Parameters: TMorphParameters): TValue; override;
    function  InvokeGetter(const PropertyName: string; const Parameters: TMorphParameters): TValue; override;
    procedure InvokeSetter(const PropertyName: string; const Parameters: TMorphParameters; const Value: TMorphValue); override;
  end;

  TServletInterface = class(TServletObject)
  protected
    constructor Create(const AServletID : TServletID; AServletObj: IInterface);
  public
    function  IsThisObject(const Obj): Boolean; override;
  end;

  IServletFactory = interface
    ['{E1369374-8FD3-4E2A-B372-BAE94BC6B7F7}']
    function  CreateServlet(const ServletID : TServletID; const Obj: TObject): TServlet;
  end;

  TServletFactories = class(TCannedList<IServletFactory>)
  public
    procedure Add(ServletFactory: IServletFactory);
    function  CreateServlet(const ServletID : TServletID; const Obj: TObject): TServlet;
  end;

implementation

uses
  TypInfo, SysUtils, SyncObjs, Morph.Core.Errors;

//  RTTI Context

var
  _RTTI: TRttiContext;
  _RTTICount: integer = 0;
  _Critical: TCriticalSection;

procedure ObtainRTTI;
begin
  _Critical.Enter;
  try
    if  _RTTICount = 0  then
      _RTTI := TRttiContext.Create;
    inc(_RTTICount);
  finally
    _Critical.Leave;
  end;
end;

procedure ReleaseRTTI;
begin
  _Critical.Enter;
  try
    dec(_RTTICount);
    if  _RTTICount = 0  then
      _RTTI.Free;
  finally
    _Critical.Leave;
  end;
end;

{ TServlet }

constructor TServlet.Create(const AServletID : TServletID);
begin
  inherited Create(AServletID);
  ObtainRTTI;
end;

destructor TServlet.Destroy;
begin
  ReleaseRTTI;
  inherited;
end;

function TServlet.GenerateParams(const Parameters: TMorphParameters): TValueArray;
var
  i: integer;
begin
  SetLength(Result, Parameters.Count);
  for i := 0 to Parameters.Count - 1 do
    Result[i] := Parameters[i].Value;
end;

{ TServletObject }

constructor TServletObject.Create(const AServletID: TServletID; const AServletObj: TObject);
begin
  inherited Create(AServletID);
  fServletObj := AServletObj;
end;

function TServletObject.InvokeGetter(const PropertyName: string; const Parameters: TMorphParameters): TValue;

  function InvokeSimpleProperty(const Prop: TRttiProperty): TValue;
  begin
    if  not Assigned(Prop)  then
      raise EMorph.Create('Property ''' + PropertyName + ''' not found.');
    if  not Prop.IsReadable then
      raise EMorph.Create('Property ''' + PropertyName + ''' is not gettable');
    Result  := Prop.GetValue(fServletObj);
  end;

  function InvokeIndexedProperty(const Prop: TRttiIndexedProperty): TValue;
  begin
    if  not Assigned(Prop)  then
      raise EMorph.Create('Property ''' + PropertyName + ''' not found.');
    if  not Prop.IsReadable then
      raise EMorph.Create('Property ''' + PropertyName + ''' is not gettable');
    Result  := Prop.GetValue(fServletObj, GenerateParams(Parameters));
  end;

var
  Info : TRttiType;
begin
  Info := _RTTI.GetType(fServletObj.ClassType);
  if  Parameters.Count = 0  then
    Result  := InvokeSimpleProperty(Info.GetProperty(PropertyName))
  else
    Result  := InvokeIndexedProperty(Info.GetIndexedProperty(PropertyName));
end;

function TServletObject.InvokeMethod(const MethodName: string; const Parameters: TMorphParameters): TValue;

  function MatchingParams(const Params: TArray<TRttiParameter>; const Parameters: TMorphParameters): Boolean;
  var
    i: integer;
  begin
    Result  := False;
    if  Length(Params) <> Parameters.Count then
      Exit;
    for i := Parameters.Count - 1 downto 0 do
      if  Params[i].ParamType.Handle <> Parameters[i].Value.TypeInfo  then
        Exit;
    Result  := True;
  end;

var
  Info : TRttiType;
  Methods : TArray<TRttiMethod>;
  Method : TRttiMethod;
  i: integer;
begin
  Info    := _RTTI.GetType(fServletObj.ClassType);
  Methods := Info.GetMethods(MethodName);
  Method  := nil;
  for i := Length(Methods) downto 0 do
    if  MatchingParams(Methods[i].GetParameters, Parameters)  then
      break;
  if  not Assigned(Methods) then
    raise EMorph.Create('Method not found.');
  Result  := Method.Invoke(fServletObj, GenerateParams(Parameters));
end;

procedure TServletObject.InvokeSetter(const PropertyName: string; const Parameters: TMorphParameters; const Value: TMorphValue);

  function InvokeSimpleProperty(const Prop: TRttiProperty; const Value: TMorphValue): TValue;
  begin
    if  not Assigned(Prop)  then
      raise EMorph.Create('Property ''' + PropertyName + ''' not found.');
    if  not Prop.IsReadable then
      raise EMorph.Create('Property ''' + PropertyName + ''' is not settable');
    Prop.SetValue(fServletObj, Value);
  end;

  function InvokeIndexedProperty(const Prop: TRttiIndexedProperty; const Value: TMorphValue): TValue;
  begin
    if  not Assigned(Prop)  then
      raise EMorph.Create('Property ''' + PropertyName + ''' not found.');
    if  not Prop.IsReadable then
      raise EMorph.Create('Property ''' + PropertyName + ''' is not settable');
    Prop.SetValue(fServletObj, GenerateParams(Parameters), Value);
  end;

var
  Info : TRttiType;
begin
  Info := _RTTI.GetType(fServletObj.ClassType);
  if  Parameters.Count = 0  then
    InvokeSimpleProperty(Info.GetProperty(PropertyName), Value)
  else
    InvokeIndexedProperty(Info.GetIndexedProperty(PropertyName), Value);
end;

function TServletObject.IsThisObject(const Obj): Boolean;
var
  o: TObject absolute Obj;
begin
  Result  := fServletObj = o
end;

{ TServletInterface }

constructor TServletInterface.Create(const AServletID: TServletID; AServletObj: IInterface);
begin
  inherited Create(AServletID, AServletObj as TObject);
end;

function TServletInterface.IsThisObject(const Obj): Boolean;
var
  o: TObject absolute Obj;
begin
  Result  := Supports(o, IInterface)
end;

{ TServletFactories }

procedure TServletFactories.Add(ServletFactory: IServletFactory);
begin
  inherited Add(ServletFactory);
end;

function TServletFactories.CreateServlet(const ServletID : TServletID; const Obj: TObject): TServlet;
var
  i: integer;
begin
  for i := 0 to Count - 1 do
    begin
      Result  := Values[i].CreateServlet(ServletID, Obj);
      if  Assigned(Result)  then
        exit;
    end;
  raise EMorphUsage.Create('Servlet object type not recognised.');
end;

initialization
  _Critical := TCriticalSection.Create;

finalization
  _Critical.Free;

end.
