unit Morph.Core.Containers;

interface

uses
  Classes, Generics.Collections;

type
  TCannedList<T> = class
  private
    fValues: TList<T>;
    function  GetValues(const i: integer): T;
    function  GetCount: integer;
  protected
    procedure Add(const Value: T);
    procedure Insert(const Value: T; const Index: integer);
    procedure Delete(const Index: integer); virtual;
    procedure Clear; virtual;
    constructor Create;
  public
    destructor  Destroy; override;
    property  Values[const i: integer] : T read GetValues; default;
    property  Count : integer read GetCount;
  end;

  TCannedObjectList<T: class> = class(TCannedList<T>)
  private
    fOwnsObjects: Boolean;
  protected
    procedure Delete(const Index: integer); override;
    procedure Clear; override;
    property  OwnsObjects: Boolean  read fOwnsObjects;
  public
    constructor Create(const AOwnsObjects: Boolean = True);
  end;

  TComparer<TKey> = function (const Key1, Key2: TKey): integer of object;

  TCannedSortedItems<TKey; TValue: class> = class
  protected
    type
      TSortedItem = record
        Key: TKey;
        Value: TValue;
      end;
  private
    fItems: TList<TSortedItem>;
    fOwnsObjects: Boolean;
    fComparer: TComparer<TKey>;
    function GetCount: integer;
    function GetPower: integer;
  protected
    function  IndexOfKey(const Key: TKey): integer;
    function  IndexOfValue(const Value: TValue): integer;
    function  ItemByKey(const Key: TKey): TSortedItem;
    function  ItemByIndex(const Index: integer): TSortedItem;
    procedure Add(const Key: TKey; const Value: TValue);
    procedure Delete(const Key: TKey);
    procedure Clear;
    property  Count: integer  read GetCount;
    property  OwnsObjects: Boolean  read fOwnsObjects write fOwnsObjects;
    constructor Create(const AComparer: TComparer<TKey>; const AOwnsObjects: Boolean = True);
  public
    destructor  Destroy; override;
    function  Has(const Key: TKey): Boolean;
    function  Find(const Key: TKey): TValue;
  end;

  TSortedItems<TKey; TValue: class> = class(TCannedSortedItems<TKey, TValue>)
  public
    procedure Add(const Key: TKey; const Value: TValue); inline;
    procedure Delete(const Key: TKey); inline;
    procedure Clear; inline;
    function  ByKey(const Key: TKey): TValue; inline;
    function  ByIndex(const Index: integer): TValue; inline;
    property  Count;
    property  OwnsObjects;
  end;

  TCannedIntegerObjectList<T: class> = class(TCannedSortedItems<integer, T>)
  protected
    function  Comparer(const Key1, Key2: integer): integer;
  public
    constructor Create(const AOwnsObjects: Boolean = True);
    function  ByKey(const Key: integer): T;
    function  ByIndex(const Index: integer): T;
    property  Count;
  end;

  TIntegerObjectList<T: class> = class(TCannedIntegerObjectList<T>)
  public
    constructor Create(const AOwnsObjects: Boolean = True);
    procedure Add(const Key: integer; const Value: T); inline;
    procedure Delete(const Key: integer); inline;
    procedure Clear; inline;
    property  Count;
    property  OwnsObjects;
  end;

  TCannedStringObjectList<T: class> = class(TCannedSortedItems<string, T>)
  private
    function GetValuesName(const Name: string): T;
    function GetValuesIndex(const Index: integer): T;
  protected
    function  Comparer(const Key1, Key2: string): integer;
  public
    constructor Create(const AOwnsObjects: Boolean = True);
    property  Values[const Name: string]  : T read GetValuesName; default;
    property  Values[const Index: integer]: T read GetValuesIndex; default;
    property  Count;
    procedure ListAll(const Values: TStrings);
  end;

implementation

uses
  SysUtils, RTLConsts;

{ TCannedList<T> }

procedure TCannedList<T>.Add(const Value: T);
begin
  fValues.Add(Value);
end;

procedure TCannedList<T>.Clear;
begin
  fValues.Clear;
end;

constructor TCannedList<T>.Create;
begin
  inherited;
  fValues := TList<T>.Create;
end;

procedure TCannedList<T>.Delete(const Index: integer);
begin
  fValues.Delete(Index)
end;

destructor TCannedList<T>.Destroy;
begin
  Clear;
  fValues.Free;
  inherited;
end;

function TCannedList<T>.GetCount: integer;
begin
  Result  := fValues.Count
end;

function TCannedList<T>.GetValues(const i: integer): T;
begin
  Result  := fValues[i]
end;

procedure TCannedList<T>.Insert(const Value: T; const Index: integer);
begin
  fValues.Insert(Index, Value)
end;

{ TCannedObjectList<T> }

procedure TCannedObjectList<T>.Clear;
var
  i: integer;
begin
  for i := Count - 1 downto 0 do
    fValues[i].Free;
  inherited;
end;

constructor TCannedObjectList<T>.Create(const AOwnsObjects: Boolean);
begin
  inherited Create;
  fOwnsObjects  := AOwnsObjects;
end;

procedure TCannedObjectList<T>.Delete(const Index: integer);
begin
  fValues[Index].Free;
  inherited;
end;

{ TCannedSortedItems<TKey, TValue> }

procedure TCannedSortedItems<TKey, TValue>.Add(const Key: TKey; const Value: TValue);
var
  i: integer;
  Item: TSortedItem;
begin
  i := IndexOfKey(Key);
  if  i >= 0  then
    raise EArgumentOutOfRangeException.CreateRes(@SArgumentOutOfRange);
  Item.Key    := Key;
  Item.Value  := Value;
  fItems.Insert(i, Item);
end;

procedure TCannedSortedItems<TKey, TValue>.Clear;
var
  i: integer;
begin
  if  OwnsObjects then
    for i := Count - 1 downto 0 do
      ItemByIndex(i).Value.Free;
  fItems.Clear;
end;

constructor TCannedSortedItems<TKey, TValue>.Create(const AComparer: TComparer<TKey>; const AOwnsObjects: Boolean);
begin
  inherited Create;
  fItems  := TList<TSortedItem>.Create;
  fOwnsObjects  := AOwnsObjects;
end;

procedure TCannedSortedItems<TKey, TValue>.Delete(const Key: TKey);
var
  i: integer;
  Item: TSortedItem;
begin
  i := IndexOfKey(Key);
  if  i < 0 then
    raise EArgumentOutOfRangeException.CreateRes(@SArgumentOutOfRange);
  if  OwnsObjects then
    fItems[i].Value.Free;
  fItems.Delete(i);
end;

destructor TCannedSortedItems<TKey, TValue>.Destroy;
begin
  Clear;
  fItems.Free;
  inherited;
end;

function TCannedSortedItems<TKey, TValue>.Find(const Key: TKey): TValue;
var
  i: integer;
begin
  i := IndexOfKey(Key);
  if  i < 0 then
    Result  := nil
  else
    Result  := fItems[i].Value
end;

function TCannedSortedItems<TKey, TValue>.GetCount: integer;
begin
  Result  := fItems.Count
end;

function TCannedSortedItems<TKey, TValue>.GetPower: integer;
begin
  Result  := 1;
  while Result < fItems.Count do
    Result  := Result shl 1;
end;

function TCannedSortedItems<TKey, TValue>.Has(const Key: TKey): Boolean;
begin
  Result  := IndexOfKey(Key) >= 0
end;

function TCannedSortedItems<TKey, TValue>.IndexOfKey(const Key: TKey): integer;
var
  Jump, Range, Comp: integer;
  AtKey: TKey;
begin
  Range   := fItems.Count;
  if  Range = 0 then
    begin
      Result  := -1;
      exit;
    end;
  Result  := GetPower shr 1;
  Jump    := Result shr 1;
  while Jump > 0 do
    begin
      if  Result < Range  then
        begin
          Comp  := fComparer(fItems[Result].Key, Key);
          if  Comp = 0  then
            exit;
          if  Comp < 0  then
            inc(Result, Jump)
          else
            dec(Result, Jump);
        end;
      Jump  := Jump shr 1;
    end;
  if  fComparer(fItems[Result].Key, Key) < 0  then
    Result  := - Result - 1
  else
    Result  := - Result - 2;
end;

function TCannedSortedItems<TKey, TValue>.IndexOfValue(const Value: TValue): integer;
begin
  Result  := Count - 1;
  while Result >= 0 do
    if  fItems[Result].Value = Value  then
      break
    else
      dec(Result);
end;

function TCannedSortedItems<TKey, TValue>.ItemByIndex(const Index: integer): TSortedItem;
begin
  Result  := fItems[Index]
end;

function TCannedSortedItems<TKey, TValue>.ItemByKey(const Key: TKey): TSortedItem;
begin
  Result  := fItems[IndexOfKey(Key)]
end;

{ TSortedItems<TKey, TValue> }

procedure TSortedItems<TKey, TValue>.Add(const Key: TKey; const Value: TValue);
begin
  inherited Add(Key, Value);
end;

function TSortedItems<TKey, TValue>.ByIndex(const Index: integer): TValue;
begin
  Result  := ItemByIndex(Index).Value
end;

function TSortedItems<TKey, TValue>.ByKey(const Key: TKey): TValue;
begin
  Result  := ItemByKey(Key).Value
end;

procedure TSortedItems<TKey, TValue>.Clear;
begin
  inherited Clear;
end;

procedure TSortedItems<TKey, TValue>.Delete(const Key: TKey);
begin
  inherited Delete(Key);
end;

{ TCannedIntegerObjectList<T> }

function TCannedIntegerObjectList<T>.Comparer(const Key1, Key2: integer): integer;
begin
  Result  := Key2 - Key1
end;

constructor TCannedIntegerObjectList<T>.Create(const AOwnsObjects: Boolean);
begin
  inherited Create(Comparer, AOwnsObjects)
end;

function TCannedIntegerObjectList<T>.ByIndex(const Index: integer): T;
begin
  Result  := ItemByIndex(Index).Value
end;

function TCannedIntegerObjectList<T>.ByKey(const Key: integer): T;
begin
  Result  := ItemByKey(Key).Value
end;

{ TIntegerObjectList<T> }

procedure TIntegerObjectList<T>.Add(const Key: integer; const Value: T);
begin
  inherited Add(Key, Value)
end;

procedure TIntegerObjectList<T>.Clear;
begin
  inherited Clear
end;

constructor TIntegerObjectList<T>.Create(const AOwnsObjects: Boolean);
begin
  inherited Create(AOwnsObjects);
end;

procedure TIntegerObjectList<T>.Delete(const Key: integer);
begin
  inherited Delete(Key);
end;

{ TCannedStringObjectList<T> }

function TCannedStringObjectList<T>.Comparer(const Key1, Key2: string): integer;
begin
  Result  := CompareStr(Key1, Key2)
end;

constructor TCannedStringObjectList<T>.Create(const AOwnsObjects: Boolean);
begin
  inherited Create(Comparer, AOwnsObjects);
end;

function TCannedStringObjectList<T>.GetValuesIndex(const Index: integer): T;
begin
  Result  := ItemByIndex(Index).Value
end;

function TCannedStringObjectList<T>.GetValuesName(const Name: string): T;
begin
  Result  := ItemByKey(Name).Value
end;

procedure TCannedStringObjectList<T>.ListAll(const Values: TStrings);
var
  i: integer;
  Item: TSortedItem;
begin
  Values.Clear;
  for i := 0 to Count - 1 do
    begin
      Item  := fItems[i];
      Values.AddObject(Item.Key, Item.Value);
    end;
end;

end.
