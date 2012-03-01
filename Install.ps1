param($installPath, $toolsPath, $package, $project)

$ref=$project.Object.References | Where {$_.Name.ToString() -eq "Xtensive.Orm"} | Select -First 1
if($ref -ne $null)
{
	$version=[string]::Format("{0}.{1}.{2}", $ref.MajorVersion, $ref.MinorVersion, $ref.BuildNumber)
	$ref=$project.Object.References | Where {$_.Name.ToString() -eq "DataObjectsExtensions"} | Select -First 1
	if($ref -ne $null)
	{
		$ref.Remove()
	}
	$path=$toolsPath+"\"+$version+"\DataObjectsExtensions.dll";
	$project.Object.References.Add($path)
}