
# MediaParser

![.net](https://img.shields.io/badge/Frameworks-net6-purple)
[![License: MIT](https://img.shields.io/badge/License-Apache-yellow.svg)](https://opensource.org/licenses/Apache)
[![NuGet version (AMSMigrate)](https://img.shields.io/nuget/v/Mp4.Parser.svg)](https://www.nuget.org/packages/Mp4.Parser/)

ISO Base File Format (MP4) Parser and Writer
MPEG-2 TS Format Parser and Writer.
Released under Apache License. Please read License.md for the license.

## Defining Boxes

MediaParser relies on partial classes annotated with `BoxAttribute` (and derived attributes) plus a source generator to produce parsing and serialization logic. Every runtime box class derives from `Box`, `FullBox`, or `ContainerBox`, and is decorated with metadata describing its four-character code or UUID.

```csharp
using Media.ISO;
using Media.ISO.Boxes;

[Box(BoxType.FreeBox)]
public partial class FreeBox : RawBox
{
  public string Notes { get; set; } = string.Empty;

  [Reserved(4)]
  public int ReservedSpace { get; set; }
}
```

- `[Box(BoxType.FreeBox)]` ties the class to the `free` atom so `BoxFactory` can instantiate it.
- Each property can be annotated with helpers like `[Reserved]`, `[FlagOptional]`, or `[VersionDependentSize]` to control how the generator reads and writes the data.
- Classes must be `partial` so the generator can emit `.BoxContent.g.cs` files with the actual `ParseBoxContent`, `WriteBoxContent`, and `ContentSize` implementations.

### Generated Serialization

During build, `MP4Parser.SourceGenerators` scans for these attributes and writes strongly typed code. The generated `ParseBoxContent` method automatically tracks the number of bytes remaining in the box and, for the final string property, passes that remaining length to `BoxReader.ReadString()` to avoid reading past the box boundary.

```csharp
protected override void ParseBoxContent(BoxReader reader)
{
  var __remaining = (int)Math.Max(0L, Size - HeaderSize);
  ReservedSpace = reader.ReadInt32();
  __remaining -= sizeof(int);
  Notes = reader.ReadString(__remaining); // string is last property, so generator passes __remaining
  __remaining -= (Notes?.Length ?? 0) + 1;
}
```

`BoxWriter` code is generated in a similar fashion so the same class can be serialized back out.

If a particular box requires bespoke behavior, simply override `ParseBoxContent(BoxReader reader)` and/or `WriteBoxContent(BoxWriter writer)` yourself in the partial class. When these overrides exist the generator skips emitting serialization code for that type, giving you full control without fighting the tooling.

## Building Code

```bash
dotnet build
```

## Running tests

```bash
dotnet test
```
