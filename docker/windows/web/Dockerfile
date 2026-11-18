# escape=`
# ExpertPdf Anywhere - ASP.NET Razor Pages web sample in a Windows container.
#
# Builds the sample project in windows/Web/csharp/ExpertPdf.Anywhere.Samples/ - the same code as the
# non-container sample. The conversion engines come with the ExpertPdf.Anywhere.Windows package and are
# published with the app; nothing is copied by hand.
#
# Run from the samples folder (the folder that holds windows/, linux/, mac/ and docker/):
#
#   docker build -f docker/windows/web/Dockerfile -t expertpdf-anywhere:web-windows .
#   docker run --rm -p 8080:8080 expertpdf-anywhere:web-windows
#   # then open http://localhost:8080
#
# Packages are restored from nuget.org. To build against packages you have locally, put the
# .nupkg files in a "nupkg" folder next to the docker folder; they are used as an extra source.

# ---- fonts ---------------------------------------------------------------------------------
# Windows Server Core images ship almost no fonts, and the Chromium engine needs
# the standard Windows families. They are copied from the full Windows Server image and
# registered in the runtime stage.
FROM mcr.microsoft.com/windows/server:ltsc2022 AS fonts

# ---- build ---------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0-windowsservercore-ltsc2022 AS build
WORKDIR C:\src
# The optional nupkg folder (see above). docker/README.md is always present, so the copy never
# fails when there is no nupkg folder.
COPY docker/README.md nupk[g] ./local-packages/
RUN dotnet nuget add source C:\src\local-packages --name local-packages
COPY windows/Web/csharp/ExpertPdf.Anywhere.Samples ./app
WORKDIR C:\src\app
RUN dotnet publish -c Release -f net10.0 -o C:\out

# ---- runtime -------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-windowsservercore-ltsc2022 AS runtime
COPY --from=fonts ["C:/Windows/Fonts/segoeui.ttf", "C:/Windows/Fonts/segoeuib.ttf", "C:/Windows/Fonts/segoeuii.ttf", "C:/Windows/Fonts/segoeuiz.ttf", "C:/Windows/Fonts/arial.ttf", "C:/Windows/Fonts/arialbd.ttf", "C:/Windows/Fonts/ariali.ttf", "C:/Windows/Fonts/arialbi.ttf", "C:/Windows/Fonts/times.ttf", "C:/Windows/Fonts/timesbd.ttf", "C:/Windows/Fonts/timesi.ttf", "C:/Windows/Fonts/timesbi.ttf", "C:/Windows/Fonts/cour.ttf", "C:/Windows/Fonts/courbd.ttf", "C:/Windows/Fonts/couri.ttf", "C:/Windows/Fonts/courbi.ttf", "C:/Windows/Fonts/verdana.ttf", "C:/Windows/Fonts/verdanab.ttf", "C:/Windows/Fonts/verdanai.ttf", "C:/Windows/Fonts/verdanaz.ttf", "C:/Windows/Fonts/tahoma.ttf", "C:/Windows/Fonts/tahomabd.ttf", "C:/Windows/Fonts/"]
RUN powershell -Command "Get-ChildItem C:\Windows\Fonts -Filter *.ttf | ForEach-Object { New-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts' -Name ($_.BaseName + ' (TrueType)') -Value $_.Name -PropertyType String -Force | Out-Null }"
WORKDIR /app
COPY --from=build C:\out ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ExpertPdf.Anywhere.Samples.dll"]
