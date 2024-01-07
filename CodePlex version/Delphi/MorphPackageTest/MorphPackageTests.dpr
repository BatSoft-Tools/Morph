program MorphPackageTests;
{

  Delphi DUnit Test Project
  -------------------------
  This project contains the DUnit test framework and the GUI/Console test runners.
  Add "CONSOLE_TESTRUNNER" to the conditional defines entry in the project options
  to use the console test runner.  Otherwise the GUI test runner will be used by
  default.

}

{$IFDEF CONSOLE_TESTRUNNER}
{$APPTYPE CONSOLE}
{$ENDIF}

uses
  DUnitTestRunner,
  Morph.Client.Test in 'Morph.Client.Test.pas',
  Morph.Client in '..\MorphPackage\Morph.Client.pas',
  Morph.Link.Test in 'Morph.Link.Test.pas',
  Morph.Link_Internet in '..\MorphPackage\Morph.Link_Internet.pas',
  Morph.Link_Member in '..\MorphPackage\Morph.Link_Member.pas',
  Morph.Link_Servlet in '..\MorphPackage\Morph.Link_Servlet.pas',
  Morph.Link_Service in '..\MorphPackage\Morph.Link_Service.pas',
  Morph.Link_Message in '..\MorphPackage\Morph.Link_Message.pas',
  Morph.Link_End in '..\MorphPackage\Morph.Link_End.pas',
  uStreamTest in 'uStreamTest.pas';

{R *.RES}

begin
  DUnitTestRunner.RunRegisteredTests;
end.

