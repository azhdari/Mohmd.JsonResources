# Mohmd.JsonResources
JSON Localization Resources for ASP.NET Core 2.0 and 3.0

[![License](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://github.com/azhdari/Mohmd.JsonResources/blob/master/License.txt)
[![NuGet](https://img.shields.io/badge/nuget-1.2.0--preview2-blue.svg?style=flat-square)](https://www.nuget.org/packages/Mohmd.JsonResources/1.2.0-preview2)

## Getting Started
Use these instructions to get the package and use it.

### Install
From the command prompt
```bash
dotnet add package Mohmd.JsonResources --version 1.2.0-preview2
```
or
```bash
Install-Package Mohmd.JsonResources -Version 1.2.0-preview2
```
or
```bash
paket add Mohmd.JsonResources --version 1.2.0-preview2
```

### Configure
Add service
```csharp
public void ConfigureServices(IServiceCollection services)
{
  // We can also use appsettings.json to configure options.
  services.AddJsonLocalization(options =>
  {
	options.ResourcesPath = "Resources"; // based on project's root
	options.GlobalResourceFileName = "global";
	options.AreasResourcePrefix = "areas";
  });

  services.AddMvc()
	.AddViewLocalization(); // add this line to enable localization in views
}
```
Then add the middleware
```csharp
public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            IOptions<RequestLocalizationOptions> localizationOptions)
{
	// Localization
	app.UseJsonLocalizer();
	app.UseRequestLocalization(localizationOptions.Value);
	
	app.UseMvc();
}
```

### JSON file structure
```json
{
  "key": "locale",
  "group.key": "something"
}
```

### Add Resources

There are 3 general ways to add locale resources to your projects.  
1) Single Global json file
2) One json file per Area
3) One json file for every single file in the project

We can use all of along side each other.

#### Global File
Create global.json (or whatever you set
in `options.GlobalResourceFileName`) in root of resources directory.  
```
global.json
global.fa-IR.json
global.de-DE.json
```

#### Per Area File
Create a json file for every area you have.  
For example if you have "Admin" area, and `options.AreasResourcePrefix` is set to '*Area*',
then json file would be `Area.Admin.json`
```
Area.Admin.json
Area.Admin.fa-IR.json
Area.Admin.de-DE.json
```

#### Per file
Every .cs or .cshtml file needs a json resource.
Naming follows default xml based localization in ASP.NET Core.
So if we have a file like this: `Views/Home/Index.cshtml`. Resource file can be one of these:  
```
Resources/Views/Home/Index.json
Resources/Views/Home.Index.json
Resources/Views.Home.Index.json

Resources/Views.Home.Index.fa-IR.json
Resources/Views.Home.Index.de-DE.json
```

### Based on another repo
This project originally created by @hishamco ([repo](https://github.com/hishamco/My.Extensions.Localization.Json)).  
I've made a few changes on it, and used it for a long time.
