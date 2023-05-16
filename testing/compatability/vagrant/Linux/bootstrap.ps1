#!/usr/bin/env pwsh
param([string] $serverUrl,
[string] $serverPollingPort,
[string] $serverApiKey,
[string] $environment,
[string] $pollingRoll,
[string] $listeningRoll,
[string] $space,
[string] $tentacleVersions,
[string] $listening,
[string] $polling,
[string] $deploymentTargets,
[string] $workers,
[string] $serverThumbprint,
[string] $workerPool)

echo "serverUrl:$serverUrl"
echo "serverPollingPort:$serverPollingPort"
echo "serverApiKey:$serverApiKey"
echo "environment:$environment"
echo "listeningRoll:$listeningRoll"
echo "pollingRoll:$pollingRoll"
echo "space:$space"
echo "workerPool:$workerPool"

sudo ufw disable

$versions = $tentacleVersions.Split(",")

echo "verisons:$versions"
echo "listening:$listening"
echo "polling:$polling"

echo "deploymentTargets:$deploymentTargets"
echo "workers:$workers"

cd /tmp/

$port = 12900

apt-key adv --fetch-keys https://apt.octopus.com/public.key
add-apt-repository "deb https://apt.octopus.com/ stretch main"
apt-get update

foreach ( $version in $versions )
{
    ./install.ps1 -version $version

    if($polling.ToLower() -eq "true")
    {
        if($deploymentTargets.ToLower() -eq "true")
        {
            ./polling.ps1 -deploymentTarget $True -version "$version" -serverUrl "$serverUrl" -serverPollingPort "$serverPollingPort" -serverApiKey "$serverApiKey" -environment "$environment" -roll "$listeningRoll" -space "$space" -workerpool "na"
        }

        if($workers.ToLower() -eq "true")
        {
            ./polling.ps1 -deploymentTarget $False -version "$version" -serverUrl "$serverUrl" -serverPollingPort "$serverPollingPort" -serverApiKey "$serverApiKey" -environment "na" -roll "na" -space "$space" -workerpool "$workerPool"
        }
    }

    if($listening.ToLower() -eq "true")
    {
        if($deploymentTargets.ToLower() -eq "true")
        {
            ./listening.ps1 -deploymentTarget $True -version "$version" -serverUrl "$serverUrl" -serverApiKey "$serverApiKey" -environment "$environment" -roll "$pollingRoll" -space "$space" -port $port.ToString() -serverthumbprint $serverThumbprint -workerpool "na"
            $port = $port + 1
        }

        if($workers.ToLower() -eq "true")
        {
            .\listening.ps1 -deploymentTarget $False -version "$version" -serverUrl "$serverUrl" -serverApiKey "$serverApiKey" -environment "na" -roll "na" -space "$space" -port $port.ToString() -serverthumbprint $serverThumbprint -workerpool "$workerPool"
            $port = $port + 1
        }
    }
}
