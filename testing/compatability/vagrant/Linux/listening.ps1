param ([string] $version,
[string] $serverUrl,
[string] $serverusername,
[string] $serverApiKey,
[string] $environment,
[string] $roll,
[string] $space,
[string] $port,
[string] $serverthumbprint,
[bool] $deploymentTarget,
[string] $workerPool)

$hasSpaces = !$version.StartsWith("3.")
$path = "/opt/octopus/tentacle$version"
$ip = $(/sbin/ip -o -4 addr list enp0s8 | awk '{print $4}' | cut -d/ -f1)

if($deploymentTarget){
    $typeName = "DeploymentTarget"
 }else {
    $typeName = "Worker"
 }

."$path/Tentacle" create-instance --instance "LinuxListening$typeName.$version" --config "/opt/OctopusListening$typeName$version/Tentacle.config"
."$path/Tentacle" new-certificate --instance "LinuxListening$typeName.$version" --if-blank
."$path/Tentacle" configure --instance "LinuxListening$typeName.$version" --reset-trust
."$path/Tentacle" configure --instance "LinuxListening$typeName.$version" --app "/opt/OctopusListening$typeName$version/Applications" --port "$port" --noListen "False"
."$path/Tentacle" configure --instance  "LinuxListening$typeName.$version" --trust "$serverthumbprint"

if($deploymentTarget)
{
    if($hasSpaces)
    {
        ."$path/Tentacle" register-with --instance "LinuxListening$typeName.$version" --server "$serverUrl" --name "LinuxListening$typeName.$version" --comms-style "TentaclePassive" --tentacle-comms-port "$port" --force --apiKey "$serverApiKey" --environment "$environment" --role "$roll" --space $space --publicHostName $ip
    }
    else
    {
        ."$path/Tentacle" register-with --instance "LinuxListening$typeName.$version" --server "$serverUrl" --name "LinuxListening$typeName.$version" --comms-style "TentaclePassive" --tentacle-comms-port "$port" --force --apiKey "$serverApiKey" --environment "$environment" --role "$roll" --publicHostName $ip        
    }
}
else
{
    if($hasSpaces)
    {
        ."$path/Tentacle" register-worker --instance "LinuxListening$typeName.$version" --server "$serverUrl" --name "LinuxListening$typeName.$version" --comms-style "TentaclePassive" --tentacle-comms-port "$port" --force --apiKey "$serverApiKey" --space $space --workerpool "$workerPool" --publicHostName $ip
    }
    else
    {
        ."$path/Tentacle" register-worker --instance "LinuxListening$typeName.$version" --server "$serverUrl" --name "LinuxListening$typeName.$version" --comms-style "TentaclePassive" --tentacle-comms-port "$port" --force --apiKey "$serverApiKey" --workerpool "$workerPool" --publicHostName $ip        
    }
}

."$path/Tentacle" service --instance "LinuxListening$typeName.$version" --install --stop --start