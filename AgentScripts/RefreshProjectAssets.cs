using UnityEditor;
using UnityEditor.Compilation;

public static class RefreshProjectAssets
{
    public static string Run()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
        return "Asset refresh and script compilation requested.";
    }
}
