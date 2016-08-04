"%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\installutil.exe" Syncer.exe
net start Syncer
sc config Syncer start= auto