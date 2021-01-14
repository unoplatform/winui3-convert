# Uno.WinUI3Convert

Migrate UWP projects to WinUI3/NET5.

This tool is commonly used in CI environments to automatically generate a WinUI 3 compatible source tree, built separately from the UWP source tree. This allows for the generation of WinUI 3 compatible nuget packages for libraries without having to maintain two separate codebases.

```
Usage:
  winui3convert [options] <source> <destination>

Arguments:
  <source>         Source directory
  <destination>    Destination directory

Options:
  --overwrite       Overwrite destination
  --version         Show version information
  -?, -h, --help    Show help and usage information
```

## Installation

dotnet tool install --global uno.winui3convert

## Conversion adjustments

This tool is meant to help migrate your projects by rewriting namespaces and project files. It won't resolve collisions, work around unsupported features or change code in significant ways.

Manual source adjustments are to be expected in some cases.