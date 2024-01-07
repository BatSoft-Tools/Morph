unit Morph.Core.Types;

interface

type
  TLinkType = (
    linkEnd         = $0,
    linkMessage     = $8,
    linkData        = $4,
    linkInformation = $C,
    linkService     = $2,
    linkServlet     = $A,
    linkMember      = $6,
    link_E          = $E,
    linkProcess     = $1,
    linkInternet    = $9,
    link_5          = $5,
    link_D          = $D,
    linkSequence    = $3,
    linkEncoding    = $B,
    linkStream      = $7,
    link_F          = $F);

  TID               = Int32;

  TApartmentID      = TID;
  TApartmentProxyID = TID;
  TSessionID        = UInt64;
  TServletID        = TID;

const
  NoID  = 0;

implementation

end.
