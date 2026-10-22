<#
.SYNOPSIS
    Finds the ACT logs that contain a Dancing Mad (Ultimate) pull that got through Phase 3.

.DESCRIPTION
    Reads your ACT network logs (Network_*.log) and, for every Dancing Mad (Ultimate) pull in
    them, works out how far the pull got. It then lists the logs with at least one pull that
    cleared Phase 3 (reached Kefka Says in Phase 4).

    It only reads the logs: nothing is changed, uploaded or deleted. -CopyTo copies the
    matching logs into a folder of your choice so you can zip and send them.

    How to run it:
      1. Save this file somewhere, e.g. your Desktop.
      2. Open PowerShell in that folder (Shift + right-click the folder background,
         "Open PowerShell window here", or type "powershell" in Explorer's address bar).
      3. Run it with -LogDir set to the folder your Network_*.log files are in:
           powershell -ExecutionPolicy Bypass -File .\FindP3Clears.ps1 -LogDir "D:\ACT\FFXIVLogs"
         or, to also copy the matching logs to a folder on your Desktop:
           powershell -ExecutionPolicy Bypass -File .\FindP3Clears.ps1 -LogDir "D:\ACT\FFXIVLogs" -CopyTo "$HOME\Desktop\P3Logs"

    Keep the quotes around folder paths. Without -LogDir it looks in ACT's default log folder
    (%APPDATA%\Advanced Combat Tracker\FFXIVLogs), and asks for your folder if that one
    doesn't exist. Big logs take a few seconds each.

.PARAMETER LogDir
    The folder holding the Network_*.log files. Defaults to ACT's log folder. -Path and
    -LogDirectory work too, or give the folder as the first argument.

.PARAMETER Reached
    How far a pull must get for its log to be listed: P3 (reached Phase 3), P4 (cleared
    Phase 3; the default), P5 (cleared Phase 4) or Clear (killed the boss).

.PARAMETER CopyTo
    A folder to copy the matching logs into. Created if it doesn't exist.

.EXAMPLE
    .\FindP3Clears.ps1 -LogDir "D:\ACT\FFXIVLogs"
.EXAMPLE
    .\FindP3Clears.ps1 -LogDir "D:\ACT\FFXIVLogs" -CopyTo "$HOME\Desktop\P3Logs"
.EXAMPLE
    .\FindP3Clears.ps1 "D:\ACT\FFXIVLogs" -Reached P3
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [Alias('Path', 'LogDirectory')]
    [string]$LogDir,
    [ValidateSet('P3', 'P4', 'P5', 'Clear')]
    [string]$Reached = 'P4',
    [string]$CopyTo
)

$ErrorActionPreference = 'Stop'

if (-not $LogDir) {
    $LogDir = Join-Path $env:APPDATA 'Advanced Combat Tracker\FFXIVLogs'
    if (-not (Test-Path -LiteralPath $LogDir -PathType Container)) {
        Write-Host "ACT's default log folder isn't here: $LogDir" -ForegroundColor Yellow
        $LogDir = (Read-Host 'Paste the folder your Network_*.log files are in').Trim().Trim('"')
    }
}
$Path = $LogDir

# C# 5 only: Windows PowerShell 5.1 compiles this with the old .NET Framework compiler.
if (-not ('DmuLogScanner' -as [type])) {
    Add-Type -Language CSharp -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

public class DmuPull
{
    public string Start;
    public string End;
    // 1 = P1-P2, 3 = P3, 4 = P4, 5 = P5, 6 = clear.
    public int Phase = 1;
    public bool HadCombat;
}

public static class DmuLogScanner
{
    // ACT writes the zone id in hex; 0x553 is Dancing Mad (Ultimate).
    const string DmuZone = "553";

    public static List<DmuPull> Scan(string path)
    {
        var pulls = new List<DmuPull>();
        DmuPull current = null;
        var inDmu = false;
        // Between a wipe and the next (re)commence the boss's leftovers still act; that's no pull.
        var betweenPulls = false;
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1 << 20))
        using (var reader = new StreamReader(stream))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length < 4 || line[2] != '|') continue;
                var a = line[0];
                var b = line[1];

                if (a == '0' && b == '1')
                {
                    var f = line.Split('|');
                    if (f.Length < 3) continue;
                    var nowDmu = string.Equals(f[2], DmuZone, StringComparison.OrdinalIgnoreCase);
                    if (inDmu && !nowDmu) Close(ref current, pulls);
                    inDmu = nowDmu;
                    betweenPulls = false;
                    continue;
                }
                if (!inDmu) continue;

                if (a == '3' && b == '3')
                {
                    var f = line.Split('|');
                    if (f.Length < 4) continue;
                    if (f[3] == "40000001" || f[3] == "40000006") betweenPulls = false;
                    if (f[3] == "40000005" || f[3] == "40000003")
                    {
                        if (current == null) current = new DmuPull();
                        current.End = f[1];
                        if (f[3] == "40000003") current.Phase = 6;
                        Close(ref current, pulls);
                        betweenPulls = true;
                    }
                }
                else if (betweenPulls) continue;
                else if (a == '2' && (b == '1' || b == '2'))
                {
                    // Ability lines: a pull has begun once an enemy (id 4xxxxxxx) acts.
                    var stampEnd = line.IndexOf('|', 3);
                    if (stampEnd < 0 || stampEnd + 1 >= line.Length || line[stampEnd + 1] != '4') continue;
                    var stamp = line.Substring(3, stampEnd - 3);
                    if (current == null) current = new DmuPull();
                    if (!current.HadCombat) { current.HadCombat = true; current.Start = stamp; }
                    current.End = stamp;
                }
                else if (a == '2' && b == '0')
                {
                    var f = line.Split('|');
                    if (f.Length < 5) continue;
                    var phase = f[4] == "C3F7" ? 3 : f[4] == "C2DC" ? 4 : f[4] == "BB40" ? 5 : 0;
                    if (phase == 0) continue;
                    if (current == null) current = new DmuPull();
                    if (phase > current.Phase) current.Phase = phase;
                }
                else if (a == '0' && b == '3')
                {
                    var f = line.Split('|');
                    if (f.Length < 11 || (f[10] != "19508" && f[10] != "19509")) continue;
                    if (current == null) current = new DmuPull();
                    if (current.Phase < 3) current.Phase = 3;
                }
            }
        }
        Close(ref current, pulls);
        return pulls;
    }

    static void Close(ref DmuPull current, List<DmuPull> pulls)
    {
        if (current != null && (current.HadCombat || current.Phase > 1)) pulls.Add(current);
        current = null;
    }

    public static string Clock(string stamp)
    {
        DateTimeOffset t;
        if (stamp == null || !DateTimeOffset.TryParse(stamp, CultureInfo.InvariantCulture, DateTimeStyles.None, out t)) return "?";
        return t.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    }

    public static string Length(string start, string end)
    {
        DateTimeOffset s, e;
        if (start == null || end == null
            || !DateTimeOffset.TryParse(start, CultureInfo.InvariantCulture, DateTimeStyles.None, out s)
            || !DateTimeOffset.TryParse(end, CultureInfo.InvariantCulture, DateTimeStyles.None, out e)) return "?";
        var span = e - s;
        return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}", (int)span.TotalMinutes, span.Seconds);
    }
}
'@
}

$phaseNames = @{ 1 = 'P1-P2'; 3 = 'P3'; 4 = 'P4'; 5 = 'P5'; 6 = 'Clear' }
$minimum = @{ P3 = 3; P4 = 4; P5 = 5; Clear = 6 }[$Reached]

if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
    Write-Host "Log folder not found: $Path" -ForegroundColor Red
    Write-Host 'Pass the folder your Network_*.log files are in with -LogDir "D:\path\to\FFXIVLogs".'
    exit 1
}

$files = @(Get-ChildItem -LiteralPath $Path -Filter '*Network_*.log' -File | Sort-Object Name)
if ($files.Count -eq 0) {
    Write-Host "No Network_*.log files in $Path" -ForegroundColor Yellow
    exit 1
}

Write-Host "Scanning $($files.Count) log(s) in $Path ..."
$results = New-Object System.Collections.Generic.List[object]
$i = 0
foreach ($file in $files) {
    $i++
    Write-Progress -Activity 'Scanning ACT logs' -Status $file.Name -PercentComplete ([int](100 * $i / $files.Count))
    try {
        $pulls = [DmuLogScanner]::Scan($file.FullName)
    }
    catch {
        Write-Host "  Couldn't read $($file.Name): $($_.Exception.Message)" -ForegroundColor Yellow
        continue
    }
    if ($pulls.Count -eq 0) { continue }
    $best = ($pulls | Measure-Object -Property Phase -Maximum).Maximum
    $results.Add([pscustomobject]@{
        Log         = $file.Name
        SizeMB      = [int][math]::Round($file.Length / 1MB)
        Pulls       = $pulls.Count
        'Reached P3' = @($pulls | Where-Object { $_.Phase -ge 3 }).Count
        'Cleared P3' = @($pulls | Where-Object { $_.Phase -ge 4 }).Count
        Best        = $phaseNames[[int]$best]
        File        = $file
        PullList    = $pulls
    })
}
Write-Progress -Activity 'Scanning ACT logs' -Completed

if ($results.Count -eq 0) {
    Write-Host 'None of these logs has a Dancing Mad (Ultimate) pull.' -ForegroundColor Yellow
    exit 0
}

Write-Host ''
Write-Host 'Logs with Dancing Mad (Ultimate) pulls:' -ForegroundColor Cyan
$results | Format-Table Log, SizeMB, Pulls, 'Reached P3', 'Cleared P3', Best -AutoSize | Out-String -Width 200 | Write-Host

$label = @{ P3 = 'reached Phase 3'; P4 = 'cleared Phase 3'; P5 = 'cleared Phase 4'; Clear = 'killed the boss' }[$Reached]
$hits = @($results | Where-Object { @($_.PullList | Where-Object { $_.Phase -ge $minimum }).Count -gt 0 })
if ($hits.Count -eq 0) {
    Write-Host "No log has a pull that $label." -ForegroundColor Yellow
    exit 0
}

Write-Host "Logs with a pull that $label ($($hits.Count)):" -ForegroundColor Green
foreach ($m in $hits) {
    Write-Host "  $($m.Log)"
    $n = 0
    foreach ($p in $m.PullList) {
        $n++
        if ($p.Phase -lt $minimum) { continue }
        Write-Host ("    pull {0,-3} {1}  lasted {2,-6} got to {3}" -f $n, [DmuLogScanner]::Clock($p.Start), [DmuLogScanner]::Length($p.Start, $p.End), $phaseNames[$p.Phase])
    }
}

if ($CopyTo) {
    New-Item -ItemType Directory -Force -Path $CopyTo | Out-Null
    foreach ($m in $hits) { Copy-Item -LiteralPath $m.File.FullName -Destination $CopyTo -Force }
    Write-Host ''
    Write-Host "Copied $($hits.Count) log(s) to $CopyTo" -ForegroundColor Green
}
