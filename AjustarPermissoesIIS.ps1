$paths = @("C:\inetpub\tecflow\api", "C:\inetpub\tecflow\webui")
foreach ($path in $paths) {
    if (Test-Path $path) {
        $acl = Get-Acl $path
        $permission = "IIS_IUSRS","ReadAndExecute","Allow"
        $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($permission)
        $acl.SetAccessRule($accessRule)
        Set-Acl $path $acl

        $permission2 = "IUSR","ReadAndExecute","Allow"
        $accessRule2 = New-Object System.Security.AccessControl.FileSystemAccessRule($permission2)
        $acl.SetAccessRule($accessRule2)
        Set-Acl $path $acl
    }
}
