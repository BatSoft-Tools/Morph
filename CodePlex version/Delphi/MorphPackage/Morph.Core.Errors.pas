unit Morph.Core.Errors;

interface

uses
  SysUtils;

type
  EMorph = class(Exception);

  EMorphUsage = class(Exception);

  EMorphImplementation = class(EMorph);

implementation

end.
