param($installPath, $toolsPath, $package, $project)

$dte.ItemOperations.OpenFile((Join-Path $toolsPath 'Readme.txt'))