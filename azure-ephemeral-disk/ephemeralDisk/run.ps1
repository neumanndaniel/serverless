using namespace System.Net

# Input bindings are passed in via param block.
param($Request, $TriggerMetadata)

function Get-OsDiskSize ($Size) {
  if ($Size -gt 2048) {
    return 2048
  }
  else {
    return $Size
  }
}

function Get-Skus ($Location) {
  $EphemeralOsDisk = @()
  $Skus = Get-AzComputeResourceSku -Location $Location | Where-Object { $_.ResourceType -eq "virtualMachines" }
  foreach ($Sku in $Skus) {
    if (($Sku.Capabilities | Where-Object { $_.Name -eq "EphemeralOSDiskSupported" }).Value -eq $true -and ($Sku.Capabilities | Where-Object { $_.Name -eq "PremiumIO" }).Value -eq $true -and $null -ne ($Sku.Capabilities | Where-Object { $_.Name -eq "CachedDiskBytes" }).Value) {
      $VmSku = New-Object PSObject -Property @{
        Name                     = $Sku.Name
        Family                   = $Sku.Family -replace "standard", "" -replace "Family", "" -replace " ", ""
        EphemeralOsDiskSupported = [bool]($Sku.Capabilities | Where-Object { $_.Name -eq "EphemeralOSDiskSupported" }).Value
        MaxEphemeralOsDiskSizeGb = Get-OsDiskSize -Size (($Sku.Capabilities | Where-Object { $_.Name -eq "CachedDiskBytes" }).Value / 1GB)
      }
      $EphemeralOsDisk += $VmSku
    }
  }
  return $EphemeralOsDisk
}

# Write to the Azure Functions log stream.
Write-Host "PowerShell HTTP trigger function processed a request."

# Interact with query parameters or the body of the request.
$Location = $Request.Query.location
if (-not $Location) {
  $Location = $Request.Body.location
}

$Family = $Request.Query.family
if (-not $Family) {
  $Family = $Request.Body.family
}

if ($null -eq $Family) {
  $Family = ""
}

if ($null -ne $Location) {
  $Result = Get-Skus -Location $Location

  if ($Family -ne "") {
    $body = $Result | Where-Object { $_.Family -eq $Family } | Select-Object -Property Name, Family, MaxEphemeralOsDiskSizeGb, EphemeralOsDiskSupported | ConvertTo-Json
  }
  else {
    $body = $Result | Select-Object -Property Name, Family, MaxEphemeralOsDiskSizeGb, EphemeralOsDiskSupported | ConvertTo-Json
  }
}
else {
  $body = "This HTTP triggered function executed successfully. Pass an Azure location in the query string or in the request body for a personalized response."
}

# Associate values to output bindings by calling 'Push-OutputBinding'.
Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
    StatusCode = [HttpStatusCode]::OK
    Body       = $body
  })
