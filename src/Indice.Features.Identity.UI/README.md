# This is a Razor class library

The reason that this has not StaticWebAssets is that in order to create a web application 
that the developer can override the assets comming from here one must know how todo 
MSBuild gymnastics..

According to this article someone could potentially tap into 
the static asset generation process and potentialy enhance it to take into account 
a client side build pipeline like npm
https://devblogs.microsoft.com/dotnet/build-client-web-assets-for-your-razor-class-library/

According to this article here there is a way to use a custom build target that removes conflicting assets 

https://github.com/dotnet/aspnetcore/issues/14568

ResolveStaticWebAssetsInputs 
GetCurrentProjectStaticWebAssets

This helped alot as well as debuggin the MSBuild logs using this tool here
https://msbuildlog.com/
https://stackoverflow.com/questions/72107400/msbuild-project-get-item-list-from-another-project-and-print-foreach
```xml
  <PropertyGroup>
    <ResolveStaticWebAssetsInputsDependsOn>RemoveIdentityAssets</ResolveStaticWebAssetsInputsDependsOn>
  </PropertyGroup>
  <!-- This will remove any duplicate assets found on the Host web application and the UI project. Host always wins-->
  <Target Name="RemoveIdentityAssets">
    <ItemGroup>
      <StaticWebAsset Remove="@(StaticWebAsset)" Condition="%(SourceId) == 'Indice.Features.Identity.UI' And Exists($([System.IO.Path]::GetFullPath($(MSBuildProjectDirectory)/wwwroot%(StaticWebAsset.BasePath)%(StaticWebAsset.RelativePath))))" />
    </ItemGroup>
  </Target>
```
Without the exists condition it would not be able to exclude depending on the host project.