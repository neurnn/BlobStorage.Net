## BlobStorage.NET

How to setup this library to project.
```
IServiceCollection Services = ....;

Services.AddDiskStorage(
	Identifier: null, 
	Directory: new DirectoryInfo("path/to/store"),
	HttpBase: Access Base URL to map)
	;

// `null` identifier == "default".
// if no HttpBase specified, it can not make outer access url.
```

How to get storage by identifier.
```
IServiceProvider Services = ....;

// --> use `GetStorage` or `GetRequiredStorage`.
var Storage = Services.GetStorage(null);
```

How to store blob to storage.
```
using var DataStream = File.OpenRead(...);
await Storage.CreateDirectoryAsync("full/path");

if (await Storage.WriteAsync("full/path/name", DataStream) == true)
{
	var Uri = await Storage.MakeUriStringAsync("full/path/name");

	// --> base URI.
}
```

How to delete blob from storage.
```
await Storage.DeleteAsync("full/path/name");
```

## TODO List

1. Load configurations from file or some configuration adapters.