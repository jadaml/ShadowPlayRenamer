# Shadow Play Renamer tool #

Shadow Play Renamer as it's “creative” name suggests, renames the video file
names that Shadow Play records. The main goal of this tool to rename the date
format in the file name, from the MM.DD.YYYY format to the YYYY.MM.DD format.

The service makes use of the fact, that Shadow Play lock the video file that is
being recorded, and gets unlocked once it finishes recording. During the period
of the file lock, the service periodically checks the file by attempting to
rename the file.
The tools are only focusing on `.mp4` files.

**Note**
This is a quick and dirty fix for the lack of implementation for specifying
custom file names in NVIDIA Shadow Play feature. Configuring it might require
some diligence for users not as involved in programming.
I primarily made for my self and to study how to implement services in .NET, but
share it here so others can benefit from the project.

[<img align="left" src="https://www.gnu.org/graphics/gplv3-127x51.png"/>][GPLv3]
This project is licensed under the GPL v3 which you can read more about in the
[COPYING.md][COPYING.md] file, or click the logo to the left, to see the latest
version.

**Disclaimers**
I do not take any responsibility for any loss of data this tool might cause.
Use it at your own risk.

<!-- Trademark and Copyright disclaimers against NVidia and Shadow Play -->

  [GPLv3L]: https://www.gnu.org/graphics/gplv3-127x51.png
  [GPLv3]: https://www.gnu.org/licenses/gpl-3.0.en.html
  [COPYING.md]: COPYING.md

# Download #

## Pre-compiled ##

**Note**
As of writing this readme file, I still not sure where I am going to publish the
binary files as this is one of the first project I release in such manner.
My apologies in any inconvenience this causes.  
Updates to this document will be made only the next time I update the source
files.

You can find the latest release on the [Releases page][Latest] at the top of the
page, where you can download either the installer or just the binary files.

  [Latest]: /releases/latest

## Source code ##

You can alway download the source code from here, on GitHub by clicking on
*Clone or Download* then selecting *Download ZIP*.

After you extract it, you can open the solution file with Visual Studio and
compile it in Release mode. Although the project is made with Visual Studio
2017, it should open in Visual Studio 2015 as well.

The compiler must be at least C# 6.0 which is what used by Visual Studio 2015.

# Usage #

The primary use of this tool is by installing the `SPRSVC.exe` service either
by the installer or by hand. However I have provided an `SPRCLI.exe` which uses
the exact same logic as the service.

## Installation ##

### By MSI ###

The MSI installer will automatically install the service, asks for the folder
where you record your videos, and configure them during installation.  
If you whish to further configure the service, you may do so by following the
[Fine tuning](#tuning) section.

You can specify the following properties to automate the process:

INSTALLFOLDER  
:   The path to install the binary files.

SPPATH  
:   The path to your video recordings.

INSTALLPDB=1  
:   Install the debug symbols. Debug symbols are not required for the normal
    operation of the software. It is only provided, if you encounter a problem
    and you wish to report the issue.

    Debug symbols can be also downloaded separately.

### Manually ###

1. Copy the binaries that you either compiled yourself or downloaded to a
   location of your choice.
1. Open a command-prompt with administrator privileges, and navigate to the
   location you have copied the files.
1. Type in the following command: `SPRSVC.exe Monitor "<Path to video
   recordings>"`

   *This will update the SPRSVC.exe.config file to the path you have typed in the
   command. If the command fails, an exception is written in the console.*
1. Type in the following command next: `SPRSVC.exe Install`

   *If the command fails, an exception is written in the console.*
1. *The next 3 steps are required because the service writing in the event log,
   and it is not prepared if the event log doesn't exists.*

   Open `Regedit`, and navigate to the
   `HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\EventLog\Application`
   key.
1. Right-click on the `Application` key and create a new key called `SPRSVC`
   and navigate in to it.
1. Right-click in the right side of `regedit` interface, and create a new
   string value. Name it `EventMessageFile`.
1. Double-click the value, and type
   `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\EventLogMessages.dll`
   in the value field, then click *ok*.
1. Then finally type in either `SPRSVC.exe Start` or `net start SPRSVC` but
   *NOT BOTH*. These commands both start the service, but they cannot start
   a running service.

By default it will search the specified file for files that matches the
`* MM.DD.YYYY - HH.MM.SS.SS.mp4` pseudo-pattern, and—once the file is free
to be modified—will be renamed to `* YYYY.MM.DD - HH.MM.SS.SS.mp4`.
If this is not suitable for you, then follow the [next section](#tuning) for
more information on how to change this behavior, otherwise you are *done!*

#### Fine tuning #### {#tuning}

If you are not satisfied how the service operates, you can change the behavior
by modifying the SPRSVC.exe.config file.
The configuration file already contains help in comments what you can change.
For completeness, the provided help also repeated and elaborate on it more in
this document.

The configuration of the behavior of the service is contained in the
configuration ▸ appSettings tag. When you wish to change the configuration of
the service, you should focus to this section.
These settings are only working with the file name without the extension, and
preserves that extension after the transformation.
The following keys are available:

MonitorPath  
:   (Required; initial: *empty*)  
    This is the path, that the service will monitor. This is the key that the
    installer will modify, and can be modified with the `SPRSVC.exe Monitor
    <path>` command.

RetryDelay  
:   (Required; initial: `10000`)  
    This key controls the delay in milliseconds the service should wait before
    trying to rename the file again.

    Since there are no monitors to check if a file gets unlocked, the only way
    the service could check for it is to monitor file creation and continuously
    make an attempt to modify it. Since there are the possibility that this
    could ruin the performance of the PC for some, this key was introduced.

Pattern  
:   (Optional; default value:
    `(?&lt;name&gt;.*)\s+(?&lt;date&gt;\d{2}\.\d{2}\.\d{4}) -
    (?&lt;time&gt;\d{2}\.\d{2}\.\d{2}.\d{2})`)  
    This is the pattern that is used to determine whether a file is needed to be
    processed or not, and if so, which part of the file it should interpret it
    as date or time, and which part of it is the name.
    The pattern syntax is the same as [.NET Regular expression syntax][REGQR].
    The `<` and `>` characters can be typed with the `&lt;` and `&gt;`
    entities, and `"` with the `&quot;` entity.

    The *name* named group will be captured as the first argument for the
    ***OutputFormat*** value.

    The *date* and *time* named group are parsed with the ***InputDateFormat***
    and ***InputTimeFormat*** and captured as the second argument for the
    ***OutputFormat*** value.

OutputFormat  
:   (Optional; default value: `{0} {1:yyyy\.MM\.dd} - {1:HH\.mm\.ss\.ff}`)
    This is the format that the file name will be after the rename.  

    The first argument will be the part that was captured with the *name* named
    group, and can be referred to as `{0}`.

    The second argument will be the date and time parts that was captured with
    the *date* and *time* named group, and can be referred to as `{1}`.

    The arguments can be further formatted by providing the string format after
    the number separated with a colon. For example, the `{1:yyyy\.MM\.dd}`
    will format the date and time as *2017.01.29*.

    The accepted date and time format are the same that the .NET framework
    accepts as either [Standard Date and Time format strings][SDateTime] or
    [Custom Date and Time format strings][CDateTime].

InputDateFormat  
:   (Optional; Default value: `MM\.dd\.yyyy`)  
    The format the file to be modified specifies the date captured by the *date*
    named group.

    This follows the same formatting as the .NET [Standard Date and Time format
    strings][SDateTime] and [Custom Date and Time format strings][CDateTime].

InputTimeFormat  
:   (Optional; Default value: `hh\.mm\.ss\.ff`)  
    The format the file to be modified specifies the time captured by the *time*
    named group.

    This follows the same formatting as the .NET [Standard TimeSpan format
    strings][STimeSpan] and [Custom TimeSpan format strings][CTimeSpan].

Although the service logs in to the Windows Event Log, a more detailed log can
be configured in the configuration ▸ system.diagnostics tag.
This may be required for debugging the software.

You can change the level of logging by changing the `switchValue` attribute of
the configuration ▸ system.diagnostics ▸ sources ▸ source tag with the
`ShadowPlayRenamerSvc` name attribute. You can specify more than one value
separated by commas. The values can be the following:

All  
:   Turns on all the log level specified below. No other level specifications
    are required after this.

Critical  
:   Turns on the *Critical* logging level messages.

Error  
:   Turns on the *Error* logging level messages.

Warning  
:   Turns on the *Warning* logging level messages.

Information  
:   Turns on the *Information* logging level messages.

Verbose  
:   Turns on the *Verbose* logging level messages.

ActivityTracking  
:   Turns on the messages that reports the service activities, like starting the
    service, stopping the service, an action starts or stops, suspended or
    resumed.

Below you can see a sample configuration file.

~~~ .xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <startup> 
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.5" />
  </startup>
  <appSettings>
    <add key="MonitorPath" value=""/>
    <add key="RetryDelay" value="10000"/>
    <add key="Pattern" value="(?&lt;name&gt;.*)\s+(?&lt;date&gt;\d{2}\.\d{2}\.\d{4}) - (?&lt;time&gt;\d{2}\.\d{2}\.\d{2}.\d{2})"/>
    <add key="OutputFormat" value="{0} {1:yyyy\.MM\.dd} - {1:HH\.mm\.ss\.ff}"/>
    <add key="InputDateFormat" value="MM\.dd\.yyyy"/>
    <add key="InputTimeFormat" value="hh\.mm\.ss\.ff"/>
  </appSettings>
  <system.diagnostics>
    <trace autoflush="true" />
    <sources>
      <source name="ShadowPlayRenamerSvc" switchValue="Critical,ActivityTracing">
        <listeners>
          <add name="svclog" initializeData=".\SPRSVC.svclog" type="System.Diagnostics.XmlWriterTraceListener"/>
        </listeners>
      </source>
    </sources>
  </system.diagnostics>
</configuration>
~~~

All, Verbose, Critical, Error, Warning, Information, ActivityTracing

## Using the service ##

The service is started automatically and does not require further configuration.
If however further configuration is needed, it is recommended to stop the
service before modifying it's configuration, then restart it after done
modifying the configuration.

If the service does not function in the desired manner, you can always disable
it and use the SPRCLI.exe command instead.

## Using the command-line interface ##

This section assumes the user have an adequate knowledge to use the
command-prompt in Windows.

To be able to use the command-line interface from any directory, you need to add
the installation folder of the `SPRCLI.exe` command in your `PATH` environment
variable.

The command can be invoked like so:

~~~.bat
SPRSVC.exe <options> <path>
~~~

*Path* is the path to your video recording folders, where Shadow Play records
all the videos. If this argument is omitted, it will scan the current working
directory.

The following options are known by the command:

/?
:    Displays a help message for the command.

/Pattern &lt;pattern&gt;
:   Renames the files that matches the regular expression pattern specified by
    the argument of the switch.

    The pattern syntax is the same as [.NET Regular expression syntax][REGQR].

    The *name* named group will be captured as the first argument for the
    ***OutputFormat*** value.

    The *date* and *time* named group are parsed with the ***InputDateFormat***
    and ***InputTimeFormat*** and captured as the second argument for the
    ***OutputFormat*** value.

    Default value:
    `(?<name>.*)\s+(?<date>\d{2}\.\d{2}\.\d{4}) -
    (?<time>\d{2}\.\d{2}\.\d{2}.\d{2})`

/Output &lt;format&gt;
:   Sets the target format of the file name after renaming.

    The first argument will be the part that was captured with the *name* named
    group, and can be referred to as `{0}`.

    The second argument will be the date and time parts that was captured with
    the *date* and *time* named group, and can be referred to as `{1}`.

    The arguments can be further formatted by providing the string format after
    the number separated with a colon. For example, the `{1:yyyy\.MM\.dd}`
    will format the date and time as *2017.01.29*.

    The accepted date and time format are the same that the .NET framework
    accepts as either [Standard Date and Time format strings][SDateTime] or
    [Custom Date and Time format strings][CDateTime].

    Default: `{0} {1:yyyy\.MM\.dd} - {1:HH\.mm\.ss\.ff}`

/Date &lt;format&gt;
:   Sets the format of the *date* captured named group.

    This follows the same formatting as the .NET [Standard Date and Time format
    strings][SDateTime] and [Custom Date and Time format strings][CDateTime].

    Default: `MM\.dd\.yyyy`

/Time &lt;format&gt;
:   Sets the format of the *time* captured named group.

    This follows the same formatting as the .NET [Standard TimeSpan format
    strings][STimeSpan] and [Custom TimeSpan format strings][CTimeSpan].

    Default: `hh\.mm\.ss\.ff`

  [REGQR]: https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference "Regular Expression Language - Quick Reference"
  [SDateTime]: https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings "Standard Date and Time Format Strings"
  [CDateTime]: https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings "Custom Date and Time Format Strings"
  [STimeSpan]: https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-timespan-format-strings "Standard TimeSpan Format Strings"
  [CTimeSpan]: https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-timespan-format-strings "Custom TimeSpan Format Strings"
