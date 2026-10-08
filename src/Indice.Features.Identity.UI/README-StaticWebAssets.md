# About this Razor class library

- This project is a Razor class library that provides static web assets for driving the identity UI and all internet facing identity related operations.
  These include login, registration, password reset, multi factor authentication, external providers etc. as well as self managing the user profile (preferences, timezone, locale, picture etc.).
- The goal of this project is to provide a fully functional identity UI that can be used out of the box with minimal configuration out of the box but capable for partial override and customization of the presentation layer. 
- Also one needs to be able to override these assets from the host application.
  by simply adding a static file under the same logical location under the wwwroot (Web Root).
  This is important for theming and customization purposes.

## Static Web Assets Behavior

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
- Debug MSBuild logs https://msbuildlog.com/
- [MSBuild cheat sheet](https://gist.github.com/dotMorten/7db5cc3ae4ab72db784df0793b45d6ac)
- [MSBuild print list of included items](https://stackoverflow.com/questions/72107400/msbuild-project-get-item-list-from-another-project-and-print-foreach)
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

<!-- https://stackoverflow.com/questions/5187671/including-files-with-directory-specified-separately-in-msbuild -->

Add the following to your project file to generate a debug file that lists all the duplicate assets found in the project. This is useful for debugging and ensuring that no unwanted assets are included in the final build.
```xml
<!-- Append to FileWrites so the file will be removed on clean -->
    <ItemGroup>
      <DebugFile Include="$(MSBuildProjectDirectory)\wwwroot\DebugFile.txt" />
    </ItemGroup>
    <!-- Generate config file here -->
    <WriteLinesToFile File="@(DebugFile)" Lines="@(DuplicateAsset->'%(RelativePath) :: %(Path) :: %(_RelativePath)')" Overwrite="true" />
    <ItemGroup>
      <FileWrites Include="@(DebugFile)"/>
    </ItemGroup>
```

To debug this regex you need a list of test paths that you want to match.
```regex
(#\[.+\]\?)(\.[a-zA-Z]+)+(?=\.gz|\.br)(\.gz|\.br)|(#\[.+\]\?)(\.[a-zA-Z]+)+
```
this is a sample list of paths that you can use to test the regex
```
admin/assets/img/logo#[.{fingerprint=bxdwwh9ky4}]?.svg.gz
admin/assets/img/logo#[.{fingerprint}]?.svg
admin/assets/img/logo#[.{fingerprint=bxdwwh9ky4}]?.en.md.gz
admin/assets/img/logo#[.{fingerprint}]?.en.md
```