Extra info that might be useful:

Form validation uses jQuery Validation (with jQuery version 3.6.0), Typescript uses jQuery version 3.5.29 via npm.
This should not be a problem, because when the Typescript code is compiled to Javascript then it should use the same jQuery version that the rest of the project uses (3.6.0).
The jQuery npm package's only real purpose is to ensure that the Typescript compiler can recognise jQuery and compile.

sending server side validation error messages to the front end without Javascript has not been implemented.
(the front end needs Javascript anyway for core functionality, so this is not as much of an issue)

The following warning is displayed when rebuilding the project:
"warning NU1701: Package 'DataAnnotationsExtensions 5.0.1.27' was restored using '.NETFramework,Version=v4.6.1, .NETFramework,Version=v4.6.2, .NETFramework,Version=v4.7, .NETFramework,Version=v4.7.1, .NETFramework,Version=v4.7.2, .NETFramework,Version=v4.8, .NETFramework,Version=v4.8.1' instead of the project target framework 'net8.0'. This package may not be fully compatible with your project."
The [Min()] data annotation from this NuGet package has been tested, and it is currently (27/03/2024) working as intended.