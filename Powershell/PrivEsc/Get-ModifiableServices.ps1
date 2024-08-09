function Get-UserGroupSIDs {
    [System.Security.Principal.WindowsIdentity]::GetCurrent().Groups |
        ForEach-Object { $_.Translate([System.Security.Principal.NTAccount]).Value } |
        ForEach-Object {
            $groupName = $_.Split('\')[-1]
            $searcher = New-Object System.DirectoryServices.DirectorySearcher([ADSI]"")
            $searcher.Filter = "(&(objectClass=group)(sAMAccountName=$groupName))"
            $searcher.PropertiesToLoad.Add("objectSid") | Out-Null
            $result = $searcher.FindOne()
            if ($result) {
                $sidBytes = $result.Properties["objectSid"][0]
                $sid = New-Object System.Security.Principal.SecurityIdentifier($sidBytes, 0)
                $sid.Value
            }
        }
}

# Obter os SIDs dos grupos do usuário atual
$groupSIDs = Get-UserGroupSIDs
# Obter todos os serviços
$services = Get-WmiObject -Class Win32_Service

foreach ($service in $services) {
	$serviceName = $service.Name
	$command = "cmd /c 'sc sdshow $serviceName'"
	$sd = Invoke-Expression $command
	$securityDescriptor = New-Object System.Security.AccessControl.CommonSecurityDescriptor $false, $false, $sd
	$dacl = $securityDescriptor.DiscretionaryAcl
	foreach ($ace in $dacl) {
		$sid = $ace.SecurityIdentifier
		$accessMask = $ace.AccessMask
		$permissions = [System.Security.AccessControl.FileSystemRights]$accessMask
		$permissionList = $permissions -split ',\s*'
		if ($groupSIDs -contains $sid) {
		# Imprimir apenas as ACEs com SIDs que pertencem aos grupos
			$permissionList = $permissions -split ',\s*'
			foreach ($permission in $permissionList) {
				if ($permission -eq 'Write'){
					Write-Output "$serviceName $sid $permission"
				}			
			}
		}	   
	}
}