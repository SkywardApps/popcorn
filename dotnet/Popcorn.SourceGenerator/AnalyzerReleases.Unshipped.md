; Unshipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md
; Rules first shipped in 8.0.0-preview.1; they stay here until a stable release header
; (release tracking cannot parse prerelease versions like "8.0.0-preview.1"), at which
; point they move to AnalyzerReleases.Shipped.md under "## Release 8.0.0".

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
JSG001  | SourceGenerator | Error | Source generation failed for a type or the registration file; the exception message is surfaced verbatim.
JSG002  | SourceGenerator | Info | Generator log output surfaced as a diagnostic (informational).
JSG003  | SourceGenerator | Warning | Envelope marked [PopcornEnvelope] has no [PopcornPayload] property.
JSG004  | SourceGenerator | Warning | Envelope has multiple properties with the same Popcorn marker attribute.
JSG005  | SourceGenerator | Warning | [PopcornPayload] property is not typed as Pop&lt;T&gt;.
JSG006  | SourceGenerator | Warning | [PopcornError] property is not typed as ApiError.
JSG007  | SourceGenerator | Warning | Envelope nested inside a generic outer type is not supported.
JSG008  | SourceGenerator | Warning | Member type (object/abstract/interface) cannot be resolved at build time; AOT non-starter.
