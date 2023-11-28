[CmdletBinding()]
param(
  [switch]$ForceRecreate
)

#requires -RunAsAdministrator

$category = @{Name="NServiceBus"; Description="NServiceBus statistics"}
$counters = New-Object System.Diagnostics.CounterCreationDataCollection
$counters.AddRange(@(
	New-Object System.Diagnostics.CounterCreationData "SLA violation countdown", "Duration in seconds until the configured Service Level Agreement (SLA) for this endpoint is breached. This is an instantaneous snapshot, not an average over the time interval.",  NumberOfItems32
	New-Object System.Diagnostics.CounterCreationData "Critical Time Average", "Average duration in seconds for sending and processing of all messages during the sample interval. Useful to understand how long a new message added to the queue right now will take to be processed.",  AverageTimer32
	New-Object System.Diagnostics.CounterCreationData "Critical Time AverageBase", "A base counter used to calculate the Critical Time Average",  AverageBase
	New-Object System.Diagnostics.CounterCreationData "Critical Time", "Duration in seconds for sending and processing the last processed message, useful to understand how long a new message added to the queue right now will take to be processed. This is an instantaneous snapshot, not an average over the time interval.",  NumberOfItems32
	New-Object System.Diagnostics.CounterCreationData "Processing Time Average", "Average duration in seconds of all successfully processed messages during the sample interval.",  AverageTimer32
	New-Object System.Diagnostics.CounterCreationData "Processing Time AverageBase", "A base counter used to calculate the Processing Time.",  AverageBase
	New-Object System.Diagnostics.CounterCreationData "Processing Time", "Duration in seconds of the last successfully processed message. This is an instantaneous snapshot, not an average over the time interval.",  NumberOfItems32
	New-Object System.Diagnostics.CounterCreationData "# of msgs failures / sec", "The current number of failed processed messages by the transport per second.",  RateOfCountsPerSecond32
	New-Object System.Diagnostics.CounterCreationData "# of msgs successfully processed / sec", "The current number of messages processed successfully by the transport per second.",  RateOfCountsPerSecond32
	New-Object System.Diagnostics.CounterCreationData "# of msgs pulled from the input queue /sec", "The current number of messages pulled from the input queue by the transport per second.",  RateOfCountsPerSecond32
	New-Object System.Diagnostics.CounterCreationData "Retries", "A message has been scheduled for retry (FLR or SLR)",  RateOfCountsPerSecond32

))

if ([System.Diagnostics.PerformanceCounterCategory]::Exists($category.Name)) {

	if($ForceRecreate) {
		Write-Host "Option -ForceRecreate was used. The performance counter category will be recreated"
		[System.Diagnostics.PerformanceCounterCategory]::Delete($category.Name)
	} 
	else {
		foreach($counter in $counters){
			$exists = [System.Diagnostics.PerformanceCounterCategory]::CounterExists($counter.CounterName, $category.Name)
			if (!$exists){
				Write-Host "One or more counters are missing.The performance counter category will be recreated"
				[System.Diagnostics.PerformanceCounterCategory]::Delete($category.Name)

				break
			}
		}
	}
}

if (![System.Diagnostics.PerformanceCounterCategory]::Exists($category.Name)) {
	Write-Host "Creating the performance counter category"
	[void] [System.Diagnostics.PerformanceCounterCategory]::Create($category.Name, $category.Description, [System.Diagnostics.PerformanceCounterCategoryType]::MultiInstance, $counters)
	}
else {
	Write-Host "No performance counters have to be created"
}

[System.Diagnostics.PerformanceCounter]::CloseSharedResources()
