# NLog.Targets.NetworkJson
NLog.Targets.NetworkJson is an [NLog] target implementation to push log messages to logstash via its TCP input plugin (or UDP for unreliable communications). 

Since this implements a simple JSON packet communication mechanism it isn't exclusively for use with logstash but since NLog.Targets.Gelf and the Gelf input plugin in logstash didn't have reliable TCP based communication I implemented this target to communicate with the tcp input plugin instead.

This target implements a newline seperated JSON packet communication in TCP mode with a single reconnect on error of the TCP socket during target communication. If reconnection fails it throws an exception currently.

In UDP mode it sends a single JSON packet in each UDP communication.

## History
Code forked from https://github.com/2020Legal/NLog.Targets.Gelf which is a fork from https://github.com/akurdyukov/Gelf4NLog which is a fork from https://github.com/RickyKeane/Gelf4NLog who forked the origonal code from https://github.com/seymen/Gelf4NLog

Although this code was originally forked from a Gelf target intended to communicate with a Graylog2 all protocol specific code has been removed and a TCP communication mechanism has been added as teh target used to support only UDP.

## TODO
Features to be implemented in the near future are:
* In TCP mode if the TCP communication fails and reconnection fails the packet will be written to file for retransmission later by filebeats or another mechanism.
* TLS based socket encryption with mutual authentication support will be added so that the TCP input plugin in logstash will be able to communicate with this target when both *ssl_enable* and *ssl_verify* are true.
* Convert over the Test and Console Runner projects to be used by this new target.

## Solution
Solution is comprised of 1 projects: *Target* is the actual NLog target implementation.

### Configuration
Here is a sample nlog configuration snippet:
```xml
<configSections>
  <section name="nlog" type="NLog.Config.ConfigSectionHandler, NLog" />
</configSections>

<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
  xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">

	<extensions>
	  <add assembly="NLog.Targets.NetworkJson"/>
	</extensions>

	<targets>
	  <!-- Other targets (e.g. console) -->
	
	  <target name="NetworkLog" 
			  xsi:type="NetworkJson" 
			  endpoint="tcp://logs.wherever.com:8888">
		<!-- Optional parameters -->
		<parameter name="param1" layout="${longdate}"/>
		<parameter name="param2" layout="${callsite}"/>
	  </target>
	</targets>

	<rules>
	  <logger name="*" minlevel="Debug" writeTo="NetworkLog" />
	</rules>

</nlog>
```

Options are the following:
* __name:__ arbitrary name given to the target
* __xsi:type:__ set this to "NetworkJson"
* __endpoint:__ the uri pointing to the tcp input plugin of the logstash service in the format {tcp or udp}://{IP or host name}:{port}

### JSON Packet Description
The *Base* JSON defined in a log message will be comprised of the following:

* __logLevel:__ The nlog LogEventInfo’s Level property.
* __message:__ The nlog LogEventInfo’s FormattedMessage property.
* __messageType:__ The type of message, set from LogEventInfo’s LoggerName property.
* __logSequenceId:__ The LogEventInfo’s SequenceID as set by nlog as a unique incrementing number for the life of the process.
* __clientTimestamp:__ A high precision representation of the LogEventInfo’s TimeStamp property, written by NewtonSoft.Json as __yyyy-MM-ddTHH:mm:ss.fffffffzzzz__.
* If UserStackFrame information is available in the LogEventInfo:
	* __line:__ The file line number.
	* __file:__ The file full path info.
* If Exception information is available in the LogEventInfo:
	* __exceptionSource:__ The exception source for the main exception.
	* __exceptionMessage:__ The messages of the exceptions, up to 10 levels deep.
	* __stackTrace:__ The stack traces of the exceptions, up to 10 levels deep.

Also included will be:

* Any __key/value__ pair defined in the LogEventInfo's Properties collection.
* Any __name/layout__ Parameter defined in the nlog configuration.

###Code
```c#
var eventInfo = new LogEventInfo
				{
					Message = comic.Title,
					Level = LogLevel.Info,
				};
eventInfo.Properties.Add("Publisher", comic.Publisher);
eventInfo.Properties.Add("ReleaseDate", comic.ReleaseDate);
Logger.Log(eventInfo);
```
or alternatively for simple log messages
```c#
Logger.Info("Simple message {0}", value);
```

[NLog]: http://nlog-project.org/
[Logstash]: https://www.elastic.co/guide/en/logstash/current/index.html
[tcp input plugin]: https://www.elastic.co/guide/en/logstash/current/plugins-inputs-tcp.html
