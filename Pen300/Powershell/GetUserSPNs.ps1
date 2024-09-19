[CmdletBinding()]
Param(
  [Parameter(Mandatory=$False,Position=1)] [string]$CatalogName,
  [Parameter(Mandatory=$False)] [string]$SearchFilter,
  [Parameter(Mandatory=$False)] [switch]$InitiateRequest,
  [Parameter(Mandatory=$False)] [switch]$UniqueUsers
)

Add-Type -AssemblyName System.IdentityModel

$GCList = @()

If ($CatalogName) {
  $GCList += $CatalogName
} else {
  $ForestDetails = [System.DirectoryServices.ActiveDirectory.Forest]::GetCurrentForest()
  $GlobalCatalogs = $ForestDetails.FindAllGlobalCatalogs()
  ForEach ($GlobalCatalog in $GlobalCatalogs) {
    $GCList += $ForestDetails.ApplicationPartitions[0].SecurityReferenceDomain
  }
}

if (-not $GCList) {
  Write-Host "No Global Catalogs Found!"
  Exit
}

ForEach ($GlobalCatalog in $GCList) {
    $dirSearch = New-Object System.DirectoryServices.DirectorySearcher
    $dirSearch.SearchRoot = "LDAP://" + $GlobalCatalog
    $dirSearch.PageSize = 1000
    $dirSearch.Filter = "(&(!objectClass=computer)(servicePrincipalName=*))"
    $dirSearch.PropertiesToLoad.Add("serviceprincipalname") | Out-Null
    $dirSearch.PropertiesToLoad.Add("name") | Out-Null
    $dirSearch.PropertiesToLoad.Add("samaccountname") | Out-Null
    $dirSearch.PropertiesToLoad.Add("memberof") | Out-Null
    $dirSearch.PropertiesToLoad.Add("pwdlastset") | Out-Null
    $dirSearch.SearchScope = "Subtree"

    $searchResults = $dirSearch.FindAll()
    
    [System.Collections.ArrayList]$userAccounts = @()
        
    foreach ($entry in $searchResults) {
        foreach ($serviceName in $entry.Properties["serviceprincipalname"]) {
            $output = Select-Object -InputObject $entry -Property `
                @{Name="SPN"; Expression={$serviceName.ToString()} }, `
                @{Name="UserName"; Expression={$entry.Properties["name"][0].ToString()} }, `
                @{Name="AccountName"; Expression={$entry.Properties["samaccountname"][0].ToString()} }, `
                @{Name="GroupMembership"; Expression={$entry.Properties["memberof"][0].ToString()} }, `
                @{Name="PwdSetTime"; Expression={[datetime]::fromFileTime($entry.Properties["pwdlastset"][0])} }

            if ($UniqueUsers) {
                if (-not $userAccounts.Contains($entry.Properties["samaccountname"][0].ToString())) {
                    $userAccounts.Add($entry.Properties["samaccountname"][0].ToString()) | Out-Null
                    $output
                    if ($InitiateRequest) {
                        New-Object System.IdentityModel.Tokens.KerberosRequestorSecurityToken -ArgumentList $serviceName.ToString() | Out-Null
                    }
                }
            } else {
                $output
                if ($InitiateRequest) {
                    New-Object System.IdentityModel.Tokens.KerberosRequestorSecurityToken -ArgumentList $serviceName.ToString() | Out-Null
                }
            }
        }
    }
}
