unit Morph.Lib.Factories;

interface

uses
  System.SyncObjs, Morph.Core.Containers, Morph.Core.Types;

type
  TMultiton<TKey> = class
  private
    fRefCount: integer;
  protected
    fKey: TKey;
    constructor Create(const AKey: TKey);
  end;

  IMultitonCreator<TKey; TObj: TMultiton<TID>> = interface
    ['{502A4306-9209-4333-9183-2F286AD0BAC2}']
    function  CreateMultiton(const Key: TKey): TObj;
  end;

  TIDGenerator<TObj: class> = class
  private
    fOwner: TCannedIntegerObjectList<TObj>;
    fCurrentID: TID;
    fCritical: TCriticalSection;
  public
    constructor Create(const AOwner: TCannedIntegerObjectList<TObj>);
    destructor  Destroy; override;
    function  Generate: TID;
  end;

  TMultitonFactoryGenID<TObj: TMultiton<TID>> = class
  private
    fCritical: TCriticalSection;
    fObjects : TCannedIntegerObjectList<TObj>;
    fIDGenerator: TIDGenerator<TObj>;
    function  Find(const ID: TID): TObj; inline;
  public
    constructor Create;
    destructor  Destroy; override;
    function  Request(const ID: TID): TObj;
    function  Obtain(MultitonCreator: IMultitonCreator<TID, TObj>): TObj; overload;
    function  Obtain(const ID: TID; const MultitonCreator: IMultitonCreator<TID, TObj>): TObj; overload;
    procedure Obtain(const Obj: TObj); overload; inline;
    procedure Release(const Obj: TObj);
  end;

implementation

{ TMultiton<TKey> }

constructor TMultiton<TKey>.Create(const AKey: TKey);
begin
  inherited Create;
  fKey  := AKey;
end;

{ TIDGenerator<TObj> }

constructor TIDGenerator<TObj>.Create(const AOwner: TCannedIntegerObjectList<TObj>);
begin
  inherited Create;
  fOwner      := AOwner;
  fCurrentID  := NoID;
  fCritical   := TCriticalSection.Create;
end;

destructor TIDGenerator<TObj>.Destroy;
begin
  fCritical.Free;
  inherited;
end;

function TIDGenerator<TObj>.Generate: TID;
begin
  fCritical.Enter;
  try
    repeat
      if  fCurrentID = High(TID)  then
        fCurrentID  := NoID;
      inc(fCurrentID);
    until not fOwner.Has(fCurrentID);
    Result  := fCurrentID;
  finally
    fCritical.Leave;
  end;
end;

{ TMultitonFactoryGenID<TObj> }

constructor TMultitonFactoryGenID<TObj>.Create;
begin
  inherited;
  fCritical := TCriticalSection.Create;
  fObjects  := TCannedIntegerObjectList<TObj>.Create(True);
  fIDGenerator  := TIDGenerator<TObj>.Create(fObjects);
end;

destructor TMultitonFactoryGenID<TObj>.Destroy;
begin
  fIDGenerator.Free;
  fObjects.Free;
  fCritical.Free;
  inherited;
end;

function TMultitonFactoryGenID<TObj>.Find(const ID: TID): TObj;
begin
  Result  := fObjects.ByKey(ID)
end;

function TMultitonFactoryGenID<TObj>.Obtain(MultitonCreator: IMultitonCreator<TID, TObj>): TObj;
begin
  fCritical.Enter;
  try
    Result  := MultitonCreator.CreateMultiton(fIDGenerator.Generate);
    fObjects.Add(Result.fKey, Result);
    Obtain(Result);
  finally
    fCritical.Leave;
  end;
end;

function TMultitonFactoryGenID<TObj>.Obtain(const ID: TID; const MultitonCreator: IMultitonCreator<TID, TObj>): TObj;
begin
  fCritical.Enter;
  try
    Result  := Find(ID);
    if  not Assigned(Result)  then
      begin
        Result  := MultitonCreator.CreateMultiton(ID);
        fObjects.Add(ID, Result);
      end;
    Obtain(Result);
  finally
    fCritical.Leave;
  end;
end;

procedure TMultitonFactoryGenID<TObj>.Obtain(const Obj: TObj);
begin
  inc(Obj.fRefCount)
end;

procedure TMultitonFactoryGenID<TObj>.Release(const Obj: TObj);
begin
  fCritical.Enter;
  try
    dec(Obj.fRefCount);
    if  Obj.fRefCount = 0  then
      fObjects.Delete(Obj.fKey);
  finally
    fCritical.Leave;
  end;
end;

function TMultitonFactoryGenID<TObj>.Request(const ID: TID): TObj;
begin
  fCritical.Enter;
  try
    Result  := Find(ID);
    if  Assigned(Result)  then
      Obtain(Result);
  finally
    fCritical.Leave;
  end;
end;

end.
