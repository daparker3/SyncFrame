# TaskResultExtensionMethods.Fault(*TFault*) Method (Task(RequestResult), *TFault*)
 _**\[This is preliminary documentation and is subject to change.\]**_

Faults this request.

**Namespace:**&nbsp;<a href="de148c19-6fcd-6ea5-c13c-94525bd1dd5b">MS.SyncFrame</a><br />**Assembly:**&nbsp;MS.SyncFrame (in MS.SyncFrame.dll) Version: 1.0.0.0 (1.0.0.0)

## Syntax

**C#**<br />
``` C#
public static Task<FaultException<TFault>> Fault<TFault>(
	this Task<RequestResult> task,
	TFault fault
)
where TFault : class

```

**VB**<br />
``` VB
<ExtensionAttribute>
Public Shared Function Fault(Of TFault As Class) ( 
	task As Task(Of RequestResult),
	fault As TFault
) As Task(Of FaultException(Of TFault))
```

**C++**<br />
``` C++
public:
[ExtensionAttribute]
generic<typename TFault>
where TFault : ref class
static Task<FaultException<TFault>^>^ Fault(
	Task<RequestResult^>^ task, 
	TFault fault
)
```

**F#**<br />
``` F#
[<ExtensionAttribute>]
static member Fault : 
        task : Task<RequestResult> * 
        fault : 'TFault -> Task<FaultException<'TFault>>  when 'TFault : not struct

```


#### Parameters
&nbsp;<dl><dt>task</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/dd321424" target="_blank">System.Threading.Tasks.Task</a>(<a href="4b256005-b920-df6f-0771-035950c2789a">RequestResult</a>)<br />The <a href="http://msdn2.microsoft.com/en-us/library/dd321424" target="_blank">Task(TResult)</a> which generated the fault.</dd><dt>fault</dt><dd>Type: *TFault*<br />The fault.</dd></dl>

#### Type Parameters
&nbsp;<dl><dt>TFault</dt><dd>The type of the fault.</dd></dl>

#### Return Value
Type: <a href="http://msdn2.microsoft.com/en-us/library/dd321424" target="_blank">Task</a>(<a href="d43efb02-9a8a-5503-83aa-183233092174">FaultException</a>(*TFault*))<br />A <a href="http://msdn2.microsoft.com/en-us/library/dd321424" target="_blank">Task(TResult)</a> which contains information about the fault and can be thrown to terminate the session.

#### Usage Note
In Visual Basic and C#, you can call this method as an instance method on any object of type <a href="http://msdn2.microsoft.com/en-us/library/dd321424" target="_blank">Task</a>(<a href="4b256005-b920-df6f-0771-035950c2789a">RequestResult</a>). When you use instance method syntax to call this method, omit the first parameter. For more information, see <a href="http://msdn.microsoft.com/en-us/library/bb384936.aspx">Extension Methods (Visual Basic)</a> or <a href="http://msdn.microsoft.com/en-us/library/bb383977.aspx">Extension Methods (C# Programming Guide)</a>.

## See Also


#### Reference
<a href="cee6733d-b9b3-7f93-4a41-7e731cd8bf82">TaskResultExtensionMethods Class</a><br /><a href="0ff1965a-9005-110a-64ce-5c7735c8e522">Fault Overload</a><br /><a href="de148c19-6fcd-6ea5-c13c-94525bd1dd5b">MS.SyncFrame Namespace</a><br />