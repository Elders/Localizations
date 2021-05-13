@echo off

%FAKE% %NYX% "target=clean" -st
%FAKE% %NYX% "target=RestoreNugetPackages" -st

IF NOT [%1]==[] (set RELEASE_NUGETKEY="%1")
IF NOT [%2]==[] (set RELEASE_TARGETSOURCE="%2")

SET SUMMARY="Localization Abstractions"
SET DESCRIPTION="Localization Abstractions"

SET SUMMARY_PHRASEAPP="PhraseApp Localization"
SET DESCRIPTION_PHRASEAPP="PhraseApp Localization"

%FAKE% %NYX% appName=Localizations                       appSummary=%SUMMARY% appDescription=%DESCRIPTION% nugetserver=%NUGET_SOURCE_DEV_PUSH% nugetkey=%RELEASE_NUGETKEY%

%FAKE% %NYX% appName=Localizations.PhraseApp             appSummary=%SUMMARY_PHRASEAPP% appDescription=%DESCRIPTION_PHRASEAPP% nugetserver=%NUGET_SOURCE_DEV_PUSH% nugetkey=%RELEASE_NUGETKEY%