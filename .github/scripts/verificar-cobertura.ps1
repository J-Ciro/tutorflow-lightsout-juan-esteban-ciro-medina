param(
    [Parameter(Mandatory = $true)][string]$Directorio,
    [double]$MinimoCore = 90,
    [double]$MinimoTotal = 80
)

$ErrorActionPreference = 'Stop'
$produccion = @('TutorFlow.Core', 'TutorFlow.Infrastructure', 'TutorFlow.Api')
$lineas = @{}

$archivos = Get-ChildItem -Path $Directorio -Recurse -Filter '*.cobertura.xml'
if ($archivos.Count -eq 0) {
    Write-Error 'No se generó ningún reporte de cobertura.'
}

foreach ($archivo in $archivos) {
    $xml = [xml](Get-Content -Raw -Path $archivo.FullName)
    foreach ($paquete in $xml.coverage.packages.package) {
        if ($produccion -notcontains $paquete.name) { continue }
        foreach ($clase in $paquete.classes.class) {
            if ($clase.filename -match '[\\/]obj[\\/]') { continue }
            foreach ($linea in $clase.SelectNodes('.//line')) {
                $clave = "$($paquete.name)|$($clase.filename)|$($linea.number)"
                $golpes = [int]$linea.hits
                if (-not $lineas.ContainsKey($clave) -or $lineas[$clave] -lt $golpes) {
                    $lineas[$clave] = $golpes
                }
            }
        }
    }
}

function Porcentaje($claves) {
    if ($claves.Count -eq 0) { return 0.0 }
    $cubiertas = @($claves | Where-Object { $lineas[$_] -gt 0 }).Count
    return 100.0 * $cubiertas / $claves.Count
}

$core = Porcentaje @($lineas.Keys | Where-Object { $_.StartsWith('TutorFlow.Core|') })
$total = Porcentaje @($lineas.Keys)
Write-Host ("Cobertura de lineas Core: {0:N2}% (minimo {1}%)" -f $core, $MinimoCore)
Write-Host ("Cobertura de lineas total: {0:N2}% (minimo {1}%)" -f $total, $MinimoTotal)

if ($core -lt $MinimoCore -or $total -lt $MinimoTotal) {
    Write-Error 'La cobertura esta por debajo del minimo.'
}
