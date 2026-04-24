; Shipped analyzer releases.
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 8.0.0

### New Rules

Rule ID | Category        | Severity | Notes
--------|-----------------|----------|----------------------------------------------------------
JSG001  | SourceGenerator | Error    | Source generation error — generator threw during emission.
JSG002  | SourceGenerator | Warning  | Informational log from the generator.
JSG003  | SourceGenerator | Warning  | Envelope missing [PopcornPayload] — middleware will fall back to default shape.
JSG004  | SourceGenerator | Warning  | Envelope has duplicate Popcorn marker attributes.
JSG005  | SourceGenerator | Warning  | Envelope [PopcornPayload] property should be Pop&lt;T&gt;.
JSG006  | SourceGenerator | Warning  | Envelope [PopcornError] property should be ApiError or ApiError?.
JSG007  | SourceGenerator | Warning  | Envelope nested inside a generic outer type is not supported.
JSG008  | SourceGenerator | Warning  | Property typed as object / abstract class / interface — AOT non-starter.
