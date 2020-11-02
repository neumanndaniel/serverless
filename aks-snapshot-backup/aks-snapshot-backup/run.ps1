# Input bindings are passed in via param block.
param($Timer)

# Get the current universal time in the default string format.
$currentUTCtime = (Get-Date).ToUniversalTime()

# The 'IsPastDue' property is 'true' when the current function invocation is later than scheduled.
if ($Timer.IsPastDue) {
    Write-Host "PowerShell timer is running late!"
}

#region function

# Variables
$StorageTableName = "akssnapshotbackup"

Write-Output "Search subscription for database store..."
$Database = Get-AzResource -TagName 'aksSnapshotBackupDatabase' -TagValue 'tableStorage'
if ($null -eq $Database) {
    Write-Error "No database store found."
    throw
}
if ($Database.length -gt 1) {
    Write-Error "More than one database store found."
    throw
}

Write-Output "Search subscription for tagged AKS Azure disks to backup..."
$Resources = Get-AzResource -TagName 'aksSnapshotBackupEnabled' -TagValue 'true'

Write-Output "Getting table storage context..."
$StorageContext = (Get-AzStorageAccount -ResourceGroupName $Database.ResourceGroupName -Name $Database.Name -ErrorAction Stop).Context

$Table = (Get-AzStorageTable -Name $StorageTableName -Context $StorageContext -ErrorAction SilentlyContinue).CloudTable
if ($null -eq $Table) {
    Write-Output "Initializing table $StorageTableName..."
    $Table = (New-AzStorageTable -Name $StorageTableName -Context $StorageContext -ErrorAction Stop).CloudTable
}

foreach ($Resource in $Resources) {
    $Date = Get-Date
    $FormattedDate = Get-Date -UFormat %Y-%m-%dT%I-%M%p

    if ($null -eq $Resource.Tags.retentionTime) {
        $RetentionTime = "3"
    }
    else {
        $RetentionTime = $Resource.Tags.retentionTime
    }

    $Message = "Creating disk snapshot for AKS Azure disk: " + $Resource.Name
    Write-Output $Message
    $SnapshotConfig = New-AzSnapshotConfig `
        -SourceResourceId $Resource.Id -Location $Resource.Location -SkuName Standard_LRS `
        -CreateOption copy -ErrorAction Continue
    $SnapshotName = $Resource.Name + "-" + $FormattedDate
    $Snapshot = New-AzSnapshot -ResourceGroupName $Database.ResourceGroupName -SnapshotName $SnapshotName -Snapshot $SnapshotConfig -ErrorAction Continue

    Write-Output "Writing table storage entry..."
    $PartitionKey = [String]$Date.Year + "-" + [String]$Date.Month + "-" + [String]$Date.Day
    $TableEntry = @{
        "region"                    = $Resource.Location;
        "retentionTime"             = $RetentionTime;
        "azureSnapshotResourceId"   = $Snapshot.Id;
        "azureSourceDiskResourceId" = $Resource.Id
    }
    Add-AzTableRow -Table $Table -PartitionKey $PartitionKey -RowKey $SnapshotName -Property $TableEntry -ErrorAction Continue
}

#endregion

# Write an information log with the current time.
Write-Host "PowerShell timer trigger function ran! TIME: $currentUTCtime"
