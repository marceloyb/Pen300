# Importa o m�dulo Active Directory
Import-Module ActiveDirectory

# Executa um comando que requer permiss�es de Domain Admin
Add-ADGroupMember -Identity "Domain Admins" -Members "L57653A"
