@echo off

@powershell -File .nyx\build.ps1 '--appname=Localizations' '--nugetPackageName=Localizations'
@powershell -File .nyx\build.ps1 '--appname=Localizations.PhraseApp' '--nugetPackageName=Localizations.PhraseApp'