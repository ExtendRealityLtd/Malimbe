# `FodyRunner.UnityIntegration`

Weaves assemblies using `FodyRunner` in the Unity software editor after the Unity software compiles them.

> There is no need to manually run the weaving process. The library just needs to be part of a Unity software project (it's configured to only run in the Editor) to be used. It hooks into the various callbacks the Unity software offers and automatically weaves any assembly on startup as well as when they change.

If the `FodyRunner.UnityIntegration` source requires updating then the Unity Editor Assemblies Path needs to be configured in the project as there are dependencies on the Unity Engine libraries.

1. Create a `FodyRunner.UnityIntegration.csproj.user` within the `Malimbe/Sources/FodyRunner.UnityIntegration/` directory and set the file contents to:
    ```xml
    <Project>
      <PropertyGroup>
        <UnityEditorAssembliesPath>path-to-your-unity-software-installation-editor-assemblies</UnityEditorAssembliesPath>
      </PropertyGroup>
    </Project>
    ```
1. Replace `path-to-your-unity-software-installation-editor-assemblies` with the actual local path to the Unity Software Editor Assemblies e.g. `C:\Program Files\Unity\Hub\Editor\2018.3.7f1\Editor\Data\Managed\`
1. Save the changes to `FodyRunner.UnityIntegration.csproj.user`