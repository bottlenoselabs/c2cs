#!/bin/bash

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"

if [[ ! -z "$1" ]]; then
    RID="$1"
    if [[ -z "$RID" ]]; then
        echo "Runtime identifier not specified."
    fi
fi

dotnet publish $DIR/src/cs/production/C2CS/C2CS.csproj \
--output $DIR/publish/$RID \
--configuration Release \
--runtime $RID \
--self-contained \
'/property:MSBuildTreatWarningsAsErrors=false' \
'/property:ILLinkTreatWarningsAsErrors=false' \
'/property:SuppressTrimAnalysisWarnings=false' \
'/property:EnableTrimAnalyzer=true' \
'/property:ILLinkWarningLevel=9999' \
'/property:TrimmerSingleWarn=false' \
'/property:TrimmerRemoveSymbols=true' \
'/property:PublishSingleFile=true' \
'/property:CopyLocalLockFileAssemblies=false' \
'/property:GenerateTargetFrameworkAttribute=false' \
'/property:DebugType=embedded' \
'/property:DebugSymbols=true' \
'/property:IncludeNativeLibrariesForSelfExtract=false' \
'/property:GenerateDocumentationFile=false' \
'/property:SatelliteResourceLanguages=en' \
'/property:InvariantGlobalization=true' \
'/property:PublishTrimmed=true' \
'/property:TrimMode=link' \
'/property:PublishReadyToRun=false' \
'/property:EnableUnsafeBinaryFormatterSerialization=false' \
'/property:EnableUnsafeUTF7Encoding=false' \
'/property:EventSourceSupport=false' \
'/property:HttpActivityPropagationSupport=false' \
'/property:MetadataUpdaterSupport=false' \
'/property:UseNativeHttpHandler=true' \
'/property:UseSystemResourceKeys=true' \
'/property:DebuggerSupport=true' \
'/property:IlcGenerateCompleteTypeMetadata=true' \
'/property:IlcInvariantGlobalization=true' \
'/property:IlcFoldIdenticalMethodBodies=true'