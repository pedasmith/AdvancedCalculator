#$currPath=get-location
#$path= "$currPath\small\word\document.xml"
#$xml=New-Object System.XML.XMLDocument  
#$xml.Load($path)
#$version=$oXMLDocument.Package.Identity.Version.split(".")
#$body = $xml.document.body


#$allt = $body | select -ExpandProperty childnodes | where {$_.name -like 'p'}
#$allt = $xml.SelectNodes("//*")

#write $body
#write $allt

$tempdir = "mydoc"
$startfile = "startdocument.zip"
$endzip = "myresult2.zip"
$endfile = "Best Calculator and BC BASIC UOPDATED.docx"

#
# Replace the version string in the manual
#
$currPath=get-location
$path= "$currPath\mydoc\word\document.xml"
$txt = [Io.File]::ReadAllText($path) 
write $txt.Length
# Unicode Pocket Calculator 🖩
$lookFor=[char][UInt16]0xd83d+[char][UInt16]0xdda9
$start = $txt.IndexOf($lookFor)
$end = $txt.IndexOf($lookFor, $start+5)
write $start, $end
write $lookFor
if ($start -gt 0) { 
	write $txt.SubString($start+$lookFor.Length, $end-$start-$lookFor.Length)
	$newtxt = $txt.Substring(0, $start) + $lookFor + " NEW AWESOME VERSION " + $lookFor + $txt.SubString($end+$lookFor.Length, $txt.Length-$end-$lookFor.Length)
	[Io.File]::WriteAllText($path, $newtxt)
	cd $tempdir 
	compress-archive "*" -destinationpath "..\$endzip"
	cd ".."
	rename-item $endzip $endfile
	
}