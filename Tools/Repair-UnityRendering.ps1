param(
    [Parameter(Mandatory)][string]$Unity,
    [string]$Project = (Join-Path (Split-Path $PSScriptRoot) 'VR')
)
$ErrorActionPreference = 'Stop'
# Unity 6000.6.9f1 bundles SRP Core 17.6.0 with two missing converter
# helpers, three missing metadata files, and a bridge that calls internal APIs
# without friend access. Restore the official helper files and invoke those
# same editor APIs through reflection. No player rendering code is changed.
$relative = 'Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.render-pipelines.core'
$bundled = Join-Path (Resolve-Path $Unity) $relative
$package = Get-Content -LiteralPath (Join-Path $bundled 'package.json') -Raw | ConvertFrom-Json
if ($package.version -ne '17.6.0') { throw 'This repair is only for bundled SRP Core 17.6.0.' }
$commit = 'e58985d30a651d621c3f3869516e5eb58bec2cae'
$base = "https://raw.githubusercontent.com/Unity-Technologies/Graphics/$commit/Packages/com.unity.render-pipelines.core/"
$files = @(
    'Editor-PrivateShared/Tools/Converter/ReadonlyMaterialConverter/ReadonlyMaterialConverter.MaterialReferenceBuilder.cs',
    'Editor-PrivateShared/Tools/Converter/ReadonlyMaterialConverter/ReadonlyMaterialConverter.MaterialReferenceBuilder.cs.meta',
    'Editor-PrivateShared/Tools/Converter/ReadonlyMaterialConverter/ReadonlyMaterialConverter.MaterialReferenceChanger.cs',
    'Editor-PrivateShared/Tools/Converter/ReadonlyMaterialConverter/ReadonlyMaterialConverter.MaterialReferenceChanger.cs.meta',
    'Editor-PrivateShared/Tools/Converter/Window/RenderPipelineConverterVisualElementListFilter.cs.meta',
    'Editor-PrivateShared/Tools/Converter/Window/RenderPipelineConverterVisualElementListFilter.uss.meta',
    'Editor-PrivateShared/Tools/Converter/Window/RenderPipelineConverterVisualElementListFilter.uxml.meta'
)
foreach ($file in $files) {
    $target = Join-Path $bundled $file
    if (!(Test-Path -LiteralPath $target)) { Invoke-WebRequest -Uri ($base + $file) -OutFile $target }
}
$bridge = 'Runtime-PrivateShared/InternalBridge/RenderPipelineEditorUtilityBridge.cs'
$path = Join-Path $bundled $bridge
$text = Get-Content -LiteralPath $path -Raw
if (!$text.Contains('System.Reflection.BindingFlags')) {
    $oldMigrate = 'UnityEditor.Rendering.RenderPipelineEditorUtility.TryMigrateRenderingLayersToTagManager<T>(renderingLayerNames)'
    $oldClear = 'UnityEditor.Rendering.RenderPipelineEditorUtility.ClearMigratedRenderPipelines()'
    if (!$text.Contains($oldMigrate) -or !$text.Contains($oldClear)) { throw 'Unexpected SRP bridge source; review before patching.' }
    Copy-Item -LiteralPath $path -Destination ($path + '.teacher-tour.bak')
    $flags = 'System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic'
    $text = $text.Replace($oldMigrate, '(bool)typeof(UnityEditor.Rendering.RenderPipelineEditorUtility).GetMethod("TryMigrateRenderingLayersToTagManager", ' + $flags + ').MakeGenericMethod(typeof(T)).Invoke(null, new object[] { renderingLayerNames })')
    $text = $text.Replace($oldClear, 'typeof(UnityEditor.Rendering.RenderPipelineEditorUtility).GetMethod("ClearMigratedRenderPipelines", ' + $flags + ').Invoke(null, null)')
    [IO.File]::WriteAllText($path, $text)
}
# A project imported before the repair already has a fingerprinted copy.
$cache = Join-Path $Project 'Library/PackageCache'
if (Test-Path -LiteralPath $cache) {
    foreach ($folder in Get-ChildItem -LiteralPath $cache -Directory -Filter 'com.unity.render-pipelines.core@*') {
        $cachedPackage = Get-Content -LiteralPath (Join-Path $folder.FullName 'package.json') -Raw | ConvertFrom-Json
        if ($cachedPackage.version -ne '17.6.0') { continue }
        foreach ($file in ($files + $bridge)) {
            $target = Join-Path $folder.FullName $file
            if (Test-Path -LiteralPath $target) { (Get-Item -LiteralPath $target).IsReadOnly = $false }
            Copy-Item -LiteralPath (Join-Path $bundled $file) -Destination $target -Force
        }
    }
}
Write-Output 'Repaired SRP Core 17.6.0 editor helpers. Reopen Unity to compile.'
