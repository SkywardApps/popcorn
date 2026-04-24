#!/usr/bin/env bash
# Run the MatrixBenchmarks across (.NET version × JIT/AOT).
#
# Each cell is a separate `dotnet run` (JIT) or `dotnet publish + binary` (AOT)
# invocation. BDN runs inside that process and produces a per-cell JSON report
# under benchmarks/matrix/results/.
#
# Prerequisites (any missing cell is skipped with a clear note):
#   - .NET 8, 9, 10 SDKs AND runtimes installed.
#   - For AOT on Windows: VS C++ build tools (cl.exe / link.exe).
#   - For AOT on Linux:   clang + zlib1g-dev.
#
# Usage: benchmarks/matrix/run-matrix.sh [tfm-filter] [mode-filter]
#   Examples:
#     benchmarks/matrix/run-matrix.sh               # run all 6 cells
#     benchmarks/matrix/run-matrix.sh net9          # only the two .NET 9 cells
#     benchmarks/matrix/run-matrix.sh all aot       # only the three AOT cells

set -u  # don't use -e: we want to continue across failed cells

REPO="$(cd "$(dirname "$0")/../.." && pwd)"
PROJECT="$REPO/dotnet/benchmarks/MatrixPerformance"
RESULTS_ROOT="$REPO/benchmarks/matrix/results"
mkdir -p "$RESULTS_ROOT"

TFM_FILTER="${1:-all}"
MODE_FILTER="${2:-all}"

TFMS=("net8.0" "net9.0" "net10.0")
MODES=("jit" "aot")

# Pick the host-runnable TFM for AOT. Must match a runtime family the SDK can publish for.
RID="${POPCORN_RID:-}"
if [ -z "$RID" ]; then
  case "$(uname -s)" in
    Linux*)  RID="linux-x64"  ;;
    Darwin*) RID="osx-x64"    ;;
    MINGW*|CYGWIN*|MSYS*) RID="win-x64" ;;
    *) RID="win-x64" ;;  # conservative default — user can override via POPCORN_RID.
  esac
fi

run_jit() {
  local tfm="$1"
  local label="JIT-${tfm}"
  local outdir="$RESULTS_ROOT/$label"
  echo ""
  echo "=============================================================="
  echo " $label — dotnet run -f $tfm"
  echo "=============================================================="
  rm -rf "$outdir"
  mkdir -p "$outdir"
  # Build artifacts live in the project's own BenchmarkDotNet.Artifacts; we copy after.
  ( cd "$PROJECT" && rm -rf BenchmarkDotNet.Artifacts && \
    dotnet run -c Release -f "$tfm" --no-launch-profile ) \
    2>&1 | tee "$outdir/stdout.log"
  if [ -d "$PROJECT/BenchmarkDotNet.Artifacts" ]; then
    cp -r "$PROJECT/BenchmarkDotNet.Artifacts/results/." "$outdir/" 2>/dev/null || true
  fi
}

run_aot() {
  local tfm="$1"
  local label="AOT-${tfm}"
  local outdir="$RESULTS_ROOT/$label"
  # On Windows the ILCompiler needs vswhere.exe + link.exe from VS build tools. vswhere is
  # usually at the VS Installer path but not on PATH. Add it if we're on Windows and the
  # file exists; harmless elsewhere.
  if [ -f "/c/Program Files (x86)/Microsoft Visual Studio/Installer/vswhere.exe" ]; then
    export PATH="/c/Program Files (x86)/Microsoft Visual Studio/Installer:$PATH"
  fi
  echo ""
  echo "=============================================================="
  echo " $label — dotnet publish -p:PublishAot=true -f $tfm -r $RID"
  echo "=============================================================="
  rm -rf "$outdir"
  mkdir -p "$outdir"
  local publishdir="$PROJECT/bin/MatrixAot/$tfm/publish"
  rm -rf "$PROJECT/bin/MatrixAot/$tfm"
  # PublishAot is set inside the csproj (not here via -p:) so it stays scoped to
  # MatrixPerformance and doesn't propagate NETSDK1207 to the netstandard2.0 ProjectReferences.
  ( cd "$PROJECT" && \
    dotnet publish -c Release -f "$tfm" -r "$RID" \
      -o "$publishdir" ) 2>&1 | tee "$outdir/publish.log"
  if [ ! -f "$publishdir/MatrixPerformance.exe" ] && [ ! -f "$publishdir/MatrixPerformance" ]; then
    echo "SKIP: AOT publish for $tfm did not produce a binary." | tee -a "$outdir/stdout.log"
    return 1
  fi
  local binary
  if [ -f "$publishdir/MatrixPerformance.exe" ]; then
    binary="$publishdir/MatrixPerformance.exe"
  else
    binary="$publishdir/MatrixPerformance"
  fi
  ( cd "$PROJECT" && rm -rf BenchmarkDotNet.Artifacts && \
    "$binary" ) 2>&1 | tee "$outdir/stdout.log"
  if [ -d "$PROJECT/BenchmarkDotNet.Artifacts" ]; then
    cp -r "$PROJECT/BenchmarkDotNet.Artifacts/results/." "$outdir/" 2>/dev/null || true
  fi
}

want_tfm() {
  local tfm="$1"
  [ "$TFM_FILTER" = "all" ] && return 0
  case "$tfm" in
    *"$TFM_FILTER"*) return 0 ;;
  esac
  return 1
}

want_mode() {
  local mode="$1"
  [ "$MODE_FILTER" = "all" ] && return 0
  [ "$MODE_FILTER" = "$mode" ] && return 0
  return 1
}

for tfm in "${TFMS[@]}"; do
  want_tfm "$tfm" || continue
  for mode in "${MODES[@]}"; do
    want_mode "$mode" || continue
    case "$mode" in
      jit) run_jit "$tfm" ;;
      aot) run_aot "$tfm" ;;
    esac
  done
done

echo ""
echo "=============================================================="
echo " Matrix run complete. Per-cell results under:"
echo "   $RESULTS_ROOT"
echo ""
echo " Aggregate report:"
echo "   python3 $REPO/benchmarks/matrix/summarize-matrix.py $RESULTS_ROOT"
echo "=============================================================="
