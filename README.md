# NLog.Targets.Gelf
Gelf4NLog is an [NLog] target implementation to push log messages to [GrayLog2]. It implements the [Gelf] specification and communicates with GrayLog server via UDP.

## History
Code forked from https://github.com/akurdyukov/Gelf4NLog which is a fork from https://github.com/RickyKeane/Gelf4NLog who forked the origonal code from https://github.com/seymen/Gelf4NLog

## Versioning
Until v1 is released on nuget we can't promise that we wont introduce breaking changes.

## Solution
Solution is comprised of 3 projects: *Target* is the actual NLog target implementation, *Tests* contains the unit tests for the NLog target, and *ConsoleRunner* is a simple console project created in order to demonstrate the library usage.
## Usage
Use Nuget:
<!--- 
```
PM> Install-Package NLog.Targets.Gelf -Pre
```
-->
```
PM> Install-Package NLog.Targets.Gelf
```
### Configuration
Here is a sample nlog configuration snippet:
```xml
<configSections>
  <section name="nlog" type="NLog.Config.ConfigSectionHandler, NLog" />
</configSections>

<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
  xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">

	<extensions>
	  <add assembly="NLog.Targets.Gelf"/>
	</extensions>

	<targets>
	  <!-- Other targets (e.g. console) -->
	
	  <target name="gelf" 
			  xsi:type="gelf" 
			  endpoint="udp://logs.local:12201"
			  facility="console-runner"
	  >
		<!-- Optional parameters -->
		<parameter name="param1" layout="${longdate}"/>
		<parameter name="param2" layout="${callsite}"/>
	  </target>
	</targets>

	<rules>
	  <logger name="*" minlevel="Debug" writeTo="gelf" />
	</rules>

</nlog>
```

Options are the following:
* __name:__ arbitrary name given to the target
* __type:__ set this to "graylog"
* __host:__ IP address or Hostname of the GrayLog2 server
* __hostport:__ Port number that GrayLog2 server is listening on
* __facility:__ The graylog2 facility to send log messages

###Code
```c#
//excerpt from ConsoleRunner
var eventInfo = new LogEventInfo
				{
					Message = comic.Title,
					Level = LogLevel.Info,
				};
eventInfo.Properties.Add("Publisher", comic.Publisher);
eventInfo.Properties.Add("ReleaseDate", comic.ReleaseDate);
Logger.Log(eventInfo);
```

[NLog]: http://nlog-project.org/
[GrayLog2]: http://graylog2.org/
[Gelf]: http://graylog2.org/about/gelf