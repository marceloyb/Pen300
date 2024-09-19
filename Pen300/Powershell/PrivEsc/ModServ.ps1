function Get-UserGroupSIDs {
    [System.Security.Principal.WindowsIdentity]::GetCurrent().Groups |
        ForEach-Object { $_.Value }  # Obtém os SIDs diretamente
}

# Obter os SIDs dos grupos do usuário atual
$groupSIDs = Get-UserGroupSIDs
$services = Get-WmiObject -Class Win32_Service

foreach ($service in $services) {
	$serviceName = $service.Name
	$command = "cmd /c 'sc sdshow $serviceName'"
	$sd = Invoke-Expression $command
	try {
		$securityDescriptor = New-Object System.Security.AccessControl.CommonSecurityDescriptor $false, $false, $sd
	} catch {
		# Suprime o erro e continua a execução
		continue
	}
	$securityDescriptor = New-Object System.Security.AccessControl.CommonSecurityDescriptor $false, $false, $sd -ErrorAction SilentlyContinue
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