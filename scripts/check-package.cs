// Checks that a packed LumexUI .nupkg contains what consumers need. Nothing else verifies the package, and a
// missing theme file or targets file only shows up in a consumer's build.
//
// Usage: dotnet run scripts/check-package.cs -- <path to LumexUI.*.nupkg> [<LumexUI project dir>]

using System.IO.Compression;

if( args.Length is < 1 or > 2 )
{
    Console.Error.WriteLine( "Usage: dotnet run scripts/check-package.cs -- <LumexUI.*.nupkg> [<project dir>]" );
    return 2;
}

var packagePath = args[0];
var projectDir = args.Length > 1 ? args[1] : Path.Combine( "src", "LumexUI" );

using var package = ZipFile.OpenRead( packagePath );
var entries = package.Entries
    .Where( entry => !entry.FullName.EndsWith( '/' ) )
    .ToDictionary( entry => entry.FullName, StringComparer.Ordinal );

var failures = new List<string>();

void Require( string path )
{
    if( !entries.TryGetValue( path, out var entry ) )
    {
        failures.Add( $"missing {path}" );
    }
    else if( entry.Length == 0 )
    {
        failures.Add( $"empty {path}" );
    }
}

// MSBuild wiring that adds the theme's Tailwind sources to a consumer's build.
Require( "buildTransitive/LumexUI.targets" );

// The theme CSS that consumers import.
foreach( var css in new[] { "theme.css", "_dark.css", "_light.css", "_layout.css", "_sonner.css", "_tw-animate.css" } )
{
    Require( $"theme/{css}" );
}

// The component style classes, which Tailwind scans for class names: exactly the project's Styles/*.cs.
var expectedStyles = Directory.GetFiles( Path.Combine( projectDir, "Styles" ), "*.cs" )
    .Select( file => $"theme/components/{Path.GetFileName( file )}" )
    .ToHashSet( StringComparer.Ordinal );
var packedStyles = entries.Keys
    .Where( path => path.StartsWith( "theme/components/", StringComparison.Ordinal ) )
    .ToHashSet( StringComparer.Ordinal );

if( expectedStyles.Count == 0 )
{
    failures.Add( $"found no Styles/*.cs under {projectDir}, so the style check would prove nothing" );
}

failures.AddRange( expectedStyles.Except( packedStyles ).Order().Select( path => $"missing {path}" ) );
failures.AddRange( packedStyles.Except( expectedStyles ).Order().Select( path => $"unexpected {path}" ) );

// The floating-ui bundle that rollup generates during the build. It isn't committed, so a build without Node
// would silently leave it out.
Require( "staticwebassets/js/components/popover.bundle.js" );

// Every target framework ships the icons assembly next to LumexUI.dll, because the project reference is private.
var frameworks = entries.Keys
    .Where( path => path.StartsWith( "lib/", StringComparison.Ordinal ) && path.EndsWith( "/LumexUI.dll", StringComparison.Ordinal ) )
    .Select( path => path.Split( '/' )[1] )
    .Order()
    .ToList();

if( frameworks.Count == 0 )
{
    failures.Add( "no lib/<framework>/LumexUI.dll" );
}

foreach( var framework in frameworks )
{
    Require( $"lib/{framework}/LumexUI.Shared.Icons.dll" );
}

if( failures.Count > 0 )
{
    Console.Error.WriteLine( $"{Path.GetFileName( packagePath )} is incomplete:" );
    foreach( var failure in failures )
    {
        Console.Error.WriteLine( $"  {failure}" );
    }

    return 1;
}

Console.WriteLine(
    $"{Path.GetFileName( packagePath )}: OK ({string.Join( ", ", frameworks )}; {packedStyles.Count} component styles)" );
return 0;
