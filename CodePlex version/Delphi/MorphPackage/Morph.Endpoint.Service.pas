unit Morph.Endpoint.Service;

interface

uses
  Morph.Endpoint.Apartment, Morph.Core.Containers;

type
  TService = class abstract
  private
    fName: string;
  public
    property  Name: string  read fName;
    function  ObtainApartment: TApartment; virtual; abstract;
  end;

  TAllServices = class(TCannedStringObjectList<TService>)
  public
    procedure Register(const Service: TService);
    procedure Unregister(const Service: TService); overload;
    procedure Unregister(const ServiceName: string); overload;
  end;

var
  AllServices: TAllServices;

implementation

{ TAllServices }

procedure TAllServices.Register(const Service: TService);
begin
  Add(Service.Name, Service)
end;

procedure TAllServices.Unregister(const Service: TService);
begin
  Delete(Service.Name)
end;

procedure TAllServices.Unregister(const ServiceName: string);
begin
  Delete(ServiceName)
end;

initialization
  AllServices    := TAllServices.Create;

finalization
  AllServices.Free;

end.
