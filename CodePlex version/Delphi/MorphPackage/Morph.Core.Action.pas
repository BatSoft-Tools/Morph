unit Morph.Core.Action;

interface

uses
  Generics.Collections, Morph.Core.Types, Morph.Core.Link, Morph.Link_Message;

type
  IMorphLinkAction = interface
    ['{44CB26C2-13C0-48D0-8BE6-26CA9955FFF2}']
    procedure Action(const Message: TMessage);
  end;

  MorphActions = class
  private
    class var _Actions: array [TLinkType] of IMorphLinkAction;
  public
    class procedure RegisterAction(const LinkType: TLinkType; const LinkAction: IMorphLinkAction);
    class procedure Action(const Message: TMessage);
  end;

implementation

uses
  Morph.Core.Errors;

{ MorphActions }

class procedure MorphActions.Action(const Message: TMessage);
begin
  if  Assigned(Message.Current) then
    if  not Assigned(_Actions[Message.Current.LinkType])  then
      if  Message.Current.LinkType = linkMessage  then
        raise EMorphUsage.Create('Nested messages are not allowed.')
      else
        raise EMorphUsage.Create('Link type is not registered')
    else
      _Actions[Message.Current.LinkType].Action(Message);
end;

class procedure MorphActions.RegisterAction(const LinkType: TLinkType; const LinkAction: IMorphLinkAction);
begin
  if  Assigned(_Actions[LinkType]) then
    raise EMorphUsage.Create('Link action is already registered');
  _Actions[LinkType] := LinkAction;
end;

end.
