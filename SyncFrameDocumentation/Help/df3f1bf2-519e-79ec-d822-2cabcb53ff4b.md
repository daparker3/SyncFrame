# MessageServer Constructor 
 _**\[This is preliminary documentation and is subject to change.\]**_

Initializes a new instance of the <a href="f9ac6753-24e8-39a3-c2af-41be495e8274">MessageServer</a> class.

**Namespace:**&nbsp;<a href="de148c19-6fcd-6ea5-c13c-94525bd1dd5b">MS.SyncFrame</a><br />**Assembly:**&nbsp;MS.SyncFrame (in MS.SyncFrame.dll) Version: 1.0.0.0 (1.0.0.0)

## Syntax

**C#**<br />
``` C#
public MessageServer(
	Stream serverStream,
	CancellationToken token
)
```

**VB**<br />
``` VB
Public Sub New ( 
	serverStream As Stream,
	token As CancellationToken
)
```

**C++**<br />
``` C++
public:
MessageServer(
	Stream^ serverStream, 
	CancellationToken token
)
```

**F#**<br />
``` F#
new : 
        serverStream : Stream * 
        token : CancellationToken -> MessageServer
```


#### Parameters
&nbsp;<dl><dt>serverStream</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/8f86tw9e" target="_blank">System.IO.Stream</a><br />The server stream.</dd><dt>token</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/dd384802" target="_blank">System.Threading.CancellationToken</a><br />The cancellation token.</dd></dl>

## See Also


#### Reference
<a href="f9ac6753-24e8-39a3-c2af-41be495e8274">MessageServer Class</a><br /><a href="de148c19-6fcd-6ea5-c13c-94525bd1dd5b">MS.SyncFrame Namespace</a><br />